using System;
using Arena.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Arena.Characters
{
    /// <summary>
    /// Shared behaviour of the enemies: moves on the NavMesh towards the player and turns to face it.
    /// The health points and the death are handled by the Destructible on the same object.
    /// The behaviour itself (melee, ranged...) is written in the child classes.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Destructible))]
    public abstract class EnemyBase : MonoBehaviour
    {
        #region FIELDS

        /// <summary>
        /// Raised once, when the enemy leaves the game (killed or destroyed). Used by the WaveSpawnManager.
        /// </summary>
        public event Action<EnemyBase> OnRemoved;

        public Destructible Destructible { get; private set; }

        protected NavMeshAgent Agent { get; private set; }

        /// <summary>
        /// True while there is a living player to chase.
        /// </summary>
        protected bool HasTarget => Player.Instance != null && !Player.Instance.Health.IsDead;

        protected Vector3 TargetPosition => Player.Instance.transform.position;

        #endregion

        #region UNITY LIFECYCLES

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Destructible = GetComponent<Destructible>();
        }

        protected virtual void OnDestroy()
        {
            OnRemoved?.Invoke(this);
        }

        #endregion

        #region PROTECTED METHODS

        /// <summary>
        /// Moves towards the player. The agent turns the enemy in the direction it walks.
        /// </summary>
        protected void Chase()
        {
            if (!Agent.isOnNavMesh)
                return;

            Agent.updateRotation = true;
            Agent.isStopped = false;
            Agent.SetDestination(TargetPosition);
        }

        /// <summary>
        /// Stops on the spot. The rotation is then given by FaceTarget.
        /// </summary>
        protected void StopMoving()
        {
            if (!Agent.isOnNavMesh)
                return;

            Agent.updateRotation = false;
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }

        /// <summary>
        /// Turns around the vertical axis towards the player.
        /// </summary>
        /// <param name="damping">Smoothing of the orientation, in seconds. 0: turns instantly.</param>
        protected void FaceTarget(float damping, float t)
        {
            Vector3 direction = GetFlatDirectionToTarget();

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion target = Quaternion.LookRotation(direction);

            if (damping <= 0f)
            {
                transform.rotation = target;
            }
            else
            {
                // Exponential smoothing: the same feel whatever the frame rate
                float blend = 1f - Mathf.Exp(-t / damping);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, blend);
            }
        }

        /// <summary>
        /// Horizontal distance to the player, in meters.
        /// </summary>
        protected float GetFlatDistanceToTarget()
        {
            return GetFlatDirectionToTarget().magnitude;
        }

        /// <summary>
        /// Horizontal angle between the forward axis of the enemy and the player, in degrees.
        /// </summary>
        protected float GetAngleToTarget()
        {
            return Vector3.Angle(transform.forward, GetFlatDirectionToTarget());
        }

        #endregion

        #region PRIVATE METHODS

        private Vector3 GetFlatDirectionToTarget()
        {
            Vector3 direction = TargetPosition - transform.position;
            direction.y = 0f;
            return direction;
        }

        #endregion
    }
}
