using Arena.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.Characters
{
    /// <summary>
    /// Health bar above a character, on a World Space canvas that is a child of the character.
    /// Follows the health of the Destructible on the same object (or in its parents), and always faces the camera.
    /// The fill is an Image of type Filled (Horizontal).
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [System.Serializable]
        private class Settings
        {
            [Tooltip("The bar is hidden while the health is full, and appears at the first hit.")]
            public bool HideWhenFull = true;

            [Tooltip("Speed at which the fill catches up with the real value, in fill units per second. 0: instant.")]
            [Min(0f)] public float FillSpeed = 4f;

            [Tooltip("Color of the fill when the health is full.")]
            public Color FullColor = new Color(0.3f, 0.9f, 0.3f);

            [Tooltip("Color of the fill when the health is almost empty.")]
            public Color LowColor = new Color(0.95f, 0.2f, 0.2f);
        }

        [System.Serializable]
        private struct References
        {
            [Tooltip("Image of type Filled that shows the health.")]
            public Image Fill;

            [Tooltip("Object hidden when the health is full. Usually the canvas or the background of the bar.")]
            public GameObject Root;
        }

        #region FIELDS

        [SerializeField] private Settings _settings;
        [SerializeField] private References _references;

        private Health _health;
        private Camera _camera;
        private float _target = 1f;

        #endregion

        #region UNITY LIFECYCLES

        void Start()
        {
            // In Start: the Destructible has initialized its health in its Awake
            Destructible destructible = GetComponentInParent<Destructible>();

            if (!destructible)
            {
                Debug.LogWarning($"{name}: no Destructible found in the parents, the health bar is disabled.");
                enabled = false;
                return;
            }

            _health = destructible.Health;
            _health.OnChange += HandleChange;

            _camera = Camera.main;
            HandleChange(_health.Current, _health.Max);
            SetFill(_target);
        }

        void OnDestroy()
        {
            if (_health != null)
                _health.OnChange -= HandleChange;
        }

        // LateUpdate: the character has already moved and turned this frame
        void LateUpdate()
        {
            if (!_camera)
                _camera = Camera.main;

            // Same rotation as the camera: the bar stays flat on the screen whatever the orientation of the enemy
            if (_camera)
                transform.rotation = _camera.transform.rotation;

            float current = _references.Fill.fillAmount;

            if (!Mathf.Approximately(current, _target))
            {
                float next = _settings.FillSpeed <= 0f
                    ? _target
                    : Mathf.MoveTowards(current, _target, _settings.FillSpeed * Time.deltaTime);

                SetFill(next);
            }
        }

        #endregion

        #region PRIVATE METHODS

        private void HandleChange(int current, int max)
        {
            _target = max > 0 ? (float)current / max : 0f;

            if (_references.Root)
                _references.Root.SetActive(!_settings.HideWhenFull || current < max);
        }

        private void SetFill(float value)
        {
            _references.Fill.fillAmount = value;
            _references.Fill.color = Color.Lerp(_settings.LowColor, _settings.FullColor, value);
        }

        #endregion
    }
}
