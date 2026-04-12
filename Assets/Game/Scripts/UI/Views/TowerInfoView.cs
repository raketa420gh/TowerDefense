using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerInfoView : DisplayableView
{
    public event Action UpgradeClicked;
    public event Action SellClicked;
    public event Action CloseClicked;

    [SerializeField]
    private TextMeshProUGUI _nameLabel;

    [SerializeField]
    private TextMeshProUGUI _levelLabel;

    [SerializeField]
    private TextMeshProUGUI _statsLabel;

    [SerializeField]
    private TextMeshProUGUI _upgradeCostLabel;

    [SerializeField]
    private TextMeshProUGUI _sellRefundLabel;

    [SerializeField]
    private Button _upgradeButton;

    [SerializeField]
    private Button _sellButton;

    [SerializeField]
    private Button _closeButton;

    protected override void Awake()
    {
        base.Awake();
        _upgradeButton.onClick.AddListener(() => UpgradeClicked?.Invoke());
        _sellButton.onClick.AddListener(() => SellClicked?.Invoke());
        _closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        Hide();
    }

    public void Populate(Tower tower, int currentGold)
    {
        var cfg = tower.Config;
        _nameLabel.text = cfg.DisplayName;
        _levelLabel.text = $"Lv {tower.Level}/{cfg.MaxLevel}";
        _statsLabel.text = $"DMG {tower.EffectiveDamage}  RNG {tower.EffectiveRange:F1}  RATE {cfg.FireRate:F1}";
        _sellRefundLabel.text = $"+{tower.SellRefund}";

        if (tower.CanUpgrade)
        {
            _upgradeCostLabel.text = tower.NextUpgradeCost.ToString();
            _upgradeButton.interactable = currentGold >= tower.NextUpgradeCost;
            _upgradeButton.gameObject.SetActive(true);
        }
        else
        {
            _upgradeCostLabel.text = "MAX";
            _upgradeButton.interactable = false;
        }
    }
}
