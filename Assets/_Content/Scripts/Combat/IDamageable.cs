namespace Arena_Unity.Combat
{
    /// <summary>
    /// Anything that can receive damage: player, enemy, destructible object.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}
