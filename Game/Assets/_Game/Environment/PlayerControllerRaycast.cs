using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerRaycast : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float raycastDistance = 0.5f;
    [SerializeField] private LayerMask collisionLayer;

    void Start()
    {
        
    }

    void Update()
    {
        Vector2 moveDirection = Vector2.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            moveDirection += Vector2.up;
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            moveDirection += Vector2.down;
        }
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveDirection += Vector2.left;
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveDirection += Vector2.right;
        }

        // Normalize for consistent diagonal speed
        if (moveDirection != Vector2.zero)
        {
            moveDirection.Normalize();
            TryMove(moveDirection);
        }

        // Get mouse position and rotate to face it
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 direction = new Vector2(
            mousePos.x - transform.position.x,
            mousePos.y - transform.position.y
        );
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle - 90f));
    }

    private void TryMove(Vector2 direction)
    {
        Vector2 movement = direction * moveSpeed * Time.deltaTime;

        // Raycast to check for tilemap collider
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, raycastDistance, collisionLayer);

        if (hit.collider == null)
        {
            // No collision, move freely
            transform.Translate(movement, Space.World);
        }
        else
        {
            // Try sliding along the wall
            // Check horizontal movement
            if (direction.x != 0)
            {
                RaycastHit2D hitX = Physics2D.Raycast(transform.position, new Vector2(direction.x, 0), raycastDistance, collisionLayer);
                if (hitX.collider == null)
                {
                    transform.Translate(new Vector2(direction.x, 0) * moveSpeed * Time.deltaTime, Space.World);
                }
            }

            // Check vertical movement
            if (direction.y != 0)
            {
                RaycastHit2D hitY = Physics2D.Raycast(transform.position, new Vector2(0, direction.y), raycastDistance, collisionLayer);
                if (hitY.collider == null)
                {
                    transform.Translate(new Vector2(0, direction.y) * moveSpeed * Time.deltaTime, Space.World);
                }
            }
        }
    }
}
