using Arena.Characters;
using UnityEngine;

namespace Arena.Combat
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

        #region FIELDS

        [SerializeField] private Settings _settings;

        private DamageInfo _damage;

        #endregion

        #region UNITY LIFECYCLES

        private void Update()
        {
            transform.position += transform.forward * _settings.Speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Ignore the shooter (and its children, such as its weapon) and the other trigger zones
            if (other.isTrigger || (_damage.Source && other.transform.IsChildOf(_damage.Source.transform)))
                return;

            // No friendly fire between enemies: the projectile is only stopped
            if (other.TryGetComponent(out IDamageable target) && !IsBetweenEnemies(other))
                target.TakeDamage(_damage);

            Destroy(gameObject);
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Starts the projectile. Called right after Instantiate by whoever fires it.
        /// </summary>
        /// <param name="damage">Damage dealt to the IDamageable it touches. Its source is the shooter, ignored by the projectile.</param>
        public void Launch(DamageInfo damage)
        {
            _damage = damage;
            Destroy(gameObject, _settings.Lifetime);
        }

        #endregion

        #region PRIVATE METHODS

        private bool IsBetweenEnemies(Collider other)
        {
            return _damage.Source
                && _damage.Source.GetComponent<EnemyBase>() != null
                && other.GetComponentInParent<EnemyBase>() != null;
        }

        #endregion
    }
}
