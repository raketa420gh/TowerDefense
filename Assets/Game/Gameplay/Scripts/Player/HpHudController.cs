using System;
using Zenject;

public class HpHudController : IInitializable, IDisposable
{
    private readonly HpBarView            _hpBarView;
    private readonly IPlayerHealthService _healthService;

    [Inject]
    public HpHudController(HpBarView hpBarView, IPlayerHealthService healthService)
    {
        _hpBarView     = hpBarView;
        _healthService = healthService;
    }

    public void Initialize()
    {
        _healthService.OnHpChanged += _hpBarView.SetProgress;
        _hpBarView.SetProgress(_healthService.NormalizedHp);
    }

    public void Dispose() => _healthService.OnHpChanged -= _hpBarView.SetProgress;
}
