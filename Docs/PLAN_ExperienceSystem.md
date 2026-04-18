# PLAN: Experience & Level-Up System

## Механика
- Убийство врага → начисляется XP
- Прогрессивная шкала: `required(level) = Round(baseXp * growthFactor^(level-1))`
- При level-up → пауза → выбор 1 из 3 апгрейдов
- UI: полоска XP вверху по центру, 3 прямоугольных панели для выбора

---

## Порядок реализации

### 1. `UpgradeDefinition.cs` — новый
`Assets/Game/Gameplay/Scripts/Experience/UpgradeDefinition.cs`
```csharp
[Serializable]
public sealed class UpgradeDefinition
{
    public string   id;
    public string   title;
    public string   description;
    public Sprite   icon;
    public StatType stat;
    public float    value;
    public bool     isPercent;
}
```

---

### 2. `ExperienceConfig.cs` — новый ScriptableObject
`Assets/Game/Configs/ExperienceConfig.cs`
```csharp
[CreateAssetMenu(menuName = "Config/ExperienceConfig")]
public class ExperienceConfig : ScriptableObject
{
    public int   baseXp       = 100;
    public float growthFactor = 1.4f;
    public int   xpPerKill    = 10;
}
```

---

### 3. `UpgradeLibraryConfig.cs` — новый ScriptableObject
`Assets/Game/Configs/UpgradeLibraryConfig.cs`
```csharp
[CreateAssetMenu(menuName = "Config/UpgradeLibraryConfig")]
public class UpgradeLibraryConfig : ScriptableObject
{
    [SerializeField]
    private UpgradeDefinition[] _upgrades;
    public UpgradeDefinition[] Upgrades => _upgrades;
}
```

---

### 4. `EnemyConfig.cs` — edit
Добавить поле:
```csharp
public int xpReward = 10;
```

---

### 5. `EnemyController.cs` — edit
Добавить событие и переделать смерть:
```csharp
public event Action<int> OnKilled;

// в Initialize():
_health.OnDied += HandleDied;

private void HandleDied()
{
    _health.OnDied -= HandleDied;
    OnKilled?.Invoke(_config.xpReward);
    _pool.Return(this);
}
```
Убрать прямую подписку `_health.OnDied += ReturnToPool`.
`EnemyController` получает `EnemyPool` через Construct.

---

### 6. `EnemyPool.cs` — edit
```csharp
public event Action<int> OnEnemyKilled;

// в Get() после активации:
enemy.OnKilled += HandleEnemyKilled;

private void HandleEnemyKilled(int xp) => OnEnemyKilled?.Invoke(xp);

public void Return(EnemyController e)
{
    e.OnKilled -= HandleEnemyKilled;
    e.gameObject.SetActive(false);
}
```

---

### 7. `IExperienceService.cs` — новый
`Assets/Game/Gameplay/Scripts/Experience/IExperienceService.cs`
```csharp
public interface IExperienceService
{
    public int   CurrentLevel   { get; }
    public int   CurrentXp      { get; }
    public int   XpForNextLevel { get; }
    public float NormalizedXp   { get; }

    public event Action<int> OnXpChanged;
    public event Action<int> OnLevelUp;
}
```

---

### 8. `ExperienceService.cs` — новый
`Assets/Game/Gameplay/Scripts/Experience/ExperienceService.cs`
```csharp
public class ExperienceService : IExperienceService, IInitializable, IDisposable
{
    private readonly ExperienceConfig _config;
    private readonly EnemyPool        _enemyPool;
    private int _currentXp;
    private int _currentLevel = 1;

    public int   CurrentLevel   => _currentLevel;
    public int   CurrentXp      => _currentXp;
    public int   XpForNextLevel => Mathf.RoundToInt(_config.baseXp * Mathf.Pow(_config.growthFactor, _currentLevel - 1));
    public float NormalizedXp   => (float)_currentXp / XpForNextLevel;

    public event Action<int> OnXpChanged;
    public event Action<int> OnLevelUp;

    [Inject]
    public void Construct(ExperienceConfig config, EnemyPool enemyPool)
    {
        _config    = config;
        _enemyPool = enemyPool;
    }

    public void Initialize() => _enemyPool.OnEnemyKilled += AddXp;
    public void Dispose()    => _enemyPool.OnEnemyKilled -= AddXp;

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
```

---

### 9. `PlayerStatsService.cs` — новый
`Assets/Game/Gameplay/Scripts/Experience/PlayerStatsService.cs`
```csharp
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
```

---

### 10. `IUpgradeService.cs` — новый
`Assets/Game/Gameplay/Scripts/Experience/IUpgradeService.cs`
```csharp
public interface IUpgradeService
{
    public event Action<UpgradeDefinition[]> OnUpgradeChoicesReady;
    public void ApplyUpgrade(UpgradeDefinition upgrade);
}
```

---

### 11. `UpgradeService.cs` — новый
`Assets/Game/Gameplay/Scripts/Experience/UpgradeService.cs`
```csharp
public class UpgradeService : IUpgradeService, IInitializable, IDisposable
{
    private readonly UpgradeLibraryConfig _library;
    private readonly IExperienceService   _experience;
    private readonly PlayerStatsService   _stats;

    public event Action<UpgradeDefinition[]> OnUpgradeChoicesReady;

    [Inject]
    public void Construct(UpgradeLibraryConfig library, IExperienceService experience, PlayerStatsService stats)
    {
        _library    = library;
        _experience = experience;
        _stats      = stats;
    }

    public void Initialize() => _experience.OnLevelUp += HandleLevelUp;
    public void Dispose()    => _experience.OnLevelUp -= HandleLevelUp;

    private void HandleLevelUp(int _)
        => OnUpgradeChoicesReady?.Invoke(PickRandom(_library.Upgrades, 3));

    public void ApplyUpgrade(UpgradeDefinition upgrade) => _stats.ApplyUpgrade(upgrade);

    private static UpgradeDefinition[] PickRandom(UpgradeDefinition[] source, int count)
    {
        var list = new List<UpgradeDefinition>(source);
        var result = new UpgradeDefinition[count];
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(i, list.Count);
            (list[i], list[idx]) = (list[idx], list[i]);
            result[i] = list[i];
        }
        return result;
    }
}
```

---

### 12. `ExperienceBarView.cs` — новый
`Assets/Game/Gameplay/Scripts/Views/ExperienceBarView.cs`
```csharp
public class ExperienceBarView : DisplayableView
{
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private TMP_Text _levelLabel;

    public void SetProgress(float normalized) => _slider.value = normalized;
    public void SetLevel(int level)           => _levelLabel.text = $"Lv {level}";
}
```
Префаб: Canvas → top-center anchor, Slider + TMP_Text.

---

### 13. `UpgradeCardView.cs` — новый
`Assets/Game/Gameplay/Scripts/Views/UpgradeCardView.cs`
```csharp
public class UpgradeCardView : MonoBehaviour
{
    public event Action<UpgradeDefinition> OnChosen;

    [SerializeField]
    private Image _icon;
    [SerializeField]
    private TMP_Text _title;
    [SerializeField]
    private TMP_Text _description;
    [SerializeField]
    private Button _button;

    private UpgradeDefinition _data;

    private void Awake() => _button.onClick.AddListener(() => OnChosen?.Invoke(_data));

    public void Render(UpgradeDefinition data)
    {
        _data             = data;
        _icon.sprite      = data.icon;
        _title.text       = data.title;
        _description.text = data.description;
    }
}
```

---

### 14. `UpgradeSelectionView.cs` — новый
`Assets/Game/Gameplay/Scripts/Views/UpgradeSelectionView.cs`
```csharp
public class UpgradeSelectionView : DisplayableView
{
    public event Action<UpgradeDefinition> OnUpgradeChosen;

    [SerializeField]
    private UpgradeCardView[] _cards; // ровно 3

    public void Present(UpgradeDefinition[] choices)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].OnChosen -= HandleChoice;
            _cards[i].Render(choices[i]);
            _cards[i].OnChosen += HandleChoice;
        }
        Show();
    }

    private void HandleChoice(UpgradeDefinition upgrade)
    {
        foreach (var c in _cards) c.OnChosen -= HandleChoice;
        OnUpgradeChosen?.Invoke(upgrade);
        Hide();
    }
}
```
Префаб: fullscreen overlay, 3 прямоугольных панели по горизонтали по центру.

---

### 15. `ExperienceHudController.cs` — новый
`Assets/Game/Gameplay/Scripts/Experience/ExperienceHudController.cs`
```csharp
public class ExperienceHudController : IInitializable, IDisposable
{
    private readonly IExperienceService   _experience;
    private readonly IUpgradeService      _upgradeService;
    private readonly ExperienceBarView    _barView;
    private readonly UpgradeSelectionView _selectionView;

    [Inject]
    public void Construct(
        IExperienceService   experience,
        IUpgradeService      upgradeService,
        ExperienceBarView    barView,
        UpgradeSelectionView selectionView)
    {
        _experience      = experience;
        _upgradeService  = upgradeService;
        _barView         = barView;
        _selectionView   = selectionView;
    }

    public void Initialize()
    {
        _experience.OnXpChanged              += HandleXpChanged;
        _experience.OnLevelUp                += HandleLevelUp;
        _upgradeService.OnUpgradeChoicesReady += HandleChoicesReady;
        _selectionView.OnUpgradeChosen        += HandleUpgradeChosen;

        _barView.SetLevel(_experience.CurrentLevel);
        _barView.SetProgress(0f);
        _selectionView.Hide();
    }

    public void Dispose()
    {
        _experience.OnXpChanged              -= HandleXpChanged;
        _experience.OnLevelUp                -= HandleLevelUp;
        _upgradeService.OnUpgradeChoicesReady -= HandleChoicesReady;
        _selectionView.OnUpgradeChosen        -= HandleUpgradeChosen;
    }

    private void HandleXpChanged(int _)    => _barView.SetProgress(_experience.NormalizedXp);
    private void HandleLevelUp(int level)  => _barView.SetLevel(level);

    private void HandleChoicesReady(UpgradeDefinition[] choices)
    {
        Time.timeScale = 0f;
        _selectionView.Present(choices);
    }

    private void HandleUpgradeChosen(UpgradeDefinition upgrade)
    {
        _upgradeService.ApplyUpgrade(upgrade);
        Time.timeScale = 1f;
    }
}
```

---

### 16. `GameplayInstaller.cs` — edit
Добавить:
```csharp
[SerializeField] private ExperienceBarView    _experienceBarView;
[SerializeField] private UpgradeSelectionView _upgradeSelectionView;
[SerializeField] private ExperienceConfig     _experienceConfig;
[SerializeField] private UpgradeLibraryConfig _upgradeLibraryConfig;

// В InstallBindings():
Container.BindInstance(_experienceConfig);
Container.BindInstance(_upgradeLibraryConfig);
Container.BindInstance(_experienceBarView);
Container.BindInstance(_upgradeSelectionView);

Container.BindInterfacesAndSelfTo<ExperienceService>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<UpgradeService>().AsSingle().NonLazy();
Container.Bind<PlayerStatsService>().AsSingle();
Container.BindInterfacesAndSelfTo<ExperienceHudController>().AsSingle().NonLazy();
```

---

### 17. `EnemySpawner.cs` — edit (fix)
Заменить field injection на method injection:
```csharp
[Inject]
public void Construct(EnemyPool pool, WaveConfig waveConfig, EnemyConfig enemyConfig)
{
    _pool        = pool;
    _waveConfig  = waveConfig;
    _enemyConfig = enemyConfig;
}
```

---

## Ассеты для создания в Unity

| Ассет | Путь |
|-------|------|
| `ExperienceConfig.asset` | `Assets/Game/Configs/` |
| `UpgradeLibraryConfig.asset` | `Assets/Game/Configs/` |
| `ExperienceBar.prefab` | `Assets/Game/Prefabs/UI/` |
| `UpgradeCard.prefab` | `Assets/Game/Prefabs/UI/` |
| `UpgradeSelection.prefab` | `Assets/Game/Prefabs/UI/` |
