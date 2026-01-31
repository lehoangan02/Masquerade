using UnityEngine;

public class Enemy_Standard : EnemyBase
{
    [Header("Patrol Settings")]
    public bool patrolVertical = false; 
    public float patrolRange = 3f;      
    public float patrolSpeed = 1.5f;    
    public float chaseSpeed = 4f;       
    public float giveUpTime = 2f; 
    
    private Vector2 startPos;
    private bool movingPositive = true;
    private float loseAggroTimer = 0f; 

    protected override void Start()
    {
        base.Start();
        startPos = transform.position;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        // -------------------------------------------------------------------
        // 1. RED MASK LOGIC (Reckless - IGNORES PITS)
        // -------------------------------------------------------------------
        if (currentMask == MaskType.Red)
        {
            moveSpeed = chaseSpeed * 1.5f; // Berserk Speed
            
            if (distanceToPlayer <= attackRange)
            {
                TryAttack(); 
            }
            else if (distanceToPlayer > stoppingDistance) 
            {
                // CRITICAL LINE:
                // We pass ONLY 'obstacleLayer'. 
                // Because 'pitLayer' is missing, the 360-Radar in EnemyBase is DISABLED.
                // The enemy will walk straight into the pit.
                MoveToSmart(player.position, obstacleLayer); 
            }
            else 
            {
                StopMoving();
            }
            return; // Return early so we don't run the Normal logic
        }

        // -------------------------------------------------------------------
        // 2. NORMAL LOGIC (Smart - AVOIDS PITS)
        // -------------------------------------------------------------------
        bool canSeePlayer = IsPlayerVisible(distanceToPlayer);

        if (canSeePlayer || isAlerted) 
        {
            loseAggroTimer = giveUpTime;
            if (isAlerted) isAlerted = false; 
        }
        else if (loseAggroTimer > 0) 
        {
            loseAggroTimer -= Time.deltaTime;
        }

        if (loseAggroTimer > 0)
        {
            moveSpeed = chaseSpeed;

            if (distanceToPlayer <= attackRange)
            {
                TryAttack();
            }
            else if (distanceToPlayer > stoppingDistance) 
            {
                // SAFE CHASE:
                // Default MoveToSmart uses (obstacleLayer | pitLayer).
                // The 360-Radar sees the 'pitLayer' flag and activates repulsion.
                MoveToSmart(player.position); 
            }
            else 
            {
                StopMoving();
            }
        }
        else
        {
            // PATROL
            moveSpeed = patrolSpeed;
            PatrolLogic();
        }
    }
    
    private void PatrolLogic()
    {
        Vector2 direction = patrolVertical ? Vector2.up : Vector2.right;
        float offset = movingPositive ? patrolRange : -patrolRange;
        Vector2 target = startPos + (direction * offset);
        
        // Patrol is always safe
        MoveToSmart(target); 
        
        if (Vector2.Distance(transform.position, target) < 0.1f) 
            movingPositive = !movingPositive;
    }
    
    protected override void OnAlertReceived(Vector3 p, Vector3 o, float r)
    {
        base.OnAlertReceived(p, o, r);
        if (isAlerted) loseAggroTimer = giveUpTime; 
    }
}