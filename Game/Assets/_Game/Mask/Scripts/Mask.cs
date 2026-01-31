using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    private Vector3 dropPosition;
    private float dropTime;
    private GameObject attachedSpotLightObject;
    private Light2D attachedSpotLight;
    private Collider2D playerCollider;

    // Interface Properties
    public float Speed => speed;
    public int Damage => damage;
    public string MaskName => maskName;
    public Color MaskColor => maskColor;
    public MaskType Type => maskType;
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
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    public void IgnorePlayerCollision(Collider2D playerCol)
    {
        playerCollider = playerCol;
        if (playerCollider != null && col != null)
        {
            Physics2D.IgnoreCollision(col, playerCollider, true);
        }
    }

    void Start()
    {
        if (spawnAsPickup)
        {
            currentState = MaskState.Dropped;
            dropPosition = transform.position;
            dropTime = Time.time;
            transform.localScale = Vector3.one * MASK_SCALE;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            if (col != null) col.isTrigger = true;
        }
        else
        {
            Invoke(nameof(CheckDestroy), lifetime);
        }
    }

    void Update()
    {
        if (currentState == MaskState.Attached && attachedTarget == null) Drop();
        if (currentState == MaskState.Dropped && player != null) HandlePickupBehavior();
    }

    void CheckDestroy()
    {
        if (currentState == MaskState.Idle || currentState == MaskState.Thrown)
        {
            Destroy(gameObject);
        }
    }

    public void Throw(Vector2 direction)
    {
        if (currentState == MaskState.Thrown) return;
        currentState = MaskState.Thrown;
        
        transform.localScale = Vector3.one * MASK_SCALE;
        if (col != null) col.isTrigger = false; 
        
        rb.linearVelocity = direction.normalized * speed;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetColor(Color color)
    {
        maskColor = color;
        if (spriteRenderer != null) spriteRenderer.color = color;
    }

    public void SetMaskType(MaskType type)
    {
        maskType = type;
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

    void OnTriggerEnter2D(Collider2D other) => HandleCollision(other.gameObject, other.transform);
    void OnCollisionEnter2D(Collision2D collision) => HandleCollision(collision.gameObject, collision.transform);

    void HandleCollision(GameObject other, Transform otherTransform)
    {
        // 1. Pickup Logic (Only if dropped)
        if (currentState == MaskState.Dropped)
        {
            if (other.CompareTag("Player")) PickUp();
            return;
        }
        
        // 2. Thrown Logic
        if (currentState != MaskState.Thrown) return;
        
        // Explicitly ignore Player (Physics2D.IgnoreCollision handles this, but safety first)
        if (other.CompareTag("Player")) return;

        // 3. Enemy Hit Logic
        if (other.CompareTag("Enemy"))
        {
            AttachToTarget(otherTransform);
            
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null) target.TakeDamage(damage);
            return;
        }
        
        // 4. Wall/Obstacle Hit Logic (Catch-All)
        // If we hit ANYTHING else (Wall, Pillar, Crate, Untagged object), we stop.
        Debug.Log($"[{maskName}] Hit {other.name} (Wall/Obstacle). Dropping.");
        Drop();
    }

    void AttachToTarget(Transform target)
    {
        currentState = MaskState.Attached;
        attachedTarget = target;
        
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        if (col != null) col.enabled = false;
        
        transform.SetParent(target);
        transform.localPosition = stickOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * MASK_SCALE;

        if (attachSpotLightOnHit) AttachSpotLight(target);
        
        OnAttach(target.gameObject);
    }

    public void Drop()
    {
        if (currentState == MaskState.Dropped) return;
        
        currentState = MaskState.Dropped;
        dropTime = Time.time;
        
        // Detach
        Vector3 worldPos = transform.position;
        transform.SetParent(null);
        transform.position = worldPos;
        dropPosition = worldPos;

        CleanupSpotLight();
        
        // Reset Visuals
        transform.localScale = Vector3.one * MASK_SCALE;
        transform.rotation = Quaternion.identity;
        
        // --- INSTANT STOP PHYSICS ---
        rb.bodyType = RigidbodyType2D.Kinematic; // Prevents physics engine from processing bounce
        rb.linearVelocity = Vector2.zero;        // Zero out any momentum
        rb.angularVelocity = 0f;
        
        // Re-enable collider for Pickup trigger
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = true; // Become trigger so we don't push player around
            
            // Allow player to touch it again
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(col, playerCollider, false);
            }
        }
    }

    void HandlePickupBehavior()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= pickupRadius)
        {
            PickUp();
            return;
        }
        
        if (distanceToPlayer <= attractionRadius)
        {
            float speedMultiplier = 1f + (1f - (distanceToPlayer / attractionRadius)) * 2f;
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * attractionSpeed * speedMultiplier * Time.deltaTime);
            transform.Rotate(0, 0, rotateSpeed * 2f * Time.deltaTime);
        }
        else
        {
            // Idle bob
            float elapsed = Time.time - dropTime;
            float yOffset = Mathf.Sin(elapsed * bobSpeed) * bobAmount;
            transform.position = new Vector3(dropPosition.x, dropPosition.y + yOffset, dropPosition.z);
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }
    }

    void PickUp()
    {
        if (currentState != MaskState.Dropped) return;
        
        if (BulletTypeWidget.Instance != null)
        {
            BulletTypeWidget.Instance.AddAmmo(ammoRefund);
        }
        
        Destroy(gameObject);
    }

    public void ApplyEffect(GameObject target)
    {
        // Default implementation for IMask
        Debug.Log($"[{maskName}] Effect applied to {target.name}!");
    }

    // ... [Spotlight methods remain unchanged] ...
    
    private void AttachSpotLight(Transform target)
    {
        if (target == null || attachedSpotLightObject != null) return;
        
        // (Simplified for brevity - keep your existing light code here)
        if (spotLightPrefab != null)
            attachedSpotLightObject = Instantiate(spotLightPrefab, target);
        else
            attachedSpotLightObject = new GameObject("EnemySpotLight2D");
            
        attachedSpotLightObject.transform.SetParent(target);
        attachedSpotLightObject.transform.localPosition = spotLightLocalOffset;
        
        // Ensure Light component exists and register it...
        attachedSpotLight = attachedSpotLightObject.GetComponent<Light2D>();
        if(attachedSpotLight == null) attachedSpotLight = attachedSpotLightObject.AddComponent<Light2D>();
        
        attachedSpotLight.color = spotLightColor;
        // ... set other light properties ...
        
        if(attachedSpotLight != null) SpotLight2DSystem.Register(attachedSpotLight);
    }

    private void CleanupSpotLight()
    {
        if (attachedSpotLight != null) SpotLight2DSystem.Unregister(attachedSpotLight);
        if (attachedSpotLightObject != null) Destroy(attachedSpotLightObject);
        attachedSpotLight = null;
        attachedSpotLightObject = null;
    }

    public virtual void OnAttach(GameObject target) { }

    // Editor visualization
    void OnDrawGizmosSelected()
    {
        if (currentState == MaskState.Dropped)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attractionRadius);
            
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}