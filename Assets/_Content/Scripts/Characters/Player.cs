using Arena.Combat;
using Arena_Unity.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arena_Unity.Characters
{
    public class Player : MonoBehaviour, IDamageable
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Movement speed, in km/h.")]
            public float Speed = 10;
        }

        [System.Serializable]
        private class References
        {
            public CharacterController Controller;
            public InputActionReference MoveAction;
        }


        [System.Serializable]
        public class StateContainer
        {
            public bool IsMoving = false;
        }


        #region FIELDS

        public static Player Instance { get; private set; }

        const float KMH_TO_MS = 1 / 3.6f;

        [SerializeField] private Settings _settings;
        [SerializeField] private References _references;
        [SerializeField] private StateContainer _state;
        [SerializeField] private Health _health;

        public StateContainer State => _state;

        private Vector2 _moveInput;

        private Camera _camera;

        #endregion

        #region UNITY LIFECYCLE

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogWarning($"A second Player was found on {name}, it is destroyed.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _camera = Camera.main;

            _health.Initialize();
        }

        private void OnEnable()
        {
            if (Instance == this)
            {
                _references.MoveAction.action.Enable();
            }

        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                _references.MoveAction.action.Disable();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Update()
        {
            if(_health.IsDead)
                return;

            float t = Time.deltaTime;

            GetInputs();
            Move(t);
        }
        #endregion

        #region public Methods
        public void TakeDamage(int amount)
        {
            _health.TakeDamage(amount);
        }
        #endregion

        #region Private Methods

#if UNITY_EDITOR
        [ContextMenu("Debug/Take 25 Damage")]
        private void DebugTakeDamage()
        {
            TakeDamage(25);
        }
#endif

        private void GetInputs()
        {
            _moveInput = _references.MoveAction.action.ReadValue<Vector2>();
        }

        private void Move(float t)
        {
            Vector3 direction = ToWorldDirection(_moveInput);
            Vector3 velocity = direction * KMH_TO_MS * _settings.Speed;

            _state.IsMoving = velocity.sqrMagnitude > .01f;

            _references.Controller.Move(velocity * t);
        }

        private Vector3 ToWorldDirection(Vector2 input)
        {
            Quaternion cameraHeading = Quaternion.Euler(0f, _camera.transform.eulerAngles.y, 0f);
            return cameraHeading * new Vector3(input.x, 0, input.y);
        }

        #endregion
    }
}  
