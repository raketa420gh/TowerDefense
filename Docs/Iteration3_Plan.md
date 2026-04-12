# Итерация 3 — Башни, атака, снаряды, экономика

**Цель:** игрок кликает `TowerSlot` → открывается `BuildMenuView` → выбирает башню → золото списывается, башня ставится, стреляет в ближайшего врага в радиусе, снаряд летит, наносит урон, убитый враг возвращает золото в `Wallet`.

**Зависимости из итерации 2:** `LevelContext`, `PlayerBase`, `EnemyFactory`, `WaveSpawner`, `ICoroutineRunner`, сигналы `EnemySpawnedSignal` / `EnemyKilledSignal`.

---

## Прогресс

- [x] 1. Новые сигналы (`GoldChangedSignal`, `TowerBuiltSignal`, `TowerSoldSignal`, `ProjectileHitSignal`)
- [x] 2. `TowerConfig` (SO)
- [x] 3. `TowerCatalog` (SO)
- [x] 4. `Wallet` + подписка на `EnemyKilledSignal`
- [x] 5. `Projectile` + `ProjectilePool` (без Zenject MemoryPool — простой `InstantiatePrefabForComponent` + `Destroy`)
- [x] 6. `Tower` + `TowerAttack`
- [x] 7. `TowerSlot` + click-input
- [x] 8. `TowerFactory`
- [x] 9. `BuildMenuView` + `BuildMenuPresenter`
- [x] 10. Расширение `LevelContext` (slots, towerRoot, buildMenu)
- [x] 11. `GameplayInstaller` — биндинги Wallet / TowerFactory / ProjectilePool / BuildMenu
- [x] 12. `GameplayState` — init Wallet из `LevelConfig.StartingGold`
- [x] 13. `ProjectInstaller` — DeclareSignal для новых сигналов
- [x] 14. Layer `Enemies`, коллайдер + Kinematic Rigidbody на префабе врага
- [x] 15. Префаб `Tower_Ballista` + `Projectile_Arrow`
- [x] 16. Ассеты: `TowerConfig_Ballista.asset`, `TowerCatalog.asset`
- [x] 17. Сцена `Gameplay.unity`: 3–4 `TowerSlot`, Canvas + `BuildMenuView`
- [x] 18. Ручной тест: ставим башню → стрельба → враг умирает → +10 золота

---

## 1. Сигналы

Дополнить `Assets/Game/Scripts/Core/Signals/GameplaySignals.cs`:

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

public struct GoldChangedSignal { public int Current; }
public struct TowerBuiltSignal { public Tower Tower; }
public struct TowerSoldSignal { public Tower Tower; public int Refund; }
public struct ProjectileHitSignal { public Enemy Enemy; public int Damage; }
```

Регистрация в `ProjectInstaller.InstallBindings()` — добавить после существующих DeclareSignal:

```csharp
Container.DeclareSignal<GoldChangedSignal>();
Container.DeclareSignal<TowerBuiltSignal>();
Container.DeclareSignal<TowerSoldSignal>();
Container.DeclareSignal<ProjectileHitSignal>();
```

---

## 2. TowerConfig

`Assets/Game/Scripts/Configs/TowerConfig.cs`
```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/TowerConfig", fileName = "TowerConfig")]
public class TowerConfig : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private Tower _prefab;
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private int _cost = 50;
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _range = 5f;
    [SerializeField] private float _fireRate = 1f;
    [SerializeField] private float _projectileSpeed = 15f;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public Tower Prefab => _prefab;
    public Projectile ProjectilePrefab => _projectilePrefab;
    public int Cost => _cost;
    public int Damage => _damage;
    public float Range => _range;
    public float FireRate => _fireRate;
    public float ProjectileSpeed => _projectileSpeed;
}
```

---

## 3. TowerCatalog

`Assets/Game/Scripts/Configs/TowerCatalog.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/TowerCatalog", fileName = "TowerCatalog")]
public class TowerCatalog : ScriptableObject
{
    [SerializeField]
    private List<TowerConfig> _towers = new();

    public IReadOnlyList<TowerConfig> Towers => _towers;
}
```

---

## 4. Wallet

`Assets/Game/Scripts/Gameplay/Economy/Wallet.cs`
```csharp
using System;
using Zenject;

public class Wallet : IInitializable, IDisposable
{
    public int Current => _current;

    private readonly SignalBus _signalBus;
    private int _current;

    public Wallet(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<EnemyKilledSignal>(OnEnemyKilled);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<EnemyKilledSignal>(OnEnemyKilled);
    }

    public void SetStartingGold(int amount)
    {
        _current = amount;
        _signalBus.Fire(new GoldChangedSignal { Current = _current });
    }

    public bool CanAfford(int amount) => _current >= amount;

    public bool Spend(int amount)
    {
        if (!CanAfford(amount)) return false;
        _current -= amount;
        _signalBus.Fire(new GoldChangedSignal { Current = _current });
        return true;
    }

    public void Add(int amount)
    {
        _current += amount;
        _signalBus.Fire(new GoldChangedSignal { Current = _current });
    }

    private void OnEnemyKilled(EnemyKilledSignal signal)
    {
        Add(signal.Reward);
    }
}
```

---

## 5. Projectile + ProjectilePool

`Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs`
```csharp
using UnityEngine;
using Zenject;

public class Projectile : MonoBehaviour, IPoolable<Vector3, Enemy, ProjectileSpawnParams, IMemoryPool>
{
    private Enemy _target;
    private int _damage;
    private float _speed;
    private IMemoryPool _pool;
    private bool _active;

    public void OnSpawned(Vector3 origin, Enemy target, ProjectileSpawnParams p, IMemoryPool pool)
    {
        _pool = pool;
        _target = target;
        _damage = p.Damage;
        _speed = p.Speed;
        transform.position = origin;
        _active = true;
        gameObject.SetActive(true);
    }

    public void OnDespawned()
    {
        _target = null;
        _pool = null;
        _active = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_active) return;
        if (_target == null || _target.Health.IsDead)
        {
            Release();
            return;
        }

        var targetPos = _target.transform.position + Vector3.up * 0.5f;
        var next = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);
        transform.position = next;
        transform.forward = (targetPos - next).sqrMagnitude > 0.0001f
            ? (targetPos - next).normalized
            : transform.forward;

        if ((next - targetPos).sqrMagnitude < 0.04f)
        {
            _target.Health.TakeDamage(_damage);
            Release();
        }
    }

    private void Release()
    {
        if (_pool != null) _pool.Despawn(this);
        else gameObject.SetActive(false);
    }

    public class Factory : PlaceholderFactory<Vector3, Enemy, ProjectileSpawnParams, Projectile> { }
    public class Pool : MonoPoolableMemoryPool<Vector3, Enemy, ProjectileSpawnParams, IMemoryPool, Projectile> { }
}

public struct ProjectileSpawnParams
{
    public int Damage;
    public float Speed;
}
```

`Assets/Game/Scripts/Gameplay/Projectiles/ProjectilePool.cs`
```csharp
using UnityEngine;
using Zenject;

public class ProjectilePool
{
    private readonly DiContainer _container;
    private readonly Transform _root;

    public ProjectilePool(DiContainer container, [Inject(Id = "ProjectileRoot")] Transform root)
    {
        _container = container;
        _root = root;
    }

    public void Spawn(Projectile prefab, Vector3 origin, Enemy target, int damage, float speed)
    {
        var instance = _container.InstantiatePrefabForComponent<Projectile>(prefab, _root);
        instance.OnSpawned(origin, target, new ProjectileSpawnParams { Damage = damage, Speed = speed }, null);
    }
}
```

> Для MVP используем простой spawn без `MemoryPool` — когда `Projectile.Release()` не находит пула, делает `SetActive(false)` и затем `Destroy` можно добавить вручную. Полноценный пул (Zenject `MemoryPool<Vector3, Enemy, ProjectileSpawnParams, Projectile.Pool>`) переносим в полишинг-итерацию 7. Упростим `Release` так:

```csharp
private void Release()
{
    gameObject.SetActive(false);
    Destroy(gameObject);
}
```

— и `ProjectilePool.Spawn` остаётся как есть. Первый проход — без пулинга, но вызовы через `ProjectilePool` чтобы позже подменить реализацию.

---

## 6. Tower + TowerAttack

`Assets/Game/Scripts/Gameplay/Towers/Tower.cs`
```csharp
using UnityEngine;

[RequireComponent(typeof(TowerAttack))]
public class Tower : MonoBehaviour
{
    public TowerConfig Config => _config;
    public TowerAttack Attack => _attack;

    private TowerConfig _config;
    private TowerAttack _attack;

    private void Awake()
    {
        _attack = GetComponent<TowerAttack>();
    }

    public void Init(TowerConfig config)
    {
        _config = config;
        _attack.Init(config);
    }
}
```

`Assets/Game/Scripts/Gameplay/Towers/TowerAttack.cs`
```csharp
using UnityEngine;
using Zenject;

public class TowerAttack : MonoBehaviour
{
    private const int MaxOverlap = 16;
    private const float ScanInterval = 0.1f;

    [SerializeField]
    private Transform _muzzle;

    private ProjectilePool _pool;
    private TowerConfig _config;
    private LayerMask _enemyMask;
    private readonly Collider[] _buffer = new Collider[MaxOverlap];

    private float _cooldown;
    private float _scanTimer;
    private Enemy _currentTarget;

    [Inject]
    public void Construct(ProjectilePool pool)
    {
        _pool = pool;
        _enemyMask = LayerMask.GetMask("Enemies");
    }

    public void Init(TowerConfig config)
    {
        _config = config;
        _cooldown = 0f;
        _scanTimer = 0f;
    }

    private void Update()
    {
        if (_config == null) return;

        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            _scanTimer = ScanInterval;
            _currentTarget = FindNearest();
        }

        if (_currentTarget == null || _currentTarget.Health.IsDead)
            return;

        var flat = _currentTarget.transform.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(flat), 10f * Time.deltaTime);

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        _cooldown = 1f / Mathf.Max(0.0001f, _config.FireRate);
        var origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up;
        _pool.Spawn(_config.ProjectilePrefab, origin, _currentTarget, _config.Damage, _config.ProjectileSpeed);
    }

    private Enemy FindNearest()
    {
        var count = Physics.OverlapSphereNonAlloc(transform.position, _config.Range, _buffer, _enemyMask);
        Enemy best = null;
        float bestSqr = float.MaxValue;
        var pos = transform.position;
        for (int i = 0; i < count; i++)
        {
            var e = _buffer[i].GetComponentInParent<Enemy>();
            if (e == null || e.Health.IsDead) continue;
            var d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = e; }
        }
        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _config.Range);
    }
#endif
}
```

---

## 7. TowerSlot

`Assets/Game/Scripts/Gameplay/Towers/TowerSlot.cs`
```csharp
using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TowerSlot : MonoBehaviour
{
    public event Action<TowerSlot> Clicked;

    public bool IsOccupied => _tower != null;
    public Tower Tower => _tower;
    public Vector3 Position => transform.position;

    [SerializeField]
    private GameObject _highlight;

    private Tower _tower;

    public void Attach(Tower tower)
    {
        _tower = tower;
        if (_highlight != null) _highlight.SetActive(false);
    }

    public void Detach()
    {
        _tower = null;
        if (_highlight != null) _highlight.SetActive(true);
    }

    private void OnMouseDown()
    {
        if (IsOccupied) return;
        Clicked?.Invoke(this);
    }
}
```

> `OnMouseDown` требует Physics Raycaster на камере и коллайдер на слоте — работает и с мышью в Editor, и с тапом на мобилке. Альтернатива — Unity Input System + `IPointerClickHandler`; оставим для итерации 7 (полишинг).

---

## 8. TowerFactory

`Assets/Game/Scripts/Gameplay/Towers/TowerFactory.cs`
```csharp
using UnityEngine;
using Zenject;

public class TowerFactory
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;
    private readonly Wallet _wallet;
    private readonly Transform _root;

    public TowerFactory(DiContainer container, SignalBus signalBus, Wallet wallet,
        [Inject(Id = "TowerRoot")] Transform root)
    {
        _container = container;
        _signalBus = signalBus;
        _wallet = wallet;
        _root = root;
    }

    public bool TryBuild(TowerConfig config, TowerSlot slot)
    {
        if (slot == null || slot.IsOccupied) return false;
        if (!_wallet.CanAfford(config.Cost)) return false;

        _wallet.Spend(config.Cost);
        var tower = _container.InstantiatePrefabForComponent<Tower>(config.Prefab, slot.Position,
            Quaternion.identity, _root);
        tower.Init(config);
        slot.Attach(tower);
        _signalBus.Fire(new TowerBuiltSignal { Tower = tower });
        return true;
    }
}
```

---

## 9. BuildMenuView + Presenter

`Assets/Game/Scripts/UI/Views/BuildMenuView.cs`
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuView : DisplayableView
{
    public event Action<TowerConfig> TowerPicked;
    public event Action Dismissed;

    [SerializeField]
    private RectTransform _root;

    [SerializeField]
    private BuildMenuButton _buttonPrefab;

    [SerializeField]
    private Button _closeButton;

    private readonly List<BuildMenuButton> _spawned = new();

    protected override void Awake()
    {
        base.Awake();
        _closeButton.onClick.AddListener(() => Dismissed?.Invoke());
        Hide();
    }

    public void Populate(IReadOnlyList<TowerConfig> towers, int currentGold)
    {
        foreach (var b in _spawned) Destroy(b.gameObject);
        _spawned.Clear();

        foreach (var config in towers)
        {
            var btn = Instantiate(_buttonPrefab, _root);
            btn.Bind(config, currentGold >= config.Cost);
            btn.Clicked += OnClicked;
            _spawned.Add(btn);
        }
    }

    public void UpdateAffordability(int currentGold)
    {
        foreach (var b in _spawned)
            b.SetInteractable(currentGold >= b.Config.Cost);
    }

    private void OnClicked(TowerConfig config)
    {
        TowerPicked?.Invoke(config);
    }
}
```

`Assets/Game/Scripts/UI/Views/BuildMenuButton.cs`
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuButton : MonoBehaviour
{
    public event Action<TowerConfig> Clicked;

    public TowerConfig Config => _config;

    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Text _costLabel;

    private TowerConfig _config;

    private void Awake()
    {
        _button.onClick.AddListener(() => Clicked?.Invoke(_config));
    }

    public void Bind(TowerConfig config, bool affordable)
    {
        _config = config;
        _icon.sprite = config.Icon;
        _costLabel.text = config.Cost.ToString();
        _button.interactable = affordable;
    }

    public void SetInteractable(bool value)
    {
        _button.interactable = value;
    }
}
```

`Assets/Game/Scripts/UI/Presenters/BuildMenuPresenter.cs`
```csharp
using System;
using Zenject;

public class BuildMenuPresenter : IInitializable, IDisposable
{
    private readonly BuildMenuView _view;
    private readonly TowerCatalog _catalog;
    private readonly TowerFactory _factory;
    private readonly Wallet _wallet;
    private readonly SignalBus _signalBus;
    private readonly LevelContext _levelContext;

    private TowerSlot _activeSlot;

    public BuildMenuPresenter(BuildMenuView view, TowerCatalog catalog, TowerFactory factory,
        Wallet wallet, SignalBus signalBus, LevelContext levelContext)
    {
        _view = view;
        _catalog = catalog;
        _factory = factory;
        _wallet = wallet;
        _signalBus = signalBus;
        _levelContext = levelContext;
    }

    public void Initialize()
    {
        foreach (var slot in _levelContext.Slots)
            slot.Clicked += OnSlotClicked;

        _view.TowerPicked += OnTowerPicked;
        _view.Dismissed += Close;
        _signalBus.Subscribe<GoldChangedSignal>(OnGoldChanged);

        _view.Hide();
    }

    public void Dispose()
    {
        foreach (var slot in _levelContext.Slots)
            slot.Clicked -= OnSlotClicked;

        _view.TowerPicked -= OnTowerPicked;
        _view.Dismissed -= Close;
        _signalBus.TryUnsubscribe<GoldChangedSignal>(OnGoldChanged);
    }

    private void OnSlotClicked(TowerSlot slot)
    {
        _activeSlot = slot;
        _view.Populate(_catalog.Towers, _wallet.Current);
        _view.Show();
    }

    private void OnTowerPicked(TowerConfig config)
    {
        if (_activeSlot == null) return;
        if (_factory.TryBuild(config, _activeSlot))
            Close();
    }

    private void OnGoldChanged(GoldChangedSignal signal)
    {
        if (_view.IsVisible)
            _view.UpdateAffordability(signal.Current);
    }

    private void Close()
    {
        _activeSlot = null;
        _view.Hide();
    }
}
```

---

## 10. LevelContext — расширение

`Assets/Game/Scripts/Gameplay/Level/LevelContext.cs`
```csharp
using UnityEngine;

public class LevelContext : MonoBehaviour
{
    [SerializeField] private LevelConfig _config;
    [SerializeField] private Path _path;
    [SerializeField] private PlayerBase _playerBase;
    [SerializeField] private Transform _enemyRoot;

    [SerializeField] private TowerSlot[] _slots;
    [SerializeField] private Transform _towerRoot;
    [SerializeField] private Transform _projectileRoot;
    [SerializeField] private BuildMenuView _buildMenu;

    public LevelConfig Config => _config;
    public Path Path => _path;
    public PlayerBase PlayerBase => _playerBase;
    public Transform EnemyRoot => _enemyRoot;
    public TowerSlot[] Slots => _slots;
    public Transform TowerRoot => _towerRoot;
    public Transform ProjectileRoot => _projectileRoot;
    public BuildMenuView BuildMenu => _buildMenu;
}
```

---

## 11. GameplayInstaller — биндинги итерации 3

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

    [SerializeField]
    private TowerCatalog _towerCatalog;

    public override void InstallBindings()
    {
        Container.Bind<LevelContext>().FromInstance(_levelContext).AsSingle();
        Container.Bind<Path>().FromInstance(_levelContext.Path).AsSingle();
        Container.Bind<PlayerBase>().FromInstance(_levelContext.PlayerBase).AsSingle();

        Container.Bind<Transform>().WithId("EnemyRoot")
            .FromInstance(_levelContext.EnemyRoot).AsCached();
        Container.Bind<Transform>().WithId("TowerRoot")
            .FromInstance(_levelContext.TowerRoot).AsCached();
        Container.Bind<Transform>().WithId("ProjectileRoot")
            .FromInstance(_levelContext.ProjectileRoot).AsCached();

        Container.Bind<ICoroutineRunner>().FromInstance(_coroutineRunner).AsSingle();

        Container.Bind<EnemyFactory>().AsSingle();
        Container.Bind<WaveSpawner>().AsSingle();

        Container.Bind<TowerCatalog>().FromInstance(_towerCatalog).AsSingle();
        Container.BindInterfacesAndSelfTo<Wallet>().AsSingle();
        Container.Bind<ProjectilePool>().AsSingle();
        Container.Bind<TowerFactory>().AsSingle();

        Container.Bind<BuildMenuView>().FromInstance(_levelContext.BuildMenu).AsSingle();
        Container.BindInterfacesAndSelfTo<BuildMenuPresenter>().AsSingle().NonLazy();
    }
}
```

---

## 12. GameplayState — инициализация Wallet

Добавить резолв `Wallet` и вызов `SetStartingGold` перед запуском спавнера:

```csharp
public override void OnStateActivated()
{
    Debug.Log("[GameplayState] activated");

    var levelContext = Object.FindFirstObjectByType<LevelContext>();
    if (levelContext == null)
    {
        Debug.LogError("[GameplayState] LevelContext not found in scene");
        return;
    }

    var sceneContext = Object.FindFirstObjectByType<SceneContext>();
    var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

    levelContext.PlayerBase.Init(levelContext.Config.BaseHealth);

    var wallet = sceneContainer.Resolve<Wallet>();
    wallet.SetStartingGold(levelContext.Config.StartingGold);

    var spawner = sceneContainer.Resolve<WaveSpawner>();
    spawner.Run(levelContext.Config.Waves, levelContext.Path);
}
```

---

## 13. ProjectInstaller — DeclareSignal

После существующих объявлений сигналов:
```csharp
Container.DeclareSignal<GoldChangedSignal>();
Container.DeclareSignal<TowerBuiltSignal>();
Container.DeclareSignal<TowerSoldSignal>();
Container.DeclareSignal<ProjectileHitSignal>();
```

---

## 14. Layers / Physics

1. Создать Layer `Enemies` (Edit → Project Settings → Tags and Layers).
2. На префабе `Enemy_UfoA`:
   - `SphereCollider` radius ≈ 0.5 (IsTrigger = false).
   - `Rigidbody` — Kinematic, UseGravity = false (чтобы коллайдер двигался без физики).
   - Layer = `Enemies` (применить на всех детей).
3. `TowerSlot` префаб — `BoxCollider` размером с тайл, без Rigidbody (статичный).

---

## 15. Префабы

### `Assets/Game/Prefabs/Towers/Tower_Ballista.prefab`
- Root: GameObject с `Tower` + `TowerAttack`.
- Children: Kenney-модули `tower-round-base` + `tower-round-bottom-a` + `tower-round-middle-a` + `tower-round-roof-a` + `weapon-ballista`.
- `_muzzle` = пустой Transform в позиции ствола.

### `Assets/Game/Prefabs/Projectiles/Projectile_Arrow.prefab`
- Root: GameObject с `Projectile` + модель `weapon-ammo-arrow`.
- Масштаб/ориентация — стрелой вперёд (`+Z`).

### `Assets/Game/Prefabs/UI/BuildMenuButton.prefab`
- `Button` + `Image` (icon) + `Text` (cost) + компонент `BuildMenuButton`.

---

## 16. Ассеты (SO)

- `Assets/Game/Configs/Towers/TowerConfig_Ballista.asset`
  - id=`ballista`, cost=50, damage=10, range=5, fireRate=1, projectileSpeed=15, prefab=`Tower_Ballista`, projectilePrefab=`Projectile_Arrow`, icon=спрайт из Kenney-пака.
- `Assets/Game/Settings/TowerCatalog.asset` — список: `[TowerConfig_Ballista]`.

В `GameplayInstaller` (инспектор) проставить `_towerCatalog = TowerCatalog.asset`.

В `LevelConfig_01.asset` убедиться, что `startingGold = 300`.

---

## 17. Сцена Gameplay.unity

1. Открыть `Gameplay.unity`.
2. В корне `LevelContext`:
   - Дочерний `TowerSlots` с 3–4 `TowerSlot` (каждый — `BoxCollider` + `Highlight` quad материал `selection-a`).
   - `TowerRoot` — пустой GO для спавненных башен.
   - `ProjectileRoot` — пустой GO для снарядов.
3. Canvas (Screen Space — Overlay) с `BuildMenuView`:
   - Панель, кнопка Close, `Content` (`VerticalLayoutGroup`) — ссылка на `_root`, `_buttonPrefab = BuildMenuButton`.
4. В `LevelContext` прокинуть все ссылки: `_slots`, `_towerRoot`, `_projectileRoot`, `_buildMenu`.
5. Камера — перспективная, смотрит сверху под углом ~45°, так чтобы `Physics.OverlapSphere` и `OnMouseDown` работали.
6. Проверить Physics Raycaster на камере (добавить компонент `Physics Raycaster`) — нужен для UI-блокировки кликов через слоты, если понадобится.

---

## 18. Тест-план

1. `Play` из `SampleScene` → Menu → кнопка «Level 1» → сцена `Gameplay.unity` загружена.
2. Лог: `[GameplayState] activated`, `GoldChangedSignal { Current = 300 }`.
3. Спавнится первый UFO, движется по пути.
4. Клик по первому `TowerSlot` → `BuildMenuView` показан, в списке `Ballista` со стоимостью 50, кнопка интерактивна.
5. Клик `Ballista` → золото → 250 (`GoldChangedSignal`), в `TowerRoot` появилась башня, `BuildMenuView` скрыт, слот `IsOccupied`.
6. UFO входит в радиус → башня поворачивается к цели → спавнится `Projectile_Arrow` → долетает → `EnemyHealth.TakeDamage(10)` → после 5 попаданий враг умирает → `EnemyKilledSignal` → `Wallet.Add(10)` → золото 260.
7. Повторный клик на занятом слоте — ничего (IsOccupied).
8. Пройти всех врагов — база уцелела; `AllWavesCompletedSignal` → `LevelComplete` (заглушка из итерации 2).
9. Console — без `NullReferenceException`, без утечек (при выходе в меню снаряды и башни уничтожаются вместе со сценой).

---

## Отклонения от плана

- **TowerSlot**: финальный вариант — два события `Clicked` (пустой слот) и `TowerClicked` (занятый слот), оба вызываются из `OnTap()`. `OnMouseDown() => OnTap()` добавлен как fallback. Реализовано в итерации 5/7.
- **Projectile/ProjectilePool**: пулинг через Zenject `MemoryPool` отложен — используется `InstantiatePrefabForComponent` + `Destroy`.
- **Tower**: финальная версия содержит дополнительные поля `Slot`, `AttachSlot()`, `OnTap()` (итерация 5/7). Код в этом документе — промежуточная версия.
- **TowerConfig**: в итерации 5 расширен полями `UpgradeMeshes`, `SplashRadius`, `SlowMultiplier`, `SlowDuration`, `MaxLevel`.

---

## Технические заметки

- **Пулинг снарядов** отложен: `ProjectilePool.Spawn` использует `InstantiatePrefabForComponent` и `Destroy` — этого хватит для MVP, замена на `MemoryPool<Vector3, Enemy, ProjectileSpawnParams, Projectile.Pool>` — в итерации 7 (полишинг/оптимизация).
- **`OnMouseDown`** требует `Physics Raycaster` на камере + коллайдер на слоте. Для мобилки сработает автоматически (тап считается как клик).
- **Layer `Enemies`** — критично для `Physics.OverlapSphere`; без него `TowerAttack.FindNearest` вернёт 0 результатов.
- **`Wallet` как `IInitializable`** — подписка на `EnemyKilledSignal` живёт весь цикл сцены; `Dispose` отпишет при смене сцены.
- **`BuildMenuPresenter`** подписывается на `Slot.Clicked` — все слоты должны существовать в сцене до `Initialize()`, поэтому `NonLazy` корректен.
- **Камера** должна быть настроена раньше — слоты должны быть видимы и кликабельны. Если используется orthographic — `Physics.OverlapSphere` работает независимо от камеры.
- **ProjectileRoot** — чтобы сцена не захламлялась снарядами в hierarchy.
- **Ассеты**: 4 типа башен в GDD — в этой итерации реализуем только Ballista; Cannon/Catapult/Turret — в итерации 5 (вместе с апгрейдами и AoE).
