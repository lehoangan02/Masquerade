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
        
        // --- FIX STARTS HERE ---
        if (currentMask == MaskType.Red)
        {
            // Requirement 1: Increase Calling (Shout) Size
            currentShoutRange = 20f; 

            // Requirement 2: Increase Vision Size
            // EnemyBase sets it to 1f by default. We force it bigger here.
            visionMultiplier = 2.0f; 
        }
        // --- FIX ENDS HERE ---

        // 2. Logic
        // IsPlayerVisible uses 'visionMultiplier'. 
        // If Red, it uses 2.0. If Yellow, EnemyBase made it 0.5. If None, it's 1.0.
        if (IsPlayerVisible(distanceToPlayer))
        {
            EnemyAlertSystem.TriggerAlert(player.position, transform.position, currentShoutRange);
            if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
        }
    }

    // Lazy enemies usually don't react to alerts from others, they CAUSE the alerts.
    protected override void OnAlertReceived(Vector3 p, Vector3 o, float r) { }

   private void OnDrawGizmosSelected()
    {
        float rangeToDraw = baseShoutRange;

        if (Application.isPlaying)
        {
            if (currentMask == MaskType.Red) rangeToDraw = 20f;
        }

        // Yellow Gizmo for Shout Range
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, rangeToDraw);
    }
}