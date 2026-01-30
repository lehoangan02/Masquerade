using UnityEngine;

/// <summary>
/// Default bullet projectile. Moves in a straight line and damages IDamageable targets.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour, IThrowable
{
    [Header("Stats")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifetime = 3f;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;

    private bool hasBeenThrown = false;

    // IThrowable implementation
    public float Speed => speed;
    public int Damage => damage;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        
        // Configure rigidbody for projectile
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    public void Throw(Vector2 direction)
    {
        if (hasBeenThrown) return;
        hasBeenThrown = true;
        
        // Set velocity
        rb.linearVelocity = direction.normalized * speed;
        
        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Don't hit the player
        if (other.CompareTag("Player")) return;

        // Try to damage target (uses Interface Rule!)
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        // Destroy bullet on any hit (except player)
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Don't hit the player
        if (collision.collider.CompareTag("Player")) return;

        // Try to damage target
        IDamageable target = collision.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        // Destroy on collision
        Destroy(gameObject);
    }
}
