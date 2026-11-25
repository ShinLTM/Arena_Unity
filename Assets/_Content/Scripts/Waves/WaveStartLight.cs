using System.Collections;
using Arena.Waves;
using UnityEngine;

namespace Arena.Waves
{
    /// <summary>
    /// Light effect played when a wave starts: the lights flash to an alert color, then come back to normal.
    /// Subscribes to the WaveSpawnManager by itself, or Play can be linked to a UnityEvent (On Wave Started).
    /// </summary>
    public class WaveStartLight : MonoBehaviour
    {
        [System.Serializable]
        private class Settings
        {
            [Tooltip("Subscribes to the WaveSpawnManager of the scene. Turn off to call Play from a UnityEvent instead.")]
            public bool AutoSubscribe = true;

            [Tooltip("Color of the lights at the peak of the effect.")]
            public Color FlashColor = new Color(1f, 0.15f, 0.1f);

            [Tooltip("Intensity multiplier at the peak of the effect.")]
            [Min(0f)] public float IntensityMultiplier = 3f;

            [Tooltip("Total duration of the effect, in seconds.")]
            [Min(0.1f)] public float Duration = 1.5f;

            [Tooltip("Number of pulses during the effect.")]
            [Min(1)] public int Pulses = 2;
        }

        [System.Serializable]
        private struct References
        {
            [Tooltip("Lights that flash: the lights of the arena, a dedicated light, the directional light...")]
            public Light[] Lights;
        }

        #region FIELDS

        [SerializeField] private Settings _settings;
        [SerializeField] private References _references;

        private Color[] _baseColors;
        private float[] _baseIntensities;
        private Coroutine _routine;

        #endregion

        #region UNITY LIFECYCLES

        void Awake()
        {
            int count = _references.Lights.Length;
            _baseColors = new Color[count];
            _baseIntensities = new float[count];

            for (int i = 0; i < count; i++)
            {
                if (!_references.Lights[i])
                    continue;

                _baseColors[i] = _references.Lights[i].color;
                _baseIntensities[i] = _references.Lights[i].intensity;
            }
        }

        void Start()
        {
            // In Start: the manager registers itself in its Awake
            if (_settings.AutoSubscribe && WaveSpawnManager.Instance)
                WaveSpawnManager.Instance.OnWaveStarted += HandleWaveStarted;
        }

        void OnDestroy()
        {
            if (WaveSpawnManager.Instance)
                WaveSpawnManager.Instance.OnWaveStarted -= HandleWaveStarted;
        }

        void OnDisable()
        {
            Restore();
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Plays the effect. Can be linked to a UnityEvent.
        /// </summary>
        [ContextMenu("Debug/Play")]
        public void Play()
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(Flash());
        }

        #endregion

        #region PRIVATE METHODS

        private void HandleWaveStarted(int waveIndex)
        {
            Play();
        }

        private IEnumerator Flash()
        {
            float elapsed = 0f;

            while (elapsed < _settings.Duration)
            {
                float progress = elapsed / _settings.Duration;

                // Pulses that fade out: 0 -> 1 -> 0 for each pulse, getting weaker with time
                float wave = Mathf.Sin(progress * _settings.Pulses * Mathf.PI);
                float strength = Mathf.Abs(wave) * (1f - progress);

                Apply(strength);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Restore();
            _routine = null;
        }

        private void Apply(float strength)
        {
            for (int i = 0; i < _references.Lights.Length; i++)
            {
                Light light = _references.Lights[i];

                if (!light)
                    continue;

                light.color = Color.Lerp(_baseColors[i], _settings.FlashColor, strength);
                light.intensity = Mathf.Lerp(_baseIntensities[i], _baseIntensities[i] * _settings.IntensityMultiplier, strength);
            }
        }

        private void Restore()
        {
            if (_baseColors == null)
                return;

            Apply(0f);
        }

        #endregion
    }
}
