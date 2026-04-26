using System;
using System.Collections;
using UnityEngine;
using Zenject;

public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    public struct EnemySpawnEntry
    {
        public EnemyPool   Pool;
        public EnemyConfig Config;
        [Range(0f, 1f)]
        public float       Weight;
    }

    [SerializeField]
    private Transform _playerTarget;

    [SerializeField]
    private EnemySpawnEntry[] _enemies;

    private WaveConfig           _waveConfig;
    private IPlayerHealthService _playerHealth;
    private EnemyProjectilePool  _projectilePool;
    private float                _totalWeight;
    private float                _elapsedTime;

    [Inject]
    public void Construct(WaveConfig waveConfig, IPlayerHealthService playerHealth, EnemyProjectilePool projectilePool)
    {
        _waveConfig     = waveConfig;
        _playerHealth   = playerHealth;
        _projectilePool = projectilePool;

        foreach (var e in _enemies)
            _totalWeight += e.Weight;
    }

    private void Update() => _elapsedTime += Time.deltaTime;

    private void Start() => StartCoroutine(SpawnLoop());

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float interval = _waveConfig.spawnIntervalOverTime.Evaluate(_elapsedTime);
            yield return new WaitForSeconds(interval);

            int count = Mathf.RoundToInt(_waveConfig.enemyCountOverTime.Evaluate(_elapsedTime));
            SpawnWave(count);
        }
    }

    private void SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var entry = PickWeightedEntry();
            if (entry.Pool == null || entry.Config == null) continue;
            var enemy = entry.Pool.Get();
            enemy.transform.position = GetSpawnPosition();
            enemy.Initialize(entry.Config, _playerTarget, entry.Pool, _playerHealth, _projectilePool);
        }
    }

    private EnemySpawnEntry PickWeightedEntry()
    {
        float r          = UnityEngine.Random.Range(0f, _totalWeight);
        float cumulative = 0f;

        foreach (var e in _enemies)
        {
            cumulative += e.Weight;
            if (r <= cumulative) return e;
        }

        return _enemies[^1];
    }

    private Vector3 GetSpawnPosition()
    {
        int   side     = UnityEngine.Random.Range(0, 4);
        float half     = _waveConfig.arenaHalfSize;
        float offset   = _waveConfig.spawnEdgeOffset;
        float variance = UnityEngine.Random.Range(-_waveConfig.spawnEdgeVariance, _waveConfig.spawnEdgeVariance);

        return side switch
        {
            0 => new Vector3(variance,       1f,  half + offset),
            1 => new Vector3(variance,       1f, -half - offset),
            2 => new Vector3( half + offset, 1f, variance),
            _ => new Vector3(-half - offset, 1f, variance),
        };
    }
}
