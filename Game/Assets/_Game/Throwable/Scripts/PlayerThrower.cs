using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrower : MonoBehaviour
{
    [Header("Throwable Settings")]
    [SerializeField] private GameObject defaultPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwCooldown = 0.2f;

    [Header("Aimlock Settings")]
    [SerializeField] private float aimlockRadius = 2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Color aimLineColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private float aimLineWidth = 0.05f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string throwTriggerName = "Throw";
    [SerializeField] private float throwAnimationDelay = 0.2f; // Reduced slightly for responsiveness

    [Header("Input")]
    private InputAction throwAction;
    private Camera mainCamera;
    
    private float lastThrowTime;
    private Transform lockedTarget;
    private LineRenderer aimLine;
    private Collider2D playerCollider; // Reference to player's collider

    void Awake()
    {
        mainCamera = Camera.main;
        playerCollider = GetComponent<Collider2D>(); // Get player collider
        
        throwAction = new InputAction("Throw", InputActionType.Button, "<Mouse>/leftButton");
        
        SetupAimLine();
    }

    void SetupAimLine()
    {
        GameObject lineObj = new GameObject("AimLine");
        lineObj.transform.SetParent(transform);
        aimLine = lineObj.AddComponent<LineRenderer>();
        
        aimLine.startWidth = aimLineWidth;
        aimLine.endWidth = aimLineWidth;
        aimLine.positionCount = 2;
        aimLine.useWorldSpace = true;
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = aimLineColor;
        aimLine.endColor = aimLineColor;
        aimLine.enabled = false;
    }

    void OnEnable() => throwAction.Enable();
    void OnDisable() => throwAction.Disable();

    void Start()
    {
        if (throwPoint == null)
        {
            GameObject point = new GameObject("ThrowPoint");
            point.transform.SetParent(transform);
            // Default to slightly in front to help visuals, though IgnoreCollision handles the physics
            point.transform.localPosition = new Vector3(0.5f, 0, 0); 
            throwPoint = point.transform;
        }
    }

    void Update()
    {
        UpdateAimlock();
        UpdateAimLine();
        
        if (throwAction.WasPressedThisFrame())
        {
            TryThrow();
        }
    }

    void UpdateAimlock()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        lockedTarget = FindClosestEnemy(mouseWorldPos);
    }

    Transform FindClosestEnemy(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, aimlockRadius, enemyLayer);
        
        // Priority: Layer mask -> Tag fallback
        if (colliders.Length > 0)
        {
            Transform closest = null;
            float closestDist = aimlockRadius;
            foreach (Collider2D col in colliders)
            {
                float dist = Vector2.Distance(position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.transform;
                }
            }
            return closest;
        }

        // Fallback to Tag search
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closestTag = null;
        float closestDistTag = aimlockRadius;
        
        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(position, enemy.transform.position);
            if (dist < closestDistTag)
            {
                closestDistTag = dist;
                closestTag = enemy.transform;
            }
        }
        return closestTag;
    }

    void UpdateAimLine()
    {
        if (lockedTarget != null && aimLine != null)
        {
            aimLine.enabled = true;
            aimLine.SetPosition(0, throwPoint.position);
            aimLine.SetPosition(1, lockedTarget.position);
        }
        else if (aimLine != null)
        {
            aimLine.enabled = false;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;
        return mouseWorldPos;
    }

    void TryThrow()
    {
        if (Time.time - lastThrowTime < throwCooldown) return;
        
        // Ammo Check
        if (BulletTypeWidget.Instance != null && !BulletTypeWidget.Instance.UseCurrentAmmo())
        {
            Debug.Log("Out of ammo!");
            return;
        }

        // Prefab Selection
        GameObject prefabToThrow = defaultPrefab;
        if (BulletTypeWidget.Instance != null && BulletTypeWidget.Instance.CurrentPrefab != null)
        {
            prefabToThrow = BulletTypeWidget.Instance.CurrentPrefab;
        }
        
        if (prefabToThrow == null) return;

        if (animator != null) animator.SetTrigger(throwTriggerName);
        
        StartCoroutine(DelayedThrow(prefabToThrow));
        lastThrowTime = Time.time;
    }

    private System.Collections.IEnumerator DelayedThrow(GameObject prefabToThrow)
    {
        yield return new WaitForSeconds(throwAnimationDelay);
        
        Vector2 aimDirection = GetAimDirection();
        ThrowProjectile(prefabToThrow, aimDirection);
    }

    public Vector2 GetAimDirection()
    {
        Vector3 targetPos;
        // Use throwPoint position as origin to ensure accuracy even if player moved
        Vector3 origin = throwPoint != null ? throwPoint.position : transform.position;

        if (lockedTarget != null)
        {
            targetPos = lockedTarget.position;
        }
        else
        {
            targetPos = GetMouseWorldPosition();
        }
        
        return (targetPos - origin).normalized;
    }

    void ThrowProjectile(GameObject prefab, Vector2 direction)
    {
        GameObject projectile = Instantiate(prefab, throwPoint.position, Quaternion.identity);
        
        // --- 1. SETUP MASK PROPERTIES ---
        Mask mask = projectile.GetComponent<Mask>();
        if (mask != null)
        {
            // CRITICAL FIX: Pass player collider to mask to ignore collision
            mask.IgnorePlayerCollision(playerCollider);

            if (BulletTypeWidget.Instance != null)
            {
                mask.SetMaskType(BulletTypeWidget.Instance.CurrentMaskTypeEnum);
            }
        }
        else
        {
            // Fallback for bullets
            Bullet bullet = projectile.GetComponent<Bullet>();
            if (bullet != null && BulletTypeWidget.Instance != null)
            {
                bullet.SetColor(BulletTypeWidget.Instance.CurrentColor);
            }
        }
        
        // --- 2. THROW PHYSICS ---
        IThrowable throwable = projectile.GetComponent<IThrowable>();
        if (throwable != null)
        {
            throwable.Throw(direction);
        }
    }

    /// <summary>
    /// Change the default throwable prefab.
    /// </summary>
    public void SetDefaultPrefab(GameObject newPrefab)
    {
        defaultPrefab = newPrefab;
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
