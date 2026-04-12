using UnityEngine;
using Zenject;

[CreateAssetMenu(menuName = "Installers/ProjectInstaller", fileName = "ProjectInstaller")]
public class ProjectInstaller : ScriptableObjectInstaller<ProjectInstaller>
{
    [SerializeField]
    private LevelCatalog _levelCatalog;

    public override void InstallBindings()
    {
        SignalBusInstaller.Install(Container);

        Container.Bind<LevelCatalog>().FromInstance(_levelCatalog).AsSingle();
        Container.Bind<PersistenceService>().AsSingle();
        Container.Bind<SceneLoader>().AsSingle();
        Container.Bind<PlayerProgress>().AsSingle();
        Container.Bind<GameLoopStateMachine>().AsSingle();

        Container.DeclareSignal<LevelStartRequestedSignal>().OptionalSubscriber();
        Container.DeclareSignal<LevelLoadedSignal>().OptionalSubscriber();

        Container.DeclareSignal<EnemySpawnedSignal>().OptionalSubscriber();
        Container.DeclareSignal<EnemyKilledSignal>().OptionalSubscriber();
        Container.DeclareSignal<EnemyReachedBaseSignal>().OptionalSubscriber();
        Container.DeclareSignal<BaseHealthChangedSignal>().OptionalSubscriber();
        Container.DeclareSignal<BaseDestroyedSignal>().OptionalSubscriber();
        Container.DeclareSignal<WaveStartedSignal>().OptionalSubscriber();
        Container.DeclareSignal<WaveCompletedSignal>().OptionalSubscriber();
        Container.DeclareSignal<AllWavesCompletedSignal>().OptionalSubscriber();
        Container.DeclareSignal<WaveBreakStartedSignal>().OptionalSubscriber();
        Container.DeclareSignal<LevelFailedSignal>().OptionalSubscriber();
        Container.DeclareSignal<LevelCompletedSignal>().OptionalSubscriber();
        Container.DeclareSignal<WaveEarlyStartRequestedSignal>().OptionalSubscriber();

        Container.DeclareSignal<GoldChangedSignal>().OptionalSubscriber();
        Container.DeclareSignal<TowerBuiltSignal>().OptionalSubscriber();
        Container.DeclareSignal<TowerSoldSignal>().OptionalSubscriber();
        Container.DeclareSignal<TowerUpgradedSignal>().OptionalSubscriber();
        Container.DeclareSignal<ProjectileHitSignal>().OptionalSubscriber();
        Container.DeclareSignal<PauseRequestedSignal>().OptionalSubscriber();
        Container.DeclareSignal<PauseResumedSignal>().OptionalSubscriber();

        var sfx = Resources.Load<SfxConfig>("SfxConfig");
        if (sfx != null)
            Container.Bind<SfxConfig>().FromInstance(sfx).AsSingle();
        Container.BindInterfacesAndSelfTo<AudioService>().AsSingle().NonLazy();
    }
}
