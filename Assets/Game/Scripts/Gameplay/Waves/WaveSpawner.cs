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

    public WaveSpawner(EnemyFactory factory, SignalBus signalBus, ICoroutineRunner runner)
    {
        _factory = factory;
        _signalBus = signalBus;
        _runner = runner;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<WaveEarlyStartRequestedSignal>(OnEarlyStartRequested);
        _signalBus.Subscribe<EnemyKilledSignal>(OnEnemyKilled);
        _signalBus.Subscribe<EnemyReachedBaseSignal>(OnEnemyReachedBase);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<WaveEarlyStartRequestedSignal>(OnEarlyStartRequested);
        _signalBus.TryUnsubscribe<EnemyKilledSignal>(OnEnemyKilled);
        _signalBus.TryUnsubscribe<EnemyReachedBaseSignal>(OnEnemyReachedBase);
        Stop();
    }

    public void Run(IReadOnlyList<WaveConfig> waves, IReadOnlyList<Path> paths)
    {
        Stop();
        _aliveEnemies = 0;
        Debug.Log($"[WaveSpawner] Run: {waves.Count} волн, {paths.Count} путей");
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
            Debug.Log($"[WaveSpawner] Волна {i + 1}/{waves.Count} — начало");
            _signalBus.Fire(new WaveStartedSignal { Index = i, Total = waves.Count });

            var wave = waves[i];
            foreach (var sub in wave.SubWaves)
            {
                var path = paths[Mathf.Clamp(sub.PathIndex, 0, paths.Count - 1)];
                Debug.Log($"[WaveSpawner] SubWave: враг={sub.Enemy.name}, кол-во={sub.Count}, интервал={sub.Interval}с, путь={sub.PathIndex}");
                for (int c = 0; c < sub.Count; c++)
                {
                    _aliveEnemies++;
                    _factory.Spawn(sub.Enemy, path);
                    Debug.Log($"[WaveSpawner] Враг заспавнен: alive={_aliveEnemies}");
                    yield return new WaitForSeconds(sub.Interval);
                }
            }

            Debug.Log($"[WaveSpawner] Волна {i + 1} — спавн завершён, ждём гибели врагов (alive={_aliveEnemies})");
            while (_aliveEnemies > 0)
                yield return null;

            Debug.Log($"[WaveSpawner] Волна {i + 1} — завершена, награда={wave.Reward}");
            _signalBus.Fire(new WaveCompletedSignal { Index = i, Reward = wave.Reward });

            if (i == waves.Count - 1) break;

            Debug.Log($"[WaveSpawner] Перерыв {wave.DelayAfter}с перед волной {i + 2}");
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

            if (_skipBreak)
                Debug.Log($"[WaveSpawner] Перерыв пропущен досрочно");
        }

        Debug.Log("[WaveSpawner] Все волны завершены → AllWavesCompletedSignal");
        _signalBus.Fire(new AllWavesCompletedSignal());
    }

    private void OnEarlyStartRequested() => _skipBreak = true;

    private void OnEnemyKilled(EnemyKilledSignal _)
    {
        _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
        Debug.Log($"[WaveSpawner] Враг убит: alive={_aliveEnemies}");
    }

    private void OnEnemyReachedBase(EnemyReachedBaseSignal _)
    {
        _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
        Debug.Log($"[WaveSpawner] Враг достиг базы: alive={_aliveEnemies}");
    }
}
