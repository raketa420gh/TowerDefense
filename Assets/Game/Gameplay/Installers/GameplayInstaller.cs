using UnityEngine;
using Zenject;

public class GameplayInstaller : MonoInstaller
{
    [SerializeField]
    private MovementComponent _movement;

    [SerializeField]
    private GameplayHudView _hudView;

    [SerializeField]
    private PlayerConfig _playerConfig;

    [SerializeField]
    private EnemyPool _enemyPool;

    [SerializeField]
    private EnemySpawner _enemySpawner;

    [SerializeField]
    private WaveConfig _waveConfig;

    [SerializeField]
    private EnemyConfig _enemyConfig;

    [SerializeField]
    private StaffFloatingBehaviour _staffFloating;

    [SerializeField]
    private StaffCombat _staffCombat;

    [SerializeField]
    private ProjectilePool _projectilePool;

    [SerializeField]
    private StaffCombatConfig _staffCombatConfig;

    [SerializeField]
    private ExperienceConfig _experienceConfig;

    [SerializeField]
    private UpgradeLibraryConfig _upgradeLibraryConfig;

    [SerializeField]
    private ExperienceBarView _experienceBarView;

    [SerializeField]
    private UpgradeSelectionView _upgradeSelectionView;

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

        Container.BindInstance(_experienceConfig);
        Container.BindInstance(_upgradeLibraryConfig);
        Container.BindInstance(_experienceBarView);
        Container.BindInstance(_upgradeSelectionView);

        Container.BindInterfacesAndSelfTo<ExperienceService>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<UpgradeService>().AsSingle().NonLazy();
        Container.Bind<PlayerStatsService>().AsSingle();
        Container.BindInterfacesAndSelfTo<ExperienceHudController>().AsSingle().NonLazy();
    }
}
