using UnityEngine;
using Zenject;

public class ProjectilePool
{
    private readonly DiContainer _container;
    private readonly Transform _root;

    public ProjectilePool(DiContainer container, [Inject(Id = "ProjectileRoot")] Transform root)
    {
        _container = container;
        _root = root;
    }

    public void Spawn(Projectile prefab, Vector3 origin, Enemy target,
        ProjectileImpact impact, float speed)
    {
        var instance = _container.InstantiatePrefabForComponent<Projectile>(prefab, origin,
            Quaternion.identity, _root);
        instance.Launch(origin, target, impact, speed);
    }
}
