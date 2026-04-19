using System;
using System.Collections.Generic;
using MagicStaff.Staff;
using UnityEngine;
using Zenject;

public class UpgradeService : IUpgradeService, IInitializable, IDisposable
{
    private UpgradeLibraryConfig _library;
    private IExperienceService   _experience;
    private PlayerStatsService   _stats;
    private IPlayerHealthService _health;

    public event Action<UpgradeDefinition[]> OnUpgradeChoicesReady;

    [Inject]
    public void Construct(UpgradeLibraryConfig library, IExperienceService experience, PlayerStatsService stats, IPlayerHealthService health)
    {
        _library    = library;
        _experience = experience;
        _stats      = stats;
        _health     = health;
    }

    public void Initialize() => _experience.OnLevelUp += HandleLevelUp;
    public void Dispose()    => _experience.OnLevelUp -= HandleLevelUp;

    private void HandleLevelUp(int _)
        => OnUpgradeChoicesReady?.Invoke(PickRandom(_library.Upgrades, 3));

    public void ApplyUpgrade(UpgradeDefinition upgrade)
    {
        switch (upgrade.effectType)
        {
            case EffectType.InstantHeal:
                _health.Heal(_health.MaxHp * upgrade.value);
                break;
            default:
                _stats.ApplyUpgrade(upgrade);
                if (upgrade.stat == StatType.MaxHp)
                    _health.ForceNotify();
                break;
        }
    }

    private static UpgradeDefinition[] PickRandom(UpgradeDefinition[] source, int count)
    {
        var list   = new List<UpgradeDefinition>(source);
        var result = new UpgradeDefinition[count];
        for (int i = 0; i < count; i++)
        {
            int idx = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[idx]) = (list[idx], list[i]);
            result[i] = list[i];
        }
        return result;
    }
}
