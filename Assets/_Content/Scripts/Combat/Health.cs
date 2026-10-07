using System;
using UnityEngine;

namespace Arena.Combat
{
    /// <summary>
    /// Hit points of an entity. Serialized inside the component that owns it (Player, Enemy),
    /// which calls Initialize in its Awake.
    /// </summary>
    [System.Serializable]
    public class Health
    {
        #region Fields

        [Tooltip("Hit points when full.")]
        [Min(1)] public int Max = 100;

        [Tooltip("Current hit points, visible for debugging.")]
        [SerializeField] private int _current;

        /// <summary>
        /// Raised with the current and max hit points each time they change.
        /// </summary>
        public event Action<int, int> OnChange;

        /// <summary>
        /// Raised once, when the hit points reach zero.
        /// </summary>
        public event Action OnDied;

        public int Current => _current;
        public bool IsDead => _current <= 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Fills the hit points. Called by the owner in its Awake.
        /// </summary>
        public void Initialize()
        {
            SetCurrent(Max);
        }

        /// <summary>
        /// Removes hit points and raises OnDied the first time they reach zero.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
                return;

            SetCurrent(_current - amount);

            if (IsDead)
                OnDied?.Invoke();
        }

        /// <summary>
        /// Gives hit points back without exceeding Max. Has no effect once dead.
        /// </summary>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
                return;

            SetCurrent(_current + amount);
        }

        #endregion

        #region Private Methods

        private void SetCurrent(int value)
        {
            _current = Mathf.Clamp(value, 0, Max);
            OnChange?.Invoke(_current, Max);
        }

        #endregion
    }
}
