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
    public int visionSortingOrder = 0;

    [Header("Combat")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public int attackDamage = 10;
    private float lastAttackTime = -999f;

    [Header("Vision Settings")]
    public Vector3 visionOffset = Vector3.zero;

    [Header("Movement Smoothing")]
    public float slideInertia = 0.1f;
    [Tooltip("How fast the enemy rotates its movement direction.")]
    public float steeringSpeed = 7f;

    [Header("Pathfinding & Obstacles")]
    [Tooltip("Select layers the enemy should slide against (e.g., Environment, Obstacles).")]
    public LayerMask obstacleLayer;
    
    [Tooltip("Add any tags here that should act as solid walls (e.g., Wall, Environment, Pillar).")]
    public List<string> obstacleTags = new List<string> { "Wall" };
    
    public string pitTag = "Pit";
    
    [Header("Detection Settings")]
    public float avoidRange = 1.5f;
    public float bodyWidth = 0.4f;

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

    // Internal Physics State
    private bool debugPitDetected = false;
    private Vector2 currentVelocityRef;
    private Vector2 currentSteeringDir;

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

        if (spriteRenderer) spriteRenderer.color = skinColor;
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

    protected virtual void LateUpdate()
    {
        // if (showVisionCircle && lineRenderer != null) DrawVisionCone();
    }

    // --- SMART MOVEMENT ---
    protected void MoveToSmart(Vector2 target, bool avoidPits = true)
    {
        if (isDead) return;

        Vector2 desiredDir = (target - (Vector2)transform.position).normalized;
        Vector2 avoidanceForce = Vector2.zero;
        debugPitDetected = false;

        // 1. PIT AVOIDANCE
        if (avoidPits)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, avoidRange);
            foreach (var col in nearby)
            {
                if (col.CompareTag(pitTag))
                {
                    debugPitDetected = true;
                    Vector2 dirAway = (Vector2)transform.position - col.ClosestPoint(transform.position);
                    float intensity = 1f - Mathf.Clamp01(dirAway.magnitude / avoidRange);
                    avoidanceForce += dirAway.normalized * intensity;
                }
            }
        }

        // 2. STEERING BLEND
        Vector2 targetDir = (desiredDir + (avoidanceForce * 2.5f)).normalized;
        currentSteeringDir = Vector2.Lerp(currentSteeringDir, targetDir, Time.deltaTime * steeringSpeed);
        Vector2 finalDir = currentSteeringDir.normalized;

        // 3. OPTIONAL OBSTACLE SLIDING (Layer OR Tag)
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, bodyWidth, finalDir, 0.5f, obstacleLayer);
        
        // If the layer didn't hit, check the optional tags list
        if (hit.collider == null)
        {
            hit = GetObstacleTagHit(finalDir);
        }

        if (hit.collider != null)
        {
            Vector2 slideDir = Vector2.Perpendicular(hit.normal).normalized;
            finalDir = Vector2.Dot(slideDir, finalDir) > Vector2.Dot(-slideDir, finalDir) ? slideDir : -slideDir;
        }

        ApplyVelocity(finalDir);
    }

    private RaycastHit2D GetObstacleTagHit(Vector2 dir)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, bodyWidth, dir, 0.5f);
        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            foreach (string t in obstacleTags)
            {
                if (h.collider.CompareTag(t)) return h;
            }
        }
        return new RaycastHit2D();
    }

    private void ApplyVelocity(Vector2 dir)
    {
        Vector2 targetVelocity = dir * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocityRef, slideInertia);
    }

    protected void StopMoving()
    {
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, Vector2.zero, ref currentVelocityRef, slideInertia);
    }

    // --- ANIMATION & COMBAT ---
    protected void ChangeAnimationState(AnimState newState)
    {
        if (currentState == newState || animator == null) return;
        currentState = newState;
        string trigger = newState switch {
            AnimState.Idle => "DoIdle",
            AnimState.Walk => "DoWalk",
            AnimState.Run => "DoRun",
            AnimState.Attack => "Attack",
            AnimState.Dead => "Die",
            _ => "DoIdle"
        };
        animator.SetTrigger(trigger);
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
        }
    }

    private IEnumerator ResetAttackState()
    {
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        ChangeAnimationState(AnimState.Idle);
    }

    protected void UpdateAnimation()
    {
        if (animator == null || isDead || isAttacking) return;
        Vector2 vel = rb.linearVelocity;
        if (vel.magnitude > 0.01f)
        {
            animator.SetFloat("Horizontal", vel.normalized.x);
            animator.SetFloat("Vertical", vel.normalized.y);
        }
        AnimState target = (vel.magnitude < 0.1f) ? AnimState.Idle : (isAlerted ? AnimState.Run : AnimState.Walk);
        if (target != currentState) ChangeAnimationState(target);
    }

    // --- TRIGGERS & DEATH ---
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDead && collision.CompareTag(pitTag)) Die();
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        StopMoving();
        rb.simulated = false;
        DropMasks();
        if (animator != null)
        {
            ChangeAnimationState(AnimState.Dead);
            StartCoroutine(WaitAndDestroy(1.0f));
        }
        else Destroy(gameObject);
    }

    private IEnumerator WaitAndDestroy(float delay) { yield return new WaitForSeconds(delay); Destroy(gameObject); }

    private void DropMasks()
    {
        foreach (Transform child in transform) {
            if (child.name.Contains("Mask")) {
                child.SetParent(null);
                child.rotation = Quaternion.identity;
            }
        }
    }

    // --- MASK MANAGEMENT (UPDATED) ---

    /// <summary>
    /// Helper to cleanly attach a new mask. Removes old ones, instantiates new one.
    /// Call this from your projectile or interaction script.
    /// </summary>
    public void EquipNewMask(GameObject maskPrefab)
    {
        if (maskPrefab == null) return;

        // Create the new mask
        GameObject newMask = Instantiate(maskPrefab, transform.position, Quaternion.identity, transform);
        
        // Naming it helps the UpdateMaskStatus logic run cleanly without "(Clone)" strings
        newMask.name = maskPrefab.name;

        // Force an update to clean up old masks immediately
        UpdateMaskStatus();
    }

    public void UpdateMaskStatus()
    {
        currentMask = MaskType.None;
        visionMultiplier = 1f;

        bool foundNewestMask = false;

        // Iterate BACKWARDS through children.
        // Unity adds new children to the END of the list.
        // So the last child is the newest one.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child.name.Contains("Mask"))
            {
                if (!foundNewestMask)
                {
                    // This is the last (newest) mask we found. Keep it.
                    foundNewestMask = true;

                    if (child.name.Contains("RedMask")) currentMask = MaskType.Red;
                    else if (child.name.Contains("YellowMask")) { currentMask = MaskType.Yellow; visionMultiplier = 0.5f; }
                    else if (child.name.Contains("GreenMask")) currentMask = MaskType.Green;
                }
                else
                {
                    // We already found a newer mask, so this child is old. Destroy it.
                    Destroy(child.gameObject);
                }
            }
        }
    }

    // --- VISION ---
    public bool CanSeePlayer() => player != null && IsPlayerVisible(Vector2.Distance(transform.position, player.position));

    protected bool IsPlayerVisible(float dist)
    {
        float actualVision = visionRange * visionMultiplier;
        if (dist > actualVision) return false;
        if (isAlerted) return true;

        Vector2 facingDir = (animator != null) ? (Vector2)(Quaternion.Euler(0, 0, GetFacingAngleFromAnimator()) * Vector2.right) : Vector2.right;
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        bool sees = Vector2.Angle(facingDir, dirToPlayer) < (fovAngle / 2f);
        if (sees) isAlerted = true;
        return sees;
    }

    protected abstract void PerformBehavior(float distanceToPlayer);

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
        int s = 50;
        lineRenderer.positionCount = s + 3;
        float ang = (animator ? GetFacingAngleFromAnimator() : 0f) - fovAngle / 2f;
        float step = fovAngle / s;
        float rng = visionRange * visionMultiplier;
        lineRenderer.SetPosition(0, visionOffset);
        for (int i = 0; i <= s; i++) {
            float r = Mathf.Deg2Rad * (ang + step * i);
            lineRenderer.SetPosition(i + 1, new Vector3(Mathf.Cos(r) * rng, Mathf.Sin(r) * rng) + visionOffset);
        }
        lineRenderer.SetPosition(s + 2, visionOffset);
    }

    float GetFacingAngleFromAnimator()
    {
        if (!animator) return 0f;
        float x = animator.GetFloat("Horizontal"), y = animator.GetFloat("Vertical");
        return (Mathf.Abs(x) < 0.1f && Mathf.Abs(y) < 0.1f) ? 270f : Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }

    protected virtual void OnAlertReceived(Vector3 p, Vector3 o, float r) { if (Vector2.Distance(transform.position, o) <= r) isAlerted = true; }
    protected virtual void OnDestroy() { EnemyAlertSystem.OnPlayerFound -= OnAlertReceived; }
    private void OnDrawGizmos() { Gizmos.color = debugPitDetected ? Color.red : Color.green; Gizmos.DrawWireSphere(transform.position, avoidRange); }
}