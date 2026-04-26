using System;
using UnityEngine;
using Zenject;

public class ExperienceService : IExperienceService, IInitializable, IDisposable
{
    private ExperienceConfig _config;
    private EnemyDeathBus    _deathBus;
    private int _currentXp;
    private int _currentLevel = 1;

    public int   CurrentLevel   => _currentLevel;
    public int   CurrentXp      => _currentXp;
    public int   XpForNextLevel => Mathf.RoundToInt(_config.baseXp * Mathf.Pow(_config.growthFactor, _currentLevel - 1));
    public float NormalizedXp   => (float)_currentXp / XpForNextLevel;

    public event Action<int> OnXpChanged;
    public event Action<int> OnLevelUp;

    [Inject]
    public void Construct(ExperienceConfig config, EnemyDeathBus deathBus)
    {
        _config   = config;
        _deathBus = deathBus;
    }

    public void Initialize() => _deathBus.OnEnemyKilled += AddXp;
    public void Dispose()    => _deathBus.OnEnemyKilled -= AddXp;

    private void AddXp(int amount)
    {
        _currentXp += amount;
        OnXpChanged?.Invoke(_currentXp);
        while (_currentXp >= XpForNextLevel) ProcessLevelUp();
    }

    private void ProcessLevelUp()
    {
        _currentXp -= XpForNextLevel;
        _currentLevel++;
        OnLevelUp?.Invoke(_currentLevel);
    }
}
