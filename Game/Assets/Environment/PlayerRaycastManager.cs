using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerRaycastManager : MonoBehaviour
{
    public float rayDistance = 1.5f;
    public float hitRadius = 0.06f;
    public LayerMask hitMask;

    Vector2[] baseDirections =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2(1,1).normalized,
        new Vector2(-1,1).normalized,
        new Vector2(1,-1).normalized,
        new Vector2(-1,-1).normalized
    };

    RaycastHit2D[] hits;

    void Awake()
    {
        hits = new RaycastHit2D[8];
    }

    void Update()
    {
        float angle = transform.eulerAngles.z;

        for (int i = 0; i < baseDirections.Length; i++)
        {
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDirections[i];
            Vector2 origin = (Vector2)transform.position + dir * 0.02f;
            hits[i] = Physics2D.Raycast(origin, dir, rayDistance, hitMask);
        }
    }

    public bool HitTilemap(int index, out Tilemap tilemap, out Vector3Int cell)
    {
        tilemap = null;
        cell = Vector3Int.zero;

        if (index < 0 || index >= hits.Length)
            return false;

        if (hits[index].collider == null)
            return false;

        tilemap = hits[index].collider.GetComponentInParent<Tilemap>();

        if (tilemap == null)
            return false;

        cell = tilemap.WorldToCell(hits[index].point);
        return true;
    }

    void OnDrawGizmos()
    {
        if (baseDirections == null)
            return;

        float angle = transform.eulerAngles.z;

        for (int i = 0; i < baseDirections.Length; i++)
        {
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDirections[i];
            Vector2 origin = (Vector2)transform.position + dir * 0.02f;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + dir * rayDistance);

            if (hits != null && i < hits.Length && hits[i].collider != null)
            {
                Tilemap tm = hits[i].collider.GetComponentInParent<Tilemap>();
                Gizmos.color = tm != null ? Color.green : Color.yellow;
                Gizmos.DrawSphere(hits[i].point, hitRadius);
            }
        }
    }
}
