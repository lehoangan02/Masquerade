using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main player controller handling movement, state management, and animation.
/// Uses PlayerInput for input handling (modular design).
/// Attach to Player prefab in _Game/Prefabs/Player/
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    // Define the States
    public enum State { Normal, Locked, Dashing }
    [SerializeField] private State currentState;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public Rigidbody2D rb;
    [Tooltip("Optional")]
    [SerializeField] private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down; // Default facing down

    // Public accessors
    public State CurrentState => currentState;
    public Vector2 LastMoveDirection => lastMoveDirection;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

    void Awake()
    {
        // Auto-get components if not assigned
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();

        // Configure Rigidbody2D for top-down movement
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        currentState = State.Normal;
    }

    void Update()
    {
        // 1. Check Inputs based on State
        switch (currentState)
        {
            case State.Normal:
                HandleInput();
                break;

            case State.Locked:
                // Cutscene, dialogue, or death state - no input
                moveInput = Vector2.zero;
                break;

            case State.Dashing:
                // Dashing state - handled elsewhere
                break;
        }
        
        // 2. Animation (Optional - ready for when sprites are added)
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (currentState == State.Normal)
        {
            // Physics-based movement for proper collision
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleInput()
    {
        // Get input from PlayerInput component (or fallback to direct input)
        if (playerInput != null)
        {
            moveInput = playerInput.MoveInput;
        }
        else
        {
            // Fallback: Direct WASD input using new Input System
            Vector2 rawInput = Vector2.zero;
            if (Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;
                if (Keyboard.current.wKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed) y -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                rawInput = new Vector2(x, y);
            }
            moveInput = rawInput.normalized;
        }

        // Store last move direction for animations/attacks
        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveInput.normalized;
        }

        // Visual Flip (horizontal only for side-view sprites)
        // Remove or modify this for true top-down with directional sprites
        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
        }
    }

    void UpdateAnimation()
    {
        if (animator == null) return;
        
        // Basic animation parameters (expand when sprites are ready)
        animator.SetFloat("Speed", moveInput.sqrMagnitude);
        animator.SetFloat("Horizontal", lastMoveDirection.x);
        animator.SetFloat("Vertical", lastMoveDirection.y);
    }

    /// <summary>
    /// Lock player movement (for cutscenes, dialogue, etc.)
    /// </summary>
    public void LockMovement()
    {
        currentState = State.Locked;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Unlock player movement.
    /// </summary>
    public void UnlockMovement()
    {
        currentState = State.Normal;
    }

    /// <summary>
    /// Set movement speed (for buffs/debuffs).
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    /// <summary>
    /// Get current movement speed.
    /// </summary>
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }
}