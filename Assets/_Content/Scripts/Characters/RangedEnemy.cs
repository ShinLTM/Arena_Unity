using Arena.Combat;
using UnityEngine;

namespace Arena.Characters
{
    /// <summary>
    /// Ranged enemy: comes closer until it sees the player within range, then stops and shoots at it.
    /// Fires with a ProjectileLauncher, as the player does (same weapon model and projectiles).
    /// </summary>
    public class RangedEnemy : EnemyBase
    {
        [System.Serializable]
        private class Settings
        {
            [Tooltip("Maximum distance from which the enemy shoots, in meters.")]
            [Min(0.5f)] public float ShootRange = 12f;

            [Tooltip("Distance at which the enemy stops coming closer, in meters. Lower than or equal to the shoot range.")]
            [Min(0f)] public float PreferredDistance = 8f;

            [Tooltip("The enemy shoots only when the player is within this angle in front of it, in degrees.")]
            [Range(1f, 180f)] public float AimTolerance = 10f;

            [Tooltip("Delay before the first shot once the player is visible, in seconds. Gives the player time to react.")]
            [Min(0f)] public float AimDelay = 0.5f;

            [Tooltip("Height of the point aimed at, above the player's feet, in meters.")]
            public float TargetHeight = 1f;

            [Tooltip("Height of the eyes, above the enemy's feet, used to check that the player is visible, in meters.")]
            public float EyeHeight = 1.2f;

            [Tooltip("Smoothing of the orientation while aiming, in seconds. 0: the enemy turns instantly.")]
            [Min(0f)] public float RotationDamping = 0.15f;
        }

        [System.Serializable]
        private struct References
        {
            [Tooltip("Launcher of the weapon held by the enemy. Its weapon data should use infinite ammo.")]
            public ProjectileLauncher Launcher;
        }

        [System.Serializable]
        public class StateContainer
        {
            public bool CanSeeTarget;
            public bool IsAiming;
        }

        #region FIELDS

        [SerializeField] private Settings _settings;
        [SerializeField] private References _references;
        [SerializeField] private StateContainer _state;

        public StateContainer State => _state;

        private float _nextShotTime;

        #endregion

        #region UNITY LIFECYCLES

        private void Update()
        {
            if (!HasTarget)
            {
                _state.IsAiming = false;
                StopMoving();
                return;
            }

            float distance = GetFlatDistanceToTarget();
            _state.CanSeeTarget = distance <= _settings.ShootRange && CanSeeTarget();

            if (!_state.CanSeeTarget)
            {
                _state.IsAiming = false;
                Chase();
                return;
            }

            // Spotting the player: the first shot waits for the aim delay
            if (!_state.IsAiming)
                _nextShotTime = Mathf.Max(_nextShotTime, Time.time + _settings.AimDelay);

            _state.IsAiming = true;

            // Keeps coming closer up to the preferred distance, while still shooting
            if (distance > _settings.PreferredDistance)
                Chase();
            else
                StopMoving();

            // The agent would turn the enemy towards its path: it faces the player instead
            Agent.updateRotation = false;
            FaceTarget(_settings.RotationDamping, Time.deltaTime);
            AimMuzzle();

            if (Time.time >= _nextShotTime && GetAngleToTarget() <= _settings.AimTolerance)
                _references.Launcher.TryFire();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _settings.ShootRange);

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _settings.PreferredDistance);
        }

        #endregion

        #region PRIVATE METHODS

        private Vector3 GetAimPoint()
        {
            return TargetPosition + Vector3.up * _settings.TargetHeight;
        }

        /// <summary>
        /// True when nothing solid stands between the eyes of the enemy and the player.
        /// </summary>
        private bool CanSeeTarget()
        {
            Vector3 eyes = transform.position + Vector3.up * _settings.EyeHeight;
            Vector3 toTarget = GetAimPoint() - eyes;

            // The ray starts inside the enemy's own collider, which it does not hit
            if (!Physics.Raycast(eyes, toTarget, out RaycastHit hit, toTarget.magnitude + 1f, ~0, QueryTriggerInteraction.Ignore))
                return true;

            return hit.collider.GetComponentInParent<Player>() != null;
        }

        /// <summary>
        /// Points the muzzle at the player, so that the projectiles also follow its height (jump, stairs...).
        /// </summary>
        private void AimMuzzle()
        {
            Transform muzzle = _references.Launcher.Muzzle;
            Vector3 direction = GetAimPoint() - muzzle.position;

            if (direction.sqrMagnitude > 0.0001f)
                muzzle.rotation = Quaternion.LookRotation(direction);
        }

        #endregion
    }
}
