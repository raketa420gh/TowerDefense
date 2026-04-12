using UnityEngine;
using Zenject;

public class MenuInstaller : MonoInstaller
{
    [SerializeField]
    private MainMenuView _mainMenuView;

    [SerializeField]
    private LevelSelectView _levelSelectView;

    public override void InstallBindings()
    {
        Container.Bind<MainMenuView>().FromInstance(_mainMenuView).AsSingle();
        Container.Bind<LevelSelectView>().FromInstance(_levelSelectView).AsSingle();
        Container.BindInterfacesAndSelfTo<MainMenuPresenter>().AsSingle().NonLazy();
    }
}
