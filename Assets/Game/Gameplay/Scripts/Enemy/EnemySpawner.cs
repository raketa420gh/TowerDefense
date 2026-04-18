using System.Collections;
using UnityEngine;
using Zenject;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private Transform _playerTarget;

    private EnemyPool   _pool;
    private WaveConfig  _waveConfig;
    private EnemyConfig _enemyConfig;

    private float _elapsedTime;

    [Inject]
    public void Construct(EnemyPool pool, WaveConfig waveConfig, EnemyConfig enemyConfig)
    {
        _pool        = pool;
        _waveConfig  = waveConfig;
        _enemyConfig = enemyConfig;
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
            var enemy = _pool.Get();
            enemy.transform.position = GetSpawnPosition();
            enemy.Initialize(_enemyConfig, _playerTarget, _pool);
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
            0 => new Vector3(variance,       1f,  half + offset),
            1 => new Vector3(variance,       1f, -half - offset),
            2 => new Vector3( half + offset, 1f, variance),
            _ => new Vector3(-half - offset, 1f, variance),
        };
    }
}
