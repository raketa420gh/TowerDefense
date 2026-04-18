using System;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public event Action<int> OnKilled;

    [SerializeField]
    private HealthComponent _health;

    private EnemyConfig _config;
    private Transform   _target;
    private EnemyPool   _pool;
    private Rigidbody   _rb;

    private void Awake() => _rb = GetComponent<Rigidbody>();

    public void Initialize(EnemyConfig config, Transform target, EnemyPool pool)
    {
        _config = config;
        _target = target;
        _pool   = pool;
        _health.Initialize(config.maxHp);
        _health.OnDied += HandleDied;
    }

    private void FixedUpdate()
    {
        if (_config == null || _target == null) return;

        var dir = (_target.position - _rb.position);
        dir.y = 0f;
        dir.Normalize();

        _rb.MovePosition(_rb.position + dir * _config.moveSpeed * Time.fixedDeltaTime);

        if (dir.sqrMagnitude > 0.01f)
            _rb.rotation = Quaternion.LookRotation(dir);
    }

    private void HandleDied()
    {
        _health.OnDied -= HandleDied;
        OnKilled?.Invoke(_config.xpReward);
        _pool.Return(this);
    }
}
