using System;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public event Action<int> OnKilled;

    [SerializeField]
    private HealthComponent _health;

    private EnemyConfig       _config;
    private EnemyPool         _pool;
    private IEnemyBehaviour[] _behaviours;
    private Renderer          _renderer;

    private void Awake()
    {
        _behaviours = GetComponents<IEnemyBehaviour>();
        _renderer   = GetComponentInChildren<Renderer>();
        if (_renderer != null) _ = _renderer.material; // pre-create instanced material
    }

    public void Initialize(EnemyConfig config, Transform target, EnemyPool pool,
                           IPlayerHealthService playerHealth, EnemyProjectilePool projectilePool = null)
    {
        _config = config;
        _pool   = pool;

        _health.OnDied -= HandleDied;
        _health.Initialize(config.maxHp);
        _health.OnDied += HandleDied;

        var ctx = new EnemyBehaviourContext
        {
            Config         = config,
            Target         = target,
            Rb             = GetComponent<Rigidbody>(),
            OwnerPool      = pool,
            PlayerHealth   = playerHealth,
            ProjectilePool = projectilePool
        };

        foreach (var b in _behaviours)
            b.Initialize(ctx);

        foreach (var b in _behaviours)
            b.OnActivated();

        if (_renderer != null)
            _renderer.material.color = config.color;
    }

    private void HandleDied()
    {
        _health.OnDied -= HandleDied;

        foreach (var b in _behaviours)
            b.OnDeactivated();

        OnKilled?.Invoke(_config.xpReward);
        _pool.Return(this);
    }
}
