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
        // 1. RED MASK LOGIC (Aggressive + Dumb to Pits)
        if (currentMask == MaskType.Red)
        {
            moveSpeed = chaseSpeed * 1.5f; 
            
            // SMART CHASE (BERSERK):
            // We pass 'obstacleLayer' only. This means it IGNORES the 'pitLayer'.
            // Result: It chases fast but will fall into pits.
            if (distanceToPlayer > stoppingDistance) 
            {
                MoveToSmart(player.position, obstacleLayer);
            }
            else 
            {
                StopMoving();
            }
            
            return;
        }

        // 2. NORMAL LOGIC
        bool canSeePlayer = IsPlayerVisible(distanceToPlayer);

        if (canSeePlayer || isAlerted) 
        {
            loseAggroTimer = giveUpTime;
            if (isAlerted) isAlerted = false; 
        }
        else if (loseAggroTimer > 0) loseAggroTimer -= Time.deltaTime;

        if (loseAggroTimer > 0)
        {
            // CHASE STATE (Smart & Safe)
            moveSpeed = chaseSpeed;
            
            // Standard MoveToSmart automatically avoids Walls AND Pits
            if (distanceToPlayer > stoppingDistance) MoveToSmart(player.position); 
            else StopMoving();
        }
        else
        {
            // PATROL STATE
            moveSpeed = patrolSpeed;
            PatrolLogic();
        }
    }
    
    private void PatrolLogic()
    {
        Vector2 direction = patrolVertical ? Vector2.up : Vector2.right;
        float offset = movingPositive ? patrolRange : -patrolRange;
        Vector2 target = startPos + (direction * offset);
        
        // --- FIX IS HERE ---
        // Changed MoveTo -> MoveToSmart
        // This ensures they don't walk into walls/pits while patrolling
        MoveToSmart(target); 
        
        if (Vector2.Distance(transform.position, target) < 0.1f) movingPositive = !movingPositive;
    }
    
    protected override void OnAlertReceived(Vector3 p, Vector3 o, float r)
    {
        base.OnAlertReceived(p, o, r);
        if (isAlerted) loseAggroTimer = giveUpTime; 
    }
}