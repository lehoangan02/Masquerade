using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main player controller - WASD movement for top-down 2D.
/// Requires: Rigidbody2D, BoxCollider2D
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    public enum State { Normal, Locked }
    
    [Header("State")]
    [SerializeField] private State currentState = State.Normal;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    
    // Input Action
    private InputAction moveAction;

    // Public accessors
    public State CurrentState => currentState;
    public Vector2 MoveInput => moveInput;
    public Vector2 LastMoveDirection => lastMoveDirection;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        
        // Configure for top-down
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Setup Input Action for WASD + Arrow Keys
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
    }

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Normal:
                HandleInput();
                break;
            case State.Locked:
                moveInput = Vector2.zero;
                break;
        }
        
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
        // WASD Movement (New Input System)
        moveInput = moveAction.ReadValue<Vector2>().normalized;

        // Track last direction for animations/attacks
        if (moveInput.sqrMagnitude > 0.01f)
            lastMoveDirection = moveInput;

        // Horizontal flip
        if (moveInput.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
    }

    void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }

    public void LockMovement()
    {
        currentState = State.Locked;
        moveInput = Vector2.zero;
    }

    public void UnlockMovement()
    {
        currentState = State.Normal;
    }
}
