using System;
using Zenject;

namespace MagicStaff.MainMenu
{
    public class MainMenuController : IInitializable, IDisposable
    {
        readonly MainMenuView _menuView;
        readonly StaffModificationView _staffView;
        readonly ISceneLoader _sceneLoader;

        [Inject]
        public MainMenuController(MainMenuView menuView,
                                  StaffModificationView staffView,
                                  ISceneLoader sceneLoader)
        {
            _menuView = menuView;
            _staffView = staffView;
            _sceneLoader = sceneLoader;
        }

        public void Initialize()
        {
            _menuView.OnPlayClicked  += HandlePlay;
            _menuView.OnStaffClicked += HandleStaff;
        }

        public void Dispose()
        {
            _menuView.OnPlayClicked  -= HandlePlay;
            _menuView.OnStaffClicked -= HandleStaff;
        }

        void HandlePlay()  => _sceneLoader.Load("Gameplay");
        void HandleStaff() => _staffView.Show();
    }
}
