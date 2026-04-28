using UnityEngine;

[CreateAssetMenu(fileName = "PeriodicHealPassive",
                 menuName  = "MagicStaff/Passives/Periodic Heal")]
public class PeriodicHealPassiveConfig : PassiveEffectDefinition
{
    [SerializeField]
    private int   _healAmount;
    [SerializeField]
    private float _intervalSeconds;

    public int   HealAmount      => _healAmount;
    public float IntervalSeconds => _intervalSeconds;
}
