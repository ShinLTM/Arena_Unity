using System;
using UnityEngine;

namespace Arena.Combat
{
    /// <summary>
    /// Fires projectiles from a muzzle, limited by the fire interval and the ammo count.
    /// Placed on the weapon or on the shooter. The shooter (first IDamageable found in the parents: Player, Destructible)
    /// and all its children are ignored by the projectiles.
    /// </summary>
    public class ProjectileLauncher : MonoBehaviour
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Weapon data: damage, fire interval, ammo, projectile.")]
            public WeaponData Weapon;

            [Tooltip("The ammo never decreases. Used by the enemies.")]
            public bool InfiniteAmmo;
        }

        [System.Serializable]
        public struct References
        {
            [Tooltip("Spawn point of the projectiles. Its forward axis gives the direction.")]
            public Transform Muzzle;
        }

        [System.Serializable]
        public class StateContainer
        {
            public int Ammo;
        }

        #region FIELDS

        [SerializeField] private Settings _settings;
        [SerializeField] private References _references;
        [SerializeField] private StateContainer _state;

        /// <summary>
        /// Raised with the current and max ammo each time the ammo changes.
        /// </summary>
        public event Action<int, int> OnAmmoChange;

        public StateContainer State => _state;
        public int MaxAmmo => _settings.Weapon.MaxAmmo;
        public Transform Muzzle => _references.Muzzle;

        private float _nextFireTime;

        // Given as damage source: the projectiles ignore it and all its children
        private GameObject _owner;

        #endregion

        #region UNITY LIFECYCLES

        private void Awake()
        {
            _state.Ammo = MaxAmmo;

            // The launcher is usually on the weapon: the shooter is the character that holds it
            IDamageable owner = GetComponentInParent<IDamageable>();
            _owner = owner != null ? ((Component)owner).gameObject : gameObject;
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Fires one projectile if the weapon is ready and has ammo. Returns true if a shot was fired.
        /// </summary>
        public bool TryFire()
        {
            if (Time.time < _nextFireTime || _state.Ammo <= 0)
                return false;

            _nextFireTime = Time.time + _settings.Weapon.FireInterval;

            if (!_settings.InfiniteAmmo)
                SetAmmo(_state.Ammo - 1);

            Transform muzzle = _references.Muzzle;
            Projectile projectile = Instantiate(_settings.Weapon.ProjectilePrefab, muzzle.position, muzzle.rotation);
            projectile.Launch(new DamageInfo(_settings.Weapon.Damage, _settings.Weapon.DamageType, _owner));
            return true;
        }

        /// <summary>
        /// Adds ammo without exceeding the weapon maximum.
        /// </summary>
        public void AddAmmo(int amount)
        {
            if (amount <= 0)
                return;

            SetAmmo(Mathf.Min(_state.Ammo + amount, MaxAmmo));
        }

        #endregion

        #region PRIVATE METHODS

        private void SetAmmo(int value)
        {
            _state.Ammo = value;
            OnAmmoChange?.Invoke(_state.Ammo, MaxAmmo);
        }

        [ContextMenu("Debug/Try Fire")]
        private void DebugTryFire()
        {
            TryFire();
        }

        #endregion
    }
}
