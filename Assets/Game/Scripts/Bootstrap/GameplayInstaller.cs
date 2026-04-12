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
        Container.Bind<Path>().FromInstance(_levelContext.Path).AsSingle();
        Container.Bind<PlayerBase>().FromInstance(_levelContext.PlayerBase).AsSingle();

        Container.Bind<Transform>().WithId("EnemyRoot")
            .FromInstance(_levelContext.EnemyRoot).AsCached();
        Container.Bind<Transform>().WithId("TowerRoot")
            .FromInstance(_levelContext.TowerRoot).AsCached();
        Container.Bind<Transform>().WithId("ProjectileRoot")
            .FromInstance(_levelContext.ProjectileRoot).AsCached();

        Container.Bind<ICoroutineRunner>().FromInstance(_coroutineRunner).AsSingle();

        Container.Bind<EnemyFactory>().AsSingle();
        Container.Bind<WaveSpawner>().AsSingle();

        Container.Bind<TowerCatalog>().FromInstance(_towerCatalog).AsSingle();
        Container.BindInterfacesAndSelfTo<Wallet>().AsSingle();
        Container.Bind<ProjectilePool>().AsSingle();
        Container.Bind<TowerFactory>().AsSingle();

        Container.Bind<BuildMenuView>().FromInstance(_levelContext.BuildMenu).AsSingle();
        Container.BindInterfacesAndSelfTo<BuildMenuPresenter>().AsSingle().NonLazy();
    }
}
