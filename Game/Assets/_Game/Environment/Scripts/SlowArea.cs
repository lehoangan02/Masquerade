using UnityEngine;
using UnityEngine.AI;

public class SlowArea : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float slowFactor = 0.5f; 

    private float originalSpeed;
    private PlayerController player;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();
            if (player != null)
            {

                originalSpeed = player.moveSpeed;
                player.moveSpeed *= slowFactor;
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                originalSpeed = agent.speed;
                agent.speed *= slowFactor;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && player != null)
        {
            player.moveSpeed = originalSpeed;
            player = null;
        }
        else if (other.CompareTag("Enemy"))
        {
            NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = originalSpeed;
            }
        }
    }
}