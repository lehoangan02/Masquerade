using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 3f;
    public float visionRange = 5f;
    public float stoppingDistance = 0.6f; // New: Stops before hitting player
    public Color skinColor = Color.white;
    public bool showVisionCircle = true;

    [Header("References")]
    public Transform player;

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected bool isAlerted = false;
    protected LineRenderer lineRenderer;

    // --- SETUP ---
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Physics Setup
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        // Visual Setup
        if(spriteRenderer) spriteRenderer.color = skinColor;
        EnemyAlertSystem.OnPlayerFound += OnAlertReceived;

        if (showVisionCircle) SetupLineRenderer();
    }

    protected virtual void OnDestroy()
    {
        EnemyAlertSystem.OnPlayerFound -= OnAlertReceived;
    }

    // --- LOOP ---
    protected virtual void FixedUpdate()
    {
        if (player == null) return;

        // Calculate distance for everyone to use
        float dist = Vector2.Distance(transform.position, player.position);

        // RUN THE SPECIFIC LOGIC (Defined in Child Scripts)
        PerformBehavior(dist);
    }

    void LateUpdate()
    {
        if (showVisionCircle && lineRenderer != null) DrawCircle();
    }

    // --- METHODS FOR CHILDREN TO USE ---
    
    // Abstract: Children MUST implement this
    protected abstract void PerformBehavior(float distanceToPlayer);

    // Common Logic: Chase if close
    protected void Logic_ChaseIfInRange(float dist)
    {
        if (dist <= visionRange || isAlerted)
        {
            // NEW: Only move if we are further than stopping distance
            if (dist > stoppingDistance)
            {
                MoveTo(player.position);
            }
            else
            {
                // We are close enough to attack, stop moving
                StopMoving();
                // Optional: Keep looking at player while stopped
                if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
            }

            // If we reached the spot and player is gone, calm down
            if (isAlerted && dist < 1f) isAlerted = false;
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
        
        // Flip Sprite (No Teleporting)
        if(spriteRenderer) spriteRenderer.flipX = target.x < transform.position.x;
    }

    protected void StopMoving() 
    {
        // FIX: Changed linearVelocity to velocity for compatibility
        rb.linearVelocity = Vector2.zero; 
    }

    // Standard Alert Receiver
    protected virtual void OnAlertReceived(Vector3 pos)
    {
        isAlerted = true;
    }

    // --- VISUALS (Automatic) ---
    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 51;
        lineRenderer.loop = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.sortingOrder = -1;
        lineRenderer.startColor = skinColor;
        lineRenderer.endColor = skinColor;
    }

    void DrawCircle()
    {
        float angle = 0f;
        for (int i = 0; i < 51; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * visionRange;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * visionRange;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += (360f / 50);
        }
    }
}