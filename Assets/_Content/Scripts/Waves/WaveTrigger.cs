using Arena.Characters;
using UnityEngine;

namespace Arena.Waves
{
    /// <summary>
    /// Trigger box that starts a wave of the WaveSpawnManager when the player enters it:
    /// the next wave of the list, or a chosen one.
    /// Ignored while a wave is in progress.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WaveTrigger : MonoBehaviour
    {
        public enum Mode
        {
            NextWave,
            SpecificWave
        }

        [System.Serializable]
        private class Settings
        {
            [Tooltip("NextWave: the wave after the last one played. SpecificWave: the wave chosen below.")]
            public Mode Mode;

            [Tooltip("SpecificWave mode: index of the wave in the Waves list of the WaveSpawnManager. 0: the first wave.")]
            [Min(0)] public int WaveIndex;

            [Tooltip("The trigger works only once, then turns itself off.")]
            public bool TriggerOnce = true;
        }

        [System.Serializable]
        private struct References
        {
            [Tooltip("Optional. Empty: the WaveSpawnManager of the scene is used.")]
            public WaveSpawnManager Manager;
        }

        #region FIELDS

        [SerializeField] private Settings _settings;
        [SerializeField] private References _references;

        #endregion

        #region UNITY LIFECYCLES

        void Reset()
        {
            // Called when the component is added: a trigger box
            GetComponent<BoxCollider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Player.Instance == null || other.gameObject != Player.Instance.gameObject)
                return;

            WaveSpawnManager manager = _references.Manager ? _references.Manager : WaveSpawnManager.Instance;

            if (!manager)
            {
                Debug.LogWarning($"{name}: no WaveSpawnManager found.");
                return;
            }

            bool isStarted = _settings.Mode == Mode.SpecificWave
                ? manager.TryStartWave(_settings.WaveIndex)
                : manager.TryStartNextWave();

            if (isStarted && _settings.TriggerOnce)
                gameObject.SetActive(false);
        }

        void OnDrawGizmos()
        {
            BoxCollider box = GetComponent<BoxCollider>();

            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }

        #endregion
    }
}
