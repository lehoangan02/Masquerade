using UnityEngine;

public class BushArea : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.isHidden = true;
                other.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.isHidden = false;
                other.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }
}