using UnityEngine;

public sealed class TemporaryStatBuffPassive : IActivePassive
{
    public string Name        => _config.DisplayName;
    public string Description => _config.Description;
    public Sprite Icon        => _config.Icon;

    private readonly TemporaryStatBuffPassiveConfig _config;
    private readonly PlayerStatsService             _stats;
    private float _intervalTimer;
    private float _buffTimer;
    private bool  _buffActive;

    public TemporaryStatBuffPassive(TemporaryStatBuffPassiveConfig config,
                                    PlayerStatsService             stats)
    {
        _config = config;
        _stats  = stats;
    }

    public void Tick(float deltaTime)
    {
        if (_buffActive)
        {
            _buffTimer -= deltaTime;
            if (_buffTimer <= 0f)
            {
                _buffActive = false;
                _stats.RemoveBonus(_config.StatType, _config.BonusAmount);
            }
            return;
        }

        _intervalTimer += deltaTime;
        if (_intervalTimer < _config.IntervalSeconds) return;
        _intervalTimer = 0f;
        _buffActive    = true;
        _buffTimer     = _config.DurationSeconds;
        _stats.AddBonus(_config.StatType, _config.BonusAmount);
    }

    public void Cleanup()
    {
        if (_buffActive)
            _stats.RemoveBonus(_config.StatType, _config.BonusAmount);
    }
}
