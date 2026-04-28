using UnityEngine;

public sealed class PeriodicHealPassive : IActivePassive
{
    public string Name        => _config.DisplayName;
    public string Description => _config.Description;
    public Sprite Icon        => _config.Icon;

    private readonly PeriodicHealPassiveConfig _config;
    private readonly IPlayerHealthService      _health;
    private float _timer;

    public PeriodicHealPassive(PeriodicHealPassiveConfig config,
                               IPlayerHealthService      health)
    {
        _config = config;
        _health = health;
    }

    public void Tick(float deltaTime)
    {
        _timer += deltaTime;
        if (_timer < _config.IntervalSeconds) return;
        _timer -= _config.IntervalSeconds;
        _health.Heal(_config.HealAmount);
    }
}
