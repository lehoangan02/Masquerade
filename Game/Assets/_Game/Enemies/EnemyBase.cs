using UnityEngine;
using System.Collections;
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

    [Header("Combat")] // <--- NEW SECTION
    public float attackRange = 1.2f;   // Distance required to attack
    public float attackCooldown = 1.0f; // Time between attacks
    public int attackDamage = 10;
    private float lastAttackTime = -999f; // Allow immediate first attack

    [Header("Vision Settings")]
    public Vector3 visionOffset = new Vector3(0, 0.5f, 0); 

    [Header("Movement Feel")]
    public float slideInertia = 0.1f; 

    [Header("Pathfinding & Pits")]
    public LayerMask obstacleLayer; 
    public LayerMask pitLayer;      
    public float avoidRange = 1.5f; 

    // References
    protected MaskType currentMask = MaskType.None;
    protected float visionMultiplier = 1f;
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected LineRenderer lineRenderer;
    protected Animator animator; 
    protected bool isAlerted = false;
    protected bool isDead = false; 

    private Vector2 currentVelocityRef;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.linearDamping = 1f; 

        if(spriteRenderer) spriteRenderer.color = skinColor;
        EnemyAlertSystem.OnPlayerFound += OnAlertReceived;
        
        if (showVisionCircle) SetupLineRenderer();
    }

    protected virtual void Update() 
    { 
        if (isDead) return;
        UpdateMaskStatus();
        UpdateAnimation(); 
    }

    protected virtual void FixedUpdate()
    {
        if (player == null || isDead) return;
        PerformBehavior(Vector2.Distance(transform.position, player.position));
    }

    void LateUpdate() { if (showVisionCircle && lineRenderer != null) DrawVisionCone(); }

    // --- COMBAT LOGIC (NEW) ---
    
    protected void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            
            // 1. Stop moving so we don't slide while attacking
            StopMoving();
            
            // 2. Play Animation
            if (animator) animator.SetTrigger("Attack");
            
            // 3. Deal Damage
            // (You can replace this with your real PlayerHealth script later)
            Debug.Log($"<color=red>{gameObject.name} attacked Player for {attackDamage} damage!</color>");
            
            // Example:
            // player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
        }
    }

    // --- ANIMATION LOGIC ---
    protected void UpdateAnimation()
    {
        if (animator == null) return;

        Vector2 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        animator.SetFloat("Speed", speed);

        if (speed > 0.01f)
        {
            velocity.Normalize(); 
            animator.SetFloat("Horizontal", velocity.x);
            animator.SetFloat("Vertical", velocity.y);
        }
    }

    // --- DEATH & PITS ---
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        if (collision.CompareTag("Pit")) Die();
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        StopMoving();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; 

        DropMasks();

        if (animator != null)
        {
            animator.SetTrigger("Die");
            StartCoroutine(WaitAndDestroy(0.5f)); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator WaitAndDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void DropMasks()
    {
        List<Transform> masksToDrop = new List<Transform>();
        foreach (Transform child in transform) { if (child.name.Contains("Mask")) masksToDrop.Add(child); }
        foreach (Transform mask in masksToDrop) { mask.SetParent(null); mask.rotation = Quaternion.identity; }
    }

    // --- MOVEMENT & HELPERS ---
    private void OnTransformChildrenChanged()
    {
        if (isDead) return;
        List<Transform> masks = new List<Transform>();
        foreach (Transform child in transform) { if (child.name.Contains("Mask")) masks.Add(child); }
        if (masks.Count > 1) { for (int i = 0; i < masks.Count - 1; i++) Destroy(masks[i].gameObject); masks[masks.Count - 1].localPosition = Vector3.zero; }
    }

    public void UpdateMaskStatus()
    {
        currentMask = MaskType.None;
        visionMultiplier = 1f;
        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("Mask")) {
                if (child.name.Contains("RedMask")) currentMask = MaskType.Red;
                else if (child.name.Contains("YellowMask")) { currentMask = MaskType.Yellow; visionMultiplier = 0.5f; }
                else if (child.name.Contains("GreenMask")) currentMask = MaskType.Green;
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
        Vector2 facingDir = (animator != null) ? (Vector2)(Quaternion.Euler(0,0,GetFacingAngleFromAnimator()) * Vector2.right) : (spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right);
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        return Vector2.Angle(facingDir, dirToPlayer) < (fovAngle / 2f);
    }

    protected abstract void PerformBehavior(float distanceToPlayer);

    protected void MoveToSmart(Vector2 target, LayerMask avoidanceLayers)
    {
        if (isDead) return;
        Vector2 desiredDir = (target - (Vector2)transform.position).normalized;
        Vector2 finalDir = desiredDir;
        if (Physics2D.Raycast(transform.position, desiredDir, avoidRange, avoidanceLayers).collider != null) {
            Vector2[] directionsToCheck = new Vector2[] { RotateVector(desiredDir, 45), RotateVector(desiredDir, -45), RotateVector(desiredDir, 90), RotateVector(desiredDir, -90) };
            foreach (Vector2 checkDir in directionsToCheck) { if (Physics2D.Raycast(transform.position, checkDir, avoidRange, avoidanceLayers).collider == null) { finalDir = checkDir; break; } }
        }
        ApplyVelocity(finalDir);
    }

    protected void MoveToSmart(Vector2 target) { MoveToSmart(target, obstacleLayer | pitLayer); }

    private void ApplyVelocity(Vector2 dir) {
        Vector2 targetVelocity = dir * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocityRef, slideInertia);
    }

    protected void StopMoving() { rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, Vector2.zero, ref currentVelocityRef, slideInertia); }
    Vector2 RotateVector(Vector2 v, float degrees) { float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y); }
    
    // VISUALS
    void SetupLineRenderer() { if (!TryGetComponent(out lineRenderer)) lineRenderer = gameObject.AddComponent<LineRenderer>(); lineRenderer.material = new Material(Shader.Find("Sprites/Default")); lineRenderer.startColor = lineRenderer.endColor = skinColor; lineRenderer.startWidth = lineRenderer.endWidth = 0.05f; lineRenderer.useWorldSpace = false; lineRenderer.sortingOrder = -1; }
    void DrawVisionCone() { if (!lineRenderer) return; int s = 50; lineRenderer.positionCount = s + 2; float ang = (animator ? GetFacingAngleFromAnimator() : 0f) - fovAngle/2f, step = fovAngle/s, rng = visionRange * visionMultiplier; lineRenderer.SetPosition(0, visionOffset); for(int i=0; i<=s; i++) { float r = Mathf.Deg2Rad*(ang+step*i); lineRenderer.SetPosition(i+1, new Vector3(Mathf.Cos(r)*rng, Mathf.Sin(r)*rng) + visionOffset); } }
    float GetFacingAngleFromAnimator() { float x = animator.GetFloat("Horizontal"), y = animator.GetFloat("Vertical"); return (Mathf.Abs(x)<0.1f && Mathf.Abs(y)<0.1f) ? 270f : Mathf.Atan2(y, x) * Mathf.Rad2Deg; }
    protected virtual void OnAlertReceived(Vector3 p, Vector3 o, float r) { if (Vector2.Distance(transform.position, o) <= r) isAlerted = true; }
    protected virtual void OnDestroy() { EnemyAlertSystem.OnPlayerFound -= OnAlertReceived; }
}