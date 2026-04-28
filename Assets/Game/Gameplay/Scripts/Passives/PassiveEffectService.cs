using System;
using System.Collections.Generic;
using MagicStaff.Staff;
using Zenject;

public class PassiveEffectService : IPassiveEffectService, ITickable, IInitializable, IDisposable
{
    public event Action OnPassivesChanged;
    public IReadOnlyList<IActivePassive> ActivePassives => _activePassives;

    private readonly List<IActivePassive>          _activePassives = new();
    private readonly List<PeriodicHealPassive>      _healPassives   = new();
    private readonly List<TemporaryStatBuffPassive> _buffPassives   = new();

    private StaffLoadoutService  _loadoutService;
    private IPlayerHealthService _health;
    private PlayerStatsService   _stats;

    [Inject]
    public void Construct(StaffLoadoutService  loadoutService,
                          IPlayerHealthService health,
                          PlayerStatsService   stats)
    {
        _loadoutService = loadoutService;
        _health         = health;
        _stats          = stats;
    }

    public void Initialize()
    {
        var shaft = _loadoutService.ActiveLoadout.GetPart(StaffSlot.Shaft);
        if (shaft == null || shaft.passives == null) return;

        foreach (var def in shaft.passives)
        {
            switch (def)
            {
                case PeriodicHealPassiveConfig healCfg:
                {
                    var p = new PeriodicHealPassive(healCfg, _health);
                    _healPassives.Add(p);
                    _activePassives.Add(p);
                    break;
                }
                case TemporaryStatBuffPassiveConfig buffCfg:
                {
                    var p = new TemporaryStatBuffPassive(buffCfg, _stats);
                    _buffPassives.Add(p);
                    _activePassives.Add(p);
                    break;
                }
            }
        }

        if (_activePassives.Count > 0)
            OnPassivesChanged?.Invoke();
    }

    public void Tick()
    {
        var dt = UnityEngine.Time.deltaTime;
        foreach (var p in _healPassives) p.Tick(dt);
        foreach (var p in _buffPassives) p.Tick(dt);
    }

    public void Dispose()
    {
        foreach (var p in _buffPassives) p.Cleanup();
    }
}
