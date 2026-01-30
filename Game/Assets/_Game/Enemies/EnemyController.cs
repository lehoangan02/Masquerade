using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController2D : MonoBehaviour
{
    [Header("Configuration")]
    public EnemyStats currentStats; // Your ScriptableObject
    public Transform player;

    private Rigidbody2D rb;
    private bool isAlerted = false;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Important for Top-Down 2D: Turn off Gravity!
        rb.gravityScale = 0; 
        rb.freezeRotation = true; // Stop enemy from spinning like a beyblade

        // Apply initial stats
        if (currentStats != null) ApplyStats(currentStats);

        // Subscribe to alerts
        EnemyAlertSystem.OnPlayerFound += HandleAlert;
    }

    void OnDestroy()
    {
        EnemyAlertSystem.OnPlayerFound -= HandleAlert;
    }

    void FixedUpdate()
    {
        if (player == null || currentStats == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentStats.type)
        {
            case EnemyType.Base:
            case EnemyType.Angry:
                HandleChaserLogic(distanceToPlayer);
                break;

            case EnemyType.Lazy:
                HandleLazyLogic(distanceToPlayer);
                break;
        }
    }

    void HandleChaserLogic(float distance)
    {
        // 1. If we see player OR we were alerted
        if (distance <= currentStats.visionRange || isAlerted)
        {
            MoveTowards(player.position);

            // If we reached the spot and player is gone, stop being alerted
            if (isAlerted && distance < 1f) isAlerted = false;
        }
        else
        {
            StopMoving();
        }
    }

    void HandleLazyLogic(float distance)
    {
        StopMoving();

        if (distance <= currentStats.visionRange)
        {
            // Alert everyone!
            EnemyAlertSystem.TriggerAlert(player.position);
            
            // Optional: Flip to face player
            FaceTarget(player.position);
        }
    }

    void MoveTowards(Vector2 targetPos)
    {
        // Calculate direction
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        
        // Move using Physics (Smooth and handles collisions)
        rb.MovePosition(rb.position + direction * currentStats.moveSpeed * Time.fixedDeltaTime);

        FaceTarget(targetPos);
    }

    void StopMoving()
    {
        // Stop physics momentum
        rb.linearVelocity = Vector2.zero;
    }

    void FaceTarget(Vector2 targetPos)
    {
        // Simple Sprite Flip: If target is to the left, flip sprite
        if (targetPos.x < transform.position.x)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }

    void HandleAlert(Vector3 targetPos)
    {
        if (currentStats.type == EnemyType.Lazy) return;
        isAlerted = true;
    }

    public void SwapMask(EnemyStats newStats)
    {
        ApplyStats(newStats);
    }

    void ApplyStats(EnemyStats stats)
    {
        currentStats = stats;
        // Change color to visually show the type change
        if(spriteRenderer) spriteRenderer.color = stats.skinColor;
    }
    
    // Debug Vision Range
    void OnDrawGizmosSelected()
    {
        if (currentStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, currentStats.visionRange);
        }
    }
}