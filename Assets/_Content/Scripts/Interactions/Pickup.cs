using Arena.Characters;
using Arena.Waves;
using UnityEngine;

namespace Arena.Interactions
{
    /// <summary>
    /// Item on the ground that the player takes by walking on it (ammo, health...).
    /// Once taken, the item disappears, then comes back:
    /// AfterDelay: after a few seconds.
    /// EndOfWave: when the current wave is cleared, or at the end of the next wave if it was taken between two waves.
    /// Needs a trigger collider. The script stays on an always active object: only the collider and the visual are hidden.
    /// What the item gives is written in the child classes (AmmoPickup, HealthPickup).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Pickup : MonoBehaviour
    {
        public enum RespawnMode
        {
            AfterDelay,
            EndOfWave
        }

        [System.Serializable]
        private class PickupSettings
        {
            [Tooltip("AfterDelay: the item comes back after Respawn Delay. EndOfWave: the item comes back at the end of a wave.")]
            public RespawnMode Respawn = RespawnMode.AfterDelay;

            [Tooltip("AfterDelay mode: time before the item comes back, in seconds.")]
            [Min(0f)] public float RespawnDelay = 10f;

            [Tooltip("Rotation speed of the visual around the vertical axis, in degrees per second. 0: no rotation.")]
            public float SpinSpeed = 90f;
        }

        [System.Serializable]
        private struct References
        {
            [Tooltip("Child holding the model of the item, hidden while the item is unavailable.")]
            public GameObject Visual;
        }

        [System.Serializable]
        public class StateContainer
        {
            public bool IsAvailable = true;

            [Tooltip("AfterDelay mode: time left before the item comes back, in seconds.")]
            public float RespawnTimeLeft;
        }

        #region FIELDS

        [SerializeField] private PickupSettings _pickupSettings;
        [SerializeField] private References _references;
        [SerializeField] private StateContainer _state;

        public StateContainer State => _state;

        private Collider _collider;
        private WaveSpawnManager _manager;
        private float _respawnTime;

        #endregion

        #region UNITY LIFECYCLES

        void Reset()
        {
            // Called when the component is added: a trigger the player can walk through
            GetComponent<Collider>().isTrigger = true;
        }

        protected virtual void Awake()
        {
            _collider = GetComponent<Collider>();

            // A model dragged from the Project window is an asset, not an object of the level: it must be a child in the scene
            if (_references.Visual && !_references.Visual.scene.IsValid())
            {
                Debug.LogWarning($"{name}: Visual points to an asset of the Project, not to an object of the scene. " +
                                 "Put the model as a child of the item in the scene, then drag that child into Visual.");
                _references.Visual = null;
            }
        }

        protected virtual void Start()
        {
            // In Start: the manager registers itself in its Awake
            _manager = WaveSpawnManager.Instance;

            if (_manager)
                _manager.OnWaveCleared += HandleWaveCleared;
            else if (_pickupSettings.Respawn == RespawnMode.EndOfWave)
                Debug.LogWarning($"{name}: no WaveSpawnManager found, the item will never come back once taken.");

            SetAvailable(true);
        }

        protected virtual void OnDestroy()
        {
            if (_manager)
                _manager.OnWaveCleared -= HandleWaveCleared;
        }

        void Update()
        {
            if (!_state.IsAvailable)
            {
                UpdateRespawnDelay();
                return;
            }

            if (_references.Visual && _pickupSettings.SpinSpeed != 0f)
                _references.Visual.transform.Rotate(0f, _pickupSettings.SpinSpeed * Time.deltaTime, 0f, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!_state.IsAvailable || Player.Instance == null || other.gameObject != Player.Instance.gameObject)
                return;

            if (TryApply(Player.Instance))
                PickUp();
        }

        #endregion

        #region PROTECTED METHODS

        /// <summary>
        /// Gives the content of the item to the player. Returns false if the item is not taken (the player does not need it).
        /// </summary>
        protected abstract bool TryApply(Player player);

        #endregion

        #region PRIVATE METHODS

        private void PickUp()
        {
            SetAvailable(false);
            _respawnTime = Time.time + _pickupSettings.RespawnDelay;
        }

        private void UpdateRespawnDelay()
        {
            if (_pickupSettings.Respawn != RespawnMode.AfterDelay)
                return;

            _state.RespawnTimeLeft = Mathf.Max(0f, _respawnTime - Time.time);

            if (_state.RespawnTimeLeft <= 0f)
                SetAvailable(true);
        }

        /// <summary>
        /// EndOfWave mode. Taken during a wave: back at its end. Taken between two waves: back at the end of the next one,
        /// whatever its index (the waves can be started in any order).
        /// </summary>
        private void HandleWaveCleared(int waveIndex)
        {
            if (_pickupSettings.Respawn == RespawnMode.EndOfWave && !_state.IsAvailable)
                SetAvailable(true);
        }

        private void SetAvailable(bool isAvailable)
        {
            _state.IsAvailable = isAvailable;
            _collider.enabled = isAvailable;

            if (isAvailable)
                _state.RespawnTimeLeft = 0f;

            if (_references.Visual)
                _references.Visual.SetActive(isAvailable);
        }

        #endregion
    }
}
