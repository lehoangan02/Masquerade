using UnityEngine;

public class Enemy_Lazy : EnemyBase
{
    // --- STEP 1: Sets Default Values in Inspector ---
    // This runs only when you first add the component (or right-click -> Reset).
    // It does NOT run when the game starts, so your Inspector changes are safe.
    private void Reset()
    {
        moveSpeed = 0f;        // Lazy = No movement
        visionRange = 6f;      // Good vision to spot player
        stoppingDistance = 0f; // Doesn't matter, he doesn't move
        skinColor = Color.green;
    }

    // --- STEP 2: The Setup ---
    protected override void Start()
    {
        // Do NOT put stats here anymore.
        // Just run the parent setup (Physics, Visuals, LineRenderer)
        base.Start();
    }

    // --- STEP 3: The Logic ---
    protected override void PerformBehavior(float distanceToPlayer)
    {
        // 1. Ensure we don't move (Force stop every frame)
        StopMoving();

        // 2. If player is close -> Alert everyone
        if (distanceToPlayer <= visionRange)
        {
            // Trigger the static event to alert Base and Angry enemies
            EnemyAlertSystem.TriggerAlert(player.position);
            
            // Visual feedback: Face the player
            if(spriteRenderer) 
            {
                spriteRenderer.flipX = player.position.x < transform.position.x;
            }
        }
    }

    // Lazy enemies ignore alerts from other people
    protected override void OnAlertReceived(Vector3 pos)
    {
        // Do nothing. I am lazy.
    }
}