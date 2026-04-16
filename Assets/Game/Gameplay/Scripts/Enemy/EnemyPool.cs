using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    [SerializeField]
    private EnemyController _prefab;

    [SerializeField]
    private int _initialSize = 20;

    private readonly List<EnemyController> _all = new();

    private void Awake()
    {
        for (int i = 0; i < _initialSize; i++)
            _all.Add(CreateInstance());
    }

    public EnemyController Get()
    {
        foreach (var e in _all)
            if (!e.gameObject.activeSelf)
            {
                e.gameObject.SetActive(true);
                return e;
            }

        var n = CreateInstance();
        _all.Add(n);
        n.gameObject.SetActive(true);
        return n;
    }

    private EnemyController CreateInstance()
    {
        var e = Instantiate(_prefab, transform);
        e.gameObject.SetActive(false);
        return e;
    }
}
