using MagicStaff.Staff;
using UnityEngine;
using Zenject;

public class StaffCombat : MonoBehaviour
{
    [SerializeField]
    private Transform _muzzlePoint;

    private StaffCombatConfig  _config;
    private ProjectilePool     _projectilePool;
    private PlayerStatsService _stats;

    private float _fireTimer;

    [Inject]
    public void Construct(StaffCombatConfig config, ProjectilePool projectilePool, PlayerStatsService stats)
    {
        _config         = config;
        _projectilePool = projectilePool;
        _stats          = stats;
    }

    private float EffectiveFireRate
        => _config.fireRate * (1f + _stats.GetBonus(StatType.AttackSpeed));

    private float EffectiveDamage
        => _config.projectileDamage * (1f + _stats.GetBonus(StatType.Damage));

    private void Update()
    {
        _fireTimer += Time.deltaTime;
        if (_fireTimer < 1f / EffectiveFireRate) return;

        var target = FindClosestEnemy();
        if (target == null) return;

        _fireTimer = 0f;
        Shoot(target);
    }

    private Transform FindClosestEnemy()
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

    private void Shoot(Transform target)
    {
        var spawnPos   = _muzzlePoint != null ? _muzzlePoint.position : transform.position;
        var dir        = (target.position - spawnPos).normalized;
        var projectile = _projectilePool.Get();
        projectile.transform.position = spawnPos;
        projectile.Launch(dir, _config.projectileSpeed, EffectiveDamage);
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _config.detectionRadius);
    }
}
