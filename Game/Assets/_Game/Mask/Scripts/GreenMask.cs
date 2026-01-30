using UnityEngine;

/// <summary>
/// Green Mask - Placeholder 
/// </summary>
public class GreenMask : Mask
{
    void Reset()
    {
        // Set defaults in editor
    }

    public override void OnAttach(GameObject target)
    {
        Debug.Log($"[Green Mask] Attached to {target.name} -  effect ready!");
        // TODO: Visual feedback (particles, etc.)
    }

    public override void ApplyEffect(GameObject target)
    {
        Debug.Log($"[Green Mask] Applying effect to {target.name}!");
        // TODO: Make enemy fight for player temporarily
    }
}
