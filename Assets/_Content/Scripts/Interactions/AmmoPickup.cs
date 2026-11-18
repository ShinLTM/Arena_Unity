using Arena.Characters;
using Arena.Combat;
using UnityEngine;

namespace Arena.Interactions
{
    /// <summary>
    /// Ammo item on the ground: gives ammo to the player who walks on it.
    /// Disappearing and coming back are handled by Pickup.
    /// </summary>
    public class AmmoPickup : Pickup
    {
        [System.Serializable]
        private class Settings
        {
            [Tooltip("Ammo given to the player.")]
            [Min(1)] public int Amount = 15;

            [Tooltip("The item is not taken while the player's ammo is full.")]
            public bool IgnoreWhenFull = true;
        }

        #region FIELDS

        [SerializeField] private Settings _settings;

        #endregion

        #region PROTECTED METHODS

        protected override bool TryApply(Player player)
        {
            ProjectileLauncher launcher = player.Launcher;

            if (_settings.IgnoreWhenFull && launcher.State.Ammo >= launcher.MaxAmmo)
                return false;

            launcher.AddAmmo(_settings.Amount);
            return true;
        }

        #endregion
    }
}
