using UnityEngine;

[CreateAssetMenu(menuName = "Config/Enemy/RangedAttackConfig")]
public class RangedAttackConfig : ScriptableObject
{
    public float attackRange    = 8f;
    public float aimDuration    = 1.5f;
    public float fireInterval   = 1f;
    public float projectileSpeed = 10f;
    public float projectileDamage = 5f;
}
