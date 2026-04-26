using UnityEngine;

public class RangedAttackBehaviour : EnemyBehaviourBase
{
    [SerializeField]
    private RangedAttackConfig _config;

    private ChaseMovementBehaviour _chase;
    private AttackState            _state;
    private float                  _timer;

    private enum AttackState { Inactive, Aiming, Firing }

    public override void Initialize(EnemyBehaviourContext ctx)
    {
        base.Initialize(ctx);
        _chase = GetComponent<ChaseMovementBehaviour>();
    }

    public override void OnActivated()   => _state = AttackState.Inactive;
    public override void OnDeactivated() => _state = AttackState.Inactive;

    private void Update()
    {
        if (Ctx.Target == null || _config == null) return;

        float dist = Vector3.Distance(transform.position, Ctx.Target.position);

        switch (_state)
        {
            case AttackState.Inactive:
                if (dist <= _config.attackRange)
                {
                    _chase?.PauseChase();
                    _state = AttackState.Aiming;
                    _timer = _config.aimDuration;
                }
                break;

            case AttackState.Aiming:
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _state = AttackState.Firing;
                    _timer = _config.fireInterval;
                    Fire();
                }
                break;

            case AttackState.Firing:
                if (dist > _config.attackRange)
                {
                    _chase?.ResumeChase();
                    _state = AttackState.Inactive;
                    break;
                }
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    Fire();
                    _timer = _config.fireInterval;
                }
                break;
        }
    }

    private void Fire()
    {
        if (Ctx.ProjectilePool == null) return;

        var dir = Ctx.Target.position - transform.position;
        dir.y = 0f;

        var projectile = Ctx.ProjectilePool.Get();
        projectile.transform.position = transform.position;
        projectile.Launch(dir, _config.projectileSpeed, _config.projectileDamage);
    }
}
