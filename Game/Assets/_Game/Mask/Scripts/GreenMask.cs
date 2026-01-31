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

}
