using System;
using UnityEngine;
using Zenject;

public class PlayerHealthService : IPlayerHealthService, IInitializable
{
    public event Action<float> OnHpChanged;
    public event Action        OnDied;

    public float MaxHp        => _config.maxHp;
    public float CurrentHp    => _currentHp;
    public float NormalizedHp => _currentHp / _config.maxHp;

    private readonly PlayerConfig _config;
    private float _currentHp;

    [Inject]
    public PlayerHealthService(PlayerConfig config) => _config = config;

    public void Initialize() => _currentHp = _config.maxHp;

    public void TakeDamage(float amount)
    {
        if (_currentHp <= 0f) return;
        _currentHp = Mathf.Max(0f, _currentHp - amount);
        OnHpChanged?.Invoke(NormalizedHp);
        if (_currentHp <= 0f) OnDied?.Invoke();
    }
}
