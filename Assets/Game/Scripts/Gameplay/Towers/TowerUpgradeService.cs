using UnityEngine;
using Zenject;

public class TowerUpgradeService
{
    private readonly Wallet _wallet;
    private readonly SignalBus _signalBus;

    public TowerUpgradeService(Wallet wallet, SignalBus signalBus)
    {
        _wallet = wallet;
        _signalBus = signalBus;
    }

    public bool TryUpgrade(Tower tower)
    {
        if (tower == null || !tower.CanUpgrade) return false;
        if (!_wallet.Spend(tower.NextUpgradeCost)) return false;

        tower.ApplyUpgrade();
        _signalBus.Fire(new TowerUpgradedSignal { Tower = tower, Level = tower.Level });
        return true;
    }

    public void Sell(Tower tower)
    {
        if (tower == null) return;
        var refund = tower.SellRefund;
        if (tower.Slot != null) tower.Slot.Detach();

        _wallet.Add(refund);
        _signalBus.Fire(new TowerSoldSignal { Tower = tower, Refund = refund });
        Object.Destroy(tower.gameObject);
    }
}
