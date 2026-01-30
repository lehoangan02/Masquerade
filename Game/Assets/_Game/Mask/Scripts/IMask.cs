using UnityEngine;

/// <summary>
/// Interface for throwable masks.
/// Masks stick to enemies and apply effects.
/// </summary>
public interface IMask : IThrowable
{
    /// <summary>
    /// The type/name of this mask.
    /// </summary>
    string MaskName { get; }
    
    /// <summary>
    /// Color of this mask.
    /// </summary>
    Color MaskColor { get; }
    
    /// <summary>
    /// Called when mask attaches to a target.
    /// </summary>
    void OnAttach(GameObject target);
    
    /// <summary>
    /// Called when mask effect should be applied (for later use).
    /// </summary>
    void ApplyEffect(GameObject target);
}
