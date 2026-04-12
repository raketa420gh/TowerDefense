# Итерация 6 — Остальные 4 уровня, новые враги, развилки, босс

**Цель:** наполнить игру контентом по таблице прогрессии (раздел 3 GDD). Создаются prefabs уровней 2–5 из Kenney-тайлов, `EnemyConfig` для `ufo-b/c/d` и `boss`, `WaveConfig`-ассеты согласно таблице, `LevelConfig` 1..5. Главный кодовый сдвиг — поддержка **нескольких путей одновременно** на уровне 3 (split): `LevelConfig` получает `Path[]`, `LevelContext` — массив `Path`, `WaveSpawner` — распределяет подволны по индексу пути, `WaveConfig.SubWave` — поле `PathIndex`. Уровень 5 использует уникальную **босс-волну** (1 враг, `EnemyConfig_Boss` = масштабированный `ufo-d` с HP 2000).

**Зависимости итерации 5:** `LevelConfig`, `LevelContext`, `WaveConfig`, `WaveSpawner`, `EnemyFactory`, `EnemyConfig`, `GameplayState`, `LevelCatalog`, `PlayerProgress`, `Path`.

---

## Прогресс

- [x] 1. `WaveConfig.SubWave` — поле `PathIndex` (default 0) для split-уровней
- [x] 2. `LevelConfig` — пути в `LevelContext.Paths`; `LevelConfig` содержит `LevelContext _levelPrefab` (вместо `SceneName`)
- [x] 3. `LevelContext` — `Path[] _paths` + `OnValidate` для обратной совместимости с `_path`
- [x] 4. `Path.cs` — без изменений
- [x] 5. `WaveSpawner.Run(IReadOnlyList<WaveConfig>, IReadOnlyList<Path>)` — выбор пути по `sub.PathIndex`, безопасный clamp
- [x] 6. `GameplayState.OnStateActivated` — передаёт `levelContext.Paths` в `WaveSpawner.Run`
- [x] 7. `EnemyConfig` — поле `_visualScale` (default 1) для босса
- [x] 8. `EnemyFactory.Spawn` — применяет `config.VisualScale` к `enemy.transform.localScale`
- [x] 9. Ассеты `EnemyConfig_UfoB/UfoC/UfoD/Boss.asset` по таблице врагов (раздел 5 GDD)
- [x] 10. Ассеты `WaveConfig_L{1..5}_W{n}.asset` — составы волн из раздела 3 GDD
- [x] 11. Ассеты `LevelConfig_L1..L5.asset` — startingGold, baseHealth, список `_waves`
- [x] 12. Обновить `LevelCatalog.asset` — 5 записей (`LevelDefinition` по 5 конфигам)
- [x] 13–17. Уровни 1–5 реализованы как **отдельные сцены**: `Gameplay.unity`, `Gameplay_L2.unity`, `Gameplay_L3.unity`, `Gameplay_L4.unity`, `Gameplay_L5.unity` (см. отклонения)
- [x] 18. `LevelDefinition.SceneName` — у каждого уровня своя сцена (`"Gameplay"`, `"Gameplay_L2"`, ...)
- [x] 19. `LevelConfig._levelPrefab` — поле `LevelContext LevelPrefab` (тип `LevelContext`, не `GameObject`)
- [~] 20. `GameplayState` **не инстанцирует** `LevelConfig.LevelPrefab` — `LevelContext` уже в сцене; `FindFirstObjectByType<LevelContext>()` (см. отклонения)
- [x] 21. Ручной тест — пройти все 5 уровней подряд, проверить сложность, split на L3, boss на L5, звёзды сохраняются через `PlayerProgress`

---

## 1. WaveConfig — поле PathIndex в SubWave

`Assets/Game/Scripts/Configs/WaveConfig.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/WaveConfig", fileName = "WaveConfig")]
public class WaveConfig : ScriptableObject
{
    [Serializable]
    public class SubWave
    {
        public EnemyConfig Enemy;
        public int Count = 1;
        public float Interval = 1f;
        public int PathIndex = 0;
    }

    [SerializeField]
    private List<SubWave> _subWaves = new();

    [SerializeField]
    private float _delayAfter = 5f;

    [SerializeField]
    private int _reward = 25;

    public IReadOnlyList<SubWave> SubWaves => _subWaves;
    public float DelayAfter => _delayAfter;
    public int Reward => _reward;
}
```

---

## 2. LevelConfig — префаб уровня

Добавляем ссылку на prefab, чтобы `GameplayState` инстанцировал нужный уровень без смены сцены.

`Assets/Game/Scripts/Configs/LevelConfig.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/LevelConfig", fileName = "LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [SerializeField] private int _id;
    [SerializeField] private string _displayName;
    [SerializeField] private LevelContext _levelPrefab;
    [SerializeField] private int _startingGold = 300;
    [SerializeField] private int _baseHealth = 20;
    [SerializeField] private List<WaveConfig> _waves = new();

    public int Id => _id;
    public string DisplayName => _displayName;
    public LevelContext LevelPrefab => _levelPrefab;
    public int StartingGold => _startingGold;
    public int BaseHealth => _baseHealth;
    public IReadOnlyList<WaveConfig> Waves => _waves;
}
```

> Поле `_sceneName` удалено: все уровни используют общую сцену `Gameplay.unity`, различается только prefab.

---

## 3. LevelContext — массив путей

`Assets/Game/Scripts/Gameplay/Level/LevelContext.cs`

```csharp
using UnityEngine;

public class LevelContext : MonoBehaviour
{
    [SerializeField] private LevelConfig _config;
    [SerializeField] private Path[] _paths;
    [SerializeField] private PlayerBase _playerBase;
    [SerializeField] private Transform _enemyRoot;
    [SerializeField] private TowerSlot[] _slots;
    [SerializeField] private Transform _towerRoot;
    [SerializeField] private Transform _projectileRoot;
    [SerializeField] private BuildMenuView _buildMenu;
    [SerializeField] private HudView _hud;
    [SerializeField] private LevelCompleteView _completeView;
    [SerializeField] private LevelFailedView _failedView;
    [SerializeField] private TowerInfoView _towerInfoView;

    public LevelConfig Config => _config;
    public Path[] Paths => _paths;
    public PlayerBase PlayerBase => _playerBase;
    public Transform EnemyRoot => _enemyRoot;
    public TowerSlot[] Slots => _slots;
    public Transform TowerRoot => _towerRoot;
    public Transform ProjectileRoot => _projectileRoot;
    public BuildMenuView BuildMenu => _buildMenu;
    public HudView Hud => _hud;
    public LevelCompleteView CompleteView => _completeView;
    public LevelFailedView FailedView => _failedView;
    public TowerInfoView TowerInfoView => _towerInfoView;

    public void SetConfig(LevelConfig config) => _config = config;
}
```

> `Path` → `Path[]`. Все потребители (`TowerAttack` — ищет врагов через `OverlapSphere`, ему путь не нужен) не завязаны на единственный путь.

---

## 4. WaveSpawner — распределение по путям

`Assets/Game/Scripts/Gameplay/Waves/WaveSpawner.cs` — меняется только `Run` и `RunRoutine`.

```csharp
public void Run(IReadOnlyList<WaveConfig> waves, IReadOnlyList<Path> paths)
{
    Stop();
    _aliveEnemies = 0;
    _pendingSpawns = 0;
    _routine = _runner.Run(RunRoutine(waves, paths));
}

private IEnumerator RunRoutine(IReadOnlyList<WaveConfig> waves, IReadOnlyList<Path> paths)
{
    for (int i = 0; i < waves.Count; i++)
    {
        _signalBus.Fire(new WaveStartedSignal { Index = i, Total = waves.Count });

        var wave = waves[i];
        foreach (var sub in wave.SubWaves)
        {
            var path = paths[Mathf.Clamp(sub.PathIndex, 0, paths.Count - 1)];
            for (int c = 0; c < sub.Count; c++)
            {
                _pendingSpawns++;
                _factory.Spawn(sub.Enemy, path);
                yield return new WaitForSeconds(sub.Interval);
            }
        }

        while (_aliveEnemies > 0 || _pendingSpawns > 0)
            yield return null;

        _signalBus.Fire(new WaveCompletedSignal { Index = i, Reward = wave.Reward });

        if (i == waves.Count - 1) break;

        _signalBus.Fire(new WaveBreakStartedSignal { NextIndex = i + 1, Seconds = wave.DelayAfter });

        _skipBreak = false;
        float t = wave.DelayAfter;
        while (t > 0f && !_skipBreak)
        {
            t -= Time.deltaTime;
            yield return null;
        }
    }

    _signalBus.Fire(new AllWavesCompletedSignal());
}
```

---

## 5. EnemyConfig — масштаб визуала для босса

`Assets/Game/Scripts/Configs/EnemyConfig.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/EnemyConfig", fileName = "EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private Enemy _prefab;
    [SerializeField] private int _maxHealth = 50;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private int _reward = 10;
    [SerializeField] private int _baseDamage = 1;
    [SerializeField] private float _visualScale = 1f;

    public string Id => _id;
    public Enemy Prefab => _prefab;
    public int MaxHealth => _maxHealth;
    public float Speed => _speed;
    public int Reward => _reward;
    public int BaseDamage => _baseDamage;
    public float VisualScale => _visualScale;
}
```

---

## 6. EnemyFactory — применение VisualScale

`Assets/Game/Scripts/Gameplay/Enemies/EnemyFactory.cs`

```csharp
public Enemy Spawn(EnemyConfig config, Path path)
{
    var enemy = _container.InstantiatePrefabForComponent<Enemy>(config.Prefab, _root);
    enemy.transform.localScale = Vector3.one * config.VisualScale;
    enemy.Init(config, path);
    enemy.Health.Died += () =>
    {
        _signalBus.Fire(new EnemyKilledSignal { Enemy = enemy, Reward = config.Reward });
        Object.Destroy(enemy.gameObject);
    };
    _signalBus.Fire(new EnemySpawnedSignal { Enemy = enemy });
    return enemy;
}
```

---

## 7. GameplayState — инстансинг префаба уровня и передача путей

`Assets/Game/Scripts/Core/GameLoop/States/GameplayState.cs`

```csharp
using UnityEngine;
using Zenject;

public class GameplayState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;
    private readonly PlayerProgress _progress;
    private readonly LevelCatalog _catalog;

    private LevelContext _spawnedLevel;

    public GameplayState(
        GameLoopStateMachine stateMachine,
        DiContainer container,
        SignalBus signalBus,
        PlayerProgress progress,
        LevelCatalog catalog)
        : base(stateMachine)
    {
        _container = container;
        _signalBus = signalBus;
        _progress = progress;
        _catalog = catalog;
    }

    public override void OnStateRegistered()
    {
        _signalBus.Subscribe<BaseDestroyedSignal>(OnBaseDestroyed);
        _signalBus.Subscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
    }

    public override void OnStateActivated()
    {
        var definition = _catalog.Get(_progress.CurrentLevelId);
        var config = definition.Config;

        var sceneContext = Object.FindFirstObjectByType<SceneContext>();
        var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

        _spawnedLevel = sceneContainer.InstantiatePrefabForComponent<LevelContext>(config.LevelPrefab);
        _spawnedLevel.SetConfig(config);

        _spawnedLevel.PlayerBase.Init(config.BaseHealth);

        var wallet = sceneContainer.Resolve<Wallet>();
        wallet.SetStartingGold(config.StartingGold);

        var spawner = sceneContainer.Resolve<WaveSpawner>();
        spawner.Run(config.Waves, _spawnedLevel.Paths);
    }

    public override void Update() { }

    public override void OnStateDisabled()
    {
        if (_spawnedLevel != null)
        {
            Object.Destroy(_spawnedLevel.gameObject);
            _spawnedLevel = null;
        }
    }

    private void OnBaseDestroyed()
    {
        _signalBus.Fire(new LevelFailedSignal());
        StateMachine.SetState(GameLoopStateMachine.State.LevelFailed);
    }

    private void OnAllWavesCompleted()
    {
        StateMachine.SetState(GameLoopStateMachine.State.LevelComplete);
    }
}
```

> Если в проекте уже есть `PlayerProgress.CurrentLevelId` / `LevelCatalog` в DI — использовать их; если нет — передавать `LevelConfig` через существующий механизм (signal/service-holder). Главное: старый `FindFirstObjectByType<LevelContext>` больше не нужен, уровень инстанцируется из префаба.

---

## 8. Ассеты — EnemyConfig

`Assets/Game/Configs/Enemies/`

| Ассет | id | HP | Speed | Reward | BaseDmg | VisualScale | Prefab |
|---|---|---|---|---|---|---|---|
| `EnemyConfig_UfoA` | ufo-a | 50 | 2.0 | 10 | 1 | 1.0 | `Enemy_UfoA.prefab` |
| `EnemyConfig_UfoB` | ufo-b | 120 | 1.5 | 20 | 1 | 1.0 | `Enemy_UfoB.prefab` |
| `EnemyConfig_UfoC` | ufo-c | 40 | 3.5 | 15 | 1 | 1.0 | `Enemy_UfoC.prefab` |
| `EnemyConfig_UfoD` | ufo-d | 200 | 1.0 | 35 | 2 | 1.0 | `Enemy_UfoD.prefab` |
| `EnemyConfig_Boss` | boss | 2000 | 0.8 | 200 | 10 | 2.5 | `Enemy_UfoD.prefab` |

Префабы врагов — копия `Enemy_UfoA` c заменой меша на соответствующий `enemy-ufo-b/c/d` из Kenney-пака (см. `ref_kenney_asset_pack.md` в памяти). `Boss` переиспользует `Enemy_UfoD.prefab`, увеличение визуала — через `VisualScale`.

---

## 9. Ассеты — WaveConfig по таблице прогрессии

Папка `Assets/Game/Configs/Waves/L{n}/`.

### Level 1 (5 волн, ufo-a, reward за волну 25)
| # | SubWaves | DelayAfter |
|---|---|---|
| W1 | a×5, interval 1.2 | 6 |
| W2 | a×8, 1.0 | 6 |
| W3 | a×10, 0.8 | 6 |
| W4 | a×12, 0.7 | 6 |
| W5 | a×15, 0.6 | 0 |

### Level 2 (7 волн, a+b)
| # | SubWaves |
|---|---|
| W1 | a×8 @1.0 |
| W2 | a×6 @0.8, b×2 @1.5 |
| W3 | a×10 @0.8 |
| W4 | b×4 @1.5 |
| W5 | a×8 @0.6, b×3 @1.5 |
| W6 | a×12 @0.5 |
| W7 | a×10 @0.6, b×5 @1.2 |

### Level 3 (8 волн, a/b/c, **2 пути**)
PathIndex чередуется: чётные подволны — path 0, нечётные — path 1.
| # | SubWaves |
|---|---|
| W1 | a×6 p0, a×6 p1 |
| W2 | a×8 p0, c×3 p1 |
| W3 | b×3 p0, a×6 p1 |
| W4 | c×5 p0, b×3 p1 |
| W5 | a×10 p0, a×10 p1 |
| W6 | b×4 p0, c×6 p1 |
| W7 | a×8 p0, b×4 p1, c×4 p0 |
| W8 | a×10 p0, b×5 p1, c×5 p1 |

### Level 4 (10 волн, a/b/c/d)
Вводится `ufo-d` на W3, `ufo-c` роятся на поздних волнах.
| # | SubWaves |
|---|---|
| W1 | a×10 |
| W2 | a×8, c×3 |
| W3 | d×1, a×6 |
| W4 | b×4, c×4 |
| W5 | d×2, a×8 |
| W6 | c×8 @0.4 |
| W7 | b×5, d×2 |
| W8 | a×12, c×6 |
| W9 | d×3, b×4 |
| W10 | a×10, b×5, c×5, d×2 |

### Level 5 (12 волн, все + босс на W12)
| # | SubWaves |
|---|---|
| W1 | a×12 |
| W2 | a×8, c×4 |
| W3 | b×5 |
| W4 | a×10, b×3, c×3 |
| W5 | d×2, c×6 |
| W6 | b×6, d×2 |
| W7 | c×10 @0.35 |
| W8 | a×15, b×5 |
| W9 | d×4, c×5 |
| W10 | a×10, b×6, c×6, d×3 |
| W11 | d×5, b×5 |
| W12 | **boss×1** (PathIndex 0) |

Формат создания (пример для `WaveConfig_L5_W12.asset`):
- `_subWaves = [ { Enemy=EnemyConfig_Boss, Count=1, Interval=0, PathIndex=0 } ]`
- `_delayAfter = 0`
- `_reward = 150`

---

## 10. Ассеты — LevelConfig

`Assets/Game/Configs/Levels/`

| Ассет | Id | StartingGold | BaseHealth | LevelPrefab | Waves |
|---|---|---|---|---|---|
| `LevelConfig_L1` | 1 | 300 | 20 | `Level_1.prefab` | L1_W1..W5 |
| `LevelConfig_L2` | 2 | 350 | 20 | `Level_2.prefab` | L2_W1..W7 |
| `LevelConfig_L3` | 3 | 400 | 18 | `Level_3.prefab` | L3_W1..W8 |
| `LevelConfig_L4` | 4 | 400 | 15 | `Level_4.prefab` | L4_W1..W10 |
| `LevelConfig_L5` | 5 | 450 | 12 | `Level_5.prefab` | L5_W1..W12 |

Обновить `LevelCatalog.asset` — 5 `LevelDefinition` с этими конфигами. `LevelSelectView` покажет 5 кнопок автоматически.

---

## 11. Prefabs уровней — сборка в редакторе

Каждый префаб — `GameObject` с компонентом `LevelContext`, дочерние:
- `Tiles/` — тайлы Kenney (`tile-straight/corner-a-b/split/crossing/spawn/spawn-round`)
- `Path` (или `Path_A`/`Path_B` для L3) — MonoBehaviour `Path` с waypoint'ами
- `Slots/` — 6–10 `TowerSlot`
- `PlayerBase` — `wood-structure-high` + `PlayerBase`
- `Decor/` — `detail-tree`, `detail-rocks`
- `Roots/` — `EnemyRoot`, `TowerRoot`, `ProjectileRoot` (пустые Transform)
- `UI Canvas` — ссылки на `BuildMenuView`, `HudView`, `LevelCompleteView`, `LevelFailedView`, `TowerInfoView` (переиспользовать префабы из итерации 5)

### Level_1 (существует)
Проверить, что `Paths[0]` заполнен, остальное — как было.

### Level_2 — «Поворот»
`tile-spawn` → 3× `tile-straight` → `tile-corner-a` → 2× `tile-straight` → `tile-corner-b` → 3× `tile-straight` → база. 8 слотов по поворотам.

### Level_3 — «Развилка» (**2 независимых пути**)
Два `tile-spawn` в разных углах, оба ведут к одной базе:
- `Path_A`: левый спавн → длинный ровный участок
- `Path_B`: правый спавн → изогнутый участок
- В `LevelContext._paths` кладутся **оба** `Path` (индексы 0 и 1)
- 10 слотов, покрывающих оба пути

### Level_4 — «Ущелье»
Один длинный (~16 тайлов) путь с парой поворотов, ограниченное количество слотов (6) — заставляет думать.

### Level_5 — «Финал»
Сложный маршрут: поворот → развилка через `tile-split` (**1 Path, визуально сливается**) → длинный финал. 12 слотов, больше декораций. Босс-волна прокатывается по единственному пути.

---

## 12. Тестовый сценарий (ручной, Editor + APK)

1. Меню → L1 — пройти на 3⭐ (HP=20) базовой ballista'ой.
2. L2 — поставить cannon, убедиться, что b-танки выносятся.
3. L3 — убедиться, что враги идут **по двум разным путям** одновременно; слоты, стоящие только на одном пути, не покрывают второй.
4. L4 — проверить, что ufo-c (fast) контрится turret'ом, d-тяжёлые — catapult splash'ем.
5. L5 — довести до W12, убедиться, что босс — единственный враг в волне, `VisualScale = 2.5`, HP огромный, при смерти даёт 200 gold, при входе на базу снимает 10 HP.
6. Возврат в меню — звёзды сохранены в `PlayerProgress` (проверить `persistentDataPath/progress.json`).
7. Все 5 уровней должны пройти подряд без утечек (проверить количество активных `Enemy`/`Projectile` после `LevelComplete`).

---

## 13. Контрольный список готовности итерации

- [x] `WaveConfig.SubWave.PathIndex` собирается и сериализуется
- [x] `LevelContext.Paths` не null, `Length ≥ 1` для всех сцен
- [x] `WaveSpawner.Run` принимает `IReadOnlyList<Path>` — вызов с одним `Path` удалён
- [~] `GameplayState` не инстанцирует уровень из префаба — уровень уже в сцене (см. отклонения)
- [x] Boss спавнится с `transform.localScale = 2.5`
- [x] Все 5 `LevelConfig` заведены в `LevelCatalog`
- [x] `LevelSelectView` показывает 5 кнопок и блокировки по `PlayerProgress.UnlockedLevel`
- [x] Компиляция без ошибок (`read_console`), прохождение всех 5 уровней подряд

---

## Отклонения от плана

### Подход к уровням: отдельные сцены vs одна сцена + prefab

Итерация 6 планировала одну сцену `Gameplay.unity` с инстанцированием `LevelConfig.LevelPrefab` в `GameplayState`. **Реализовано иначе:**

- **5 отдельных сцен**: `Gameplay.unity` (L1), `Gameplay_L2.unity`, `Gameplay_L3.unity`, `Gameplay_L4.unity`, `Gameplay_L5.unity`.
- `LevelDefinition.SceneName` содержит имя конкретной сцены (`"Gameplay"`, `"Gameplay_L2"`, ...).
- `LoadLevelState` грузит нужную сцену, в ней `LevelContext` уже настроен в инспекторе.
- `GameplayState` по-прежнему использует `FindFirstObjectByType<LevelContext>()`.
- `LevelConfig._levelPrefab` (тип `LevelContext`) существует как поле, но `GameplayState` его **не инстанцирует** — поле зарезервировано для потенциального рефакторинга.

**Почему:** подход с отдельными сценами проще в редакторе — каждый уровень правится независимо, без риска сломать чужие `LevelContext`. Разница в рантайм-поведении нулевая.

**Следствие для итерации 8:** Android Build Settings должен включать все 7 сцен (`SampleScene`, `Menu`, `Gameplay`, `Gameplay_L2..L5`).
