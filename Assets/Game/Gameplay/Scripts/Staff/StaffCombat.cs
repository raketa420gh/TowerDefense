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
    private Transform          _player;

    private float _fireTimer;

    private void Awake()
        => _player = transform.parent;

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
        var center  = _player.position;
        var hits    = Physics.OverlapSphere(center, _config.detectionRadius, LayerMask.GetMask("Enemy"));
        Transform closest = null;
        var       minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var d = (hit.transform.position - center).sqrMagnitude;
            if (d < minDist) { minDist = d; closest = hit.transform; }
        }
        return closest;
    }

    private void Shoot(Transform target)
    {
        var muzzle    = _muzzlePoint != null ? _muzzlePoint.position : transform.position;
        var shootY    = _player.position.y + _config.projectileHeight;
        var spawnPos  = new Vector3(muzzle.x, shootY, muzzle.z);
        var targetPos = new Vector3(target.position.x, shootY, target.position.z);
        var dir       = (targetPos - spawnPos).normalized;
        LaunchProjectile(spawnPos, dir);
        if (UnityEngine.Random.value < _stats.GetBonus(StatType.DoubleShot))
            LaunchProjectile(spawnPos, dir);
    }

    private void LaunchProjectile(Vector3 spawnPos, Vector3 dir)
    {
        var projectile = _projectilePool.Get();
        projectile.transform.position = spawnPos;
        projectile.Launch(dir, _config.projectileSpeed, EffectiveDamage);
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;
        var center = transform.parent != null ? transform.parent.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, _config.detectionRadius);
    }
}
