using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 3f;
    public float visionRange = 5f;
    [Range(0, 360)] public float fovAngle = 360f; // 360 = Circle, 60 = Cone
    public float stoppingDistance = 0.6f;
    public Color skinColor = Color.white;
    public bool showVisionCircle = true;

    // References
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected LineRenderer lineRenderer;
    
    // State
    protected bool isAlerted = false;

    // --- SETUP ---
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Find SpriteRenderer in children (fixes issue if visual is a child object)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
             Debug.LogError($"ENEMY ERROR: '{gameObject.name}' has no SpriteRenderer in itself or children!");

        // Auto-find Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            player = playerObj.transform;
        }
        else 
        {
            Debug.LogError("ENEMY ERROR: No object with tag 'Player' found! Please tag your player.");
        }

        // Physics Setup
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        // Visual Setup
        if(spriteRenderer) spriteRenderer.color = skinColor;
        
        // Subscribe to Radio
        EnemyAlertSystem.OnPlayerFound += OnAlertReceived;

        if (showVisionCircle) SetupLineRenderer();
    }

    protected virtual void OnDestroy()
    {
        // Unsubscribe to prevent errors when enemy dies
        EnemyAlertSystem.OnPlayerFound -= OnAlertReceived;
    }

    // --- LOOP ---
    protected virtual void FixedUpdate()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        PerformBehavior(dist);
    }

    void LateUpdate()
    {
        if (showVisionCircle && lineRenderer != null) DrawVisionCone();
    }

    // --- CORE LOGIC METHODS ---
    
    // Abstract: Children MUST implement this
    protected abstract void PerformBehavior(float distanceToPlayer);

    // Smart Vision Check (Distance + Angle + Obstacles logic could go here)
    protected bool IsPlayerVisible(float dist)
    {
        // 1. Check Distance
        if (dist > visionRange) return false;

        // 2. If Alerted, we have "Infinite 360" vision (Psychic awareness)
        if (isAlerted) return true;

        // 3. If Angle is 360, we don't need math (Lazy Enemy)
        if (fovAngle >= 360f) return true;

        // 4. CONE MATH
        // Determine which way we are facing based on the Sprite Flip
        // Assuming: flipX TRUE = Facing LEFT, flipX FALSE = Facing RIGHT
        Vector2 facingDir = (spriteRenderer != null && spriteRenderer.flipX) ? Vector2.left : Vector2.right;
        Vector2 dirToPlayer = (player.position - transform.position).normalized;

        // Calculate angle between where we look and where player is
        float angleToPlayer = Vector2.Angle(facingDir, dirToPlayer);

        // If angle is within half our FOV, we see them
        return angleToPlayer < (fovAngle / 2f);
    }

    // Common Movement Logic
    protected void Logic_ChaseIfInRange(float dist)
    {
        if (IsPlayerVisible(dist) || isAlerted)
        {
            if (!isAlerted) isAlerted = true; 

            if (dist > stoppingDistance)
            {
                MoveTo(player.position);
            }
            else
            {
                StopMoving();
                // Face player when stopped
                if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
            }
        }
        else
        {
            StopMoving();
        }
    }

    protected void MoveTo(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
        
        // Face movement direction
        if(spriteRenderer) spriteRenderer.flipX = dir.x < 0;
    }

    protected void StopMoving() => rb.linearVelocity = Vector2.zero;

    // --- ALERT SYSTEM ---
    // Now accepts Sound Origin and Range to limit who hears it
    protected virtual void OnAlertReceived(Vector3 playerPos, Vector3 soundOrigin, float soundRange)
    {
        // 1. Check distance to the scream origin
        float distToSound = Vector2.Distance(transform.position, soundOrigin);

        // 2. If too far, ignore it
        if (distToSound > soundRange) return;

        // 3. Otherwise, get angry
        isAlerted = true;
    }

    // --- VISUALS (Cone Drawing) ---
    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.sortingOrder = -1;
        lineRenderer.startColor = skinColor;
        lineRenderer.endColor = skinColor;
    }

    void DrawVisionCone()
    {
        if (lineRenderer == null) return;

        int segments = 50;
        lineRenderer.positionCount = segments + 2; 

        // Default to facing Right (0 degrees)
        float currentFacingAngle = 0f;

        // Check sprite flip safely
        if (spriteRenderer != null && spriteRenderer.flipX)
        {
            currentFacingAngle = 180f; // Face Left
        }
        
        // Start Angle (Half of FOV to the left of facing dir)
        float startAngle = currentFacingAngle - (fovAngle / 2f);
        float angleStep = fovAngle / segments;

        // Point 0 is center
        lineRenderer.SetPosition(0, Vector3.zero);

        for (int i = 0; i <= segments; i++)
        {
            float angleRad = Mathf.Deg2Rad * (startAngle + (angleStep * i));
            float x = Mathf.Cos(angleRad) * visionRange;
            float y = Mathf.Sin(angleRad) * visionRange;
            
            lineRenderer.SetPosition(i + 1, new Vector3(x, y, 0));
        }

        // Loop logic
        lineRenderer.loop = true;
        if (fovAngle >= 360) 
        {
            lineRenderer.positionCount = segments; // Clean circle
        }
    }
}