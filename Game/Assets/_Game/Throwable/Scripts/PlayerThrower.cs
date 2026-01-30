using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player throwing mechanics.
/// Attach to the Player alongside PlayerController.
/// </summary>
public class PlayerThrower : MonoBehaviour
{
    [Header("Throwable Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwCooldown = 0.2f;

    [Header("Input")]
    private InputAction throwAction;
    private Camera mainCamera;
    
    private float lastThrowTime;

    void Awake()
    {
        mainCamera = Camera.main;
        
        // Setup throw input (Left Click)
        throwAction = new InputAction("Throw", InputActionType.Button, "<Mouse>/leftButton");
    }

    void OnEnable()
    {
        throwAction.Enable();
    }

    void OnDisable()
    {
        throwAction.Disable();
    }

    void Start()
    {
        // Create throw point if not assigned
        if (throwPoint == null)
        {
            GameObject point = new GameObject("ThrowPoint");
            point.transform.SetParent(transform);
            point.transform.localPosition = Vector3.zero;
            throwPoint = point.transform;
        }
    }

    void Update()
    {
        if (throwAction.WasPressedThisFrame())
        {
            TryThrow();
        }
    }

    void TryThrow()
    {
        // Check cooldown
        if (Time.time - lastThrowTime < throwCooldown) return;
        
        // Check if we have a bullet prefab
        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerThrower: No bullet prefab assigned!");
            return;
        }

        // Get aim direction (mouse position)
        Vector2 aimDirection = GetAimDirection();
        
        // Spawn and throw
        ThrowProjectile(aimDirection);
        
        lastThrowTime = Time.time;
    }

    /// <summary>
    /// Get direction from player to mouse cursor.
    /// </summary>
    public Vector2 GetAimDirection()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;
        
        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        return direction;
    }

    /// <summary>
    /// Spawn a projectile and throw it.
    /// </summary>
    void ThrowProjectile(Vector2 direction)
    {
        // Spawn at throw point
        GameObject projectile = Instantiate(bulletPrefab, throwPoint.position, Quaternion.identity);
        
        // Get IThrowable and throw
        IThrowable throwable = projectile.GetComponent<IThrowable>();
        if (throwable != null)
        {
            throwable.Throw(direction);
        }
        else
        {
            Debug.LogWarning("PlayerThrower: Prefab doesn't have IThrowable component!");
            Destroy(projectile);
        }
    }

    /// <summary>
    /// Change the current throwable prefab (for weapon switching).
    /// </summary>
    public void SetThrowable(GameObject newPrefab)
    {
        bulletPrefab = newPrefab;
    }
}
