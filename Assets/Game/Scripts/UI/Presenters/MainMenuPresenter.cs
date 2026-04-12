using System;
using Zenject;

public class MainMenuPresenter : IInitializable, IDisposable
{
    private readonly MainMenuView _mainMenu;
    private readonly LevelSelectView _levelSelect;
    private readonly PlayerProgress _progress;
    private readonly SignalBus _signalBus;

    public MainMenuPresenter(MainMenuView mainMenu, LevelSelectView levelSelect,
        PlayerProgress progress, SignalBus signalBus)
    {
        _mainMenu = mainMenu;
        _levelSelect = levelSelect;
        _progress = progress;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _mainMenu.PlayClicked += OnPlay;
        _levelSelect.BackClicked += OnBack;
        _levelSelect.LevelSelected += OnLevelSelected;

        _mainMenu.Show();
        _levelSelect.Hide();
    }

    public void Dispose()
    {
        _mainMenu.PlayClicked -= OnPlay;
        _levelSelect.BackClicked -= OnBack;
        _levelSelect.LevelSelected -= OnLevelSelected;
    }

    private void OnPlay()
    {
        _levelSelect.Refresh(_progress);
        _mainMenu.Hide();
        _levelSelect.Show();
    }

    private void OnBack()
    {
        _levelSelect.Hide();
        _mainMenu.Show();
    }

    private void OnLevelSelected(int levelId)
    {
        if (levelId > _progress.UnlockedLevel)
            return;
        _signalBus.Fire(new LevelStartRequestedSignal { LevelId = levelId });
    }
}
