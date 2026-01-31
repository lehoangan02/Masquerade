using UnityEngine;
using UnityEngine.AI;

public class SlowArea : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float slowFactor = 0.5f; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.moveSpeed *= slowFactor;
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out Enemy_Angry enemyAngry))
            {
                enemyAngry.moveSpeed *= slowFactor;
            }
            else if (other.TryGetComponent(out Enemy_Lazy enemyLazy))
            {
                enemyLazy.moveSpeed *= slowFactor;
            }
            else if (other.TryGetComponent(out Enemy_Standard enemyStandard))
            {
                enemyStandard.moveSpeed *= slowFactor;
            }
            else
            {
                NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed *= slowFactor;
                }
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
                player.moveSpeed /= slowFactor;
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out Enemy_Angry enemyAngry))
            {
                enemyAngry.moveSpeed /= slowFactor;
            }
            else if (other.TryGetComponent(out Enemy_Lazy enemyLazy))
            {
                enemyLazy.moveSpeed /= slowFactor;
            }
            else if (other.TryGetComponent(out Enemy_Standard enemyStandard))
            {
                enemyStandard.moveSpeed /= slowFactor;
            }
            else
            {
                NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed /= slowFactor;
                }
            }
        }
    }
}