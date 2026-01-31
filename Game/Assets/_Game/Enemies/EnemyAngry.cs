using UnityEngine;

public class Enemy_Angry : EnemyBase
{
    [Header("Angry Stats")]
    public float berserkSpeed = 10f;
    
    private void Reset()
    {
        moveSpeed = 6f; 
        visionRange = 8f; 
        stoppingDistance = 0.6f;
        skinColor = Color.red;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        if (currentMask == MaskType.Red)
        {
            // --- BERSERK MODE ---
            moveSpeed = berserkSpeed;

            if (distanceToPlayer <= attackRange)
            {
                TryAttack(); // <--- ATTACK
            }
            else if (distanceToPlayer > stoppingDistance) 
            {
                // Chase fast, ignore pits
                MoveToSmart(player.position, obstacleLayer);
            }
            else StopMoving();
        }
        else
        {
            // --- NORMAL ANGRY MODE ---
            bool visible = IsPlayerVisible(distanceToPlayer);
            
            if (visible || isAlerted)
            {
                if (!isAlerted) isAlerted = true; 

                if (distanceToPlayer <= attackRange)
                {
                    TryAttack(); // <--- ATTACK
                }
                else if (distanceToPlayer > stoppingDistance)
                {
                    // Smart chase (avoids pits)
                    MoveToSmart(player.position); 
                }
                else
                {
                    StopMoving();
                }
            }
            else
            {
                StopMoving();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (currentMask == MaskType.Red)
        {
            EnemyBase otherEnemy = collision.gameObject.GetComponent<EnemyBase>();
            if (otherEnemy != null)
            {
                if(animator) animator.SetTrigger("Attack"); // Animation for smashing friend
                otherEnemy.Die(); 
            }
        }
    }
}