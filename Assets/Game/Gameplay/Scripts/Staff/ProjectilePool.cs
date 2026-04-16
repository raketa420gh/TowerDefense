using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField]
    private ProjectileController _prefab;

    [SerializeField]
    private int _initialSize = 20;

    private readonly Queue<ProjectileController> _pool = new();

    private void Awake()
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

    private ProjectileController CreateInstance()
    {
        var p = Instantiate(_prefab, transform);
        p.SetPool(this);
        p.gameObject.SetActive(false);
        _pool.Enqueue(p);
        return p;
    }
}
