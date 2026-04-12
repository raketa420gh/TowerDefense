using UnityEngine;

public static class AreaDamage
{
    private const int MaxOverlap = 32;
    private static readonly Collider[] Buffer = new Collider[MaxOverlap];

    public static void Apply(Vector3 center, ProjectileImpact impact)
    {
        var count = Physics.OverlapSphereNonAlloc(center, impact.SplashRadius, Buffer, impact.EnemyMask);
        for (int i = 0; i < count; i++)
        {
            var enemy = Buffer[i].GetComponentInParent<Enemy>();
            if (enemy == null || enemy.Health.IsDead) continue;

            enemy.Health.TakeDamage(impact.Damage);
            if (impact.SlowDuration > 0f)
                enemy.Slow.Apply(impact.SlowMultiplier, impact.SlowDuration);
        }
    }
}
