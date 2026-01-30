using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 3f;
    public float visionRange = 5f;
    public float stoppingDistance = 0.6f;
    public Color skinColor = Color.white;
    public bool showVisionCircle = true;

    // CHANGED: No longer public, so it doesn't show in Inspector
    protected Transform player; 

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected bool isAlerted = false;
    protected LineRenderer lineRenderer;

    // --- SETUP ---
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // --- NEW: AUTO-FIND PLAYER BY TAG ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            // Error safety check
            Debug.LogError("ENEMY ERROR: Could not find an object tagged 'Player'! Please assign the Tag in the Inspector.");
        }

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
        // Safety: If player wasn't found, do nothing
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        PerformBehavior(dist);
    }

    void LateUpdate()
    {
        if (showVisionCircle && lineRenderer != null) DrawCircle();
    }

    // --- METHODS FOR CHILDREN ---
    protected abstract void PerformBehavior(float distanceToPlayer);

    protected void Logic_ChaseIfInRange(float dist)
    {
        if (dist <= visionRange || isAlerted)
        {
            if (dist > stoppingDistance)
            {
                MoveTo(player.position);
            }
            else
            {
                StopMoving();
                if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
            }

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
        
        if(spriteRenderer) spriteRenderer.flipX = target.x < transform.position.x;
    }

    protected void StopMoving() 
    {
        rb.linearVelocity = Vector2.zero; 
    }

    protected virtual void OnAlertReceived(Vector3 pos)
    {
        isAlerted = true;
    }

    // --- VISUALS ---
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