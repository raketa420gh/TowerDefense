using System;
using UnityEngine;
using Zenject;

public class AudioService : IInitializable, IDisposable
{
    private readonly SignalBus _signalBus;
    private readonly SfxConfig _config;
    private readonly AudioSource _source;

    public AudioService(SignalBus signalBus, [InjectOptional] SfxConfig config)
    {
        _signalBus = signalBus;
        _config = config;
        var go = new GameObject("[AudioService]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _source = go.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<ProjectileHitSignal>(OnProjectileHit);
        _signalBus.Subscribe<EnemyKilledSignal>(OnEnemyKilled);
        _signalBus.Subscribe<TowerBuiltSignal>(OnTowerBuilt);
        _signalBus.Subscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
        _signalBus.Subscribe<LevelFailedSignal>(OnLevelFailed);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<ProjectileHitSignal>(OnProjectileHit);
        _signalBus.TryUnsubscribe<EnemyKilledSignal>(OnEnemyKilled);
        _signalBus.TryUnsubscribe<TowerBuiltSignal>(OnTowerBuilt);
        _signalBus.TryUnsubscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
        _signalBus.TryUnsubscribe<LevelFailedSignal>(OnLevelFailed);
    }

    public void Play(AudioClip clip)
    {
        if (clip == null || _source == null) return;
        _source.PlayOneShot(clip);
    }

    private void OnProjectileHit(ProjectileHitSignal _) => Play(_config?.Shot);
    private void OnEnemyKilled(EnemyKilledSignal _) => Play(_config?.EnemyDeath);
    private void OnTowerBuilt(TowerBuiltSignal _) => Play(_config?.TowerBuilt);
    private void OnAllWavesCompleted() => Play(_config?.LevelWin);
    private void OnLevelFailed() => Play(_config?.LevelFail);
}
