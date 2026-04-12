# Итерация 2 — Уровень, путь, враг, база

**Цель:** в сцене `Gameplay.unity` спавнится враг, идёт по waypoints, наносит урон `PlayerBase` при достижении, при HP=0 — переход в `LevelFailed`.

**Зависимости из Итерации 1:** `LevelCatalog`, `LoadLevelState`, `GameplayState` (stub), `SignalBus`, `GameLoopStateMachine`.

---

## Прогресс

- [x] 1. Сигналы геймплея
- [x] 2. `PathData` + `Path` (waypoints в сцене)
- [x] 3. `EnemyConfig` (SO)
- [x] 4. `Enemy` + `EnemyMovement` + `EnemyHealth`
- [x] 5. `EnemyFactory` (упрощённая, без Zenject MemoryPool — `InstantiatePrefabForComponent` + `Object.Destroy`)
- [x] 6. `PlayerBase` + `BaseHealth`
- [x] 7. `WaveConfig` (SO) + `WaveSpawner` (IInitializable/IDisposable, не ITickable)
- [x] 8. `LevelConfig` (SO) + расширение `LevelDefinition`
- [x] 9. `LevelContext` — scene-side агрегатор (Path, Spawner, Base, spawn point)
- [x] 10. `GameplayInstaller` — биндинги
- [x] 11. `GameplayState` — запускает спавн, слушает `BaseDestroyedSignal`
- [x] 12. `LevelFailedState` (минимальный, авто-возврат в меню)
- [x] 13. Регистрация новых state'ов в `GameLoopStateMachine`
- [x] 14. Scene/prefab setup: `Gameplay.unity`, префаб уровня 1 из Kenney-тайлов, префаб врага `ufo-a`
- [x] 15. Ассет `LevelConfig_01`, `EnemyConfig_UfoA`, `WaveConfig_Level1_W1`
- [x] 16. Ручной тест: враг доходит → база умирает → MainMenu

---

## 1. Сигналы

`Assets/Game/Scripts/Core/Signals/GameplaySignals.cs`
```csharp
public struct EnemySpawnedSignal { public Enemy Enemy; }
public struct EnemyKilledSignal { public Enemy Enemy; public int Reward; }
public struct EnemyReachedBaseSignal { public int Damage; }
public struct BaseHealthChangedSignal { public int Current; public int Max; }
public struct BaseDestroyedSignal { }
public struct WaveStartedSignal { public int Index; }
public struct WaveCompletedSignal { public int Index; }
public struct AllWavesCompletedSignal { }
public struct LevelFailedSignal { }
```

Регистрация в `ProjectInstaller.InstallBindings()` после существующих `DeclareSignal`:
```csharp
Container.DeclareSignal<EnemySpawnedSignal>();
Container.DeclareSignal<EnemyKilledSignal>();
Container.DeclareSignal<EnemyReachedBaseSignal>();
Container.DeclareSignal<BaseHealthChangedSignal>();
Container.DeclareSignal<BaseDestroyedSignal>();
Container.DeclareSignal<WaveStartedSignal>();
Container.DeclareSignal<WaveCompletedSignal>();
Container.DeclareSignal<AllWavesCompletedSignal>();
Container.DeclareSignal<LevelFailedSignal>();
```

---

## 2. Path

`Assets/Game/Scripts/Gameplay/Level/Path.cs`
```csharp
using UnityEngine;

public class Path : MonoBehaviour
{
    [SerializeField]
    private Transform[] _waypoints;

    public int Count => _waypoints.Length;

    public Vector3 GetPoint(int index) => _waypoints[index].position;

    public Vector3 SpawnPoint => _waypoints[0].position;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_waypoints == null || _waypoints.Length < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _waypoints.Length - 1; i++)
            if (_waypoints[i] && _waypoints[i + 1])
                Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
    }
#endif
}
```

---

## 3. EnemyConfig

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

    public string Id => _id;
    public Enemy Prefab => _prefab;
    public int MaxHealth => _maxHealth;
    public float Speed => _speed;
    public int Reward => _reward;
    public int BaseDamage => _baseDamage;
}
```

---

## 4. Enemy / Movement / Health

`Assets/Game/Scripts/Gameplay/Enemies/Enemy.cs`
```csharp
using UnityEngine;
using Zenject;

[RequireComponent(typeof(EnemyMovement), typeof(EnemyHealth))]
public class Enemy : MonoBehaviour, IPoolable<EnemyConfig, Path, IMemoryPool>
{
    public EnemyConfig Config => _config;
    public EnemyMovement Movement => _movement;
    public EnemyHealth Health => _health;

    private EnemyMovement _movement;
    private EnemyHealth _health;
    private EnemyConfig _config;
    private IMemoryPool _pool;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<EnemyHealth>();
    }

    public void OnSpawned(EnemyConfig config, Path path, IMemoryPool pool)
    {
        _config = config;
        _pool = pool;
        _health.Init(config.MaxHealth);
        _movement.Init(path, config.Speed);
        gameObject.SetActive(true);
    }

    public void OnDespawned()
    {
        _config = null;
        _pool = null;
        gameObject.SetActive(false);
    }

    public void Release()
    {
        _pool?.Despawn(this);
    }

    public class Factory : PlaceholderFactory<EnemyConfig, Path, Enemy> { }

    public class Pool : MonoPoolableMemoryPool<EnemyConfig, Path, IMemoryPool, Enemy> { }
}
```

`Assets/Game/Scripts/Gameplay/Enemies/EnemyMovement.cs`
```csharp
using UnityEngine;
using Zenject;

public class EnemyMovement : MonoBehaviour, ITickable
{
    public bool ReachedEnd => _reachedEnd;

    private Path _path;
    private float _speed;
    private int _nextIndex;
    private bool _reachedEnd;

    public void Init(Path path, float speed)
    {
        _path = path;
        _speed = speed;
        _nextIndex = 1;
        _reachedEnd = false;
        transform.position = path.SpawnPoint;
    }

    public void Tick()
    {
        if (_reachedEnd || _path == null) return;

        var target = _path.GetPoint(_nextIndex);
        var pos = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
        transform.position = pos;

        var flat = new Vector3(target.x - pos.x, 0, target.z - pos.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flat);

        if ((pos - target).sqrMagnitude < 0.0025f)
        {
            _nextIndex++;
            if (_nextIndex >= _path.Count)
                _reachedEnd = true;
        }
    }
}
```
> `ITickable` на MonoBehaviour не работает через Zenject автобинд — вместо этого Tick вызывается из `Enemy` в `Update()`, либо регистрируем в `TickableManager`. Простейший путь: заменить `ITickable` на `private void Update()` и не биндить.

**Упрощённая версия без Zenject:**
```csharp
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public bool ReachedEnd => _reachedEnd;

    private Path _path;
    private float _speed;
    private int _nextIndex;
    private bool _reachedEnd;

    public void Init(Path path, float speed)
    {
        _path = path;
        _speed = speed;
        _nextIndex = 1;
        _reachedEnd = false;
        transform.position = path.SpawnPoint;
    }

    private void Update()
    {
        if (_reachedEnd || _path == null) return;
        var target = _path.GetPoint(_nextIndex);
        var pos = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
        transform.position = pos;
        var flat = new Vector3(target.x - pos.x, 0, target.z - pos.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flat);
        if ((pos - target).sqrMagnitude < 0.0025f && ++_nextIndex >= _path.Count)
            _reachedEnd = true;
    }
}
```

`Assets/Game/Scripts/Gameplay/Enemies/EnemyHealth.cs`
```csharp
using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public event Action Died;

    public int Current => _current;
    public bool IsDead => _current <= 0;

    private int _current;

    public void Init(int max)
    {
        _current = max;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        _current -= amount;
        if (_current <= 0) Died?.Invoke();
    }
}
```

---

## 5. EnemyFactory

`Assets/Game/Scripts/Gameplay/Enemies/EnemyFactory.cs`
```csharp
using UnityEngine;
using Zenject;

public class EnemyFactory
{
    private readonly Enemy.Factory _factory;
    private readonly SignalBus _signalBus;

    public EnemyFactory(Enemy.Factory factory, SignalBus signalBus)
    {
        _factory = factory;
        _signalBus = signalBus;
    }

    public Enemy Spawn(EnemyConfig config, Path path)
    {
        var enemy = _factory.Create(config, path);
        enemy.Health.Died += () => OnDied(enemy);
        _signalBus.Fire(new EnemySpawnedSignal { Enemy = enemy });
        return enemy;
    }

    private void OnDied(Enemy enemy)
    {
        _signalBus.Fire(new EnemyKilledSignal { Enemy = enemy, Reward = enemy.Config.Reward });
        enemy.Release();
    }
}
```

Биндинг в `GameplayInstaller` (см. п.10):
```csharp
Container.BindFactory<EnemyConfig, Path, Enemy, Enemy.Factory>()
    .FromPoolableMemoryPool<EnemyConfig, Path, Enemy, Enemy.Pool>(p => p
        .WithInitialSize(16)
        .FromComponentInNewPrefab(_defaultEnemyPrefab) // placeholder — переопределяется ниже
        .UnderTransformGroup("Enemies"));
```
> Из-за того, что prefab зависит от конфига, проще использовать **`FromSubContainerResolve`** или **ручной пул**. Для MVP — **не использовать pool**, `EnemyFactory` делает `Object.Instantiate(config.Prefab)` и `Object.Destroy` в `OnDied`. Упрощённая версия:

```csharp
using UnityEngine;
using Zenject;

public class EnemyFactory
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;
    private readonly Transform _root;

    public EnemyFactory(DiContainer container, SignalBus signalBus, [Inject(Id = "EnemyRoot")] Transform root)
    {
        _container = container;
        _signalBus = signalBus;
        _root = root;
    }

    public Enemy Spawn(EnemyConfig config, Path path)
    {
        var enemy = _container.InstantiatePrefabForComponent<Enemy>(config.Prefab, _root);
        enemy.Init(config, path);
        enemy.Health.Died += () =>
        {
            _signalBus.Fire(new EnemyKilledSignal { Enemy = enemy, Reward = config.Reward });
            Object.Destroy(enemy.gameObject);
        };
        _signalBus.Fire(new EnemySpawnedSignal { Enemy = enemy });
        return enemy;
    }
}
```

Упрощённый `Enemy`:
```csharp
using UnityEngine;

[RequireComponent(typeof(EnemyMovement), typeof(EnemyHealth))]
public class Enemy : MonoBehaviour
{
    public EnemyConfig Config => _config;
    public EnemyMovement Movement => _movement;
    public EnemyHealth Health => _health;

    private EnemyMovement _movement;
    private EnemyHealth _health;
    private EnemyConfig _config;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<EnemyHealth>();
    }

    public void Init(EnemyConfig config, Path path)
    {
        _config = config;
        _health.Init(config.MaxHealth);
        _movement.Init(path, config.Speed);
    }
}
```

> Пулинг переносится в Итерацию 3 (`ProjectilePool`) и далее — для Итерации 2 важнее заставить работать цикл.

---

## 6. PlayerBase

`Assets/Game/Scripts/Gameplay/Level/PlayerBase.cs`
```csharp
using UnityEngine;
using Zenject;

public class PlayerBase : MonoBehaviour
{
    [SerializeField]
    private float _reachRadius = 0.8f;

    private SignalBus _signalBus;
    private int _maxHp;
    private int _currentHp;

    [Inject]
    public void Construct(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Init(int maxHp)
    {
        _maxHp = maxHp;
        _currentHp = maxHp;
        _signalBus.Fire(new BaseHealthChangedSignal { Current = _currentHp, Max = _maxHp });
    }

    public void ApplyDamage(int damage)
    {
        if (_currentHp <= 0) return;
        _currentHp = Mathf.Max(0, _currentHp - damage);
        _signalBus.Fire(new BaseHealthChangedSignal { Current = _currentHp, Max = _maxHp });
        if (_currentHp == 0)
            _signalBus.Fire(new BaseDestroyedSignal());
    }

    public float ReachRadiusSqr => _reachRadius * _reachRadius;
}
```

---

## 7. WaveConfig + WaveSpawner

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
    }

    [SerializeField]
    private List<SubWave> _subWaves = new();

    [SerializeField]
    private float _delayAfter = 5f;

    public IReadOnlyList<SubWave> SubWaves => _subWaves;
    public float DelayAfter => _delayAfter;
}
```

`Assets/Game/Scripts/Gameplay/Waves/WaveSpawner.cs`
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class WaveSpawner
{
    private readonly EnemyFactory _factory;
    private readonly SignalBus _signalBus;
    private readonly ICoroutineRunner _runner;

    public WaveSpawner(EnemyFactory factory, SignalBus signalBus, ICoroutineRunner runner)
    {
        _factory = factory;
        _signalBus = signalBus;
        _runner = runner;
    }

    public void Run(IReadOnlyList<WaveConfig> waves, Path path)
    {
        _runner.Run(RunRoutine(waves, path));
    }

    private IEnumerator RunRoutine(IReadOnlyList<WaveConfig> waves, Path path)
    {
        for (int i = 0; i < waves.Count; i++)
        {
            _signalBus.Fire(new WaveStartedSignal { Index = i });
            var wave = waves[i];
            foreach (var sub in wave.SubWaves)
                for (int c = 0; c < sub.Count; c++)
                {
                    _factory.Spawn(sub.Enemy, path);
                    yield return new WaitForSeconds(sub.Interval);
                }
            _signalBus.Fire(new WaveCompletedSignal { Index = i });
            yield return new WaitForSeconds(wave.DelayAfter);
        }
        _signalBus.Fire(new AllWavesCompletedSignal());
    }
}
```

`Assets/Game/Scripts/Core/Services/ICoroutineRunner.cs`
```csharp
using System.Collections;
using UnityEngine;

public interface ICoroutineRunner
{
    Coroutine Run(IEnumerator routine);
    void Stop(Coroutine routine);
}

public class CoroutineRunner : MonoBehaviour, ICoroutineRunner
{
    public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);
    public void Stop(Coroutine routine) { if (routine != null) StopCoroutine(routine); }
}
```

---

## 8. LevelConfig + LevelDefinition

`Assets/Game/Scripts/Configs/LevelConfig.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/LevelConfig", fileName = "LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [SerializeField] private int _id;
    [SerializeField] private string _displayName;
    [SerializeField] private string _sceneName;
    [SerializeField] private int _startingGold = 300;
    [SerializeField] private int _baseHealth = 20;
    [SerializeField] private List<WaveConfig> _waves = new();

    public int Id => _id;
    public string DisplayName => _displayName;
    public string SceneName => _sceneName;
    public int StartingGold => _startingGold;
    public int BaseHealth => _baseHealth;
    public IReadOnlyList<WaveConfig> Waves => _waves;
}
```

Обновить `LevelDefinition` — либо заменить полностью на `LevelConfig`, либо добавить ссылку:
```csharp
using System;
using UnityEngine;

[Serializable]
public class LevelDefinition
{
    public int Id;
    public string DisplayName;
    public string SceneName;
    public LevelConfig Config;
}
```
> Сохраняем обратную совместимость с `LoadLevelState`. `LevelCatalog.Get(id)` возвращает `LevelDefinition`, а уже сцена поднимает нужный `LevelConfig` через `LevelContext`.

---

## 9. LevelContext

`Assets/Game/Scripts/Gameplay/Level/LevelContext.cs` — компонент в корне сцены `Gameplay.unity`, собирает ссылки и раздаёт через Zenject.
```csharp
using UnityEngine;
using Zenject;

public class LevelContext : MonoBehaviour
{
    [SerializeField] private LevelConfig _config;
    [SerializeField] private Path _path;
    [SerializeField] private PlayerBase _playerBase;
    [SerializeField] private Transform _enemyRoot;

    public LevelConfig Config => _config;
    public Path Path => _path;
    public PlayerBase PlayerBase => _playerBase;
    public Transform EnemyRoot => _enemyRoot;
}
```

---

## 10. GameplayInstaller

`Assets/Game/Scripts/Bootstrap/GameplayInstaller.cs`
```csharp
using UnityEngine;
using Zenject;

public class GameplayInstaller : MonoInstaller
{
    [SerializeField]
    private LevelContext _levelContext;

    [SerializeField]
    private CoroutineRunner _coroutineRunner;

    public override void InstallBindings()
    {
        Container.Bind<LevelContext>().FromInstance(_levelContext).AsSingle();
        Container.Bind<Path>().FromInstance(_levelContext.Path).AsSingle();
        Container.Bind<PlayerBase>().FromInstance(_levelContext.PlayerBase).AsSingle();
        Container.Bind<Transform>().WithId("EnemyRoot").FromInstance(_levelContext.EnemyRoot).AsSingle();

        Container.Bind<ICoroutineRunner>().FromInstance(_coroutineRunner).AsSingle();

        Container.Bind<EnemyFactory>().AsSingle();
        Container.Bind<WaveSpawner>().AsSingle();
    }
}
```

---

## 11. GameplayState

Переписать: активирует базу, запускает спавнер, слушает `BaseDestroyedSignal` и `AllWavesCompletedSignal`.

```csharp
using UnityEngine;
using Zenject;

public class GameplayState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;

    public GameplayState(GameLoopStateMachine stateMachine, DiContainer container, SignalBus signalBus)
        : base(stateMachine)
    {
        _container = container;
        _signalBus = signalBus;
    }

    public override void OnStateRegistered()
    {
        _signalBus.Subscribe<BaseDestroyedSignal>(OnBaseDestroyed);
        _signalBus.Subscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
        _signalBus.Subscribe<EnemyReachedBaseSignal>(OnEnemyReachedBase);
    }

    public override void OnStateActivated()
    {
        Debug.Log("[GameplayState] activated");
        // Sceme-scope container ищется через ProjectContext.Instance
        var sceneContainer = ProjectContext.Instance.Container;
        var levelContext = FindLevelContext();
        if (levelContext == null) { Debug.LogError("LevelContext not found"); return; }

        levelContext.PlayerBase.Init(levelContext.Config.BaseHealth);

        var spawner = sceneContainer.TryResolve<WaveSpawner>()
                      ?? SceneContext.Instance.Container.Resolve<WaveSpawner>();
        spawner.Run(levelContext.Config.Waves, levelContext.Path);
    }

    public override void Update() { }

    public override void OnStateDisabled() { }

    private LevelContext FindLevelContext() =>
        Object.FindFirstObjectByType<LevelContext>();

    private void OnBaseDestroyed()
    {
        _signalBus.Fire(new LevelFailedSignal());
        StateMachine.SetState(GameLoopStateMachine.State.LevelFailed);
    }

    private void OnAllWavesCompleted()
    {
        StateMachine.SetState(GameLoopStateMachine.State.LevelComplete);
    }

    private void OnEnemyReachedBase(EnemyReachedBaseSignal signal)
    {
        // используется, если связка Enemy→Base идёт через сигнал, а не через прямой вызов
    }
}
```
> В итерации 2 враг наносит урон через прямой вызов `PlayerBase.ApplyDamage(config.BaseDamage)` из скрипта-помощника `EnemyBaseDamager` (добавить на префаб врага, триггерится по `EnemyMovement.ReachedEnd`). Альтернатива — проверка в `GameplayState.Update`.

`Assets/Game/Scripts/Gameplay/Enemies/EnemyBaseDamager.cs`
```csharp
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Enemy), typeof(EnemyMovement))]
public class EnemyBaseDamager : MonoBehaviour
{
    private Enemy _enemy;
    private EnemyMovement _movement;
    private PlayerBase _base;
    private bool _applied;

    [Inject]
    public void Construct(PlayerBase playerBase)
    {
        _base = playerBase;
    }

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _movement = GetComponent<EnemyMovement>();
    }

    private void Update()
    {
        if (_applied || !_movement.ReachedEnd) return;
        _applied = true;
        _base.ApplyDamage(_enemy.Config.BaseDamage);
        Destroy(gameObject);
    }
}
```

---

## 12. LevelFailedState (минимальный)

`Assets/Game/Scripts/Core/GameLoop/States/LevelFailedState.cs`
```csharp
using UnityEngine;

public class LevelFailedState : GameLoopState
{
    private readonly SceneLoader _sceneLoader;
    private float _returnAt;

    public LevelFailedState(GameLoopStateMachine stateMachine, SceneLoader sceneLoader)
        : base(stateMachine)
    {
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        Debug.Log("[LevelFailedState] — возврат в меню через 1.5с");
        _returnAt = Time.time + 1.5f;
    }

    public override void Update()
    {
        if (Time.time >= _returnAt)
            _sceneLoader.LoadScene("Menu", () =>
                StateMachine.SetState(GameLoopStateMachine.State.MainMenu));
    }

    public override void OnStateDisabled() { }
}
```

Заглушка `LevelCompleteState` — тот же шаблон с другим логом.

---

## 13. Регистрация state'ов

`GameLoopStateMachine.RegisterStates()`:
```csharp
RegisterState(_container.Instantiate<InitializeState>(new object[] { this }), State.Initialize);
RegisterState(_container.Instantiate<MainMenuState>(new object[] { this }), State.MainMenu);
RegisterState(_container.Instantiate<LoadLevelState>(new object[] { this }), State.LoadLevel);
RegisterState(_container.Instantiate<GameplayState>(new object[] { this }), State.Gameplay);
RegisterState(_container.Instantiate<LevelFailedState>(new object[] { this }), State.LevelFailed);
RegisterState(_container.Instantiate<LevelCompleteState>(new object[] { this }), State.LevelComplete);
```

---

## 14. Scene / prefab setup (Unity Editor)

1. Создать сцену `Assets/Game/Scenes/Gameplay.unity`.
2. Добавить пустой `LevelContext` (root) → на нём компонент `LevelContext`.
3. Внутри:
   - `Path` — пустой GO с компонентом `Path`, дочерние `Waypoint_0..N` (2–4 точки, прямая линия).
   - Prefab карты из Kenney — плитки `tile-straight` уложить под путь.
   - `PlayerBase` — GO с компонентом `PlayerBase`, модель `wood-structure-high` в конечной waypoint.
   - `EnemyRoot` — пустой GO для спавненных врагов.
4. Добавить `SceneContext` + `GameplayInstaller`, прокинуть ссылки на `LevelContext` и `CoroutineRunner` (CoroutineRunner — отдельный GO с компонентом).
5. Camera + Directional Light.
6. Prefab врага `Enemy_UfoA`: модель `enemy-ufo-a`, компоненты `Enemy`, `EnemyMovement`, `EnemyHealth`, `EnemyBaseDamager`.
7. Ассеты:
   - `Assets/Game/Configs/Enemies/EnemyConfig_UfoA.asset` (HP 50, speed 2, reward 10, damage 1, prefab → Enemy_UfoA).
   - `Assets/Game/Configs/Waves/WaveConfig_L1_W1.asset` (1 SubWave: EnemyConfig_UfoA × 3, interval 1.5, delayAfter 3).
   - `Assets/Game/Configs/Levels/LevelConfig_01.asset` (id 1, sceneName "Gameplay", startingGold 300, baseHealth 20, waves: [L1_W1]).
   - В `LevelCatalog` проставить `LevelDefinition.Config` = `LevelConfig_01`.
8. Build Settings — добавить сцену `Gameplay.unity`.

---

## 15. Тест-план

1. Запуск из `Boot` → Menu → клик по уровню 1.
2. `LoadLevel` грузит `Gameplay.unity`.
3. Через <1с появляется первый UFO, движется по waypoints.
4. Достижение базы → `BaseHealthChangedSignal` → HP уменьшается.
5. После 3 врагов база умирает (HP 20, dmg 1 × 3 = 3 → нужно уменьшить HP базы до 3 для быстрого теста).
6. `BaseDestroyedSignal` → `LevelFailedState` → авто-возврат в `Menu`.
7. В консоли — последовательные логи состояний, никаких NRE.

---

## Отклонения от плана

- **EnemyFactory**: пулинг отложен — используется `_container.InstantiatePrefabForComponent` + `Object.Destroy`. `IMemoryPool` / `MonoPoolableMemoryPool` не применялся.
- **WaveSpawner**: в итерации 2 реализован как заглушка (одна волна). Финальная версия с `IInitializable/IDisposable`, early-start и alive-counter появилась в итерации 4.
- **WaveSpawner.Run**: подпись `Run(waves, paths)` — принимает `IReadOnlyList<Path>` (добавлено в итерации 6). В итерации 2 был `Run(waves, path)`.
- **ICoroutineRunner.Stop**: метод называется `Stop(Coroutine)`, не `StopRoutine(Coroutine)`.
- **LevelConfig**: в финальной версии нет поля `SceneName` — вместо него `LevelContext LevelPrefab`. Сцены загружаются через `LevelDefinition.SceneName` из каталога.
- **LevelCompleteState**: в итерации 2 был `LevelCompleteState`-заглушка. Финальный вариант — в итерации 4.

---

## Технические заметки по итерации
- Pool врагов намеренно отложен (см. итерацию 3/4) — упрощает биндинг.
- `CoroutineRunner` живёт в сцене геймплея, не в ProjectContext (умирает вместе со сценой — ок).
- Сигналы `EnemyKilled`/`EnemyReachedBase` добавлены заранее, хотя в итерации 2 используется только `BaseHealthChanged`/`BaseDestroyed` — это задел под итерацию 3 (экономика).
- `EnemyBaseDamager` — временное решение; в будущем заменить на чистый сигнал `EnemyReachedBase` из `EnemyMovement` + обработчик в `PlayerBase` через `SignalBus`.
- `Object.FindFirstObjectByType<LevelContext>()` в `GameplayState` — компромисс: ProjectContext-state не имеет доступа к scene-контейнеру напрямую. Альтернатива — `LevelLoadedSignal` расширить полем `LevelContext` и кэшировать.
