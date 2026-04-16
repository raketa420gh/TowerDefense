using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    private const float MaxDistance = 20f;

    private float   _speed;
    private float   _damage;
    private Vector3 _direction;
    private Vector3 _startPosition;

    private ProjectilePool _pool;

    public void SetPool(ProjectilePool pool) => _pool = pool;

    public void Launch(Vector3 direction, float speed, float damage)
    {
        _direction     = direction.normalized;
        _speed         = speed;
        _damage        = damage;
        _startPosition = transform.position;
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        if ((transform.position - _startPosition).sqrMagnitude > MaxDistance * MaxDistance)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<HealthComponent>(out var hp)) return;
        hp.TakeDamage(_damage);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_pool != null)
            _pool.Return(this);
        else
            gameObject.SetActive(false);
    }
}
