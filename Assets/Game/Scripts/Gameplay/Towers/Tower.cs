using UnityEngine;

[RequireComponent(typeof(TowerAttack))]
public class Tower : MonoBehaviour
{
    public TowerConfig Config => _config;
    public TowerAttack Attack => _attack;
    public TowerSlot Slot => _slot;
    public int Level => _level;
    public int TotalInvested => _totalInvested;
    public int EffectiveDamage => _config.GetDamage(_level);
    public float EffectiveRange => _config.GetRange(_level);
    public bool CanUpgrade => _level < _config.MaxLevel;
    public int NextUpgradeCost => CanUpgrade ? _config.GetUpgradeCost(_level) : 0;
    public int SellRefund => Mathf.RoundToInt(_totalInvested * 0.7f);

    [SerializeField]
    private TowerMeshSwitcher _meshSwitcher;

    private TowerConfig _config;
    private TowerAttack _attack;
    private TowerSlot _slot;
    private int _level;
    private int _totalInvested;

    private void Awake()
    {
        _attack = GetComponent<TowerAttack>();
    }

    public void Init(TowerConfig config)
    {
        _config = config;
        _level = 1;
        _totalInvested = config.Cost;
        _attack.Init(this);
        if (_meshSwitcher != null) _meshSwitcher.Apply(config.UpgradeMeshes, _level);
    }

    public void AttachSlot(TowerSlot slot) => _slot = slot;

    public void ApplyUpgrade()
    {
        _totalInvested += _config.GetUpgradeCost(_level);
        _level++;
        _attack.RefreshStats();
        if (_meshSwitcher != null) _meshSwitcher.Apply(_config.UpgradeMeshes, _level);
    }
}
