using System;
using Zenject;

public class BuildMenuPresenter : IInitializable, IDisposable
{
    private readonly BuildMenuView _view;
    private readonly TowerCatalog _catalog;
    private readonly TowerFactory _factory;
    private readonly Wallet _wallet;
    private readonly SignalBus _signalBus;
    private readonly LevelContext _levelContext;

    private TowerSlot _activeSlot;

    public BuildMenuPresenter(BuildMenuView view, TowerCatalog catalog, TowerFactory factory,
        Wallet wallet, SignalBus signalBus, LevelContext levelContext)
    {
        _view = view;
        _catalog = catalog;
        _factory = factory;
        _wallet = wallet;
        _signalBus = signalBus;
        _levelContext = levelContext;
    }

    public void Initialize()
    {
        foreach (var slot in _levelContext.Slots)
            slot.Clicked += OnSlotClicked;

        _view.TowerPicked += OnTowerPicked;
        _view.Dismissed += Close;
        _signalBus.Subscribe<GoldChangedSignal>(OnGoldChanged);

        _view.Hide();
    }

    public void Dispose()
    {
        foreach (var slot in _levelContext.Slots)
            slot.Clicked -= OnSlotClicked;

        _view.TowerPicked -= OnTowerPicked;
        _view.Dismissed -= Close;
        _signalBus.TryUnsubscribe<GoldChangedSignal>(OnGoldChanged);
    }

    private void OnSlotClicked(TowerSlot slot)
    {
        _activeSlot = slot;
        _view.Populate(_catalog.Towers, _wallet.Current);
        _view.Show();
    }

    private void OnTowerPicked(TowerConfig config)
    {
        if (_activeSlot == null) return;
        if (_factory.TryBuild(config, _activeSlot))
            Close();
    }

    private void OnGoldChanged(GoldChangedSignal signal)
    {
        if (_view.IsVisible)
            _view.UpdateAffordability(signal.Current);
    }

    private void Close()
    {
        _activeSlot = null;
        _view.Hide();
    }
}
