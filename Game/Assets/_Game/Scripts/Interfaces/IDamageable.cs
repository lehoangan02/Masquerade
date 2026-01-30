/// <summary>
/// Interface for any object that can take damage.
/// Implements the "Interface Rule" - allows bullets, traps, etc.
/// to damage anything without knowing the specific type.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Apply damage to this object.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    void TakeDamage(int amount);
}
