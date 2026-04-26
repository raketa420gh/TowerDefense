using UnityEngine;

public class EnemyProjectileController : MonoBehaviour
{
    private const float MaxDistance = 20f;

    private IPlayerHealthService _playerHealth;
    private EnemyProjectilePool  _pool;
    private float                _damage;
    private float                _speed;
    private Vector3              _direction;
    private Vector3              _startPos;

    public void SetContext(IPlayerHealthService playerHealth, EnemyProjectilePool pool)
    {
        _playerHealth = playerHealth;
        _pool         = pool;
    }

    public void Launch(Vector3 direction, float speed, float damage)
    {
        _direction = direction.normalized;
        _speed     = speed;
        _damage    = damage;
        _startPos  = transform.position;
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        if (Vector3.Distance(_startPos, transform.position) >= MaxDistance)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerHealth?.TakeDamage(_damage);
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
