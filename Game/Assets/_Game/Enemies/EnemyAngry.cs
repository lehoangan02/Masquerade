using UnityEngine;

public class Enemy_Angry : EnemyBase
{
    private void Reset()
    {
        moveSpeed = 6f;        // Fast
        visionRange = 8f;      // Far sight
        fovAngle = 60f;        // Tunnel Vision
        stoppingDistance = 0.6f;
        skinColor = Color.red;
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        // Simple logic: If I see you, I chase.
        // If I saw you once (isAlerted), I chase forever.
        // I do NOT have a patrol logic; I stand still when not chasing.
        Logic_ChaseIfInRange(distanceToPlayer);
    }
}