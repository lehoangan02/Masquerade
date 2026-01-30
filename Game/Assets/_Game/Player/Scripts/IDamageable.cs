/// <summary>
/// Interface for anything that can take damage.
/// Follows the Interface Rule - allows universal damage without tight coupling.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
}
