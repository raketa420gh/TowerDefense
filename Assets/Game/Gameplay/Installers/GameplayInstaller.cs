using MagicStaff;
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
    private EnemySpawner _enemySpawner;

    [SerializeField]
    private WaveConfig _waveConfig;

    [SerializeField]
    private EnemyProjectilePool _enemyProjectilePool;

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

    [SerializeField]
    private HpBarView _hpBarView;

    [SerializeField]
    private DefeatView _defeatView;

    [SerializeField]
    private PlayerHitReceiver _playerHitReceiver;

    public override void InstallBindings()
    {
        Container.BindInstance(_playerConfig);
        Container.BindInstance(_movement);
        Container.BindInstance(_hudView);

        Container.BindInterfacesAndSelfTo<PlayerController>().AsSingle().NonLazy();

        Container.BindInstance(_waveConfig);
        Container.BindInstance(_enemyProjectilePool).AsSingle();
        Container.QueueForInject(_enemyProjectilePool);
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

        Container.BindInstance(_hpBarView);
        Container.BindInstance(_defeatView);
        Container.QueueForInject(_playerHitReceiver);

        Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
        Container.BindInterfacesAndSelfTo<PlayerHealthService>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<HpHudController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<DefeatController>().AsSingle().NonLazy();
    }
}
