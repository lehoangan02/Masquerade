using UnityEngine;

public class Enemy_Lazy : EnemyBase
{
    [Header("Lazy Settings")]
    public float baseShoutRange = 10f; 

    private void Reset()
    {
        moveSpeed = 0f; 
        visionRange = 6f; 
        fovAngle = 360f; 
        baseShoutRange = 10f;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        StopMoving();

        // 1. Calculate Range based on Mask
        float currentShoutRange = baseShoutRange;
        
        // If we have the Red Mask attached, scream louder
        if (currentMask == MaskType.Red)
        {
            currentShoutRange = 20f; 
        }

        // 2. Logic
        // IsPlayerVisible automatically handles Yellow Mask (Vision reduction)
        if (IsPlayerVisible(distanceToPlayer))
        {
            EnemyAlertSystem.TriggerAlert(player.position, transform.position, currentShoutRange);
            if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
        }
    }

    protected override void OnAlertReceived(Vector3 p, Vector3 o, float r) { }

   private void OnDrawGizmosSelected()
    {
        float rangeToDraw = baseShoutRange;

        // Check if the game is actually running to see the live mask effect
        if (Application.isPlaying)
        {
            if (currentMask == MaskType.Red) rangeToDraw = 20f;
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, rangeToDraw);
    }
}