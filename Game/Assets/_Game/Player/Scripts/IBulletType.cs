using UnityEngine;

/// <summary>
/// Interface for different bullet types.
/// Implement this to create new bullet variants (Fire, Ice, Poison, etc.)
/// </summary>
public interface IBulletType
{
    /// <summary>
    /// Display name for UI.
    /// </summary>
    string TypeName { get; }
    
    /// <summary>
    /// Color of this bullet type.
    /// </summary>
    Color BulletColor { get; }
    
    /// <summary>
    /// Current ammo count.
    /// </summary>
    int CurrentAmmo { get; }
    
    /// <summary>
    /// Max ammo capacity.
    /// </summary>
    int MaxAmmo { get; }
    
    /// <summary>
    /// Use one ammo. Returns false if out of ammo.
    /// </summary>
    bool UseAmmo();
    
    /// <summary>
    /// Add ammo (e.g., from pickup).
    /// </summary>
    void AddAmmo(int amount);
    
    /// <summary>
    /// Apply special effect when bullet hits (optional override).
    /// </summary>
    void OnHitEffect(GameObject target);
}
