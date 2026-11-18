using System;
using System.Collections;
using System.Collections.Generic;
using Arena.Characters;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Arena.Waves
{
    /// <summary>
    /// Spawns the enemies wave by wave, on spawn points placed in the level.
    /// A wave starts when StartNextWave or StartWave is called (WaveTrigger, UnityEvent...) and is cleared when all its enemies are gone.
    /// The number and the types of enemies of each wave are set in the Waves list.
    /// </summary>
    public class WaveSpawnManager : MonoBehaviour
    {
        [System.Serializable]
        public class EnemyGroup
        {
            [Tooltip("Enemy prefab: melee (Enemy), ranged (RangedEnemy)...")]
            public EnemyBase Prefab;

            [Tooltip("Number of enemies of this type in the wave.")]
            [Min(0)] public int Count = 3;
        }

        [System.Serializable]
        public class Wave
        {
            [Tooltip("Only used to find the wave in the inspector.")]
            public string Name = "Wave";

            [Tooltip("Types and numbers of enemies of the wave.")]
            public List<EnemyGroup> Enemies = new List<EnemyGroup>();

            [Tooltip("Time between two spawns, in seconds. 0: the whole wave appears at once.")]
            [Min(0f)] public float SpawnInterval = 0.5f;

            [Tooltip("Mixes the enemy types instead of spawning them group after group.")]
            public bool Shuffle = true;

            [Tooltip("Points where the enemies of this wave appear, picked at random. Empty: the default spawn points of the manager are used.")]
            public Transform[] SpawnPoints;

            public int TotalCount
            {
                get
                {
                    int total = 0;

                    foreach (EnemyGroup group in Enemies)
                    {
                        if (group.Prefab)
                            total += group.Count;
                    }

                    return total;
                }
            }
        }

        [System.Serializable]
        private class Settings
        {
            [Tooltip("Waves, in the order they are played.")]
            public List<Wave> Waves = new List<Wave>();

            [Tooltip("Starts a wave as soon as the game starts, without any trigger.")]
            public bool StartOnStart;

            [Tooltip("Start On Start: index of the wave started. 0: the first wave.")]
            [Min(0)] public int StartWaveIndex;

            [Tooltip("Max distance between a spawn point and the NavMesh, in meters. The enemy is placed on the closest NavMesh point.")]
            [Min(0.1f)] public float NavMeshSnapDistance = 2f;
        }

        [System.Serializable]
        private struct References
        {
            [Tooltip("Default points where the enemies appear, picked at random, for the waves that have no spawn point of their own. Their forward axis gives the enemy orientation.")]
            public Transform[] SpawnPoints;

            [Tooltip("Optional parent of the spawned enemies, to keep the hierarchy clean.")]
            public Transform EnemiesParent;
        }

        [System.Serializable]
        public class StateContainer
        {
            [Tooltip("Index of the current or last wave. -1: no wave started yet.")]
            public int WaveIndex = -1;
            public bool IsWaveInProgress;
            public int EnemiesToSpawn;
            public int EnemiesAlive;
        }

        #region FIELDS

        public static WaveSpawnManager Instance { get; private set; }

        [SerializeField] private Settings _settings;
        [SerializeField] private References _references;
        [SerializeField] private StateContainer _state;

        [Header("Events")]
        [SerializeField] private UnityEvent _onWaveStarted;
        [SerializeField] private UnityEvent _onWaveCleared;
        [SerializeField] private UnityEvent _onAllWavesCleared;

        /// <summary>
        /// Raised with the wave index when a wave starts.
        /// </summary>
        public event Action<int> OnWaveStarted;

        /// <summary>
        /// Raised with the wave index when all the enemies of a wave are gone.
        /// </summary>
        public event Action<int> OnWaveCleared;

        public StateContainer State => _state;
        public int WaveCount => _settings.Waves.Count;
        public bool HasNextWave => _state.WaveIndex + 1 < _settings.Waves.Count;

        private readonly List<EnemyBase> _spawnOrder = new List<EnemyBase>();
        private readonly List<EnemyBase> _aliveEnemies = new List<EnemyBase>();

        // Spawn points of the wave in progress: its own, or the default ones
        private Transform[] _spawnPoints;

        #endregion

        #region UNITY LIFECYCLES

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogWarning($"A second WaveSpawnManager was found on {name}, it is destroyed.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            // In Start: the player and the other objects are ready (Awake)
            if (_settings.StartOnStart)
                TryStartWave(_settings.StartWaveIndex);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnDrawGizmos()
        {
            // Default spawn points in red, spawn points of the waves in orange
            DrawSpawnPointGizmos(_references.SpawnPoints, new Color(1f, 0.25f, 0.25f, 0.8f));

            foreach (Wave wave in _settings.Waves)
                DrawSpawnPointGizmos(wave.SpawnPoints, new Color(1f, 0.6f, 0.1f, 0.8f));
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Starts the next wave, if no wave is in progress. Can be linked to a UnityEvent.
        /// </summary>
        public void StartNextWave()
        {
            TryStartNextWave();
        }

        /// <summary>
        /// Starts the next wave. Returns false if a wave is in progress or if all the waves were played.
        /// </summary>
        public bool TryStartNextWave()
        {
            if (!HasNextWave)
                return false;

            return TryStartWave(_state.WaveIndex + 1);
        }

        /// <summary>
        /// Starts a given wave, if no wave is in progress. Can be linked to a UnityEvent.
        /// </summary>
        /// <param name="index">Index in the Waves list. 0: the first wave.</param>
        public void StartWave(int index)
        {
            TryStartWave(index);
        }

        /// <summary>
        /// Starts a given wave. The next wave is then the one after it in the list.
        /// Returns false if a wave is in progress or if the index is not in the Waves list.
        /// </summary>
        /// <param name="index">Index in the Waves list. 0: the first wave.</param>
        public bool TryStartWave(int index)
        {
            if (_state.IsWaveInProgress)
                return false;

            if (index < 0 || index >= _settings.Waves.Count)
            {
                Debug.LogWarning($"{name}: there is no wave at index {index} ({_settings.Waves.Count} waves in the list).");
                return false;
            }

            Wave wave = _settings.Waves[index];
            _spawnPoints = HasSpawnPoint(wave.SpawnPoints) ? wave.SpawnPoints : _references.SpawnPoints;

            if (!HasSpawnPoint(_spawnPoints))
            {
                Debug.LogWarning($"{name}: no spawn point for the wave {index} ({wave.Name}) nor default spawn point, the wave cannot start.");
                return false;
            }

            _state.WaveIndex = index;

            _state.IsWaveInProgress = true;
            _state.EnemiesAlive = 0;
            _state.EnemiesToSpawn = wave.TotalCount;

            OnWaveStarted?.Invoke(_state.WaveIndex);
            _onWaveStarted.Invoke();

            StartCoroutine(SpawnWave(wave));
            return true;
        }

        #endregion

        #region PRIVATE METHODS

#if UNITY_EDITOR
        [ContextMenu("Debug/Start Next Wave")]
        private void DebugStartNextWave()
        {
            StartNextWave();
        }

        [ContextMenu("Debug/Kill Current Wave")]
        private void DebugKillWave()
        {
            foreach (EnemyBase enemy in _aliveEnemies)
                Destroy(enemy.gameObject);
        }
#endif

        private IEnumerator SpawnWave(Wave wave)
        {
            BuildSpawnOrder(wave);

            WaitForSeconds interval = wave.SpawnInterval > 0f ? new WaitForSeconds(wave.SpawnInterval) : null;

            foreach (EnemyBase prefab in _spawnOrder)
            {
                Spawn(prefab);
                _state.EnemiesToSpawn--;

                if (interval != null)
                    yield return interval;
            }

            // Also clears an empty wave, or a wave killed before its last spawn interval ended
            CheckWaveCleared();
        }

        private void BuildSpawnOrder(Wave wave)
        {
            _spawnOrder.Clear();

            foreach (EnemyGroup group in wave.Enemies)
            {
                if (!group.Prefab)
                    continue;

                for (int i = 0; i < group.Count; i++)
                    _spawnOrder.Add(group.Prefab);
            }

            if (!wave.Shuffle)
                return;

            // Fisher-Yates shuffle
            for (int i = _spawnOrder.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_spawnOrder[i], _spawnOrder[j]) = (_spawnOrder[j], _spawnOrder[i]);
            }
        }

        private void Spawn(EnemyBase prefab)
        {
            // The spawn points can be destroyed during the wave: the enemy is then skipped
            if (!HasSpawnPoint(_spawnPoints))
            {
                Debug.LogWarning($"{name}: the spawn points of the wave were destroyed, an enemy is skipped.");
                return;
            }

            Transform point = PickSpawnPoint();
            Vector3 position = point.position;

            // The NavMeshAgent needs to appear on the NavMesh
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, _settings.NavMeshSnapDistance, NavMesh.AllAreas))
                position = hit.position;
            else
                Debug.LogWarning($"{name}: the spawn point {point.name} is too far from the NavMesh.");

            EnemyBase enemy = Instantiate(prefab, position, point.rotation, _references.EnemiesParent);
            enemy.OnRemoved += HandleEnemyRemoved;
            _aliveEnemies.Add(enemy);
            _state.EnemiesAlive = _aliveEnemies.Count;
        }

        /// <summary>
        /// Random spawn point of the wave in progress. Empty slots of the list are skipped.
        /// </summary>
        private Transform PickSpawnPoint()
        {
            Transform point;

            do
                point = _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];
            while (!point);

            return point;
        }

        private static bool HasSpawnPoint(Transform[] points)
        {
            if (points == null)
                return false;

            foreach (Transform point in points)
            {
                if (point)
                    return true;
            }

            return false;
        }

        private static void DrawSpawnPointGizmos(Transform[] points, Color color)
        {
            if (points == null)
                return;

            Gizmos.color = color;

            foreach (Transform point in points)
            {
                if (!point)
                    continue;

                Gizmos.DrawWireSphere(point.position, 0.5f);
                Gizmos.DrawLine(point.position, point.position + point.forward);
            }
        }

        private void HandleEnemyRemoved(EnemyBase enemy)
        {
            enemy.OnRemoved -= HandleEnemyRemoved;

            // The manager can be destroyed first when the scene is unloaded
            if (!this)
                return;

            _aliveEnemies.Remove(enemy);
            _state.EnemiesAlive = _aliveEnemies.Count;
            CheckWaveCleared();
        }

        private void CheckWaveCleared()
        {
            if (!_state.IsWaveInProgress || _state.EnemiesToSpawn > 0 || _state.EnemiesAlive > 0)
                return;

            _state.IsWaveInProgress = false;

            OnWaveCleared?.Invoke(_state.WaveIndex);
            _onWaveCleared.Invoke();

            if (!HasNextWave)
                _onAllWavesCleared.Invoke();
        }

        #endregion
    }
}
