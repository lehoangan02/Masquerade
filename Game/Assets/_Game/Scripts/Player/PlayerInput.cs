using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all player input using the new Input System.
/// Separated from movement for cleaner code.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    // Movement input vector (normalized)
    public Vector2 MoveInput { get; private set; }
    
    // Action inputs (generic buttons for future use)
    public bool PrimaryActionPressed { get; private set; }   // Left Mouse
    public bool SecondaryActionPressed { get; private set; } // Space
    public bool InteractPressed { get; private set; }        // E

    // Input Actions
    private InputAction moveAction;
    private InputAction primaryAction;
    private InputAction secondaryAction;
    private InputAction interactAction;

    void Awake()
    {
        // Create input actions for WASD/Arrow movement
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

        // Action buttons (generic for future use)
        primaryAction = new InputAction("PrimaryAction", InputActionType.Button, "<Mouse>/leftButton");
        secondaryAction = new InputAction("SecondaryAction", InputActionType.Button, "<Keyboard>/space");
        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
    }

    void OnEnable()
    {
        moveAction.Enable();
        primaryAction.Enable();
        secondaryAction.Enable();
        interactAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        primaryAction.Disable();
        secondaryAction.Disable();
        interactAction.Disable();
    }

    void Update()
    {
        // Movement (WASD / Arrow Keys)
        Vector2 rawInput = moveAction.ReadValue<Vector2>();
        MoveInput = rawInput.normalized;

        // Actions (generic buttons)
        PrimaryActionPressed = primaryAction.WasPressedThisFrame();
        SecondaryActionPressed = secondaryAction.WasPressedThisFrame();
        InteractPressed = interactAction.WasPressedThisFrame();
    }

    /// <summary>
    /// Disable all input (useful for cutscenes, menus, death).
    /// </summary>
    public void DisableInput()
    {
        MoveInput = Vector2.zero;
        PrimaryActionPressed = false;
        SecondaryActionPressed = false;
        InteractPressed = false;
        enabled = false;
    }

    /// <summary>
    /// Re-enable input.
    /// </summary>
    public void EnableInput()
    {
        enabled = true;
    }
}
