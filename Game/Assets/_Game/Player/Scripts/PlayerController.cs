using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; 

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    public enum State { Normal, Locked, Attacking }
    
    [Header("State")]
    [SerializeField] private State currentState = State.Normal;
    [SerializeField] public bool isHidden = false; // Must be TRUE to stealth kill

    [Header("Movement")]
    [SerializeField] public float moveSpeed = 6f;
    
    [Header("Stealth Combat")]
    [SerializeField] private float killRange = 2.0f;     
    [SerializeField] private float dashDuration = 0.1f;  
    [SerializeField] private LayerMask enemyLayer;       

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down; // Defaults to down
    
    // Input Actions
    private InputAction moveAction;
    private InputAction killAction;

    // Public accessors
    public State CurrentState => currentState;
    public Vector2 MoveInput => moveInput;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>(); // Auto-find animator
        
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // --- 1. SETUP INPUTS ---
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Kill Button (Spacebar)
        killAction = new InputAction("Kill", InputActionType.Button);
        killAction.AddBinding("<Keyboard>/space");
        killAction.performed += ctx => TryStealthKill();
    }

    void OnEnable()
    {
        moveAction.Enable();
        killAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        killAction.Disable();
    }

    void Update()
    {
        if (currentState == State.Normal) HandleInput();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (currentState == State.Normal)
        {
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleInput()
    {
        moveInput = moveAction.ReadValue<Vector2>().normalized;

        // Keep track of last direction for Idle states
        if (moveInput.sqrMagnitude > 0.01f)
            lastMoveDirection = moveInput;
    }

    // --- 2. STEALTH KILL LOGIC ---
    private void TryStealthKill()
    {
        if (currentState != State.Normal) return; 

        if (!isHidden) return; // Must be in blind spot/hidden

        Collider2D hit = Physics2D.OverlapCircle(transform.position, killRange, enemyLayer);

        if (hit != null)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            
            if (enemy != null)
            {
                if (enemy.CanSeePlayer())
                {
                    Debug.Log("Cannot Kill! Enemy sees you!");
                    return; 
                }

                StartCoroutine(PerformDashKill(enemy));
            }
        }
    }

    private IEnumerator PerformDashKill(EnemyBase targetEnemy)
    {
        // 1. Lock Player
        currentState = State.Attacking;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero; 

        // 2. Face the Enemy immediately
        Vector2 dirToEnemy = (targetEnemy.transform.position - transform.position).normalized;
        animator.SetFloat("Horizontal", dirToEnemy.x);
        animator.SetFloat("Vertical", dirToEnemy.y);

        // 3. Play Attack Animation
        animator.SetTrigger("Attack"); 
        // 

        // 4. Dash to Enemy (Visual feel)
        Vector2 startPos = rb.position;
        Vector2 targetPos = targetEnemy.transform.position; 
        
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.MovePosition(Vector2.Lerp(startPos, targetPos, elapsed / dashDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.MovePosition(targetPos); 

        // 5. Kill Enemy
        Debug.Log("Stealth Kill Executed!");
        targetEnemy.Die();
    }

    // --- 3. ANIMATION SYNC (Updated for V9 Generator) ---
    void UpdateAnimation()
    {
        if (animator == null) return;

        // If we are attacking, don't override with Walk/Idle triggers
        if (currentState == State.Attacking) return;

        // 1. Send Direction to Blend Tree
        if (moveInput.sqrMagnitude > 0.01f)
        {
            animator.SetBool("IsWalking", true);
            animator.SetFloat("Horizontal", moveInput.x);
            animator.SetFloat("Vertical", moveInput.y);
        }
        else
        {
            // When stopped, keep facing the last direction
            animator.SetBool("IsWalking", false);
            animator.SetFloat("LastHori", lastMoveDirection.x);
            animator.SetFloat("LastVert", lastMoveDirection.y);
        }
    }

    // --- 4. PUBLIC HELPERS ---
    public void LockMovement()
    {
        currentState = State.Locked;
        moveInput = Vector2.zero;
        animator.SetTrigger("Idle");
    }

    public void UnlockMovement()
    {
        currentState = State.Normal;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRange);
    }
}