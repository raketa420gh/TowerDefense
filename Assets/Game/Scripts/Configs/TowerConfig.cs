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

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public Tower Prefab => _prefab;
    public Projectile ProjectilePrefab => _projectilePrefab;
    public int Cost => _cost;
    public int Damage => _damage;
    public float Range => _range;
    public float FireRate => _fireRate;
    public float ProjectileSpeed => _projectileSpeed;
}
