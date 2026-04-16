using UnityEngine;
using Zenject;

public class GameplayInstaller : MonoInstaller
{
    [SerializeField]
    MovementComponent _movement;

    [SerializeField]
    GameplayHudView _hudView;

    [SerializeField]
    PlayerConfig _playerConfig;

    [SerializeField]
    EnemyPool _enemyPool;

    [SerializeField]
    EnemySpawner _enemySpawner;

    [SerializeField]
    WaveConfig _waveConfig;

    [SerializeField]
    EnemyConfig _enemyConfig;

    [SerializeField]
    StaffFloatingBehaviour _staffFloating;

    [SerializeField]
    StaffCombat _staffCombat;

    [SerializeField]
    ProjectilePool _projectilePool;

    [SerializeField]
    StaffCombatConfig _staffCombatConfig;

    public override void InstallBindings()
    {
        Container.BindInstance(_playerConfig);
        Container.BindInstance(_movement);
        Container.BindInstance(_hudView);

        Container.BindInterfacesAndSelfTo<PlayerController>().AsSingle().NonLazy();

        Container.BindInstance(_waveConfig);
        Container.BindInstance(_enemyConfig);
        Container.BindInstance(_enemyPool).AsSingle();
        Container.QueueForInject(_enemySpawner);

        Container.BindInstance(_staffCombatConfig);
        Container.BindInstance(_projectilePool).AsSingle();
        Container.BindInstance(_movement.transform).WithId("PlayerTransform");
        Container.QueueForInject(_staffFloating);
        Container.QueueForInject(_staffCombat);
    }
}
