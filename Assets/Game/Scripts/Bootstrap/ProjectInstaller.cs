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

        Container.DeclareSignal<LevelStartRequestedSignal>();
        Container.DeclareSignal<LevelLoadedSignal>();

        Container.DeclareSignal<EnemySpawnedSignal>();
        Container.DeclareSignal<EnemyKilledSignal>();
        Container.DeclareSignal<EnemyReachedBaseSignal>();
        Container.DeclareSignal<BaseHealthChangedSignal>();
        Container.DeclareSignal<BaseDestroyedSignal>();
        Container.DeclareSignal<WaveStartedSignal>();
        Container.DeclareSignal<WaveCompletedSignal>();
        Container.DeclareSignal<AllWavesCompletedSignal>();
        Container.DeclareSignal<WaveBreakStartedSignal>();
        Container.DeclareSignal<LevelFailedSignal>();
        Container.DeclareSignal<LevelCompletedSignal>();
        Container.DeclareSignal<WaveEarlyStartRequestedSignal>();

        Container.DeclareSignal<GoldChangedSignal>();
        Container.DeclareSignal<TowerBuiltSignal>();
        Container.DeclareSignal<TowerSoldSignal>();
        Container.DeclareSignal<TowerUpgradedSignal>();
        Container.DeclareSignal<ProjectileHitSignal>();
    }
}
