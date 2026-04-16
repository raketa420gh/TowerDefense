using UnityEngine;
using Zenject;

public class StaffCombat : MonoBehaviour
{
    StaffCombatConfig _config;
    ProjectilePool    _projectilePool;

    float _fireTimer;

    [Inject]
    public void Construct(StaffCombatConfig config, ProjectilePool projectilePool)
    {
        _config         = config;
        _projectilePool = projectilePool;
    }

    void Update()
    {
        _fireTimer += Time.deltaTime;
        if (_fireTimer < 1f / _config.fireRate) return;

        var target = FindClosestEnemy();
        if (target == null) return;

        _fireTimer = 0f;
        Shoot(target);
    }

    Transform FindClosestEnemy()
    {
        var hits     = Physics.OverlapSphere(transform.position, _config.detectionRadius, LayerMask.GetMask("Enemy"));
        Transform closest = null;
        var       minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var d = (hit.transform.position - transform.position).sqrMagnitude;
            if (d < minDist) { minDist = d; closest = hit.transform; }
        }
        return closest;
    }

    void Shoot(Transform target)
    {
        var dir        = (target.position - transform.position).normalized;
        var projectile = _projectilePool.Get();
        projectile.transform.position = transform.position + Vector3.up * 0.5f;
        projectile.Launch(dir, _config.projectileSpeed, _config.projectileDamage);
    }

    void OnDrawGizmosSelected()
    {
        if (_config == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _config.detectionRadius);
    }
}
