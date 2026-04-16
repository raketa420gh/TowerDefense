using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField]
    HealthComponent _health;

    EnemyConfig _config;
    Transform   _target;
    Rigidbody   _rb;

    void Awake() => _rb = GetComponent<Rigidbody>();

    public void Initialize(EnemyConfig config, Transform target)
    {
        _config = config;
        _target = target;
        _health.Initialize(config.maxHp);
        _health.OnDied += ReturnToPool;
    }

    void FixedUpdate()
    {
        if (_config == null || _target == null) return;

        var dir = (_target.position - _rb.position);
        dir.y = 0f;
        dir.Normalize();

        _rb.MovePosition(_rb.position + dir * _config.moveSpeed * Time.fixedDeltaTime);

        if (dir.sqrMagnitude > 0.01f)
            _rb.rotation = Quaternion.LookRotation(dir);
    }

    void ReturnToPool()
    {
        _health.OnDied -= ReturnToPool;
        gameObject.SetActive(false);
    }
}
