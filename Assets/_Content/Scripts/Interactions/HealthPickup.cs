using Arena.Characters;
using Arena.Combat;
using UnityEngine;

namespace Arena.Interactions
{
    /// <summary>
    /// Health kit on the ground: heals the player who walks on it.
    /// Disappearing and coming back are handled by Pickup.
    /// </summary>
    public class HealthPickup : Pickup
    {
        [System.Serializable]
        private class Settings
        {
            [Tooltip("Health points given to the player.")]
            [Min(1)] public int Amount = 25;

            [Tooltip("The kit is not taken while the player's health is full.")]
            public bool IgnoreWhenFull = true;
        }

        #region FIELDS

        [SerializeField] private Settings _settings;

        #endregion

        #region PROTECTED METHODS

        protected override bool TryApply(Player player)
        {
            Health health = player.Health;

            if (_settings.IgnoreWhenFull && health.Current >= health.Max)
                return false;

            health.Heal(_settings.Amount);
            return true;
        }

        #endregion
    }
}
