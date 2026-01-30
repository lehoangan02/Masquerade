using UnityEngine;

public class Enemy_Lazy : EnemyBase
{
    [Header("Lazy Settings")]
    public float shoutRange = 10f; // The "Zone Alert" radius

    // --- STEP 1: Defaults ---
    private void Reset()
    {
        moveSpeed = 0f;
        visionRange = 6f;
        fovAngle = 360f; // Full Circle Vision
        stoppingDistance = 0f;
        skinColor = Color.green;
        shoutRange = 10f;
    }

    // --- STEP 2: Logic ---
    protected override void PerformBehavior(float distanceToPlayer)
    {
        // 1. Ensure we don't move
        StopMoving();

        // 2. Custom Logic: If player is close, scream
        if (IsPlayerVisible(distanceToPlayer))
        {
            // Trigger the alert with the Zone Info:
            // (Target, My Position, My Shout Radius)
            EnemyAlertSystem.TriggerAlert(player.position, transform.position, shoutRange);
            
            // Visual feedback: Face the player
            if(spriteRenderer) 
                spriteRenderer.flipX = player.position.x < transform.position.x;
        }
    }

    // Lazy enemies usually don't care about alerts from others
    protected override void OnAlertReceived(Vector3 pos, Vector3 origin, float range)
    {
        // Do nothing. I am lazy.
    }

    // --- STEP 3: DRAW THE ZONE ---
    // This draws the Yellow Circle in the Scene View when you click the enemy
    private void OnDrawGizmosSelected()
    {
        // Draw the Vision Range (White) handled by base class usually, 
        // but here is the Alarm Range (Yellow):
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow with transparency
        Gizmos.DrawWireSphere(transform.position, shoutRange);
    }
}