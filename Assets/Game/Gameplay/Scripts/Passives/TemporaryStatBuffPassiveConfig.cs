using MagicStaff.Staff;
using UnityEngine;

[CreateAssetMenu(fileName = "TemporaryStatBuffPassive",
                 menuName  = "MagicStaff/Passives/Temporary Stat Buff")]
public class TemporaryStatBuffPassiveConfig : PassiveEffectDefinition
{
    [SerializeField]
    private StatType _statType;
    [SerializeField]
    private float    _bonusAmount;
    [SerializeField]
    private float    _durationSeconds;
    [SerializeField]
    private float    _intervalSeconds;

    public StatType StatType        => _statType;
    public float    BonusAmount     => _bonusAmount;
    public float    DurationSeconds => _durationSeconds;
    public float    IntervalSeconds => _intervalSeconds;
}
