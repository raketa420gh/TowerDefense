using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class WaveSpawner
{
    private readonly EnemyFactory _factory;
    private readonly SignalBus _signalBus;
    private readonly ICoroutineRunner _runner;

    public WaveSpawner(EnemyFactory factory, SignalBus signalBus, ICoroutineRunner runner)
    {
        _factory = factory;
        _signalBus = signalBus;
        _runner = runner;
    }

    public void Run(IReadOnlyList<WaveConfig> waves, Path path)
    {
        _runner.Run(RunRoutine(waves, path));
    }

    private IEnumerator RunRoutine(IReadOnlyList<WaveConfig> waves, Path path)
    {
        for (int i = 0; i < waves.Count; i++)
        {
            _signalBus.Fire(new WaveStartedSignal { Index = i });
            var wave = waves[i];
            foreach (var sub in wave.SubWaves)
                for (int c = 0; c < sub.Count; c++)
                {
                    _factory.Spawn(sub.Enemy, path);
                    yield return new WaitForSeconds(sub.Interval);
                }
            _signalBus.Fire(new WaveCompletedSignal { Index = i });
            yield return new WaitForSeconds(wave.DelayAfter);
        }
        _signalBus.Fire(new AllWavesCompletedSignal());
    }
}
