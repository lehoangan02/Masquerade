using UnityEngine;

public class Enemy_Angry : EnemyBase
{
    [Header("Angry Stats")]
    public float berserkSpeed = 10f;
    private Vector2 currentChargeTarget;

    private void Reset()
    {
        moveSpeed = 6f; 
        visionRange = 8f; 
        stoppingDistance = 0.6f;
        skinColor = Color.red;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        // Check mask directly
        if (currentMask == MaskType.Red)
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
        // 1. Determine Target (Player)
        // We re-update target constantly to track player, or you can lock it.
        // For "Straight Line" logic that refreshes:
        currentChargeTarget = player.position;

        Vector2 dir = (currentChargeTarget - (Vector2)transform.position).normalized;

        // 2. Wall Check
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 1f);
        if (hit.collider != null && hit.collider.CompareTag("Wall"))
        {
            // If wall is ahead, we keep targeting player (re-pathing)
            // Or you can add logic to slide along wall
        }

        // 3. Move Fast
        rb.MovePosition(rb.position + dir * berserkSpeed * Time.fixedDeltaTime);
        if(spriteRenderer) spriteRenderer.flipX = dir.x < 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only kill allies if we have the Red Mask
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