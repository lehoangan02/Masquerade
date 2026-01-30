using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Current Attributes")]
    public float moveSpeed = 3f;
    public float detectRange = 5f;
    public Color bodyColor = Color.white;

    private Transform player;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        // Find the player automatically using their Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Simple Logic: If player is in range, move towards them
        if (distance < detectRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, 
                player.position, 
                moveSpeed * Time.deltaTime
            );
        }
    }

    // This is the function the Player's Mask Projectile will call
    public void ChangeMask(float newSpeed, float newRange, Color newColor)
    {
        moveSpeed = newSpeed;
        detectRange = newRange;
        bodyColor = newColor;

        // Change the look of the enemy instantly
        if (sr != null) sr.color = newColor;
        
        Debug.Log("Enemy stats changed by a mask!");
    }

    // Visualizes the detect range in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}