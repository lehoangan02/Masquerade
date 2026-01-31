using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform Player;
    private NavMeshAgent m_Agent;

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        m_Agent.updateRotation = false;
        m_Agent.updateUpAxis = false;    
    }

    void Update()
    {
        m_Agent.SetDestination(Player.position);
    }
}
