using System;
using Zenject;

public class TowerInfoPresenter : IInitializable, IDisposable
{
    private readonly TowerInfoView _view;
    private readonly LevelContext _levelContext;
    private readonly TowerUpgradeService _upgradeService;
    private readonly Wallet _wallet;
    private readonly SignalBus _signalBus;

    private Tower _activeTower;

    public TowerInfoPresenter(TowerInfoView view, LevelContext levelContext,
        TowerUpgradeService upgradeService, Wallet wallet, SignalBus signalBus)
    {
        _view = view;
        _levelContext = levelContext;
        _upgradeService = upgradeService;
        _wallet = wallet;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        foreach (var slot in _levelContext.Slots)
            slot.TowerClicked += OnTowerClicked;

        _view.UpgradeClicked += OnUpgrade;
        _view.SellClicked += OnSell;
        _view.CloseClicked += Close;

        _signalBus.Subscribe<GoldChangedSignal>(OnGoldChanged);
        _signalBus.Subscribe<TowerUpgradedSignal>(OnTowerUpgraded);

        _view.Hide();
    }

    public void Dispose()
    {
        foreach (var slot in _levelContext.Slots)
            slot.TowerClicked -= OnTowerClicked;

        _view.UpgradeClicked -= OnUpgrade;
        _view.SellClicked -= OnSell;
        _view.CloseClicked -= Close;

        _signalBus.TryUnsubscribe<GoldChangedSignal>(OnGoldChanged);
        _signalBus.TryUnsubscribe<TowerUpgradedSignal>(OnTowerUpgraded);
    }

    private void OnTowerClicked(TowerSlot slot)
    {
        _activeTower = slot.Tower;
        if (_activeTower == null) return;
        _view.Populate(_activeTower, _wallet.Current);
        _view.Show();
    }

    private void OnUpgrade()
    {
        if (_activeTower == null) return;
        if (_upgradeService.TryUpgrade(_activeTower))
            _view.Populate(_activeTower, _wallet.Current);
    }

    private void OnSell()
    {
        if (_activeTower == null) return;
        _upgradeService.Sell(_activeTower);
        Close();
    }

    private void OnGoldChanged(GoldChangedSignal s)
    {
        if (_view.IsVisible && _activeTower != null)
            _view.Populate(_activeTower, s.Current);
    }

    private void OnTowerUpgraded(TowerUpgradedSignal s)
    {
        if (_view.IsVisible && s.Tower == _activeTower)
            _view.Populate(_activeTower, _wallet.Current);
    }

    private void Close()
    {
        _activeTower = null;
        _view.Hide();
    }
}
