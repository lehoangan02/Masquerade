using UnityEngine;

public enum MaskType { None, Red, Yellow, Green }

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 3f;
    public float visionRange = 5f;
    [Range(0, 360)] public float fovAngle = 360f;
    public float stoppingDistance = 0.6f;
    public Color skinColor = Color.white;
    public bool showVisionCircle = true;

    [Header("Movement Feel")]
    [Tooltip("Higher = More slippery. 0.05 = Snappy, 0.3 = Icy")]
    public float slideInertia = 0.1f; 

    [Header("Pathfinding")]
    public LayerMask obstacleLayer; 
    public float avoidRange = 1.5f; 

    // --- MASK STATE ---
    protected MaskType currentMask = MaskType.None;
    protected float visionMultiplier = 1f;

    // References
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected LineRenderer lineRenderer;
    protected bool isAlerted = false;

    // Movement Smoothing Helper
    private Vector2 currentVelocityRef; // Required by SmoothDamp

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        rb.gravityScale = 0;
        rb.freezeRotation = true;
        
        // IMPORTANT: Add some drag so they don't slide forever if physics takes over
        rb.linearDamping = 1f; 

        if(spriteRenderer) spriteRenderer.color = skinColor;
        EnemyAlertSystem.OnPlayerFound += OnAlertReceived;
        if (showVisionCircle) SetupLineRenderer();
    }

    protected virtual void Update() { UpdateMaskStatus(); }

    protected virtual void FixedUpdate()
    {
        if (player == null) return;
        PerformBehavior(Vector2.Distance(transform.position, player.position));
    }

    void LateUpdate() { if (showVisionCircle && lineRenderer != null) DrawVisionCone(); }

    public void UpdateMaskStatus()
    {
        currentMask = MaskType.None;
        visionMultiplier = 1f;
        foreach (Transform child in transform)
        {
            if (child.name.Contains("RedMask")) { currentMask = MaskType.Red; return; }
            else if (child.name.Contains("YellowMask")) { currentMask = MaskType.Yellow; visionMultiplier = 0.5f; }
            else if (child.name.Contains("GreenMask")) { currentMask = MaskType.Green; }
        }
    }

    protected bool IsPlayerVisible(float dist)
    {
        float actualVision = visionRange * visionMultiplier;
        if (dist > actualVision) return false;
        if (isAlerted) return true;
        if (fovAngle >= 360f) return true;
        Vector2 facingDir = (spriteRenderer != null && spriteRenderer.flipX) ? Vector2.left : Vector2.right;
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        return Vector2.Angle(facingDir, dirToPlayer) < (fovAngle / 2f);
    }

    protected abstract void PerformBehavior(float distanceToPlayer);

    // --- UPDATED MOVEMENT WITH INERTIA ---

    protected void MoveTo(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        
        // Calculate the Target Velocity (Where we WANT to be going)
        Vector2 targetVelocity = dir * moveSpeed;

        // Smoothly slide current velocity towards target velocity
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocityRef, slideInertia);

        // Face direction
        if(spriteRenderer && rb.linearVelocity.sqrMagnitude > 0.1f) 
            spriteRenderer.flipX = rb.linearVelocity.x < 0;
    }

    protected void MoveToSmart(Vector2 target)
    {
        Vector2 desiredDir = (target - (Vector2)transform.position).normalized;
        Vector2 finalDir = desiredDir;

        // 1. Raycast Obstacle Check
        RaycastHit2D hit = Physics2D.Raycast(transform.position, desiredDir, avoidRange, obstacleLayer);

        if (hit.collider != null)
        {
            // 2. Try to find a way around
            Vector2[] directionsToCheck = new Vector2[]
            {
                RotateVector(desiredDir, 45),  
                RotateVector(desiredDir, -45), 
                RotateVector(desiredDir, 90),  
                RotateVector(desiredDir, -90)  
            };

            foreach (Vector2 checkDir in directionsToCheck)
            {
                RaycastHit2D checkHit = Physics2D.Raycast(transform.position, checkDir, avoidRange, obstacleLayer);
                if (checkHit.collider == null)
                {
                    finalDir = checkDir;
                    break;
                }
            }
        }

        // 3. APPLY SMOOTH VELOCITY
        Vector2 targetVelocity = finalDir * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocityRef, slideInertia);
        
        // Face movement direction
        if(spriteRenderer && rb.linearVelocity.sqrMagnitude > 0.1f) 
            spriteRenderer.flipX = rb.linearVelocity.x < 0;
        
        Debug.DrawRay(transform.position, finalDir * 2f, Color.blue);
    }

    protected void StopMoving()
    {
        // Instead of stopping instantly (rb.velocity = zero), we damp it to zero
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, Vector2.zero, ref currentVelocityRef, slideInertia);
    }

    // --- HELPERS ---
    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float ca = Mathf.Cos(rad);
        float sa = Mathf.Sin(rad);
        return new Vector2(ca * v.x - sa * v.y, sa * v.x + ca * v.y);
    }

    protected void Logic_ChaseIfInRange(float dist)
    {
        if (IsPlayerVisible(dist) || isAlerted)
        {
            if (!isAlerted) isAlerted = true; 
            if (dist > stoppingDistance) MoveToSmart(player.position); // Use Smart Move
            else StopMoving();
        }
        else StopMoving();
    }

    protected virtual void OnAlertReceived(Vector3 p, Vector3 o, float r) { if (Vector2.Distance(transform.position, o) <= r) isAlerted = true; }
    protected virtual void OnDestroy() { EnemyAlertSystem.OnPlayerFound -= OnAlertReceived; }
    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f; 
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.sortingOrder = -1;
        lineRenderer.startColor = skinColor; 
        lineRenderer.endColor = skinColor;
        
        // --- FIX IS HERE ---
        // Switch to Local Space so the circle moves with the Enemy
        lineRenderer.useWorldSpace = false; 
    }

    void DrawVisionCone()
    {
        if (lineRenderer == null) return;
        int segments = 50;
        lineRenderer.positionCount = segments + 2;

        // Calculate angles
        float currentFacingAngle = (spriteRenderer != null && spriteRenderer.flipX) ? 180f : 0f;
        float startAngle = currentFacingAngle - (fovAngle / 2f);
        float angleStep = fovAngle / segments;
        float actualRange = visionRange * visionMultiplier;

        // Point 0 is the center of the enemy (Local 0,0)
        lineRenderer.SetPosition(0, Vector3.zero);

        for (int i = 0; i <= segments; i++)
        {
            float angleRad = Mathf.Deg2Rad * (startAngle + (angleStep * i));
            
            // Calculate offsets
            float x = Mathf.Cos(angleRad) * actualRange;
            float y = Mathf.Sin(angleRad) * actualRange;

            // Since we are in Local Space, we just pass x,y directly.
            // (We do NOT add transform.position here)
            lineRenderer.SetPosition(i + 1, new Vector3(x, y, 0));
        }

        lineRenderer.loop = true;
        if (fovAngle >= 360) lineRenderer.positionCount = segments;
    }
}