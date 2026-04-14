using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/TowerConfig", fileName = "TowerConfig")]
public class TowerConfig : ScriptableObject
{
    [SerializeField]
    private string _id;

    [SerializeField]
    private string _displayName;

    [SerializeField]
    private Sprite _icon;

    [SerializeField]
    private Tower _prefab;

    [SerializeField]
    private Projectile _projectilePrefab;

    [SerializeField]
    private int _cost = 50;

    [SerializeField]
    private int _damage = 10;

    [SerializeField]
    private float _range = 5f;

    [SerializeField]
    private float _fireRate = 1f;

    [SerializeField]
    private float _projectileSpeed = 15f;

    [SerializeField]
    private int _maxLevel = 3;

    [Header("Area")]
    [SerializeField]
    private float _splashRadius;

    [Header("Slow")]
    [Range(0.1f, 1f)]
    [SerializeField]
    private float _slowMultiplier = 1f;

    [SerializeField]
    private float _slowDuration;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public Tower Prefab => _prefab;
    public Projectile ProjectilePrefab => _projectilePrefab;
    public int Cost => _cost;
    public int BaseDamage => _damage;
    public float BaseRange => _range;
    public float FireRate => _fireRate;
    public float ProjectileSpeed => _projectileSpeed;
    public int MaxLevel => _maxLevel;
    public float SplashRadius => _splashRadius;
    public float SlowMultiplier => _slowMultiplier;
    public float SlowDuration => _slowDuration;

    public int GetDamage(int level) =>
        Mathf.RoundToInt(_damage * (1f + 0.25f * (level - 1)));

    public float GetRange(int level) =>
        _range * (1f + 0.10f * (level - 1));

    public int GetUpgradeCost(int currentLevel) =>
        Mathf.RoundToInt(_cost * 1.5f * currentLevel);
}
