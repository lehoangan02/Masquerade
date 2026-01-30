using UnityEngine;

/// <summary>
/// Base throwable type implementation for the widget.
/// Used for both bullets and masks.
/// </summary>
[System.Serializable]
public class ThrowableType
{
    [SerializeField] private string typeName = "Basic";
    [SerializeField] private Color color = Color.white;
    [SerializeField] private GameObject prefab;

    public string TypeName => typeName;
    public Color Color => color;
    public GameObject Prefab => prefab;

    public ThrowableType(string name, Color col, GameObject prefabRef = null)
    {
        typeName = name;
        color = col;
        prefab = prefabRef;
    }
}

/// <summary>
/// Red mask type - Rage effect.
/// </summary>
[System.Serializable]
public class RedMaskType : ThrowableType
{
    public RedMaskType(GameObject prefab = null) : base("Rage", Color.red, prefab) { }
}

/// <summary>
/// Blue mask type - Confusion effect.
/// </summary>
[System.Serializable]
public class BlueMaskType : ThrowableType
{
    public BlueMaskType(GameObject prefab = null) : base("Confusion", Color.cyan, prefab) { }
}

/// <summary>
/// Green mask type - Charm effect.
/// </summary>
[System.Serializable]
public class GreenMaskType : ThrowableType
{
    public GreenMaskType(GameObject prefab = null) : base("Charm", Color.green, prefab) { }
}


// ============================================
// LEGACY: Keep for backwards compatibility
// ============================================

/// <summary>
/// Base bullet type implementation. Extend this for custom bullet types.
/// </summary>
[System.Serializable]
public class BulletType : IBulletType
{
    [SerializeField] private string typeName = "Basic";
    [SerializeField] private Color bulletColor = Color.white;
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private int currentAmmo;

    public string TypeName => typeName;
    public Color BulletColor => bulletColor;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    public BulletType(string name, Color color, int max)
    {
        typeName = name;
        bulletColor = color;
        maxAmmo = max;
        currentAmmo = max;
    }

    public bool UseAmmo()
    {
        if (currentAmmo <= 0) return false;
        currentAmmo--;
        return true;
    }

    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
    }

    public virtual void OnHitEffect(GameObject target)
    {
        // Override in subclasses for special effects (burn, freeze, poison, etc.)
    }
}

/// <summary>
/// Red bullet type - placeholder for Fire/Damage type.
/// </summary>
[System.Serializable]
public class RedBulletType : BulletType
{
    public RedBulletType() : base("Fire", Color.red, 5) { }
    
    public override void OnHitEffect(GameObject target)
    {
        Debug.Log($"[Red/Fire] Hit {target.name}!");
    }
}

/// <summary>
/// Blue bullet type - placeholder for Ice/Slow type.
/// </summary>
[System.Serializable]
public class BlueBulletType : BulletType
{
    public BlueBulletType() : base("Ice", Color.cyan, 3) { }
    
    public override void OnHitEffect(GameObject target)
    {
        Debug.Log($"[Blue/Ice] Hit {target.name}!");
    }
}

/// <summary>
/// Green bullet type - placeholder for Poison/DoT type.
/// </summary>
[System.Serializable]
public class GreenBulletType : BulletType
{
    public GreenBulletType() : base("Poison", Color.green, 4) { }
    
    public override void OnHitEffect(GameObject target)
    {
        Debug.Log($"[Green/Poison] Hit {target.name}!");
    }
}
