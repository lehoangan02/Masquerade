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
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool hasBeenThrown = false;
    private IBulletType bulletType;

    // IThrowable implementation
    public float Speed => speed;
    public int Damage => damage;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // Configure rigidbody for projectile
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Set the bullet type (color and effects).
    /// </summary>
    public void SetBulletType(IBulletType type)
    {
        bulletType = type;
        
        // Apply color
        if (spriteRenderer != null && type != null)
        {
            spriteRenderer.color = type.BulletColor;
        }
    }

    /// <summary>
    /// Set just the color (simpler method).
    /// </summary>
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
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
            
            // Apply bullet type effect
            bulletType?.OnHitEffect(other.gameObject);
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
            
            // Apply bullet type effect
            bulletType?.OnHitEffect(collision.gameObject);
        }

        // Destroy on collision
        Destroy(gameObject);
    }
}
