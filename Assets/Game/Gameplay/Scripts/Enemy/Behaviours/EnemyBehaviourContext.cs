using UnityEngine;

public struct EnemyBehaviourContext
{
    public EnemyConfig          Config;
    public Transform            Target;
    public Rigidbody            Rb;
    public EnemyPool            OwnerPool;
    public IPlayerHealthService PlayerHealth;
    public EnemyProjectilePool  ProjectilePool;
}
