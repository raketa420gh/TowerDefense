using System.Collections;
using UnityEngine;
using Zenject;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    Transform _playerTarget;

    [Inject] EnemyPool   _pool;
    [Inject] WaveConfig  _waveConfig;
    [Inject] EnemyConfig _enemyConfig;

    float _elapsedTime;

    void Update() => _elapsedTime += Time.deltaTime;

    void Start() => StartCoroutine(SpawnLoop());

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float interval = _waveConfig.spawnIntervalOverTime.Evaluate(_elapsedTime);
            yield return new WaitForSeconds(interval);

            int count = Mathf.RoundToInt(_waveConfig.enemyCountOverTime.Evaluate(_elapsedTime));
            SpawnWave(count);
        }
    }

    void SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var enemy = _pool.Get();
            enemy.transform.position = GetSpawnPosition();
            enemy.Initialize(_enemyConfig, _playerTarget);
        }
    }

    Vector3 GetSpawnPosition()
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
