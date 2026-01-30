using UnityEngine;

/// <summary>
/// Interface for any throwable object.
/// Implement this to create new throwable types (bullets, bombs, boomerangs, etc.)
/// </summary>
public interface IThrowable
{
    /// <summary>
    /// Launch the throwable in a direction.
    /// </summary>
    /// <param name="direction">Normalized direction vector.</param>
    void Throw(Vector2 direction);
    
    /// <summary>
    /// The speed of this throwable.
    /// </summary>
    float Speed { get; }
    
    /// <summary>
    /// Damage dealt on hit.
    /// </summary>
    int Damage { get; }
}
