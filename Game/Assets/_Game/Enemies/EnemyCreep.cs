using UnityEngine;

public class Enemy_Standard : EnemyBase
{
    // This function only runs in the Editor when you add the script
    // or Right-Click -> Reset. It sets your "Default" inspector values.
    private void Reset()
    {
        moveSpeed = 3f;
        visionRange = 4f;
        stoppingDistance = 0.6f;
        skinColor = Color.white;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        Logic_ChaseIfInRange(distanceToPlayer);
    }
}