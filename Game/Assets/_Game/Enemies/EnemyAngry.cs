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
            // --- BERSERK MODE (Red Mask) ---
            moveSpeed = berserkSpeed;

            if (distanceToPlayer > stoppingDistance) 
            {
                // CRITICAL: We pass ONLY 'obstacleLayer'.
                // This means it ignores 'pitLayer', so it will run into pits and die.
                MoveToSmart(player.position, obstacleLayer);
            }
            else 
            {
                StopMoving();
            }
        }
        else
        {
            // --- NORMAL ANGRY MODE ---
            // Behaves like a standard enemy (Smart Chase: Avoids Walls AND Pits)
            
            if (IsPlayerVisible(distanceToPlayer) || isAlerted)
            {
                if (!isAlerted) isAlerted = true; 

                if (distanceToPlayer > stoppingDistance)
                {
                    // No mask passed = Defaults to (Obstacle | Pit)
                    MoveToSmart(player.position); 
                }
                else
                {
                    StopMoving();
                    if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
                }
            }
            else
            {
                StopMoving();
            }
        }
    }

    // Logic: Berserkers destroy other enemies on impact
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentMask == MaskType.Red)
        {
            EnemyBase otherEnemy = collision.gameObject.GetComponent<EnemyBase>();
            if (otherEnemy != null)
            {
                Debug.Log("Berserker smashed an ally! Masks dropped.");
                otherEnemy.Die(); // Kills the other enemy and drops their mask
            }
        }
    }
}