using UnityEngine;

[CreateAssetMenu(menuName = "Config/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    public float moveSpeed          = 5f;
    public float rotationSpeed      = 720f;
    public float maxHp              = 100f;
    public float enemyContactDamage = 10f;
    public float damageCooldown     = 0.5f;
}
