using UnityEngine;

/// <summary>
/// Blue Mask - Placeholder 
/// </summary>
public class BlueMask : Mask
{
    void Reset()
    {
        // Set defaults in editor
    }

    public override void OnAttach(GameObject target)
    {
        Debug.Log($"[Blue Mask] Attached to {target.name} - effect ready!");
        // TODO: Visual feedback (particles, etc.)
    }

    public override void ApplyEffect(GameObject target)
    {
        Debug.Log($"[Blue Mask] Applying effect to {target.name}!");
        // TODO: Make enemy move erratically or freeze
    }
}
