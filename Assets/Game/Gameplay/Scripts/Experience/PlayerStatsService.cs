using System.Collections.Generic;
using MagicStaff.Staff;

public class PlayerStatsService
{
    private readonly Dictionary<StatType, float> _bonuses = new();

    public float GetBonus(StatType stat)
        => _bonuses.TryGetValue(stat, out var v) ? v : 0f;

    public void ApplyUpgrade(UpgradeDefinition upgrade)
    {
        _bonuses.TryGetValue(upgrade.stat, out var current);
        _bonuses[upgrade.stat] = current + upgrade.value;
    }
}
