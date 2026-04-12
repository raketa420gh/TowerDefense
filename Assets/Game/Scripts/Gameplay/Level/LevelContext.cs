using UnityEngine;

public class LevelContext : MonoBehaviour
{
    [SerializeField]
    private LevelConfig _config;

    [SerializeField]
    private Path[] _paths;

    [SerializeField, HideInInspector]
    private Path _path;

    private void OnValidate()
    {
        if ((_paths == null || _paths.Length == 0) && _path != null)
        {
            _paths = new[] { _path };
        }
    }

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

    [SerializeField]
    private HudView _hud;

    [SerializeField]
    private LevelCompleteView _completeView;

    [SerializeField]
    private LevelFailedView _failedView;

    [SerializeField]
    private TowerInfoView _towerInfoView;

    public LevelConfig Config => _config;
    public Path[] Paths => _paths;
    public PlayerBase PlayerBase => _playerBase;
    public Transform EnemyRoot => _enemyRoot;
    public TowerSlot[] Slots => _slots;
    public Transform TowerRoot => _towerRoot;
    public Transform ProjectileRoot => _projectileRoot;
    public BuildMenuView BuildMenu => _buildMenu;
    public HudView Hud => _hud;
    public LevelCompleteView CompleteView => _completeView;
    public LevelFailedView FailedView => _failedView;
    public TowerInfoView TowerInfoView => _towerInfoView;

    public void SetConfig(LevelConfig config) => _config = config;
}
