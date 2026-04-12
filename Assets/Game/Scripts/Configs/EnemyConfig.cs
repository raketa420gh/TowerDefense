using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/EnemyConfig", fileName = "EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [SerializeField]
    private string _id;

    [SerializeField]
    private Enemy _prefab;

    [SerializeField]
    private int _maxHealth = 50;

    [SerializeField]
    private float _speed = 2f;

    [SerializeField]
    private int _reward = 10;

    [SerializeField]
    private int _baseDamage = 1;

    public string Id => _id;
    public Enemy Prefab => _prefab;
    public int MaxHealth => _maxHealth;
    public float Speed => _speed;
    public int Reward => _reward;
    public int BaseDamage => _baseDamage;
}
