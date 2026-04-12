using UnityEngine;
using Zenject;

public class TowerAttack : MonoBehaviour
{
    private const int MaxOverlap = 16;
    private const float ScanInterval = 0.1f;

    [SerializeField]
    private Transform _muzzle;

    private ProjectilePool _pool;
    private TowerConfig _config;
    private LayerMask _enemyMask;
    private readonly Collider[] _buffer = new Collider[MaxOverlap];

    private float _cooldown;
    private float _scanTimer;
    private Enemy _currentTarget;

    [Inject]
    public void Construct(ProjectilePool pool)
    {
        _pool = pool;
        _enemyMask = LayerMask.GetMask("Enemies");
    }

    public void Init(TowerConfig config)
    {
        _config = config;
        _cooldown = 0f;
        _scanTimer = 0f;
    }

    private void Update()
    {
        if (_config == null) return;

        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            _scanTimer = ScanInterval;
            _currentTarget = FindNearest();
        }

        if (_currentTarget == null || _currentTarget.Health.IsDead)
            return;

        var flat = _currentTarget.transform.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(flat), 10f * Time.deltaTime);

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        _cooldown = 1f / Mathf.Max(0.0001f, _config.FireRate);
        var origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up;
        _pool.Spawn(_config.ProjectilePrefab, origin, _currentTarget, _config.Damage, _config.ProjectileSpeed);
    }

    private Enemy FindNearest()
    {
        var count = Physics.OverlapSphereNonAlloc(transform.position, _config.Range, _buffer, _enemyMask);
        Enemy best = null;
        float bestSqr = float.MaxValue;
        var pos = transform.position;
        for (int i = 0; i < count; i++)
        {
            var e = _buffer[i].GetComponentInParent<Enemy>();
            if (e == null || e.Health.IsDead) continue;
            var d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _config.Range);
    }
#endif
}
