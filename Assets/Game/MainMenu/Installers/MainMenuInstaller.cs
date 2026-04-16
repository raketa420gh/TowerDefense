using UnityEngine;
using Zenject;

namespace MagicStaff.MainMenu
{
    public class MainMenuInstaller : MonoInstaller
    {
        [SerializeField]
        MainMenuView _menuView;
        [SerializeField]
        StaffModificationView _staffView;

        public override void InstallBindings()
        {
            Container.BindInstance(_menuView);
            Container.BindInstance(_staffView);

            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<MainMenuController>().AsSingle().NonLazy();
        }
    }
}
