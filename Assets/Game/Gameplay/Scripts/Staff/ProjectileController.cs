using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    const float MaxDistance = 20f;

    float   _speed;
    float   _damage;
    Vector3 _direction;
    Vector3 _startPosition;

    ProjectilePool _pool;

    public void SetPool(ProjectilePool pool) => _pool = pool;

    public void Launch(Vector3 direction, float speed, float damage)
    {
        _direction     = direction.normalized;
        _speed         = speed;
        _damage        = damage;
        _startPosition = transform.position;
    }

    void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        if ((transform.position - _startPosition).sqrMagnitude > MaxDistance * MaxDistance)
            ReturnToPool();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<HealthComponent>(out var hp)) return;
        hp.TakeDamage(_damage);
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (_pool != null)
            _pool.Return(this);
        else
            gameObject.SetActive(false);
    }
}
