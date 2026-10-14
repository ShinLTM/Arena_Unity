using Arena_Unity.Combat;
using UnityEngine;

namespace Arena_Unity.Combat
{
    /// <summary>
    /// Moves straight ahead and damages the first IDamageable it touches.
    /// Needs a trigger collider and a kinematic Rigidbody.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Speed, in meters per second.")]
            [Min(0f)] public float Speed = 20f;

            [Tooltip("Time before an unused projectile is destroyed, in seconds.")]
            [Min(0.1f)] public float Lifetime = 2f;
        }

        #region Fields

        [SerializeField] private Settings _settings;

        private int _damage;
        private GameObject _owner;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            transform.position += transform.forward * _settings.Speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ignore the shooter and the other trigger zones
            if (other.isTrigger || other.gameObject == _owner)
                return;

            if (other.TryGetComponent(out IDamageable target))
                target.TakeDamage(_damage);

            Destroy(gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the projectile. Called right after Instantiate by whoever fires it.
        /// </summary>
        /// <param name="damage">Damage dealt to the IDamageable it touches.</param>
        /// <param name="owner">Shooter, ignored by the projectile.</param>
        public void Launch(int damage, GameObject owner)
        {
            _damage = damage;
            _owner = owner;
            Destroy(gameObject, _settings.Lifetime);
        }

        #endregion
    }
}