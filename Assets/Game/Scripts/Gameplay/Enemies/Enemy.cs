using UnityEngine;

[RequireComponent(typeof(EnemyMovement), typeof(EnemyHealth))]
public class Enemy : MonoBehaviour
{
    public EnemyConfig Config => _config;
    public EnemyMovement Movement => _movement;
    public EnemyHealth Health => _health;

    private EnemyMovement _movement;
    private EnemyHealth _health;
    private EnemyConfig _config;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<EnemyHealth>();
    }

    public void Init(EnemyConfig config, Path path)
    {
        _config = config;
        _health.Init(config.MaxHealth);
        _movement.Init(path, config.Speed);
    }
}
