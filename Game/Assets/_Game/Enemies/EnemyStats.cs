using UnityEngine;

// Right-click in Project window -> Create -> Enemy Stats
[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Enemy/Stats")]
public class EnemyStats : ScriptableObject
{
    public string enemyName;
    public EnemyType type;
    public float moveSpeed;
    public float visionRange;
    public Color skinColor; // Just to visualize the change for now
}

public enum EnemyType
{
    Base,
    Angry,
    Lazy
}