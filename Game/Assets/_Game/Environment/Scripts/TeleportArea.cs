using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportArea : MonoBehaviour
{
    [Header("Teleport Setting")]
    public Transform destinationPoint;
    public Key interactKey = Key.E;

    [Header("---- (Visual) ----")]
    public GameObject interactPrompt;

    private bool isPlayerInRange = false;
    private GameObject playerRef;

    private void Start()
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerInRange && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        if (destinationPoint == null || playerRef == null) return;
        if (interactPrompt != null) interactPrompt.SetActive(false);

        Rigidbody2D rb = playerRef.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        playerRef.transform.position = destinationPoint.position;
        playerRef = null;
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerRef = other.gameObject;

            if (interactPrompt != null) interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerRef = null;

            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }
}