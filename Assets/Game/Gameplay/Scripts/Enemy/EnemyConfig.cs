using UnityEngine;

[CreateAssetMenu(menuName = "Config/Enemy/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public float moveSpeed  = 2f;
    public float maxHp      = 3f;
    public float bodyRadius = 0.5f;
    public int   xpReward   = 10;
    public Color color      = Color.red;
}
