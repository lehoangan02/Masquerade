using UnityEngine;
using System.Collections.Generic;

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

    [Header("Vision Settings")]
    [Tooltip("Adjust Y to move the cone to eye level (e.g., 0.5)")]
    public Vector3 visionOffset = new Vector3(0, 0.5f, 0); 

    [Header("Movement Feel")]
    public float slideInertia = 0.1f; 

    [Header("Pathfinding & Pits")]
    public LayerMask obstacleLayer; // Assign "Default" or "Walls"
    public LayerMask pitLayer;      // Assign "Pit" layer here
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
    private Vector2 currentVelocityRef;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        rb.gravityScale = 0;
        rb.freezeRotation = true;
        
        // Unity 6+ uses 'linearDamping', older versions use 'drag'
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

    // --- DEATH, DROPS & PITS ---
    
    // 1. Handle falling into pits
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Pit"))
        {
            Debug.Log(gameObject.name + " fell into a pit!");
            Die();
        }
    }

    // 2. Die and Drop Logic
    public virtual void Die()
    {
        List<Transform> masksToDrop = new List<Transform>();
        
        // Find masks
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Mask")) masksToDrop.Add(child);
        }

        // Drop masks
        foreach (Transform mask in masksToDrop)
        {
            mask.SetParent(null); 
            mask.rotation = Quaternion.identity; 
            
            // Optional: Re-enable collider logic here if needed
            // var col = mask.GetComponent<Collider2D>();
            // if(col) col.enabled = true;
        }

        // Kill Enemy
        Destroy(gameObject);
    }

    // --- MASK SWAPPING LOGIC ---

    // Automatically removes old masks when a new one is attached
    private void OnTransformChildrenChanged()
    {
        List<Transform> masks = new List<Transform>();
        foreach (Transform child in transform) { if (child.name.Contains("Mask")) masks.Add(child); }

        if (masks.Count > 1)
        {
            // Destroy all except the newest one (last in list)
            for (int i = 0; i < masks.Count - 1; i++) { Destroy(masks[i].gameObject); }
            
            // Center the new mask
            Transform winner = masks[masks.Count - 1];
            winner.localPosition = Vector3.zero;
        }
    }

    public void UpdateMaskStatus()
    {
        currentMask = MaskType.None;
        visionMultiplier = 1f;
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("Mask")) // Safe check if pending destroy
            {
                if (child.name.Contains("RedMask")) { currentMask = MaskType.Red; }
                else if (child.name.Contains("YellowMask")) { currentMask = MaskType.Yellow; visionMultiplier = 0.5f; }
                else if (child.name.Contains("GreenMask")) { currentMask = MaskType.Green; }
                return;
            }
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

    // --- SMART MOVEMENT ---

    // Standard Move: Avoids Walls AND Pits
    protected void MoveToSmart(Vector2 target)
    {
        MoveToSmart(target, obstacleLayer | pitLayer);
    }

    // Specific Move: Pass specific layers to avoid (e.g., only walls)
    protected void MoveToSmart(Vector2 target, LayerMask avoidanceLayers)
    {
        Vector2 desiredDir = (target - (Vector2)transform.position).normalized;
        Vector2 finalDir = desiredDir;

        // Check ahead
        RaycastHit2D hit = Physics2D.Raycast(transform.position, desiredDir, avoidRange, avoidanceLayers);

        if (hit.collider != null)
        {
            // Try alternate paths
            Vector2[] directionsToCheck = new Vector2[] {
                RotateVector(desiredDir, 45), RotateVector(desiredDir, -45), 
                RotateVector(desiredDir, 90), RotateVector(desiredDir, -90)  
            };

            foreach (Vector2 checkDir in directionsToCheck)
            {
                if (Physics2D.Raycast(transform.position, checkDir, avoidRange, avoidanceLayers).collider == null)
                {
                    finalDir = checkDir;
                    break;
                }
            }
        }
        ApplyVelocity(finalDir);
    }

    private void ApplyVelocity(Vector2 dir)
    {
        Vector2 targetVelocity = dir * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocityRef, slideInertia);
        if (spriteRenderer && rb.linearVelocity.sqrMagnitude > 0.1f) 
            spriteRenderer.flipX = rb.linearVelocity.x < 0;
    }

    protected void StopMoving()
    {
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, Vector2.zero, ref currentVelocityRef, slideInertia);
    }

    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float ca = Mathf.Cos(rad);
        float sa = Mathf.Sin(rad);
        return new Vector2(ca * v.x - sa * v.y, sa * v.x + ca * v.y);
    }

    // --- VISUALS ---

    void SetupLineRenderer()
    {
        if (!TryGetComponent<LineRenderer>(out lineRenderer)) lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f; lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.sortingOrder = -1;
        lineRenderer.startColor = skinColor; lineRenderer.endColor = skinColor;
        
        // IMPORTANT: Local space ensures cone follows enemy
        lineRenderer.useWorldSpace = false; 
    }

    void DrawVisionCone()
    {
        if (lineRenderer == null) return;
        int segments = 50;
        lineRenderer.positionCount = segments + 2;
        float currentFacingAngle = (spriteRenderer != null && spriteRenderer.flipX) ? 180f : 0f;
        float startAngle = currentFacingAngle - (fovAngle / 2f);
        float angleStep = fovAngle / segments;
        float actualRange = visionRange * visionMultiplier;

        // Start at offset (Eye level)
        lineRenderer.SetPosition(0, visionOffset);

        for (int i = 0; i <= segments; i++) {
            float angleRad = Mathf.Deg2Rad * (startAngle + (angleStep * i));
            float x = Mathf.Cos(angleRad) * actualRange;
            float y = Mathf.Sin(angleRad) * actualRange;
            
            // Apply offset to outer points too
            lineRenderer.SetPosition(i + 1, new Vector3(x, y, 0) + visionOffset);
        }
        lineRenderer.loop = true;
        if (fovAngle >= 360) lineRenderer.positionCount = segments;
    }

    protected virtual void OnAlertReceived(Vector3 p, Vector3 o, float r) { if (Vector2.Distance(transform.position, o) <= r) isAlerted = true; }
    protected virtual void OnDestroy() { EnemyAlertSystem.OnPlayerFound -= OnAlertReceived; }
}