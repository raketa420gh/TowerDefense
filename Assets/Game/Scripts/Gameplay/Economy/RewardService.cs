using System;
using Zenject;

public class RewardService : IInitializable, IDisposable
{
    private readonly SignalBus _signalBus;
    private readonly Wallet _wallet;

    public RewardService(SignalBus signalBus, Wallet wallet)
    {
        _signalBus = signalBus;
        _wallet = wallet;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<WaveCompletedSignal>(OnWaveCompleted);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<WaveCompletedSignal>(OnWaveCompleted);
    }

    private void OnWaveCompleted(WaveCompletedSignal signal)
    {
        if (signal.Reward > 0) _wallet.Add(signal.Reward);
    }
}
