using UnityEngine;

public class Enemy_Angry : EnemyBase
{
    protected override void Start()
    {
        // Custom Stats for Angry Guy
        moveSpeed = 6f; // Fast!
        visionRange = 8f; // Sees far!
        skinColor = Color.red;

        base.Start();
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        // Same chase logic, but with the boosted stats defined above
        Logic_ChaseIfInRange(distanceToPlayer);
    }
}