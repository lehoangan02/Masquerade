using UnityEngine;

public class Enemy_Standard : EnemyBase
{
    [Header("Patrol Settings")]
    public bool patrolVertical = false; 
    public float patrolRange = 3f;      
    public float patrolSpeed = 1.5f;    
    public float chaseSpeed = 4f;       
    public float giveUpTime = 2f; 
    public GameObject mask;
    
    private Vector2 startPos;
    private bool movingPositive = true;
    private float loseAggroTimer = 0f; 

    protected override void Start()
    {
        base.Start();
        startPos = transform.position;
        mask = transform.Find("CIRCLE_MASK").gameObject;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        if (currentMask == MaskType.None)
        {
            mask.SetActive(false);
        }
        else
        {
            mask.SetActive(true);
        }
        // 1. RED MASK LOGIC (Reckless)
        if (currentMask == MaskType.Red)
        {
            moveSpeed = chaseSpeed * 1.5f;
            if (distanceToPlayer <= attackRange) TryAttack();
            else if (distanceToPlayer > stoppingDistance) MoveToSmart(player.position, false); // Ignored Pits
            else StopMoving();
            return; 
        }

        // 2. NORMAL LOGIC
        bool canSeePlayer = IsPlayerVisible(distanceToPlayer);

        if (canSeePlayer) 
        {
            loseAggroTimer = giveUpTime;
            isAlerted = true;
        }
        else if (loseAggroTimer > 0) 
        {
            loseAggroTimer -= Time.deltaTime;
            if (loseAggroTimer <= 0) isAlerted = false;
        }

        if (isAlerted)
        {
            moveSpeed = chaseSpeed;
            if (distanceToPlayer <= attackRange) TryAttack();
            else if (distanceToPlayer > stoppingDistance) MoveToSmart(player.position, true); // Avoids Pits
            else StopMoving();
        }
        else
        {
            moveSpeed = patrolSpeed;
            PatrolLogic();
        }
    }
    
    private void PatrolLogic()
    {
        Vector2 direction = patrolVertical ? Vector2.up : Vector2.right;
        float offset = movingPositive ? patrolRange : -patrolRange;
        Vector2 target = startPos + (direction * offset);
        
        MoveToSmart(target, true); // Smooth, safe patrol
        
        if (Vector2.Distance(transform.position, target) < 0.2f) 
            movingPositive = !movingPositive;
    }
}