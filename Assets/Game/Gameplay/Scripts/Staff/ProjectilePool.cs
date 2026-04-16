using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField]
    ProjectileController _prefab;

    [SerializeField]
    int _initialSize = 20;

    readonly Queue<ProjectileController> _pool = new();

    void Awake()
    {
        for (var i = 0; i < _initialSize; i++)
            CreateInstance();
    }

    public ProjectileController Get()
    {
        var p = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
        p.gameObject.SetActive(true);
        return p;
    }

    public void Return(ProjectileController p)
    {
        p.gameObject.SetActive(false);
        _pool.Enqueue(p);
    }

    ProjectileController CreateInstance()
    {
        var p = Instantiate(_prefab, transform);
        p.SetPool(this);
        p.gameObject.SetActive(false);
        _pool.Enqueue(p);
        return p;
    }
}
