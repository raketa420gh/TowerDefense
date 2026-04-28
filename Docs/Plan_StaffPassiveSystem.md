# План: Система пассивных эффектов посоха

> Дата: 2026-04-28

---

## Цель

Реализовать систему пассивных эффектов посоха:
- Выбор древка (Shaft) перед забегом в MainMenu — два вида: Дуб и Ясень
- Каждое древко даёт пассивку, работающую весь забег
- Остальные 4 слота посоха заблокированы (пустые)
- Список активных пассивок отображается под HP-баром в HUD
- Модель системы работает независимо от View

---

## Пассивные эффекты

| Древко | Пассивка |
|--------|---------|
| Дуб (Oak) | Восстанавливает 1 HP каждые 10 секунд |
| Ясень (Ash) | Каждые 10 секунд повышает шанс крита на 10% на 1 секунду |

---

## Порядок реализации (компиляция без ошибок на каждом шаге)

| Шаг | Файл | Действие | Зависимости |
|-----|------|----------|-------------|
| 1 | `Gameplay/Scripts/Passives/IActivePassive.cs` | Создать | — |
| 2 | `Gameplay/Scripts/Passives/PassiveEffectDefinition.cs` | Создать | — |
| 3 | `Gameplay/Scripts/Passives/PeriodicHealPassiveConfig.cs` | Создать | Шаг 2 |
| 4 | `Gameplay/Scripts/Passives/TemporaryStatBuffPassiveConfig.cs` | Создать | Шаг 2, `StatType` |
| 5 | `Staff/Scripts/ScriptableObjects/StaffPartConfig.cs` | Изменить — добавить поле `_passives` | Шаг 2 |
| 6 | `Gameplay/Scripts/Experience/PlayerStatsService.cs` | Изменить — добавить `AddBonus`/`RemoveBonus` | — |
| 7 | `Gameplay/Scripts/Passives/IPassiveEffectService.cs` | Создать | Шаг 1 |
| 8 | `Gameplay/Scripts/Passives/Runtime/PeriodicHealPassive.cs` | Создать | Шаги 1, 3, `IPlayerHealthService` |
| 9 | `Gameplay/Scripts/Passives/Runtime/TemporaryStatBuffPassive.cs` | Создать | Шаги 1, 4, 6 |
| 10 | `Gameplay/Scripts/Passives/PassiveEffectService.cs` | Создать | Шаги 1–9 |
| 11 | `Gameplay/Scripts/Views/PassiveItemView.cs` | Создать | Шаг 1 |
| 12 | `Gameplay/Scripts/Views/ActivePassivesView.cs` | Создать | Шаги 1, 11 |
| 13 | `Gameplay/Scripts/Passives/PassiveHudController.cs` | Создать | Шаги 7, 12 |
| 14 | `MainMenu/Scripts/Views/StaffSlotView.cs` | Изменить — добавить `_lockedOverlay` + `RenderLocked()` | — |
| 15 | `MainMenu/Scripts/Views/StaffModificationView.cs` | Изменить — `RenderLoadout` блокирует все слоты кроме Shaft | Шаг 14 |
| 16 | `Gameplay/Installers/GameplayInstaller.cs` | Изменить — новые биндинги | Шаги 10, 12, 13 |
| 17 | Создать SO-ассеты в Unity Editor | Ассеты | Компиляция завершена |
| 18 | Изменения в сценах | Scene setup | Шаг 17 |

---

## Новые файлы

### `IActivePassive.cs`
```csharp
using UnityEngine;

public interface IActivePassive
{
    string Name        { get; }
    string Description { get; }
    Sprite Icon        { get; }
}
```

---

### `PassiveEffectDefinition.cs`
```csharp
using UnityEngine;

public abstract class PassiveEffectDefinition : ScriptableObject
{
    [SerializeField]
    private string _displayName;
    [SerializeField]
    private string _description;
    [SerializeField]
    private Sprite _icon;

    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon        => _icon;
}
```

---

### `PeriodicHealPassiveConfig.cs`
```csharp
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
```

---

### `TemporaryStatBuffPassiveConfig.cs`
```csharp
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
```

---

### `IPassiveEffectService.cs`
```csharp
using System;
using System.Collections.Generic;

public interface IPassiveEffectService
{
    IReadOnlyList<IActivePassive> ActivePassives { get; }
    event Action OnPassivesChanged;
}
```

---

### `Runtime/PeriodicHealPassive.cs`
```csharp
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
```

---

### `Runtime/TemporaryStatBuffPassive.cs`
```csharp
public sealed class TemporaryStatBuffPassive : IActivePassive
{
    public string Name        => _config.DisplayName;
    public string Description => _config.Description;
    public UnityEngine.Sprite Icon => _config.Icon;

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

    // Вызывается при Dispose сервиса — снимает бафф если активен
    public void Cleanup()
    {
        if (_buffActive)
            _stats.RemoveBonus(_config.StatType, _config.BonusAmount);
    }
}
```

---

### `PassiveEffectService.cs`
```csharp
using System;
using System.Collections.Generic;
using MagicStaff.Staff;
using Zenject;

public class PassiveEffectService : IPassiveEffectService, ITickable, IInitializable, IDisposable
{
    public event Action OnPassivesChanged;
    public IReadOnlyList<IActivePassive> ActivePassives => _activePassives;

    private readonly List<IActivePassive>          _activePassives = new();
    private readonly List<PeriodicHealPassive>      _healPassives   = new();
    private readonly List<TemporaryStatBuffPassive> _buffPassives   = new();

    private StaffLoadoutService  _loadoutService;
    private IPlayerHealthService _health;
    private PlayerStatsService   _stats;

    [Inject]
    public void Construct(StaffLoadoutService  loadoutService,
                          IPlayerHealthService health,
                          PlayerStatsService   stats)
    {
        _loadoutService = loadoutService;
        _health         = health;
        _stats          = stats;
    }

    public void Initialize()
    {
        var shaft = _loadoutService.ActiveLoadout.GetPart(StaffSlot.Shaft);
        if (shaft == null) return;

        foreach (var def in shaft.passives)
        {
            switch (def)
            {
                case PeriodicHealPassiveConfig healCfg:
                {
                    var p = new PeriodicHealPassive(healCfg, _health);
                    _healPassives.Add(p);
                    _activePassives.Add(p);
                    break;
                }
                case TemporaryStatBuffPassiveConfig buffCfg:
                {
                    var p = new TemporaryStatBuffPassive(buffCfg, _stats);
                    _buffPassives.Add(p);
                    _activePassives.Add(p);
                    break;
                }
            }
        }

        if (_activePassives.Count > 0)
            OnPassivesChanged?.Invoke();
    }

    public void Tick()
    {
        var dt = UnityEngine.Time.deltaTime;
        foreach (var p in _healPassives) p.Tick(dt);
        foreach (var p in _buffPassives) p.Tick(dt);
    }

    public void Dispose()
    {
        foreach (var p in _buffPassives) p.Cleanup();
    }
}
```

---

### `PassiveItemView.cs`
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassiveItemView : MonoBehaviour
{
    [SerializeField]
    private Image    _icon;
    [SerializeField]
    private TMP_Text _nameLabel;

    public void Render(IActivePassive passive)
    {
        if (_icon != null) _icon.sprite = passive.Icon;
        _nameLabel.text = passive.Name;
    }
}
```

---

### `ActivePassivesView.cs`
```csharp
using System.Collections.Generic;
using MagicStaff.Views;
using UnityEngine;

public class ActivePassivesView : DisplayableView
{
    [SerializeField]
    private Transform       _listRoot;
    [SerializeField]
    private PassiveItemView _itemPrefab;

    public void Render(IReadOnlyList<IActivePassive> passives)
    {
        foreach (Transform child in _listRoot)
            Destroy(child.gameObject);

        foreach (var p in passives)
        {
            var item = Instantiate(_itemPrefab, _listRoot);
            item.Render(p);
        }

        if (passives.Count > 0) Show(); else Hide();
    }
}
```

---

### `PassiveHudController.cs`
```csharp
using System;
using Zenject;

public class PassiveHudController : IInitializable, IDisposable
{
    private ActivePassivesView   _view;
    private IPassiveEffectService _service;

    [Inject]
    public void Construct(ActivePassivesView    view,
                          IPassiveEffectService service)
    {
        _view    = view;
        _service = service;
    }

    public void Initialize()
    {
        _service.OnPassivesChanged += Refresh;
        Refresh();
    }

    public void Dispose() => _service.OnPassivesChanged -= Refresh;

    private void Refresh() => _view.Render(_service.ActivePassives);
}
```

---

## Изменения в существующих файлах

### `StaffPartConfig.cs` — добавить поле
```csharp
[SerializeField]
private PassiveEffectDefinition[] _passives;

public PassiveEffectDefinition[] passives => _passives;
```

---

### `PlayerStatsService.cs` — добавить методы
```csharp
public void AddBonus(StatType stat, float value)
{
    _bonuses.TryGetValue(stat, out var current);
    _bonuses[stat] = current + value;
}

public void RemoveBonus(StatType stat, float value)
{
    _bonuses.TryGetValue(stat, out var current);
    _bonuses[stat] = current - value;
}
```

---

### `StaffSlotView.cs` — добавить locked-состояние
```csharp
[SerializeField]
private GameObject _lockedOverlay;

public void RenderLocked()
{
    _lockedOverlay.SetActive(true);
    _button.interactable = false;
}

// В существующем Render() добавить:
_lockedOverlay.SetActive(false);
_button.interactable = true;
```

---

### `StaffModificationView.cs` — блокировать все слоты кроме Shaft
```csharp
public void RenderLoadout(StaffLoadoutConfig loadout)
{
    foreach (var slotView in _slots)
    {
        if (slotView.Slot == StaffSlot.Shaft)
            slotView.Render(loadout.GetPart(StaffSlot.Shaft));
        else
            slotView.RenderLocked();
    }
}
```

---

### `GameplayInstaller.cs` — новые биндинги
```csharp
// Поле:
[SerializeField]
private ActivePassivesView _activePassivesView;

// В InstallBindings():
Container.BindInstance(_activePassivesView);
Container.BindInterfacesAndSelfTo<PassiveEffectService>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<PassiveHudController>().AsSingle().NonLazy();
```

`BindInterfacesAndSelfTo<PassiveEffectService>` автоматически регистрирует сервис как `IPassiveEffectService`, `ITickable`, `IInitializable`, `IDisposable`.

---

## ScriptableObject-ассеты (создать в Unity Editor)

### `Assets/Game/Staff/Resources/StaffParts/Shaft_Oak.asset`
- Тип: `StaffPartConfig`
- `_partName`: "Дуб"
- `_slot`: `StaffSlot.Shaft`
- `_description`: "Лечит 1 HP каждые 10 секунд во время забега"
- `_passives`: [PeriodicHealPassive_Oak.asset]

### `Assets/Game/Staff/Resources/StaffParts/Shaft_Ash.asset`
- Тип: `StaffPartConfig`
- `_partName`: "Ясень"
- `_slot`: `StaffSlot.Shaft`
- `_description`: "Каждые 10 секунд повышает крит. шанс на 10% на 1 секунду"
- `_passives`: [TemporaryStatBuffPassive_Ash.asset]

### `Assets/Game/Staff/Resources/Passives/PeriodicHealPassive_Oak.asset`
- Тип: `PeriodicHealPassiveConfig`
- `_displayName`: "Лечение Дуба"
- `_description`: "Восстанавливает 1 HP каждые 10 сек"
- `_healAmount`: 1
- `_intervalSeconds`: 10

### `Assets/Game/Staff/Resources/Passives/TemporaryStatBuffPassive_Ash.asset`
- Тип: `TemporaryStatBuffPassiveConfig`
- `_displayName`: "Ярость Ясеня"
- `_description`: "Крит. шанс +10% на 1 сек каждые 10 сек"
- `_statType`: `CritChance`
- `_bonusAmount`: 0.1
- `_durationSeconds`: 1
- `_intervalSeconds`: 10

---

## Изменения в сценах

### Gameplay-сцена
1. Под `HpBarView` добавить GameObject `ActivePassivesView`:
   - Компонент `ActivePassivesView`
   - Дочерний пустой Transform `ListRoot` с `VerticalLayoutGroup`
   - Prefab `PassiveItem` (`Image` + `TMP_Text` + компонент `PassiveItemView`)
2. В `GameplayInstaller` (SceneContext) заполнить поле `_activePassivesView`

### MainMenu-сцена
1. На 4 слотах кроме Shaft добавить дочерний GameObject `LockedOverlay`:
   - `Image` (серый, альфа ~0.7)
   - `TMP_Text` "Заблокировано"
2. В `StaffSlotView` заполнить поле `_lockedOverlay`
3. Немного увеличить RectTransform панели `StaffModificationView`

---

## Замечания

- `StaffLoadoutService` живёт в ProjectContainer — доступен в GameplayContainer через parent-container, дополнительных биндингов не нужно
- `bonusAmount = 0.1` для CritChance согласован с шкалой `PlayerStatsService` (0..1 = 0%..100%)
- Во время активного баффа у `TemporaryStatBuffPassive` интервальный таймер не накапливается — следующий цикл начинается строго после окончания предыдущего
- Блокировка слотов реализована через `_button.interactable = false` — событие `OnSlotClicked` физически не придёт с заблокированного слота, guard в контроллере не нужен
