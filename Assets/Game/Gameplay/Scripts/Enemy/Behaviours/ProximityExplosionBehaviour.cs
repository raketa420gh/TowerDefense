using UnityEngine;

public class ProximityExplosionBehaviour : EnemyBehaviourBase
{
    [SerializeField]
    private ExplosionConfig _config;

    private float _countdown = -1f;

    public override void OnActivated()   => _countdown = -1f;
    public override void OnDeactivated() => _countdown = -1f;

    private void Update()
    {
        if (_config == null || Ctx.Target == null) return;

        if (_countdown > 0f)
        {
            _countdown -= Time.deltaTime;
            if (_countdown <= 0f)
                Explode();
            return;
        }

        float dist = Vector3.Distance(transform.position, Ctx.Target.position);
        if (dist <= _config.triggerRadius)
            _countdown = _config.countdownDuration;
    }

    private void Explode()
    {
        float distToPlayer = Vector3.Distance(transform.position, Ctx.Target.position);
        if (distToPlayer <= _config.blastRadius)
            Ctx.PlayerHealth?.TakeDamage(_config.damage);

        // Kill self through the health system — triggers OnKilled + XP reward
        GetComponent<HealthComponent>().TakeDamage(float.MaxValue);
    }
}
