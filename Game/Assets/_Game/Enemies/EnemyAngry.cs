using UnityEngine;

public class Enemy_Angry : EnemyBase
{
    [Header("Angry Stats")]
    public float berserkSpeed = 10f;
    private float originalMoveSpeed;

    protected override void Start()
    {
        base.Start();
        originalMoveSpeed = moveSpeed;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        if (currentMask == MaskType.Red)
        {
            // --- BERSERK MODE ---
            moveSpeed = berserkSpeed;

            if (distanceToPlayer <= attackRange)
            {
                TryAttack();
            }
            else if (distanceToPlayer > stoppingDistance) 
            {
                // Pass 'false' to IGNORE pits and charge straight forward
                MoveToSmart(player.position, false);
            }
            else StopMoving();
        }
        else
        {
            // --- NORMAL ANGRY MODE ---
            moveSpeed = originalMoveSpeed;
            bool visible = IsPlayerVisible(distanceToPlayer);
            
            if (visible || isAlerted)
            {
                if (distanceToPlayer <= attackRange) TryAttack();
                else if (distanceToPlayer > stoppingDistance) MoveToSmart(player.position, true); // Avoids pits
                else StopMoving();
            }
            else StopMoving();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || currentMask != MaskType.Red) return;

        EnemyBase otherEnemy = collision.gameObject.GetComponent<EnemyBase>();
        if (otherEnemy != null)
        {
            if(animator) animator.SetTrigger("Attack"); 
            otherEnemy.Die(); 
        }
    }
}