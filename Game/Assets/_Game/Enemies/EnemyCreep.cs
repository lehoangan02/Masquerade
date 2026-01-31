using UnityEngine;

public class Enemy_Standard : EnemyBase
{
    [Header("Patrol Settings")]
    public bool patrolVertical = false; 
    public float patrolRange = 3f;      
    public float playerSpeedReference = 5f; // Reference player speed
    [Range(0f, 1f)] public float patrolSpeedMultiplier = 0.8f; // 80% of player speed per document
    public float chaseSpeed = 4f;       
    public float giveUpTime = 2f; 

    [Header("Pack Instinct")]
    public float callToArmsRadius = 8f;
    public float packSpeedBoost = 1.3f;
    
    [Header("Wall Hug Settings (Yellow Mask)")]
    public LayerMask wallLayer;
    public float wallCheckDistance = 1f;
    
    private Vector2 startPos;
    private bool movingPositive = true;
    private float loseAggroTimer = 0f;
    private bool isInCombat = false;

    protected override void Start()
    {
        base.Start();
        startPos = transform.position;
        
        // Set vision to ~150° cone as per design document
        fovAngle = 150f;
        
        // Set patrol speed to 80% of player speed
        moveSpeed = playerSpeedReference * patrolSpeedMultiplier;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        bool wasInCombat = isInCombat;

        switch (currentMask)
        {
            case MaskType.Red:
                BerserkBehavior(distanceToPlayer);
                break;
            case MaskType.Yellow:
                WallHuggerBehavior(distanceToPlayer);
                break;
            case MaskType.Green:
                SharedSightBehavior(distanceToPlayer);
                break;
            default:
                NormalBehavior(distanceToPlayer);
                break;
        }

        // Pack Instinct: Trigger Call to Arms when entering combat
        if (isInCombat && !wasInCombat)
        {
            TriggerCallToArms();
        }
    }

    #region Normal Behavior
    private void NormalBehavior(float distanceToPlayer)
    {
        bool canSeePlayer = IsPlayerVisible(distanceToPlayer);

        if (canSeePlayer || isAlerted)
        {
            loseAggroTimer = giveUpTime;
            isInCombat = true;
            if (isAlerted) isAlerted = false;
        }
        else if (loseAggroTimer > 0)
        {
            loseAggroTimer -= Time.deltaTime;
            if (loseAggroTimer <= 0) isInCombat = false;
        }

        if (loseAggroTimer > 0)
        {
            // Combat State
            moveSpeed = chaseSpeed;

            if (distanceToPlayer <= attackRange)
            {
                TryAttack();
            }
            else if (distanceToPlayer > stoppingDistance)
            {
                MoveToSmart(player.position);
            }
            else StopMoving();
        }
        else
        {
            // Patrol State - 80% player speed
            moveSpeed = playerSpeedReference * patrolSpeedMultiplier;
            PatrolLogic();
        }
    }

    private void PatrolLogic()
    {
        Vector2 direction = patrolVertical ? Vector2.up : Vector2.right;
        float offset = movingPositive ? patrolRange : -patrolRange;
        Vector2 target = startPos + (direction * offset);

        MoveToSmart(target);

        if (Vector2.Distance(transform.position, target) < 0.1f) movingPositive = !movingPositive;
    }
    #endregion

    #region RED MASK - Berserk (Attacks nearest entity: Enemy OR Player)
    private void BerserkBehavior(float distanceToPlayer)
    {
        isInCombat = true;
        moveSpeed = chaseSpeed * 1.5f;

        // Find nearest entity (player OR other enemies)
        Transform nearestTarget = FindNearestEntity();

        if (nearestTarget == null)
        {
            StopMoving();
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, nearestTarget.position);

        if (distanceToTarget <= attackRange)
        {
            if (nearestTarget == player)
            {
                TryAttack();
            }
            else
            {
                // Attack other enemy
                EnemyBase otherEnemy = nearestTarget.GetComponent<EnemyBase>();
                if (otherEnemy != null && Time.time >= attackTimer)
                {
                    attackTimer = Time.time + attackCooldown;
                    otherEnemy.TakeDamage(attackDamage);
                    StopMoving();
                }
            }
        }
        else
        {
            // Chase nearest entity, ignore pits when berserk
            MoveToSmart(nearestTarget.position, obstacleLayer);
        }
    }

    private Transform FindNearestEntity()
    {
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        // Check player
        if (player != null)
        {
            float playerDist = Vector2.Distance(transform.position, player.position);
            if (playerDist < nearestDistance)
            {
                nearestDistance = playerDist;
                nearest = player;
            }
        }

        // Check other enemies
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in allEnemies)
        {
            if (enemy == this || enemy.IsDead) continue;

            float enemyDist = Vector2.Distance(transform.position, enemy.transform.position);
            if (enemyDist < nearestDistance)
            {
                nearestDistance = enemyDist;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }
    #endregion

    #region YELLOW MASK - Wall Hugger (Follows left wall, clears main path)
    private void WallHuggerBehavior(float distanceToPlayer)
    {
        bool canSeePlayer = IsPlayerVisible(distanceToPlayer);

        if (canSeePlayer || isAlerted)
        {
            loseAggroTimer = giveUpTime;
            isInCombat = true;
            if (isAlerted) isAlerted = false;
        }
        else if (loseAggroTimer > 0)
        {
            loseAggroTimer -= Time.deltaTime;
            if (loseAggroTimer <= 0) isInCombat = false;
        }

        if (loseAggroTimer > 0)
        {
            // Combat - still chase but prefer walls
            moveSpeed = chaseSpeed;
            MoveToSmart(player.position);
        }
        else
        {
            // Wall Hugging Patrol - follow left wall to clear main path
            moveSpeed = playerSpeedReference * patrolSpeedMultiplier;
            FollowLeftWall();
        }
    }

    private void FollowLeftWall()
    {
        Vector2 currentDir = rb.linearVelocity.normalized;
        if (currentDir.sqrMagnitude < 0.01f)
        {
            currentDir = patrolVertical ? Vector2.up : Vector2.right;
        }

        // Check left side for wall
        Vector2 leftDir = new Vector2(-currentDir.y, currentDir.x);
        RaycastHit2D leftHit = Physics2D.Raycast(transform.position, leftDir, wallCheckDistance, wallLayer);

        Vector2 moveDir;

        if (leftHit.collider != null)
        {
            // Wall on left, move forward along it
            Vector2 wallNormal = leftHit.normal;
            moveDir = new Vector2(-wallNormal.y, wallNormal.x);

            // Check for obstacle ahead
            RaycastHit2D forwardHit = Physics2D.Raycast(transform.position, moveDir, wallCheckDistance, wallLayer | obstacleLayer);
            if (forwardHit.collider != null)
            {
                // Corner - turn right
                moveDir = -leftDir;
            }
        }
        else
        {
            // No wall on left, turn left to find one
            moveDir = leftDir;
        }

        // Apply movement
        rb.linearVelocity = moveDir.normalized * moveSpeed;
        if (spriteRenderer && moveDir.x != 0)
            spriteRenderer.flipX = moveDir.x < 0;
    }
    #endregion

    #region GREEN MASK - Shared Sight (Reveals Fog of War in vision cone)
    private void SharedSightBehavior(float distanceToPlayer)
    {
        // Continue normal patrol but reveal fog of war
        NormalBehavior(distanceToPlayer);

        // Reveal fog of war in vision cone for player
        RevealFogOfWar();
    }

    private void RevealFogOfWar()
    {
        // TODO: Integrate with Fog of War system when implemented
        // This should lift fog in the enemy's vision cone for the player
        // Uncomment when FogOfWar system exists:
        // FogOfWar fog = FindObjectOfType<FogOfWar>();
        // if (fog != null)
        // {
        //     Vector2 facingDir = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
        //     fog.RevealArea(transform.position, visionRange * visionMultiplier, fovAngle, facingDir);
        // }
    }
    #endregion

    #region Pack Instinct - Call to Arms
    private void TriggerCallToArms()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, callToArmsRadius);

        foreach (Collider2D col in nearbyColliders)
        {
            Enemy_Standard otherPatroller = col.GetComponent<Enemy_Standard>();
            if (otherPatroller != null && otherPatroller != this)
            {
                otherPatroller.ReceiveCallToArms(transform.position);
            }
        }
    }

    public void ReceiveCallToArms(Vector3 alertPosition)
    {
        if (!isInCombat && !isDead)
        {
            isAlerted = true;
            isInCombat = true;
            loseAggroTimer = giveUpTime;
            moveSpeed *= packSpeedBoost;
            Debug.Log($"<color=orange>{gameObject.name} received Call to Arms! Speed boosted.</color>");
        }
    }
    #endregion

    protected override void OnAlertReceived(Vector3 p, Vector3 o, float r)
    {
        base.OnAlertReceived(p, o, r);
        if (isAlerted) loseAggroTimer = giveUpTime;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw patrol range
        Gizmos.color = Color.cyan;
        Vector2 pos = Application.isPlaying ? startPos : (Vector2)transform.position;
        Vector2 dir = patrolVertical ? Vector2.up : Vector2.right;
        Gizmos.DrawLine(pos - dir * patrolRange, pos + dir * patrolRange);
        Gizmos.DrawWireSphere(pos - dir * patrolRange, 0.2f);
        Gizmos.DrawWireSphere(pos + dir * patrolRange, 0.2f);

        // Draw Call to Arms radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, callToArmsRadius);
    }
}