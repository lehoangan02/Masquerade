using UnityEngine;

public class Enemy_Angry : EnemyBase
{
    [Header("Berserk Stats")]
    public float berserkSpeed = 10f; // Double Angry Speed
    private Vector2 currentChargeTarget;
    private bool isBerserk = false;

    // Override ApplyMask to trigger Berserk
    public override void ApplyMask(MaskType type)
    {
        base.ApplyMask(type);
        if (type == MaskType.Aggressive)
        {
            isBerserk = true;
            moveSpeed = berserkSpeed;
            currentChargeTarget = player.position; // Lock on
        }
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        if (isBerserk)
        {
            BerserkLogic();
        }
        else
        {
            // Normal Angry Logic
            Logic_ChaseIfInRange(distanceToPlayer);
        }
    }

    private void BerserkLogic()
    {
        // 1. Raycast to see if we are about to hit a wall
        Vector2 dir = (currentChargeTarget - (Vector2)transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 1f);

        // "If it hits the wall, recalculate position"
        // We simulate this by constantly updating target if not blocked, 
        // or just brute forcing through if the wall is far.
        if (hit.collider != null && hit.collider.CompareTag("Wall"))
        {
             // Wall ahead! Re-calculate path (Simple: just target player again instantly)
             currentChargeTarget = player.position;
        }
        else
        {
             // No wall immediately in front? Keep charging at updated player pos
             // (Or keep old pos if you want a "Bull Charge" effect)
             currentChargeTarget = player.position; 
        }

        // 2. Move Super Fast
        rb.MovePosition(rb.position + dir * berserkSpeed * Time.fixedDeltaTime);
        if(spriteRenderer) spriteRenderer.flipX = dir.x < 0;
    }

    // 3. "Kill all enemies on the way"
    // We use OnCollisionEnter2D for physics collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBerserk)
        {
            // Check if we hit another enemy
            EnemyBase otherEnemy = collision.gameObject.GetComponent<EnemyBase>();
            if (otherEnemy != null)
            {
                // KILL THEM
                Destroy(otherEnemy.gameObject);
                Debug.Log("Berserker killed an ally!");
            }
        }
    }
}