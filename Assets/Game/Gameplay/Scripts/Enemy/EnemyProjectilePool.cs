using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class EnemyProjectilePool : MonoBehaviour
{
    [SerializeField]
    private EnemyProjectileController _prefab;

    [SerializeField]
    private int _initialSize = 10;

    private readonly Queue<EnemyProjectileController> _pool = new();
    private IPlayerHealthService _playerHealth;

    [Inject]
    public void Construct(IPlayerHealthService playerHealth) => _playerHealth = playerHealth;

    private void Start()
    {
        for (int i = 0; i < _initialSize; i++)
            _pool.Enqueue(CreateInstance());
    }

    public EnemyProjectileController Get()
    {
        var p = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
        p.gameObject.SetActive(true);
        return p;
    }

    public void Return(EnemyProjectileController p)
    {
        p.gameObject.SetActive(false);
        _pool.Enqueue(p);
    }

    private EnemyProjectileController CreateInstance()
    {
        var p = Instantiate(_prefab, transform);
        p.SetContext(_playerHealth, this);
        p.gameObject.SetActive(false);
        return p;
    }
}
