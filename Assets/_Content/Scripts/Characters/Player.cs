using Arena.Combat;
using Arena_Unity.Combat;
using System;
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
            public InputActionReference InteractAction;
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

        public Action OnInteract;

        public StateContainer State => _state;

        public Health Health => _health;


        private bool _isInteracting;

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

            //_health.Initialize();
        }

        private void OnEnable()
        {
            if (Instance == this)
            {
                _references.MoveAction.action.Enable();
                _references.InteractAction.action.Enable();
            }

        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                _references.InteractAction.action.Disable();
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
            Interact();
        }
        #endregion

        #region public Methods
        public void TakeDamage(int amount)
        {
            _health.TakeDamage(amount);
        }

        /// <summary>
        /// Teleports the player.
        /// </summary>
        /// <param name="pose">x, y, z: position. w: angle around the vertical axis, in degrees.</param>
        public void Teleport(Vector4 pose)
        {
            // An enabled CharacterController would overwrite the new position
            _references.Controller.enabled = false;

            Vector3 position = new Vector3(pose.x, pose.y, pose.z);
            Quaternion rotation = Quaternion.Euler(0f, pose.w, 0f);
            transform.SetPositionAndRotation(position, rotation);

            _references.Controller.enabled = true;
        }

        /// <summary>
        /// Teleports the player to the position and heading of a target, such as a spawn point.
        /// </summary>
        public void Teleport(Transform target)
        {
            Vector3 position = target.position;
            Vector4 pose = new Vector4(position.x, position.y, position.z, target.eulerAngles.y);
            Teleport(pose);
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
            _isInteracting = _references.InteractAction.action.WasPressedThisFrame();
        }

        private void Move(float t)
        {
            Vector3 direction = ToWorldDirection(_moveInput);
            Vector3 velocity = direction * KMH_TO_MS * _settings.Speed;

            _state.IsMoving = velocity.sqrMagnitude > .01f;

            _references.Controller.Move(velocity * t);
        }

        private void Interact()
        {
            if (_isInteracting)
                OnInteract?.Invoke();
        }

        private Vector3 ToWorldDirection(Vector2 input)
        {
            Quaternion cameraHeading = Quaternion.Euler(0f, _camera.transform.eulerAngles.y, 0f);
            return cameraHeading * new Vector3(input.x, 0, input.y);
        }

        #endregion
    }
}  
