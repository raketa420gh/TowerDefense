using System;
using UnityEngine;
using Zenject;

public class HudPresenter : IInitializable, IDisposable, ITickable
{
    private readonly HudView _view;
    private readonly SignalBus _signalBus;
    private readonly Wallet _wallet;

    private int _totalWaves;
    private int _currentWaveIndex;
    private float _breakTimeLeft;
    private bool _breakActive;

    public HudPresenter(HudView view, SignalBus signalBus, Wallet wallet)
    {
        _view = view;
        _signalBus = signalBus;
        _wallet = wallet;
    }

    public void Initialize()
    {
        _view.EarlyStartClicked += OnEarlyStartClicked;
        _view.PauseClicked += OnPauseClicked;
        _signalBus.Subscribe<GoldChangedSignal>(OnGoldChanged);
        _signalBus.Subscribe<BaseHealthChangedSignal>(OnBaseHealthChanged);
        _signalBus.Subscribe<WaveStartedSignal>(OnWaveStarted);
        _signalBus.Subscribe<WaveCompletedSignal>(OnWaveCompleted);
        _signalBus.Subscribe<WaveBreakStartedSignal>(OnWaveBreakStarted);
        _signalBus.Subscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
        _signalBus.Subscribe<LevelFailedSignal>(OnLevelFailed);

        _view.SetGold(_wallet.Current);
        _view.Show();
    }

    public void Dispose()
    {
        _view.EarlyStartClicked -= OnEarlyStartClicked;
        _view.PauseClicked -= OnPauseClicked;
        _signalBus.TryUnsubscribe<GoldChangedSignal>(OnGoldChanged);
        _signalBus.TryUnsubscribe<BaseHealthChangedSignal>(OnBaseHealthChanged);
        _signalBus.TryUnsubscribe<WaveStartedSignal>(OnWaveStarted);
        _signalBus.TryUnsubscribe<WaveCompletedSignal>(OnWaveCompleted);
        _signalBus.TryUnsubscribe<WaveBreakStartedSignal>(OnWaveBreakStarted);
        _signalBus.TryUnsubscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
        _signalBus.TryUnsubscribe<LevelFailedSignal>(OnLevelFailed);
    }

    public void Tick()
    {
        if (!_breakActive) return;
        _breakTimeLeft -= Time.deltaTime;
        if (_breakTimeLeft < 0f) _breakTimeLeft = 0f;
        _view.SetBreakTimer(_breakTimeLeft);
    }

    private void OnGoldChanged(GoldChangedSignal s) => _view.SetGold(s.Current);
    private void OnBaseHealthChanged(BaseHealthChangedSignal s) => _view.SetBaseHp(s.Current, s.Max);

    private void OnWaveStarted(WaveStartedSignal s)
    {
        _totalWaves = s.Total;
        _currentWaveIndex = s.Index;
        _breakActive = false;
        _view.SetWave(s.Index, s.Total);
        _view.SetEarlyStartVisible(false);
    }

    private void OnWaveCompleted(WaveCompletedSignal _) =>
        _view.SetEarlyStartVisible(false);

    private void OnWaveBreakStarted(WaveBreakStartedSignal s)
    {
        _breakActive = true;
        _breakTimeLeft = s.Seconds;
        _view.SetBreakTimer(_breakTimeLeft);
        _view.SetEarlyStartVisible(true);
    }

    private void OnAllWavesCompleted()
    {
        _breakActive = false;
        _view.SetEarlyStartVisible(false);
        _view.Hide();
    }

    private void OnLevelFailed() => _view.Hide();

    private void OnPauseClicked() => _signalBus.Fire(new PauseRequestedSignal());

    private void OnEarlyStartClicked()
    {
        if (!_breakActive) return;
        _breakActive = false;
        _view.SetEarlyStartVisible(false);
        _signalBus.Fire(new WaveEarlyStartRequestedSignal());
    }
}
