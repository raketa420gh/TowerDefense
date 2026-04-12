using UnityEngine;
using Zenject;

[RequireComponent(typeof(Enemy), typeof(EnemyMovement))]
public class EnemyBaseDamager : MonoBehaviour
{
    private Enemy _enemy;
    private EnemyMovement _movement;
    private PlayerBase _base;
    private bool _applied;

    [Inject]
    public void Construct(PlayerBase playerBase)
    {
        _base = playerBase;
    }

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _movement = GetComponent<EnemyMovement>();
    }

    private void Update()
    {
        if (_applied || !_movement.ReachedEnd) return;
        _applied = true;
        _base.ApplyDamage(_enemy.Config.BaseDamage);
        Destroy(gameObject);
    }
}
