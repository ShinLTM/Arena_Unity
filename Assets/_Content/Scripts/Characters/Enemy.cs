using Arena.Combat;
using UnityEngine;

namespace Arena.Characters
{
    /// <summary>
    /// Melee enemy: chases the player and hits it when it is close enough and in front of it.
    /// Each attack plays the attack animation; the damage is dealt at the moment of the hit in the animation,
    /// only if the player is still in range: the player can dodge.
    /// </summary>
    public class Enemy : EnemyBase
    {
        [System.Serializable]
        private class Settings
        {
            [Tooltip("Distance from which the enemy stops and attacks, in meters.")]
            [Min(0.1f)] public float AttackRange = 1.5f;

            [Tooltip("Width of the cone in front of the enemy where the player can be hit, in degrees.")]
            [Range(1f, 360f)] public float AttackAngle = 90f;

            [Tooltip("Damage dealt by one hit.")]
            [Min(0)] public int Damage = 10;

            public DamageType DamageType = DamageType.Physical;

            [Tooltip("Time between the start of two attacks, in seconds. Should be longer than the attack animation (0.83 s).")]
            [Min(0.05f)] public float AttackInterval = 1.2f;

            [Tooltip("Delay before the first attack once the player is in range, in seconds. Gives the player time to react.")]
            [Min(0f)] public float AttackWindup = 0.3f;

            [Tooltip("Time between the start of the attack animation and the moment the hit lands, in seconds.")]
            [Min(0f)] public float HitDelay = 0.35f;

            [Tooltip("Extra distance kept to land the hit, in meters: the player must go a bit further than the attack range to dodge.")]
            [Min(0f)] public float HitRangeTolerance = 0.3f;

            [Tooltip("Smoothing of the orientation while attacking, in seconds. 0: the enemy turns instantly.")]
            [Min(0f)] public float RotationDamping = 0.1f;
        }

        [System.Serializable]
        private struct References
        {
            [Tooltip("Optional. Empty: the CharacterAnimator on the same object is used.")]
            public CharacterAnimator CharacterAnimator;
        }

        [System.Serializable]
        public class StateContainer
        {
            public bool IsInRange;
            public bool IsAttacking;
        }

        #region FIELDS

        [SerializeField] private Settings _settings;
        [SerializeField] private References _references;
        [SerializeField] private StateContainer _state;

        public StateContainer State => _state;

        private float _nextAttackTime;
        private float _hitTime;

        #endregion

        #region UNITY LIFECYCLES

        protected override void Awake()
        {
            base.Awake();

            if (!_references.CharacterAnimator)
                _references.CharacterAnimator = GetComponent<CharacterAnimator>();
        }

        private void Update()
        {
            if (!HasTarget)
            {
                _state.IsAttacking = false;
                StopMoving();
                return;
            }

            // During a swing, the enemy stays planted until the hit lands
            if (_state.IsAttacking)
            {
                StopMoving();
                FaceTarget(_settings.RotationDamping, Time.deltaTime);

                if (Time.time >= _hitTime)
                    Hit();

                return;
            }

            bool isInRange = GetFlatDistanceToTarget() <= _settings.AttackRange;

            // Entering the range: the first attack waits for the windup
            if (isInRange && !_state.IsInRange)
                _nextAttackTime = Mathf.Max(_nextAttackTime, Time.time + _settings.AttackWindup);

            _state.IsInRange = isInRange;

            if (!isInRange)
            {
                Chase();
                return;
            }

            StopMoving();
            FaceTarget(_settings.RotationDamping, Time.deltaTime);

            if (Time.time >= _nextAttackTime && IsTargetInAttackAngle())
                StartAttack();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _settings.AttackRange);
        }

        #endregion

        #region PRIVATE METHODS

        private void StartAttack()
        {
            _state.IsAttacking = true;
            _nextAttackTime = Time.time + _settings.AttackInterval;
            _hitTime = Time.time + _settings.HitDelay;

            if (_references.CharacterAnimator)
                _references.CharacterAnimator.Attack();
        }

        /// <summary>
        /// Moment of the hit in the animation: damages the player if it did not dodge.
        /// </summary>
        private void Hit()
        {
            _state.IsAttacking = false;

            bool isStillInRange = GetFlatDistanceToTarget() <= _settings.AttackRange + _settings.HitRangeTolerance;

            if (isStillInRange && IsTargetInAttackAngle())
                Player.Instance.TakeDamage(new DamageInfo(_settings.Damage, _settings.DamageType, gameObject));
        }

        private bool IsTargetInAttackAngle()
        {
            return GetAngleToTarget() <= _settings.AttackAngle * 0.5f;
        }

        #endregion
    }
}
