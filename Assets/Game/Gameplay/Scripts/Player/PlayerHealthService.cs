using System;
using MagicStaff.Staff;
using UnityEngine;
using Zenject;

public class PlayerHealthService : IPlayerHealthService, IInitializable
{
    public event Action<float> OnHpChanged;
    public event Action        OnDied;

    public float MaxHp        => _config.maxHp * (1f + _stats.GetBonus(StatType.MaxHp));
    public float CurrentHp    => _currentHp;
    public float NormalizedHp => _currentHp / MaxHp;

    private PlayerConfig       _config;
    private PlayerStatsService _stats;
    private float _currentHp;

    [Inject]
    public void Construct(PlayerConfig config, PlayerStatsService stats)
    {
        _config = config;
        _stats  = stats;
    }

    public void Initialize() => _currentHp = MaxHp;

    public void TakeDamage(float amount)
    {
        if (_currentHp <= 0f) return;
        _currentHp = Mathf.Max(0f, _currentHp - amount);
        OnHpChanged?.Invoke(NormalizedHp);
        if (_currentHp <= 0f) OnDied?.Invoke();
    }

    public void Heal(float amount)
    {
        if (_currentHp <= 0f) return;
        _currentHp = Mathf.Min(MaxHp, _currentHp + amount);
        OnHpChanged?.Invoke(NormalizedHp);
    }

    public void ForceNotify() => OnHpChanged?.Invoke(NormalizedHp);
}
