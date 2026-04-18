using System;
using MagicStaff;
using UnityEngine;
using Zenject;

public class DefeatController : IInitializable, IDisposable
{
    private readonly IPlayerHealthService _healthService;
    private readonly ICoinService         _coinService;
    private readonly DefeatView           _defeatView;
    private readonly ISceneLoader         _sceneLoader;

    [Inject]
    public DefeatController(IPlayerHealthService healthService,
                            ICoinService         coinService,
                            DefeatView           defeatView,
                            ISceneLoader         sceneLoader)
    {
        _healthService = healthService;
        _coinService   = coinService;
        _defeatView    = defeatView;
        _sceneLoader   = sceneLoader;
    }

    public void Initialize()
    {
        _healthService.OnDied         += HandleDied;
        _defeatView.OnContinueClicked += HandleContinue;
    }

    public void Dispose()
    {
        _healthService.OnDied         -= HandleDied;
        _defeatView.OnContinueClicked -= HandleContinue;
    }

    private void HandleDied()
    {
        _coinService.AddCoins(1);
        Time.timeScale = 0f;
        _defeatView.Show();
    }

    private void HandleContinue()
    {
        Time.timeScale = 1f;
        _sceneLoader.Load("MainMenu");
    }
}
