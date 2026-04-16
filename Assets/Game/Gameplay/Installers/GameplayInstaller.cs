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
    }
}
