using System;
using Zenject;

public class LevelResultService : IInitializable, IDisposable
{
    public int LastStars => _lastStars;

    private readonly SignalBus _signalBus;
    private readonly PlayerProgress _progress;
    private readonly LevelContext _levelContext;

    private int _currentHp;
    private int _maxHp;
    private int _lastStars;

    public LevelResultService(SignalBus signalBus, PlayerProgress progress, LevelContext levelContext)
    {
        _signalBus = signalBus;
        _progress = progress;
        _levelContext = levelContext;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<BaseHealthChangedSignal>(OnBaseHealthChanged);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<BaseHealthChangedSignal>(OnBaseHealthChanged);
    }

    public int FinalizeVictory()
    {
        _lastStars = StarCalculator.Calculate(_currentHp, _maxHp);
        _progress.SetLevelResult(_levelContext.Config.Id, _lastStars);
        _signalBus.Fire(new LevelCompletedSignal
        {
            LevelId = _levelContext.Config.Id,
            Stars = _lastStars,
        });
        return _lastStars;
    }

    private void OnBaseHealthChanged(BaseHealthChangedSignal signal)
    {
        _currentHp = signal.Current;
        _maxHp = signal.Max;
    }
}
