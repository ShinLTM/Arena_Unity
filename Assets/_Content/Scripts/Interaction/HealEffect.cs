using Arena_Unity.Characters;
using UnityEngine;

namespace Arena_Unity.Interactions
{
    /// <summary>
    /// Heals the player. Apply is meant to be linked to a PlayerEvent.
    /// </summary>
    public class HealEffect : MonoBehaviour
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Hit points given to the player.")]
            [Min(1)] public int Amount = 25;
        }

        #region Fields

        [SerializeField] private Settings _settings;

        #endregion

        #region Public Methods

        public void Apply()
        {
            if (Player.Instance != null)
                Player.Instance.Health.Heal(_settings.Amount);
        }

        #endregion
    }
}
