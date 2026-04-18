using MagicStaff;
using UnityEngine;
using Zenject;

namespace MagicStaff.MainMenu
{
    public class MainMenuInstaller : MonoInstaller
    {
        [SerializeField]
        private MainMenuView _menuView;

        [SerializeField]
        private StaffModificationView _staffView;

        [SerializeField]
        private PartPickerView _pickerView;

        [SerializeField]
        private CoinCounterView _coinCounterView;

        public override void InstallBindings()
        {
            Container.BindInstance(_menuView);
            Container.BindInstance(_staffView);
            Container.BindInstance(_pickerView);
            Container.BindInstance(_coinCounterView);

            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<MainMenuController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<StaffModificationController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<CoinCounterController>().AsSingle().NonLazy();
        }
    }
}
