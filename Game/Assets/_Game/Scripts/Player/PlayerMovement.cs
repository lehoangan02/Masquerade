using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Define the States
    private enum State { Normal, Locked }
    private State currentState;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public Rigidbody2D rb;
    public Animator animator;
    private Vector2 moveInput;

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
                // Cutscene or Death state
                break;
        }
        
        // 2. Animation (Optional)
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
        // Basic Movement
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;

        // Visual Flip
        if (moveInput.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
    }

    void UpdateAnimation()
    {
        if (animator == null) return;
        
        animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }
}