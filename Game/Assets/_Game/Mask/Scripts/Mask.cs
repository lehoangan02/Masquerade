using UnityEngine;

/// <summary>
/// Base mask throwable. Sticks to enemies on hit.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Mask : MonoBehaviour, IMask
{
    [Header("Stats")]
    [SerializeField] private string maskName = "Mask";
    [SerializeField] private Color maskColor = Color.white;
    [SerializeField] private float speed = 12f;
    [SerializeField] private int damage = 5;
    [SerializeField] private float lifetime = 5f;

    [Header("Stick Settings")]
    [SerializeField] private Vector2 stickOffset = Vector2.zero;
    [SerializeField] private float stickScale = 0.5f;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D col;

    private bool hasBeenThrown = false;
    private bool isAttached = false;
    private Transform attachedTarget;

    // IThrowable implementation
    public float Speed => speed;
    public int Damage => damage;
    
    // IMask implementation
    public string MaskName => maskName;
    public Color MaskColor => maskColor;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();
        
        // Configure rigidbody for projectile
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Apply color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = maskColor;
        }
    }

    void Start()
    {
        // Auto-destroy after lifetime (only if not attached)
        Invoke(nameof(CheckDestroy), lifetime);
    }

    void CheckDestroy()
    {
        if (!isAttached)
        {
            Destroy(gameObject);
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

    public void SetColor(Color color)
    {
        maskColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject, other.transform);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject, collision.transform);
    }

    void HandleCollision(GameObject other, Transform otherTransform)
    {
        if (isAttached) return;
        
        Debug.Log($"[Mask] Hit: {other.name}, Tag: {other.tag}");
        
        if (other.CompareTag("Player")) return;

        // Check if it's an enemy
        if (other.CompareTag("Enemy"))
        {
            AttachToTarget(otherTransform);
            
            // Try to damage (uses Interface Rule)
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
            return;
        }
        
        // Hit something else - destroy
        Destroy(gameObject);
    }

    void AttachToTarget(Transform target)
    {
        isAttached = true;
        attachedTarget = target;
        
        // Stop movement
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        // Disable collider
        if (col != null) col.enabled = false;
        
        // Parent to target
        transform.SetParent(target);
        
        // Position on face (center with offset)
        transform.localPosition = stickOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * stickScale;
        
        // Call attach event
        OnAttach(target.gameObject);
        
        Debug.Log($"[{maskName}] Attached to {target.name}!");
    }

    public virtual void OnAttach(GameObject target)
    {
        // Override in subclasses for attach effects (particles, sound, etc.)
    }

    public virtual void ApplyEffect(GameObject target)
    {
        // Override in subclasses for mask effects
        // This will be called by enemy scripts later
        Debug.Log($"[{maskName}] Effect applied to {target.name}!");
    }
}
