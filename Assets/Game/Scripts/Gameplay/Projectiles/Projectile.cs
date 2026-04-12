using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Enemy _target;
    private int _damage;
    private float _speed;
    private bool _active;

    public void Launch(Vector3 origin, Enemy target, int damage, float speed)
    {
        _target = target;
        _damage = damage;
        _speed = speed;
        transform.position = origin;
        _active = true;
    }

    private void Update()
    {
        if (!_active) return;
        if (_target == null || _target.Health.IsDead)
        {
            Release();
            return;
        }

        var targetPos = _target.transform.position + Vector3.up * 0.5f;
        var next = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);
        transform.position = next;

        var dir = targetPos - next;
        if (dir.sqrMagnitude > 0.0001f)
            transform.forward = dir.normalized;

        if ((next - targetPos).sqrMagnitude < 0.04f)
        {
            _target.Health.TakeDamage(_damage);
            Release();
        }
    }

    private void Release()
    {
        _active = false;
        Destroy(gameObject);
    }
}
