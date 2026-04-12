using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class WaveSpawner : IInitializable, IDisposable
{
    private readonly EnemyFactory _factory;
    private readonly SignalBus _signalBus;
    private readonly ICoroutineRunner _runner;

    private Coroutine _routine;
    private bool _skipBreak;
    private int _aliveEnemies;
    private int _pendingSpawns;

    public WaveSpawner(EnemyFactory factory, SignalBus signalBus, ICoroutineRunner runner)
    {
        _factory = factory;
        _signalBus = signalBus;
        _runner = runner;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<WaveEarlyStartRequestedSignal>(OnEarlyStartRequested);
        _signalBus.Subscribe<EnemySpawnedSignal>(OnEnemySpawned);
        _signalBus.Subscribe<EnemyKilledSignal>(OnEnemyKilled);
        _signalBus.Subscribe<EnemyReachedBaseSignal>(OnEnemyReachedBase);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<WaveEarlyStartRequestedSignal>(OnEarlyStartRequested);
        _signalBus.TryUnsubscribe<EnemySpawnedSignal>(OnEnemySpawned);
        _signalBus.TryUnsubscribe<EnemyKilledSignal>(OnEnemyKilled);
        _signalBus.TryUnsubscribe<EnemyReachedBaseSignal>(OnEnemyReachedBase);
        Stop();
    }

    public void Run(IReadOnlyList<WaveConfig> waves, IReadOnlyList<Path> paths)
    {
        Stop();
        _aliveEnemies = 0;
        _pendingSpawns = 0;
        _routine = _runner.Run(RunRoutine(waves, paths));
    }

    public void Stop()
    {
        if (_routine != null) _runner.Stop(_routine);
        _routine = null;
        _skipBreak = false;
    }

    private IEnumerator RunRoutine(IReadOnlyList<WaveConfig> waves, IReadOnlyList<Path> paths)
    {
        for (int i = 0; i < waves.Count; i++)
        {
            _signalBus.Fire(new WaveStartedSignal { Index = i, Total = waves.Count });

            var wave = waves[i];
            foreach (var sub in wave.SubWaves)
            {
                var path = paths[Mathf.Clamp(sub.PathIndex, 0, paths.Count - 1)];
                for (int c = 0; c < sub.Count; c++)
                {
                    _pendingSpawns++;
                    _factory.Spawn(sub.Enemy, path);
                    yield return new WaitForSeconds(sub.Interval);
                }
            }

            while (_aliveEnemies > 0 || _pendingSpawns > 0)
                yield return null;

            _signalBus.Fire(new WaveCompletedSignal { Index = i, Reward = wave.Reward });

            if (i == waves.Count - 1) break;

            _signalBus.Fire(new WaveBreakStartedSignal
            {
                NextIndex = i + 1,
                Seconds = wave.DelayAfter,
            });

            _skipBreak = false;
            float t = wave.DelayAfter;
            while (t > 0f && !_skipBreak)
            {
                t -= Time.deltaTime;
                yield return null;
            }
        }

        _signalBus.Fire(new AllWavesCompletedSignal());
    }

    private void OnEarlyStartRequested() => _skipBreak = true;

    private void OnEnemySpawned(EnemySpawnedSignal _)
    {
        _pendingSpawns = Mathf.Max(0, _pendingSpawns - 1);
        _aliveEnemies++;
    }

    private void OnEnemyKilled(EnemyKilledSignal _) => _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
    private void OnEnemyReachedBase(EnemyReachedBaseSignal _) => _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
}
