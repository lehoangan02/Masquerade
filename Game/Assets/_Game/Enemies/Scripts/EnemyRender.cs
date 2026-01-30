using UnityEngine;

public class EnemyRender : MonoBehaviour
{
    public Transform Player;
    private SpriteRenderer m_SpriteRenderer;
    
    void Start()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        // m_SpriteRenderer.enabled = true;
    }

    void Update()
    {
        Vector3 directionToPlayer = Player.position - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, directionToPlayer.magnitude, LayerMask.GetMask("Default"));

        if (hit.collider == null)
        {
            Debug.Log("No hit");
            m_SpriteRenderer.enabled = true;
        }
        else
        {
            Debug.Log("Hit: " + hit.collider.name);
            m_SpriteRenderer.enabled = false;
        }
    }
}
