using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Base mask throwable. Sticks to enemies on hit.
/// When dropped (enemy dies), becomes a pickup.
/// Uses MaskType enum from EnemyBase for compatibility.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Mask : MonoBehaviour, IMask
{
    public enum MaskState { Idle, Thrown, Attached, Dropped }

    [Header("Stats")]
    [SerializeField] private string maskName = "Mask";
    [SerializeField] private MaskType maskType = MaskType.None;
    [SerializeField] private Color maskColor = Color.white;
    [SerializeField] private float speed = 12f;
    [SerializeField] private int damage = 5;
    [SerializeField] private float lifetime = 5f;

    [Header("Stick Settings")]
    [SerializeField] private Vector2 stickOffset = new Vector2(0, 0.2f);
    [SerializeField] private float stickScale = 4f;

    [Header("Pickup Settings")]
    [SerializeField] private float attractionRadius = 3f;
    [SerializeField] private float attractionSpeed = 8f;
    [SerializeField] private float pickupRadius = 0.5f;
    [SerializeField] private int ammoRefund = 1;
    
    [Header("Pickup Visuals")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.15f;
    [SerializeField] private float rotateSpeed = 90f;
    
    [Header("Spawn Settings")]
    [Tooltip("Check this to spawn as a pickup (for testing or world drops)")]
    [SerializeField] private bool spawnAsPickup = false;

    [Header("Attached Spotlight")]
    [SerializeField] private bool attachSpotLightOnHit = true;
    [SerializeField] private GameObject spotLightPrefab;
    [SerializeField] private Color spotLightColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private float spotLightIntensity = 1.0f;
    [SerializeField] private float spotLightInnerRadius = 1.0f;
    [SerializeField] private float spotLightOuterRadius = 4.0f;
    [Range(0f, 360f)] [SerializeField] private float spotLightInnerAngle = 30f;
    [Range(0f, 360f)] [SerializeField] private float spotLightOuterAngle = 70f;
    [SerializeField] private Vector3 spotLightLocalOffset = new Vector3(0f, 0.6f, 0f);

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D col;

private const float MASK_SCALE = 4f;
    private MaskState currentState = MaskState.Idle;
    private Transform attachedTarget;
    private Transform player;
    private Collider2D playerCollider; // Store reference to player collider
    private Vector3 dropPosition;
    private float dropTime;
    private GameObject attachedSpotLightObject;
    private Light2D attachedSpotLight;

    // IThrowable implementation
    public float Speed => speed;
    public int Damage => damage;
    
    // IMask implementation
    public string MaskName => maskName;
    public Color MaskColor => maskColor;
    public MaskType Type => maskType;
    
    // State accessors
    public MaskState CurrentState => currentState;
    public bool IsAttached => currentState == MaskState.Attached;
    public bool IsDropped => currentState == MaskState.Dropped;
    public Transform AttachedTarget => attachedTarget;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();
        
        transform.localScale = Vector3.one * MASK_SCALE;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (col != null) col.isTrigger = false;
        
        if (spriteRenderer != null) spriteRenderer.color = maskColor;
        
        // Find player transform for distance checks (Pickup)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    /// <summary>
    /// CALLED BY PLAYERTHROWER: Ignores physics collision with player
    /// </summary>
    public void IgnorePlayerCollision(Collider2D playerCol)
    {
        playerCollider = playerCol;
        if (playerCollider != null && col != null)
        {
            // This prevents the recoil and "left-only" ejection bug
            Physics2D.IgnoreCollision(col, playerCollider, true);
        }
    }

    void Start()
    {
        // If spawned via debug/world placement (Pickup Mode)
        if (currentState == MaskState.Dropped) // Logic handled in Drop() usually
        {
             // Handle initialization if pre-placed in scene
        }
        else
        {
            Invoke(nameof(CheckDestroy), lifetime);
        }
    }

    void Update()
    {
        // Check if attached target was destroyed (enemy died)
        if (currentState == MaskState.Attached && attachedTarget == null)
        {
            Drop();
        }
        
        // Handle pickup behavior when dropped
        if (currentState == MaskState.Dropped && player != null)
        {
            HandlePickupBehavior();
        }
    }

    void CheckDestroy()
    {
        // Only destroy if still flying or idle - never if attached or dropped
        if (currentState == MaskState.Idle || currentState == MaskState.Thrown)
        {
            Destroy(gameObject);
        }
        // If attached or dropped, don't destroy - mask persists
    }

    public void Throw(Vector2 direction)
    {
        if (currentState == MaskState.Thrown) return;
        currentState = MaskState.Thrown;
        
        // Ensure proper scale
        transform.localScale = Vector3.one * MASK_SCALE;
        
        // Ensure collider is solid for wall collision
        if (col != null) col.isTrigger = false;
        
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

    /// <summary>
    /// Set the mask type (matches EnemyBase.MaskType enum).
    /// </summary>
    public void SetMaskType(MaskType type)
    {
        maskType = type;
        
        // Update name to match what EnemyBase looks for
        switch (type)
        {
            case MaskType.Red:
                maskName = "RedMask";
                gameObject.name = "RedMask";
                SetColor(Color.red);
                break;
            case MaskType.Yellow:
                maskName = "YellowMask";
                gameObject.name = "YellowMask";
                SetColor(Color.yellow);
                break;
            case MaskType.Green:
                maskName = "GreenMask";
                gameObject.name = "GreenMask";
                SetColor(Color.green);
                break;
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
        // Pickup collision with player when dropped
        if (currentState == MaskState.Dropped && other.CompareTag("Player"))
        {
            PickUp();
            return;
        }
        
        // Only process attack collisions when thrown
        if (currentState != MaskState.Thrown) return;
        
        Debug.Log($"[Mask] Hit: {other.name}, Tag: {other.tag}");
        
        // Ignore player collisions
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
        
        // Check if it's a wall - become collectable
        if (other.CompareTag("Wall") || other.layer == LayerMask.NameToLayer("Wall"))
        {
            Drop();
            Debug.Log($"[{maskName}] Hit wall, becoming collectable!");
            return;
        }
        
        // Hit something else - ignore (don't destroy or drop)
    }

    void AttachToTarget(Transform target)
    {
        currentState = MaskState.Attached;
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
        transform.localScale = Vector3.one * MASK_SCALE;

        if (attachSpotLightOnHit)
        {
            AttachSpotLight(target);
        }
        
        // Call attach event
        OnAttach(target.gameObject);
        
        Debug.Log($"[{maskName}] Attached to {target.name}!");
    }

    /// <summary>
    /// Drop the mask (when enemy dies). Becomes a pickup.
    /// </summary>
    public void Drop()
    {
        if (currentState == MaskState.Dropped) return;
        
        currentState = MaskState.Dropped;
        dropTime = Time.time;
        
        // Detach from parent
        Vector3 worldPos = transform.position;
        transform.SetParent(null);
        transform.position = worldPos;
        dropPosition = worldPos;

        CleanupSpotLight();
        
        // Reset scale and rotation to full size
        transform.localScale = Vector3.one * MASK_SCALE;
        transform.rotation = Quaternion.identity;
        
        // Keep kinematic, no velocity
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        
        // Re-enable collider as trigger for pickup
       if (col != null)
        {
            col.enabled = true;
            col.isTrigger = true; // Become a trigger so player can overlap
            
            // CRITICAL FIX: Re-enable collision with player so OnTriggerEnter works
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(col, playerCollider, false);
            }
        }
        
        Debug.Log($"[{maskName}] Dropped! Ready for pickup.");
    }

    void HandlePickupBehavior()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Check for pickup (very close)
        if (distanceToPlayer <= pickupRadius)
        {
            PickUp();
            return;
        }
        
        // Attraction behavior
        if (distanceToPlayer <= attractionRadius)
        {
            // Move toward player, faster as it gets closer
            float speedMultiplier = 1f + (1f - (distanceToPlayer / attractionRadius)) * 2f;
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * attractionSpeed * speedMultiplier * Time.deltaTime);
            
            // Spin while moving
            transform.Rotate(0, 0, rotateSpeed * 2f * Time.deltaTime);
        }
        else
        {
            // Idle: bob up and down
            float elapsed = Time.time - dropTime;
            float yOffset = Mathf.Sin(elapsed * bobSpeed) * bobAmount;
            transform.position = new Vector3(dropPosition.x, dropPosition.y + yOffset, dropPosition.z);
            
            // Slow rotation
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }
    }

    void PickUp()
    {
        if (currentState != MaskState.Dropped) return;
        
        // Add ammo to widget
        if (BulletTypeWidget.Instance != null)
        {
            BulletTypeWidget.Instance.AddAmmo(ammoRefund);
            Debug.Log($"[{maskName}] Picked up! +{ammoRefund} ammo");
        }
        
        Destroy(gameObject);
    }

    private void AttachSpotLight(Transform target)
    {
        if (target == null) return;
        if (attachedSpotLightObject != null) return;

        if (spotLightPrefab != null)
        {
            attachedSpotLightObject = Instantiate(spotLightPrefab, target);
            attachedSpotLight = attachedSpotLightObject.GetComponent<Light2D>();
            if (attachedSpotLight == null)
            {
                attachedSpotLight = attachedSpotLightObject.GetComponentInChildren<Light2D>();
            }
            if (attachedSpotLight == null)
            {
                attachedSpotLight = attachedSpotLightObject.AddComponent<Light2D>();
                attachedSpotLight.lightType = Light2D.LightType.Point;
                attachedSpotLight.color = spotLightColor;
                attachedSpotLight.intensity = spotLightIntensity;
                attachedSpotLight.pointLightInnerRadius = spotLightInnerRadius;
                attachedSpotLight.pointLightOuterRadius = spotLightOuterRadius;
                attachedSpotLight.pointLightInnerAngle = spotLightInnerAngle;
                attachedSpotLight.pointLightOuterAngle = spotLightOuterAngle;
            }
        }
        else
        {
            attachedSpotLightObject = new GameObject("EnemySpotLight2D");
            attachedSpotLightObject.transform.SetParent(target);
            attachedSpotLightObject.transform.localPosition = spotLightLocalOffset;
            attachedSpotLightObject.transform.localRotation = Quaternion.identity;

            attachedSpotLight = attachedSpotLightObject.AddComponent<Light2D>();
            attachedSpotLight.lightType = Light2D.LightType.Point;
            attachedSpotLight.color = spotLightColor;
            attachedSpotLight.intensity = spotLightIntensity;
            attachedSpotLight.pointLightInnerRadius = spotLightInnerRadius;
            attachedSpotLight.pointLightOuterRadius = spotLightOuterRadius;
            attachedSpotLight.pointLightInnerAngle = spotLightInnerAngle;
            attachedSpotLight.pointLightOuterAngle = spotLightOuterAngle;
        }

        if (attachedSpotLightObject != null)
        {
            if (attachedSpotLightObject.GetComponent<SpotLight2DRegister>() == null)
            {
                attachedSpotLightObject.AddComponent<SpotLight2DRegister>();
            }
        }

        if (attachedSpotLight != null)
        {
            SpotLight2DSystem.Register(attachedSpotLight);
        }
    }

    private void CleanupSpotLight()
    {
        if (attachedSpotLight != null)
        {
            SpotLight2DSystem.Unregister(attachedSpotLight);
        }

        if (attachedSpotLightObject != null)
        {
            Destroy(attachedSpotLightObject);
        }

        attachedSpotLight = null;
        attachedSpotLightObject = null;
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

    // Editor visualization
    void OnDrawGizmosSelected()
    {
        if (currentState == MaskState.Dropped)
        {
            // Attraction radius
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attractionRadius);
            
            // Pickup radius
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
