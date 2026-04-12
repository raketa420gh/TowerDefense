using UnityEngine;
using Zenject;

public class EnemyFactory
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;
    private readonly Transform _root;

    public EnemyFactory(DiContainer container, SignalBus signalBus, [Inject(Id = "EnemyRoot")] Transform root)
    {
        _container = container;
        _signalBus = signalBus;
        _root = root;
    }

    public Enemy Spawn(EnemyConfig config, Path path)
    {
        var enemy = _container.InstantiatePrefabForComponent<Enemy>(config.Prefab, _root);
        enemy.transform.localScale = Vector3.one * config.VisualScale;
        enemy.Init(config, path);
        enemy.Health.Died += () =>
        {
            _signalBus.Fire(new EnemyKilledSignal { Enemy = enemy, Reward = config.Reward });
            Object.Destroy(enemy.gameObject);
        };
        _signalBus.Fire(new EnemySpawnedSignal { Enemy = enemy });
        return enemy;
    }
}
