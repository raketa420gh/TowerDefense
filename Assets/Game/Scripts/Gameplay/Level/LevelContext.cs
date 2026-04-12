using UnityEngine;

public class LevelContext : MonoBehaviour
{
    [SerializeField]
    private LevelConfig _config;

    [SerializeField]
    private Path _path;

    [SerializeField]
    private PlayerBase _playerBase;

    [SerializeField]
    private Transform _enemyRoot;

    [SerializeField]
    private TowerSlot[] _slots;

    [SerializeField]
    private Transform _towerRoot;

    [SerializeField]
    private Transform _projectileRoot;

    [SerializeField]
    private BuildMenuView _buildMenu;

    public LevelConfig Config => _config;
    public Path Path => _path;
    public PlayerBase PlayerBase => _playerBase;
    public Transform EnemyRoot => _enemyRoot;
    public TowerSlot[] Slots => _slots;
    public Transform TowerRoot => _towerRoot;
    public Transform ProjectileRoot => _projectileRoot;
    public BuildMenuView BuildMenu => _buildMenu;
}
