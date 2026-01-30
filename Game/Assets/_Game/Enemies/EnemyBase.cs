using UnityEngine;

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

    // --- NEW: MASK VARIABLES ---
    protected MaskType currentMask = MaskType.None;
    protected float visionMultiplier = 1f;

    // References
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected LineRenderer lineRenderer;
    protected bool isAlerted = false;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        rb.gravityScale = 0;
        rb.freezeRotation = true;
        if(spriteRenderer) spriteRenderer.color = skinColor;
        EnemyAlertSystem.OnPlayerFound += OnAlertReceived;
        if (showVisionCircle) SetupLineRenderer();
    }

    protected virtual void OnDestroy()
    {
        EnemyAlertSystem.OnPlayerFound -= OnAlertReceived;
    }

    protected virtual void FixedUpdate()
    {
        if (player == null) return;
        PerformBehavior(Vector2.Distance(transform.position, player.position));
    }

    void LateUpdate()
    {
        if (showVisionCircle && lineRenderer != null) DrawVisionCone();
    }

    // --- NEW: APPLY MASK LOGIC ---
    public virtual void ApplyMask(MaskType type)
    {
        currentMask = type;

        if (type == MaskType.DecreaseVision)
        {
            visionMultiplier = 0.5f; // Cut vision in half
            // Visual feedback (optional): Turn Blue
            if(spriteRenderer) spriteRenderer.color = Color.cyan; 
        }
        else if (type == MaskType.Aggressive)
        {
            visionMultiplier = 1f; // Reset vision reduction if we get aggressive
            // Visual feedback: Turn Purple
            if(spriteRenderer) spriteRenderer.color = new Color(0.5f, 0f, 1f); 
        }
    }

    // --- UPDATED VISION CHECK ---
    protected bool IsPlayerVisible(float dist)
    {
        // APPLY MULTIPLIER HERE
        float actualVision = visionRange * visionMultiplier;

        if (dist > actualVision) return false;
        if (isAlerted) return true;
        if (fovAngle >= 360f) return true;

        Vector2 facingDir = (spriteRenderer != null && spriteRenderer.flipX) ? Vector2.left : Vector2.right;
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector2.Angle(facingDir, dirToPlayer);

        return angleToPlayer < (fovAngle / 2f);
    }

    protected abstract void PerformBehavior(float distanceToPlayer);

    // ... (Keep Logic_ChaseIfInRange, MoveTo, StopMoving same as before) ...
    
    // Helper needed for Angry Berserk Logic
    protected void Logic_ChaseIfInRange(float dist)
    {
        float actualVision = visionRange * visionMultiplier;

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
        if(spriteRenderer) spriteRenderer.flipX = dir.x < 0;
    }

    protected void StopMoving() => rb.velocity = Vector2.zero;

    protected virtual void OnAlertReceived(Vector3 p, Vector3 o, float r)
    {
        if (Vector2.Distance(transform.position, o) > r) return;
        isAlerted = true;
    }

    // ... (Keep SetupLineRenderer and DrawVisionCone same as before) ...
    // Just ensure DrawVisionCone uses (visionRange * visionMultiplier) when drawing!
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

        float currentFacingAngle = (spriteRenderer != null && spriteRenderer.flipX) ? 180f : 0f;
        float startAngle = currentFacingAngle - (fovAngle / 2f);
        float angleStep = fovAngle / segments;
        
        // Use Multiplier for drawing
        float actualRange = visionRange * visionMultiplier;

        lineRenderer.SetPosition(0, Vector3.zero);
        for (int i = 0; i <= segments; i++)
        {
            float angleRad = Mathf.Deg2Rad * (startAngle + (angleStep * i));
            float x = Mathf.Cos(angleRad) * actualRange;
            float y = Mathf.Sin(angleRad) * actualRange;
            lineRenderer.SetPosition(i + 1, new Vector3(x, y, 0));
        }
        lineRenderer.loop = true;
        if (fovAngle >= 360) lineRenderer.positionCount = segments;
    }
}