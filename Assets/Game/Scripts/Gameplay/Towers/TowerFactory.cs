using UnityEngine;
using Zenject;

public class TowerFactory
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;
    private readonly Wallet _wallet;
    private readonly Transform _root;

    public TowerFactory(DiContainer container, SignalBus signalBus, Wallet wallet,
        [Inject(Id = "TowerRoot")] Transform root)
    {
        _container = container;
        _signalBus = signalBus;
        _wallet = wallet;
        _root = root;
    }

    public bool TryBuild(TowerConfig config, TowerSlot slot)
    {
        if (slot == null || slot.IsOccupied) return false;
        if (!_wallet.CanAfford(config.Cost)) return false;

        _wallet.Spend(config.Cost);
        var tower = _container.InstantiatePrefabForComponent<Tower>(config.Prefab, slot.Position,
            Quaternion.identity, _root);
        tower.Init(config);
        tower.AttachSlot(slot);
        slot.Attach(tower);
        _signalBus.Fire(new TowerBuiltSignal { Tower = tower });
        return true;
    }
}
