using System.Collections;
using UnityEngine;
using Zenject;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private Transform _playerTarget;

    [Inject] private EnemyPool   _pool;
    [Inject] private WaveConfig  _waveConfig;
    [Inject] private EnemyConfig _enemyConfig;

    private float _elapsedTime;

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
            var enemy = _pool.Get();
            enemy.transform.position = GetSpawnPosition();
            enemy.Initialize(_enemyConfig, _playerTarget);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        int   side     = Random.Range(0, 4);
        float half     = _waveConfig.arenaHalfSize;
        float offset   = _waveConfig.spawnEdgeOffset;
        float variance = Random.Range(-_waveConfig.spawnEdgeVariance, _waveConfig.spawnEdgeVariance);

        return side switch
        {
            0 => new Vector3(variance,        1f,  half + offset),  // север
            1 => new Vector3(variance,        1f, -half - offset),  // юг
            2 => new Vector3( half + offset,  1f, variance),        // восток
            _ => new Vector3(-half - offset,  1f, variance),        // запад
        };
    }
}
