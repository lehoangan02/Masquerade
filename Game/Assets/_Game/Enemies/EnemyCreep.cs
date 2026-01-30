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
        // 1. RED MASK LOGIC (Aggressive + Smart)
        if (currentMask == MaskType.Red)
        {
            moveSpeed = chaseSpeed * 1.5f; 
            // SMART CHASE: Use MoveToSmart instead of MoveTo
            if (distanceToPlayer > stoppingDistance) MoveToSmart(player.position);
            else StopMoving();
            
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
            // CHASE STATE (Smart)
            moveSpeed = chaseSpeed;
            if (distanceToPlayer > stoppingDistance) MoveToSmart(player.position); // <-- UPDATED
            else StopMoving();
        }
        else
        {
            // PATROL STATE (Dumb/Linear is fine for patrol)
            moveSpeed = patrolSpeed;
            PatrolLogic();
        }
    }
    
    private void PatrolLogic()
    {
        // Patrol can stay dumb (Linear) because we place them in clear areas usually
        Vector2 direction = patrolVertical ? Vector2.up : Vector2.right;
        float offset = movingPositive ? patrolRange : -patrolRange;
        Vector2 target = startPos + (direction * offset);
        
        MoveTo(target); // Patrol uses simple movement
        
        if (Vector2.Distance(transform.position, target) < 0.1f) movingPositive = !movingPositive;
    }
    
    protected override void OnAlertReceived(Vector3 p, Vector3 o, float r)
    {
        base.OnAlertReceived(p, o, r);
        if (isAlerted) loseAggroTimer = giveUpTime; 
    }
}