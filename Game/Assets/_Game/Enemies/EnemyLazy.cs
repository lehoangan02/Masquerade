using UnityEngine;

public class Enemy_Lazy : EnemyBase
{
    [Header("Lazy Settings")]
    public float shoutRange = 10f; 

    private void Reset()
    {
        moveSpeed = 0f;
        visionRange = 6f;
        fovAngle = 360f; 
        shoutRange = 10f;
    }

    // Override ApplyMask to handle the Range Boost
    public override void ApplyMask(MaskType type)
    {
        base.ApplyMask(type);
        if (type == MaskType.Aggressive)
        {
            shoutRange = 20f; // BIGGER ZONE!
            Debug.Log("Lazy Enemy: MY SCREAM IS NOW HUGE!");
        }
    }

    protected override void PerformBehavior(float distanceToPlayer)
    {
        StopMoving();

        if (IsPlayerVisible(distanceToPlayer))
        {
            EnemyAlertSystem.TriggerAlert(player.position, transform.position, shoutRange);
            if(spriteRenderer) spriteRenderer.flipX = player.position.x < transform.position.x;
        }
    }

    protected override void OnAlertReceived(Vector3 p, Vector3 o, float r) { }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, shoutRange);
    }
}