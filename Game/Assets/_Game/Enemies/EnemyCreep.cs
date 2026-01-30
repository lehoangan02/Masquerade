using UnityEngine;

public class Enemy_Standard : EnemyBase
{
    [Header("Patrol Settings")]
    public bool patrolVertical = false; // Check for Up/Down movement
    public float patrolRange = 3f;      
    public float patrolSpeed = 1.5f;    
    public float chaseSpeed = 4f;       
    
    [Header("Aggro Settings")]
    public float giveUpTime = 2f; // Time to chase after losing sight

    private Vector2 startPos;
    private bool movingPositive = true;
    private float loseAggroTimer = 0f; 

    // Defaults for the Inspector
    private void Reset()
    {
        moveSpeed = 3f;
        visionRange = 5f;
        fovAngle = 60f;        // Cone Vision
        stoppingDistance = 0.6f;
        skinColor = Color.white;
        
        patrolVertical = false;
        patrolRange = 3f;
        patrolSpeed = 1.5f;
        chaseSpeed = 4f;
        giveUpTime = 2f;
    }

    protected override void Start()
    {
        base.Start();
        startPos = transform.position; // Remember where we started to patrol around it
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        // 1. CHECK VISION
        bool canSeePlayer = IsPlayerVisible(distanceToPlayer);

        // 2. UPDATE TIMER
        if (canSeePlayer || isAlerted) 
        {
            // If we see them, reset the timer to full
            loseAggroTimer = giveUpTime;
            
            // If we were alerted by radio, we consume that alert now
            // so we can switch to "Timer Logic" instead of "Infinite Alert"
            if (isAlerted) isAlerted = false; 
        }
        else
        {
            // If we don't see them, start counting down
            if (loseAggroTimer > 0)
                loseAggroTimer -= Time.deltaTime;
        }

        // 3. EXECUTE STATE
        if (loseAggroTimer > 0)
        {
            // --- CHASE STATE ---
            moveSpeed = chaseSpeed;
            
            // Use manual movement logic because we are handling "isAlerted" manually with the timer
            if (distanceToPlayer > stoppingDistance)
            {
                MoveTo(player.position);
            }
            else
            {
                StopMoving();
                if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
            }
        }
        else
        {
            // --- PATROL STATE ---
            moveSpeed = patrolSpeed;
            PatrolLogic();
        }
    }

    private void PatrolLogic()
    {
        Vector2 direction = patrolVertical ? Vector2.up : Vector2.right;
        float offset = movingPositive ? patrolRange : -patrolRange;
        Vector2 target = startPos + (direction * offset);

        MoveTo(target);

        // If close to patrol point, switch direction
        if (Vector2.Distance(transform.position, target) < 0.1f)
        {
            movingPositive = !movingPositive;
        }
    }
    
    // Override Alert: If we hear the alarm, we get curious/angry for a few seconds
    protected override void OnAlertReceived(Vector3 pos, Vector3 origin, float range)
    {
        base.OnAlertReceived(pos, origin, range); // Check distance logic in Parent
        
        if (isAlerted)
        {
            loseAggroTimer = giveUpTime; // Reset timer so we start chasing
        }
    }
}