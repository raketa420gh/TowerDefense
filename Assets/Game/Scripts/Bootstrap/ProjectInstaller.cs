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
    }
}
