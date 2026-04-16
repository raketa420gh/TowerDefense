using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    [SerializeField]
    EnemyController _prefab;

    [SerializeField]
    int _initialSize = 20;

    readonly List<EnemyController> _all = new();

    void Awake()
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

    EnemyController CreateInstance()
    {
        var e = Instantiate(_prefab, transform);
        e.gameObject.SetActive(false);
        return e;
    }
}
