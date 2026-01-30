using UnityEngine;

// Define the Mask Types
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

    // --- MASK STATE ---
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

    protected virtual void Update()
    {
        // constantly check the children for masks
        UpdateMaskStatus();
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

    // --- UPDATED: FIND MASK BY NAME ---
    public void UpdateMaskStatus()
    {
        // 1. Reset Defaults
        currentMask = MaskType.None;
        visionMultiplier = 1f;

        // 2. Loop through all direct children
        foreach (Transform child in transform)
        {
            // We use .Contains() because Unity adds "(Clone)" to instantiated objects
            string childName = child.name;

            if (childName.Contains("RedMask"))
            {
                currentMask = MaskType.Red;
                // Red has highest priority, so we can stop looking and return immediately
                // (Logic for Red is handled in subclasses)
                return; 
            }
            else if (childName.Contains("YellowMask"))
            {
                currentMask = MaskType.Yellow;
                // Logic for Yellow (Decrease Vision) happens right here
                visionMultiplier = 0.5f;
            }
            else if (childName.Contains("GreenMask"))
            {
                currentMask = MaskType.Green;
            }
        }
    }

    // --- VISION LOGIC (Uses the multiplier) ---
    protected bool IsPlayerVisible(float dist)
    {
        float actualVision = visionRange * visionMultiplier;

        if (dist > actualVision) return false;
        if (isAlerted) return true;
        if (fovAngle >= 360f) return true;

        Vector2 facingDir = (spriteRenderer != null && spriteRenderer.flipX) ? Vector2.left : Vector2.right;
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector2.Angle(facingDir, dirToPlayer);

        return angleToPlayer < (fovAngle / 2f);
    }

    // --- SHARED BEHAVIOR HELPERS ---
    protected abstract void PerformBehavior(float distanceToPlayer);

    protected void Logic_ChaseIfInRange(float dist)
    {
        if (IsPlayerVisible(dist) || isAlerted)
        {
            if (!isAlerted) isAlerted = true; 
            if (dist > stoppingDistance) MoveTo(player.position);
            else 
            {
                StopMoving();
                if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
            }
        }
        else StopMoving();
    }

    protected void MoveTo(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
        if(spriteRenderer) spriteRenderer.flipX = dir.x < 0;
    }

    protected void StopMoving() => rb.linearVelocity = Vector2.zero;

    protected virtual void OnAlertReceived(Vector3 p, Vector3 o, float r)
    {
        if (Vector2.Distance(transform.position, o) > r) return;
        isAlerted = true;
    }

    protected virtual void OnDestroy() { EnemyAlertSystem.OnPlayerFound -= OnAlertReceived; }

    // --- DRAWING ---
    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f; lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.sortingOrder = -1;
        lineRenderer.startColor = skinColor; lineRenderer.endColor = skinColor;
    }

    void DrawVisionCone()
    {
        if (lineRenderer == null) return;
        int segments = 50;
        lineRenderer.positionCount = segments + 2; 

        float currentFacingAngle = (spriteRenderer != null && spriteRenderer.flipX) ? 180f : 0f;
        float startAngle = currentFacingAngle - (fovAngle / 2f);
        float angleStep = fovAngle / segments;
        
        // Draw the REDUCED vision if mask is yellow
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