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
            // BERSERK MODE (Kill walls logic is optional, but let's make him smart & fast)
            moveSpeed = berserkSpeed;
            
            // Even when Berserk, he should try to go around walls unless you want him to phase through
            // Use MoveToSmart to navigate at high speed
            if (distanceToPlayer > stoppingDistance) MoveToSmart(player.position);
            else StopMoving();
        }
        else
        {
            // NORMAL ANGRY (Smart Chase)
            // Logic_ChaseIfInRange uses MoveTo internally, so let's override it here manually
            // to use MoveToSmart
            
            if (IsPlayerVisible(distanceToPlayer) || isAlerted)
            {
                if (!isAlerted) isAlerted = true; 

                if (distanceToPlayer > stoppingDistance)
                {
                    MoveToSmart(player.position); // <-- UPDATED to Smart
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

    // Keep the collision kill logic
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentMask == MaskType.Red)
        {
            EnemyBase otherEnemy = collision.gameObject.GetComponent<EnemyBase>();
            if (otherEnemy != null)
            {
                Destroy(otherEnemy.gameObject);
                Debug.Log("Berserker smashed an ally!");
            }
        }
    }
}