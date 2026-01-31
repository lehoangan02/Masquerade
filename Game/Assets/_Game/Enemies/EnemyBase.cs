using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MaskType { None, Red, Yellow, Green }
public enum AnimState { Idle, Walk, Run, Attack, Dead }

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

    [Header("Visual Settings")]
    [Tooltip("Set to 0 to be behind Enemy.")]
    public int visionSortingOrder = 0; 

    [Header("Combat")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public int attackDamage = 10;
    private float lastAttackTime = -999f;

    [Header("Vision Settings")]
    public Vector3 visionOffset = Vector3.zero;

    [Header("Movement Feel")]
    public float slideInertia = 0.1f; 

    [Header("Pathfinding & Pits")]
    public LayerMask obstacleLayer; 
    public LayerMask pitLayer;      
    
    [Header("Detection Settings")]
    [Tooltip("The Radius of the 360 Safety Circle around the enemy.")]
    public float avoidRange = 1.5f; 

    [Tooltip("Radius of the enemy body (used for wall sliding).")]
    public float bodyWidth = 0.5f; 

    // References
    protected MaskType currentMask = MaskType.None;
    protected float visionMultiplier = 1f;
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected LineRenderer lineRenderer;
    protected Animator animator; 
    
    // State Flags
    protected bool isAlerted = false;
    protected bool isDead = false;
    protected bool isAttacking = false; 
    protected AnimState currentState = AnimState.Idle; 

    // Debugging
    private bool debugPitDetected = false;
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
        if (player == null || isDead || isAttacking) return; 
        PerformBehavior(Vector2.Distance(transform.position, player.position));
    }

    void LateUpdate() { if (showVisionCircle && lineRenderer != null) DrawVisionCone(); }

    protected void ChangeAnimationState(AnimState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        switch (newState)
        {
            case AnimState.Idle:   animator.SetTrigger("DoIdle"); break;
            case AnimState.Walk:   animator.SetTrigger("DoWalk"); break;
            case AnimState.Run:    animator.SetTrigger("DoRun"); break;
            case AnimState.Attack: animator.SetTrigger("Attack"); break;
            case AnimState.Dead:   animator.SetTrigger("Die"); break;
        }
    }

    protected void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            StopMoving();
            isAttacking = true;
            ChangeAnimationState(AnimState.Attack);
            StartCoroutine(ResetAttackState()); 
            Debug.Log($"<color=red>{gameObject.name} attacked Player for {attackDamage} damage!</color>");
        }
    }

    private IEnumerator ResetAttackState()
    {
        yield return new WaitForSeconds(0.5f); 
        isAttacking = false;
        currentState = AnimState.Idle; 
        animator.SetTrigger("DoIdle"); 
    }

    protected void UpdateAnimation()
    {
        if (animator == null || isDead || isAttacking) return;
        Vector2 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        if (speed > 0.01f)
        {
            velocity.Normalize(); 
            animator.SetFloat("Horizontal", velocity.x);
            animator.SetFloat("Vertical", velocity.y);
        }
        AnimState targetState = currentState;
        if (speed < 0.1f) targetState = AnimState.Idle;
        else 
        {
            if (isAlerted) targetState = AnimState.Run;
            else targetState = AnimState.Walk;
        }
        if (targetState != currentState) ChangeAnimationState(targetState);
    }

    // --- DEATH & PITS ---
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
        if (collision.CompareTag("Pit")) 
        {
            Debug.Log($"<color=red><b>[DEATH]</b> {gameObject.name} fell into PIT: {collision.name}</color>");
            Die();
        }
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
            ChangeAnimationState(AnimState.Dead);
            StartCoroutine(WaitAndDestroy(1.0f)); 
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

    public bool CanSeePlayer()
    {
        if (player == null) return false;
        return IsPlayerVisible(Vector2.Distance(transform.position, player.position));
    }

    protected bool IsPlayerVisible(float dist)
    {
        float actualVision = visionRange * visionMultiplier;
        if (dist > actualVision) return false;
        if (isAlerted) return true;
        if (fovAngle >= 360f) return true;
        
        Vector2 facingDir = (animator != null) ? (Vector2)(Quaternion.Euler(0,0,GetFacingAngleFromAnimator()) * Vector2.right) : (spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right);
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        bool seesPlayer = Vector2.Angle(facingDir, dirToPlayer) < (fovAngle / 2f);
        if (seesPlayer) isAlerted = true; 
        return seesPlayer;
    }

    protected abstract void PerformBehavior(float distanceToPlayer);

    // -----------------------------------------------------------------------------------
    // NEW: 360 PROXIMITY DETECTION
    // -----------------------------------------------------------------------------------
    protected void MoveToSmart(Vector2 target, LayerMask avoidanceLayers)
    {
        if (isDead) return;

        Vector2 desiredDir = (target - (Vector2)transform.position).normalized;
        Vector2 finalDir = desiredDir;

        debugPitDetected = false;

        // 1. PIT DETECTION (360 Degree Circle around Enemy)
        // We check for pits specifically if they are in the avoidance layers
        if ((avoidanceLayers.value & pitLayer.value) != 0)
        {
            // This checks a circle CENTERED on the enemy
            Collider2D pitHit = Physics2D.OverlapCircle(transform.position, avoidRange, pitLayer);

            if (pitHit != null)
            {
                debugPitDetected = true;
                Debug.Log($"<color=cyan><b>[PIT NEARBY]</b></color> Pushing away from {pitHit.name}");

                // Find the closest point on the pit to the enemy
                Vector2 closestPoint = pitHit.ClosestPoint(transform.position);
                
                // Calculate Repulsion Vector (Move AWAY from pit)
                Vector2 repulsionDir = ((Vector2)transform.position - closestPoint).normalized;

                // If we are inside the pit, this vector might be zero, so we default to -desiredDir
                if (repulsionDir == Vector2.zero) repulsionDir = -desiredDir;

                // Blend the desired direction with strong repulsion
                // The closer we are to the pit, the stronger we push away
                finalDir = (desiredDir + (repulsionDir * 2.5f)).normalized; 
            }
        }

        // 2. WALL DETECTION (Directional Ray/Circle Cast)
        // We still need to look forward for Walls so we don't walk through them
        if ((avoidanceLayers.value & obstacleLayer.value) != 0)
        {
            RaycastHit2D wallHit = Physics2D.CircleCast(transform.position, bodyWidth, finalDir, 1.0f, obstacleLayer);
            if (wallHit.collider != null)
            {
                // Slide along the wall
                Vector2 hitNormal = wallHit.normal;
                Vector2 slideDir = Vector2.Perpendicular(hitNormal).normalized;
                Vector2 left = slideDir;
                Vector2 right = -slideDir;
                finalDir = Vector2.Dot(left, finalDir) > Vector2.Dot(right, finalDir) ? left : right;
            }
        }

        ApplyVelocity(finalDir);
    }

    protected void MoveToSmart(Vector2 target) { MoveToSmart(target, obstacleLayer | pitLayer); }

    private void ApplyVelocity(Vector2 dir) {
        Vector2 targetVelocity = dir * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocityRef, slideInertia);
    }

    protected void StopMoving() { rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, Vector2.zero, ref currentVelocityRef, slideInertia); }
    
    // --- VISUALS ---
    void SetupLineRenderer() 
    { 
        if (!TryGetComponent(out lineRenderer)) lineRenderer = gameObject.AddComponent<LineRenderer>(); 
        
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); 
        lineRenderer.startColor = lineRenderer.endColor = skinColor; 
        lineRenderer.startWidth = lineRenderer.endWidth = 0.05f; 
        lineRenderer.useWorldSpace = false; 
        lineRenderer.sortingOrder = visionSortingOrder; 
    }

    void DrawVisionCone() 
    { 
        if (!lineRenderer) return; 
        lineRenderer.sortingOrder = visionSortingOrder;

        int s = 50; 
        lineRenderer.positionCount = s + 3; 

        float ang = (animator ? GetFacingAngleFromAnimator() : 0f) - fovAngle/2f;
        float step = fovAngle/s;
        float rng = visionRange * visionMultiplier; 

        lineRenderer.SetPosition(0, visionOffset); 
        for(int i=0; i<=s; i++) 
        { 
            float r = Mathf.Deg2Rad * (ang + step * i); 
            Vector3 pointOnCircle = new Vector3(Mathf.Cos(r)*rng, Mathf.Sin(r)*rng) + visionOffset;
            lineRenderer.SetPosition(i+1, pointOnCircle); 
        } 
        lineRenderer.SetPosition(s+2, visionOffset);
    }

    float GetFacingAngleFromAnimator() { float x = animator.GetFloat("Horizontal"), y = animator.GetFloat("Vertical"); return (Mathf.Abs(x)<0.1f && Mathf.Abs(y)<0.1f) ? 270f : Mathf.Atan2(y, x) * Mathf.Rad2Deg; }
    protected virtual void OnAlertReceived(Vector3 p, Vector3 o, float r) { if (Vector2.Distance(transform.position, o) <= r) isAlerted = true; }
    protected virtual void OnDestroy() { EnemyAlertSystem.OnPlayerFound -= OnAlertReceived; }
    
    // --- NEW: 360 DEBUG GIZMO (CENTERED ON ENEMY) ---
    private void OnDrawGizmos()
    {
        // Draw the Safety Circle around the enemy
        // Green = Safe
        // Red = Pit Nearby (Danger)
        if (Application.isPlaying)
        {
            Gizmos.color = debugPitDetected ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, avoidRange);
        }
        else
        {
            // Editor Preview (Cyan to distinguish from vision)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, avoidRange);
        }
    }
}