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
        // 1. RED MASK LOGIC (Reckless - DOES NOT AVOID PITS)
        // -------------------------------------------------------------------
        if (currentMask == MaskType.Red)
        {
            moveSpeed = chaseSpeed * 1.5f; // Faster speed
            
            if (distanceToPlayer <= attackRange)
            {
                TryAttack(); 
            }
            else if (distanceToPlayer > stoppingDistance) 
            {
                // UNSAFE CHASE:
                // We pass ONLY 'obstacleLayer'. The enemy is blind to the Pit Layer.
                // It will walk straight into the pit if it is between you and them.
                MoveToSmart(player.position, obstacleLayer); 
            }
            else 
            {
                StopMoving();
            }
            return; // Exit here so we don't do normal logic
        }

        // -------------------------------------------------------------------
        // 2. NORMAL LOGIC (Smart - AVOIDS PITS)
        // -------------------------------------------------------------------
        bool canSeePlayer = IsPlayerVisible(distanceToPlayer);

        // Handle Vision & Alert Memory
        if (canSeePlayer || isAlerted) 
        {
            loseAggroTimer = giveUpTime;
            if (isAlerted) isAlerted = false; 
        }
        else if (loseAggroTimer > 0) 
        {
            loseAggroTimer -= Time.deltaTime;
        }

        // Chase Logic
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
                // Calling this without arguments uses the default from EnemyBase.
                // It checks (obstacleLayer | pitLayer).
                // It sees the pit as an obstacle and finds a path AROUND it.
                MoveToSmart(player.position); 
            }
            else 
            {
                StopMoving();
            }
        }
        else
        {
            // PATROL (Always Safe)
            moveSpeed = patrolSpeed;
            PatrolLogic();
        }
    }
    
    private void PatrolLogic()
    {
        Vector2 direction = patrolVertical ? Vector2.up : Vector2.right;
        float offset = movingPositive ? patrolRange : -patrolRange;
        Vector2 target = startPos + (direction * offset);
        
        // Patrol uses Safe Pathfinding
        MoveToSmart(target); 
        
        // Switch direction when we reach the patrol point
        if (Vector2.Distance(transform.position, target) < 0.1f) 
            movingPositive = !movingPositive;
    }
    
    protected override void OnAlertReceived(Vector3 p, Vector3 o, float r)
    {
        base.OnAlertReceived(p, o, r);
        if (isAlerted) loseAggroTimer = giveUpTime; 
    }
}