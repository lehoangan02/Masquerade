using UnityEngine;

/// <summary>
/// Red Mask - Placeholder for Rage/Berserk effect.
/// </summary>
public class RedMask : Mask
{
    void Reset()
    {
        // Set defaults in editor
    }

    public override void OnAttach(GameObject target)
    {
        Debug.Log($"[Red Mask] Attached to {target.name} - Rage effect ready!");
        // TODO: Visual feedback (particles, etc.)
    }

}
