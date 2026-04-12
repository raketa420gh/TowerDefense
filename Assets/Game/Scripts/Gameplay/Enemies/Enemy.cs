using UnityEngine;

[RequireComponent(typeof(EnemyMovement), typeof(EnemyHealth), typeof(SlowEffect))]
public class Enemy : MonoBehaviour
{
    public EnemyConfig Config => _config;
    public EnemyMovement Movement => _movement;
    public EnemyHealth Health => _health;
    public SlowEffect Slow => _slow;

    private EnemyMovement _movement;
    private EnemyHealth _health;
    private SlowEffect _slow;
    private EnemyConfig _config;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<EnemyHealth>();
        _slow = GetComponent<SlowEffect>();
        _slow.Bind(_movement);
    }

    public void Init(EnemyConfig config, Path path)
    {
        _config = config;
        _health.Init(config.MaxHealth);
        _movement.Init(path, config.Speed);
        _slow.Reset();
    }
}
