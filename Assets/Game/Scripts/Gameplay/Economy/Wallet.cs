using System;
using Zenject;

public class Wallet : IInitializable, IDisposable
{
    public int Current => _current;

    private readonly SignalBus _signalBus;
    private int _current;

    public Wallet(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<EnemyKilledSignal>(OnEnemyKilled);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<EnemyKilledSignal>(OnEnemyKilled);
    }

    public void SetStartingGold(int amount)
    {
        _current = amount;
        _signalBus.Fire(new GoldChangedSignal { Current = _current });
    }

    public bool CanAfford(int amount) => _current >= amount;

    public bool Spend(int amount)
    {
        if (!CanAfford(amount)) return false;
        _current -= amount;
        _signalBus.Fire(new GoldChangedSignal { Current = _current });
        return true;
    }

    public void Add(int amount)
    {
        _current += amount;
        _signalBus.Fire(new GoldChangedSignal { Current = _current });
    }

    private void OnEnemyKilled(EnemyKilledSignal signal)
    {
        Add(signal.Reward);
    }
}
