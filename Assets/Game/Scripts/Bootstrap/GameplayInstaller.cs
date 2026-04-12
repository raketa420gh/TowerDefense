using UnityEngine;
using Zenject;

public class GameplayInstaller : MonoInstaller
{
    [SerializeField]
    private LevelContext _levelContext;

    [SerializeField]
    private CoroutineRunner _coroutineRunner;

    [SerializeField]
    private TowerCatalog _towerCatalog;

    public override void InstallBindings()
    {
        Container.Bind<LevelContext>().FromInstance(_levelContext).AsSingle();
        Container.Bind<PlayerBase>().FromInstance(_levelContext.PlayerBase).AsSingle();

        Container.Bind<Transform>().WithId("EnemyRoot")
            .FromInstance(_levelContext.EnemyRoot).AsCached();
        Container.Bind<Transform>().WithId("TowerRoot")
            .FromInstance(_levelContext.TowerRoot).AsCached();
        Container.Bind<Transform>().WithId("ProjectileRoot")
            .FromInstance(_levelContext.ProjectileRoot).AsCached();

        Container.Bind<ICoroutineRunner>().FromInstance(_coroutineRunner).AsSingle();

        Container.Bind<EnemyFactory>().AsSingle();
        Container.BindInterfacesAndSelfTo<WaveSpawner>().AsSingle();

        Container.Bind<TowerCatalog>().FromInstance(_towerCatalog).AsSingle();
        Container.BindInterfacesAndSelfTo<Wallet>().AsSingle();
        Container.Bind<ProjectilePool>().AsSingle();
        Container.Bind<TowerFactory>().AsSingle();

        Container.Bind<BuildMenuView>().FromInstance(_levelContext.BuildMenu).AsSingle();
        Container.Bind<HudView>().FromInstance(_levelContext.Hud).AsSingle();
        Container.Bind<LevelCompleteView>().FromInstance(_levelContext.CompleteView).AsSingle();
        Container.Bind<LevelFailedView>().FromInstance(_levelContext.FailedView).AsSingle();
        Container.Bind<TowerInfoView>().FromInstance(_levelContext.TowerInfoView).AsSingle();
        if (_levelContext.PauseView != null)
            Container.Bind<PauseView>().FromInstance(_levelContext.PauseView).AsSingle();
        Container.Bind<TowerUpgradeService>().AsSingle();

        Container.BindInterfacesAndSelfTo<RewardService>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<LevelResultService>().AsSingle().NonLazy();

        Container.BindInterfacesAndSelfTo<BuildMenuPresenter>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<HudPresenter>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<LevelCompletePresenter>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<LevelFailedPresenter>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<TowerInfoPresenter>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<PausePresenter>().AsSingle().NonLazy();

        Container.BindInterfacesAndSelfTo<InputReader>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<WorldTapRouter>().AsSingle().NonLazy();
    }
}
