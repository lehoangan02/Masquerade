using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player throwing mechanics with aimlock.
/// Attach to the Player alongside PlayerController.
/// </summary>
public class PlayerThrower : MonoBehaviour
{
    [Header("Throwable Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwCooldown = 0.2f;

    [Header("Aimlock Settings")]
    [SerializeField] private float aimlockRadius = 2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Color aimLineColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private float aimLineWidth = 0.05f;

    [Header("Input")]
    private InputAction throwAction;
    private Camera mainCamera;
    
    private float lastThrowTime;
    private Transform lockedTarget;
    private LineRenderer aimLine;

    void Awake()
    {
        mainCamera = Camera.main;
        
        // Setup throw input (Left Click)
        throwAction = new InputAction("Throw", InputActionType.Button, "<Mouse>/leftButton");
        
        // Setup aim line
        SetupAimLine();
    }

    void SetupAimLine()
    {
        // Create LineRenderer for aim indicator
        GameObject lineObj = new GameObject("AimLine");
        lineObj.transform.SetParent(transform);
        aimLine = lineObj.AddComponent<LineRenderer>();
        
        // Configure line appearance
        aimLine.startWidth = aimLineWidth;
        aimLine.endWidth = aimLineWidth;
        aimLine.positionCount = 2;
        aimLine.useWorldSpace = true;
        
        // Create simple material
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = aimLineColor;
        aimLine.endColor = aimLineColor;
        
        // Initially hidden
        aimLine.enabled = false;
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
        // Update aimlock
        UpdateAimlock();
        
        // Update aim line visual
        UpdateAimLine();
        
        if (throwAction.WasPressedThisFrame())
        {
            TryThrow();
        }
    }

    void UpdateAimlock()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        
        // Find closest enemy near mouse
        lockedTarget = FindClosestEnemy(mouseWorldPos);
    }

    Transform FindClosestEnemy(Vector3 position)
    {
        // Find all colliders in radius around mouse position
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, aimlockRadius, enemyLayer);
        
        // Also check by tag if no layer set
        if (colliders.Length == 0)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform closest = null;
            float closestDist = aimlockRadius;
            
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector2.Distance(position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy.transform;
                }
            }
            return closest;
        }
        
        // Find closest from layer-based search
        Transform closestTarget = null;
        float closestDistance = aimlockRadius;
        
        foreach (Collider2D col in colliders)
        {
            float dist = Vector2.Distance(position, col.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestTarget = col.transform;
            }
        }
        
        return closestTarget;
    }

    void UpdateAimLine()
    {
        if (lockedTarget != null && aimLine != null)
        {
            aimLine.enabled = true;
            aimLine.SetPosition(0, throwPoint != null ? throwPoint.position : transform.position);
            aimLine.SetPosition(1, lockedTarget.position);
        }
        else if (aimLine != null)
        {
            aimLine.enabled = false;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;
        return mouseWorldPos;
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

        // Get aim direction (locked target or mouse position)
        Vector2 aimDirection = GetAimDirection();
        
        // Spawn and throw
        ThrowProjectile(aimDirection);
        
        lastThrowTime = Time.time;
    }

    /// <summary>
    /// Get direction - prioritizes locked target, falls back to mouse.
    /// </summary>
    public Vector2 GetAimDirection()
    {
        Vector3 targetPos;
        
        if (lockedTarget != null)
        {
            // Aim at locked enemy
            targetPos = lockedTarget.position;
        }
        else
        {
            // Aim at mouse
            targetPos = GetMouseWorldPosition();
        }
        
        Vector2 direction = (targetPos - transform.position).normalized;
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

    /// <summary>
    /// Get the current locked target (for UI or other systems).
    /// </summary>
    public Transform GetLockedTarget()
    {
        return lockedTarget;
    }

    // Debug visualization in editor
    void OnDrawGizmosSelected()
    {
        // Draw aimlock radius at mouse position in editor
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Vector3 mousePos = GetMouseWorldPosition();
            Gizmos.DrawWireSphere(mousePos, aimlockRadius);
        }
    }
}
