# Итерация 5 — Апгрейд/продажа башен, 4 типа башен, AoE и замедление

**Цель:** игрок может повторно кликнуть по уже построенной башне → открывается `TowerInfoView` с её характеристиками, кнопками **Upgrade** (до уровня 3, стоимость `base × 1.5 × currentLevel`) и **Sell** (возврат 70% вложений). Визуал апгрейда — смена mesh-варианта (`middle-a/b/c` из Kenney). В каталог добавляются 4 башни: **Ballista** (single), **Cannon** (splash), **Catapult** (splash + slow), **Turret** (fast). Враги получают компонент `SlowEffect` с таймером — замедление применяется через множитель скорости в `EnemyMovement`.

**Зависимости итерации 4:** `TowerFactory`, `Tower`, `TowerAttack`, `TowerSlot`, `ProjectilePool`, `Projectile`, `Wallet`, `BuildMenuPresenter`, `LevelContext`, `HudView`, `EnemyMovement`, `EnemyHealth`, `Enemy`, `TowerCatalog`, `GameplayInstaller`, сигналы `TowerBuiltSignal` / `TowerSoldSignal` / `GoldChangedSignal`.

---

## Прогресс

- [x] 1. `TowerConfig` — поля `UpgradeMeshes[]`, `SplashRadius`, `SlowMultiplier`, `SlowDuration`, `MaxLevel`, расчёты `GetDamage/GetRange/GetUpgradeCost(level)`
- [x] 2. `Tower` — рантайм-уровень, `TotalInvested`, `SellRefund`, `ApplyUpgrade()` (вместо `Upgrade()`)
- [x] 3. `TowerMeshSwitcher` — компонент, меняет mesh при апгрейде
- [x] 4. `TowerAttack` — использует effective stats (`_tower.EffectiveDamage/EffectiveRange`), передаёт `ProjectileImpact` в пул
- [x] 5. `ProjectileImpact` (struct) + расширение `Projectile.Launch` + `ProjectilePool.Spawn`
- [x] 6. `AreaDamage` (static) — урон и slow по радиусу
- [x] 7. `Projectile` — на попадании применяет splash и slow
- [x] 8. `SlowEffect` (компонент на враге) — таймер, мультипликатор, стакается по «сильнейшему»
- [x] 9. `EnemyMovement` — множитель скорости, метод `SetSpeedMultiplier(float)`
- [x] 10. `Enemy.Awake` — получает `SlowEffect`, прокидывает в `EnemyMovement`
- [x] 11. `TowerSlot` — два события: `Clicked` (пустой) и `TowerClicked` (занятый), вызываются из `OnTap()`
- [x] 12. `TowerInfoView : DisplayableView` — характеристики, `Upgrade` / `Sell` / `Close`
- [x] 13. `TowerInfoPresenter : IInitializable, IDisposable` — открытие по клику, заполнение, операции
- [x] 14. `TowerUpgradeService` — единая точка: `TryUpgrade(Tower)`, `Sell(Tower)`, списание/возврат золота, сигналы
- [x] 15. Новый сигнал `TowerUpgradedSignal { Tower, Level }`, `DeclareSignal` в `ProjectInstaller`
- [x] 16. `LevelContext` — поле `_towerInfoView`, геттер
- [x] 17. `GameplayInstaller` — биндинги `TowerInfoView`, `TowerInfoPresenter`, `TowerUpgradeService`
- [x] 18. Ассеты: `TowerConfig_Cannon/Catapult/Turret.asset`, префабы башен, Projectile-префабы `CannonBall/Boulder/Bullet`, иконки
- [x] 19. Prefab `TowerInfoView.prefab` в сцене `Gameplay.unity`
- [x] 20. `TowerCatalog.asset` — добавить 4 башни
- [x] 21. Ручной тест — строю каждую башню, апгрейжу, продаю, проверяю splash/slow

---

## 1. TowerConfig

`Assets/Game/Scripts/Configs/TowerConfig.cs` — расширяем поля и добавляем вычисления по уровню.

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

    [SerializeField] private int _maxLevel = 3;

    [Header("Upgrade visuals (middle-a/b/c)")]
    [SerializeField] private Mesh[] _upgradeMeshes;

    [Header("Area")]
    [SerializeField] private float _splashRadius;

    [Header("Slow")]
    [Range(0.1f, 1f)]
    [SerializeField] private float _slowMultiplier = 1f;
    [SerializeField] private float _slowDuration;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public Tower Prefab => _prefab;
    public Projectile ProjectilePrefab => _projectilePrefab;
    public int Cost => _cost;
    public int BaseDamage => _damage;
    public float BaseRange => _range;
    public float FireRate => _fireRate;
    public float ProjectileSpeed => _projectileSpeed;
    public int MaxLevel => _maxLevel;
    public Mesh[] UpgradeMeshes => _upgradeMeshes;
    public float SplashRadius => _splashRadius;
    public float SlowMultiplier => _slowMultiplier;
    public float SlowDuration => _slowDuration;

    public int GetDamage(int level) =>
        Mathf.RoundToInt(_damage * (1f + 0.25f * (level - 1)));

    public float GetRange(int level) =>
        _range * (1f + 0.10f * (level - 1));

    public int GetUpgradeCost(int currentLevel) =>
        Mathf.RoundToInt(_cost * 1.5f * currentLevel);
}
```

> `Damage`/`Range` из старого интерфейса больше не читаем напрямую — всё идёт через `Tower.EffectiveDamage` / `Tower.EffectiveRange`.

---

## 2. Tower — уровень, инвестиции, refund

`Assets/Game/Scripts/Gameplay/Towers/Tower.cs`

```csharp
using UnityEngine;

[RequireComponent(typeof(TowerAttack))]
public class Tower : MonoBehaviour
{
    public TowerConfig Config => _config;
    public TowerAttack Attack => _attack;
    public int Level => _level;
    public int TotalInvested => _totalInvested;
    public int EffectiveDamage => _config.GetDamage(_level);
    public float EffectiveRange => _config.GetRange(_level);
    public bool CanUpgrade => _level < _config.MaxLevel;
    public int NextUpgradeCost => CanUpgrade ? _config.GetUpgradeCost(_level) : 0;
    public int SellRefund => Mathf.RoundToInt(_totalInvested * 0.7f);

    [SerializeField]
    private TowerMeshSwitcher _meshSwitcher;

    private TowerConfig _config;
    private TowerAttack _attack;
    private int _level;
    private int _totalInvested;

    private void Awake()
    {
        _attack = GetComponent<TowerAttack>();
    }

    public void Init(TowerConfig config)
    {
        _config = config;
        _level = 1;
        _totalInvested = config.Cost;
        _attack.Init(this);
        if (_meshSwitcher != null) _meshSwitcher.Apply(config.UpgradeMeshes, _level);
    }

    public void ApplyUpgrade()
    {
        _totalInvested += _config.GetUpgradeCost(_level);
        _level++;
        _attack.RefreshStats();
        if (_meshSwitcher != null) _meshSwitcher.Apply(_config.UpgradeMeshes, _level);
    }
}
```

---

## 3. TowerMeshSwitcher

`Assets/Game/Scripts/Gameplay/Towers/TowerMeshSwitcher.cs`

```csharp
using UnityEngine;

public class TowerMeshSwitcher : MonoBehaviour
{
    [SerializeField]
    private MeshFilter _target;

    public void Apply(Mesh[] meshes, int level)
    {
        if (_target == null || meshes == null || meshes.Length == 0) return;
        var idx = Mathf.Clamp(level - 1, 0, meshes.Length - 1);
        _target.sharedMesh = meshes[idx];
    }
}
```

> На префабе башни `_target` — MeshFilter у `tower-round-middle`. Для квадратных башен (Cannon/Catapult) оставляем массив пустым — апгрейд визуально не меняет mesh, только статы.

---

## 4. TowerAttack — effective stats + ProjectileImpact

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
    private Tower _tower;
    private LayerMask _enemyMask;
    private readonly Collider[] _buffer = new Collider[MaxOverlap];

    private float _cooldown;
    private float _scanTimer;
    private float _cachedRange;
    private int _cachedDamage;
    private Enemy _currentTarget;

    [Inject]
    public void Construct(ProjectilePool pool)
    {
        _pool = pool;
        _enemyMask = LayerMask.GetMask("Enemies");
    }

    public void Init(Tower tower)
    {
        _tower = tower;
        _cooldown = 0f;
        _scanTimer = 0f;
        RefreshStats();
    }

    public void RefreshStats()
    {
        _cachedDamage = _tower.EffectiveDamage;
        _cachedRange = _tower.EffectiveRange;
    }

    private void Update()
    {
        if (_tower == null) return;

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

        _cooldown = 1f / Mathf.Max(0.0001f, _tower.Config.FireRate);
        var origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up;

        var impact = new ProjectileImpact
        {
            Damage = _cachedDamage,
            SplashRadius = _tower.Config.SplashRadius,
            SlowMultiplier = _tower.Config.SlowMultiplier,
            SlowDuration = _tower.Config.SlowDuration,
            EnemyMask = _enemyMask,
        };

        _pool.Spawn(_tower.Config.ProjectilePrefab, origin, _currentTarget,
            impact, _tower.Config.ProjectileSpeed);
    }

    private Enemy FindNearest()
    {
        var count = Physics.OverlapSphereNonAlloc(transform.position, _cachedRange, _buffer, _enemyMask);
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
        if (_tower == null || _tower.Config == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _tower.EffectiveRange);
    }
#endif
}
```

---

## 5. ProjectileImpact + Projectile/ProjectilePool

`Assets/Game/Scripts/Gameplay/Projectiles/ProjectileImpact.cs`

```csharp
using UnityEngine;

public struct ProjectileImpact
{
    public int Damage;
    public float SplashRadius;
    public float SlowMultiplier;
    public float SlowDuration;
    public LayerMask EnemyMask;
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

    public void Spawn(Projectile prefab, Vector3 origin, Enemy target,
        ProjectileImpact impact, float speed)
    {
        var instance = _container.InstantiatePrefabForComponent<Projectile>(prefab, origin,
            Quaternion.identity, _root);
        instance.Launch(origin, target, impact, speed);
    }
}
```

`Assets/Game/Scripts/Gameplay/Projectiles/Projectile.cs`

```csharp
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Enemy _target;
    private ProjectileImpact _impact;
    private float _speed;
    private bool _active;

    public void Launch(Vector3 origin, Enemy target, ProjectileImpact impact, float speed)
    {
        _target = target;
        _impact = impact;
        _speed = speed;
        transform.position = origin;
        _active = true;
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

        var dir = targetPos - next;
        if (dir.sqrMagnitude > 0.0001f)
            transform.forward = dir.normalized;

        if ((next - targetPos).sqrMagnitude < 0.04f)
        {
            ApplyImpact(targetPos);
            Release();
        }
    }

    private void ApplyImpact(Vector3 hitPos)
    {
        if (_impact.SplashRadius > 0f)
        {
            AreaDamage.Apply(hitPos, _impact);
            return;
        }

        _target.Health.TakeDamage(_impact.Damage);
        if (_impact.SlowDuration > 0f)
            _target.Slow.Apply(_impact.SlowMultiplier, _impact.SlowDuration);
    }

    private void Release()
    {
        _active = false;
        Destroy(gameObject);
    }
}
```

---

## 6. AreaDamage

`Assets/Game/Scripts/Gameplay/Projectiles/AreaDamage.cs`

```csharp
using UnityEngine;

public static class AreaDamage
{
    private const int MaxOverlap = 32;
    private static readonly Collider[] Buffer = new Collider[MaxOverlap];

    public static void Apply(Vector3 center, ProjectileImpact impact)
    {
        var count = Physics.OverlapSphereNonAlloc(center, impact.SplashRadius, Buffer, impact.EnemyMask);
        for (int i = 0; i < count; i++)
        {
            var enemy = Buffer[i].GetComponentInParent<Enemy>();
            if (enemy == null || enemy.Health.IsDead) continue;

            enemy.Health.TakeDamage(impact.Damage);
            if (impact.SlowDuration > 0f)
                enemy.Slow.Apply(impact.SlowMultiplier, impact.SlowDuration);
        }
    }
}
```

---

## 7. SlowEffect + EnemyMovement + Enemy

`Assets/Game/Scripts/Gameplay/Enemies/SlowEffect.cs`

```csharp
using UnityEngine;

public class SlowEffect : MonoBehaviour
{
    public float Multiplier => _active ? _multiplier : 1f;

    private EnemyMovement _movement;
    private float _multiplier = 1f;
    private float _timeLeft;
    private bool _active;

    public void Bind(EnemyMovement movement)
    {
        _movement = movement;
    }

    public void Apply(float multiplier, float duration)
    {
        if (!_active || multiplier < _multiplier || duration > _timeLeft)
        {
            _multiplier = Mathf.Min(_active ? _multiplier : 1f, multiplier);
            _timeLeft = Mathf.Max(_timeLeft, duration);
            _active = true;
            _movement.SetSpeedMultiplier(_multiplier);
        }
    }

    public void Reset()
    {
        _active = false;
        _multiplier = 1f;
        _timeLeft = 0f;
        if (_movement != null) _movement.SetSpeedMultiplier(1f);
    }

    private void Update()
    {
        if (!_active) return;
        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f) Reset();
    }
}
```

`Assets/Game/Scripts/Gameplay/Enemies/EnemyMovement.cs`

```csharp
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public bool ReachedEnd => _reachedEnd;

    private Path _path;
    private float _speed;
    private float _multiplier = 1f;
    private int _nextIndex;
    private bool _reachedEnd;

    public void Init(Path path, float speed)
    {
        _path = path;
        _speed = speed;
        _multiplier = 1f;
        _nextIndex = 1;
        _reachedEnd = false;
        transform.position = path.SpawnPoint;
    }

    public void SetSpeedMultiplier(float multiplier) => _multiplier = Mathf.Clamp(multiplier, 0.1f, 1f);

    private void Update()
    {
        if (_reachedEnd || _path == null) return;

        var target = _path.GetPoint(_nextIndex);
        var step = _speed * _multiplier * Time.deltaTime;
        var pos = Vector3.MoveTowards(transform.position, target, step);
        transform.position = pos;

        var flat = new Vector3(target.x - pos.x, 0, target.z - pos.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flat);

        if ((pos - target).sqrMagnitude < 0.0025f && ++_nextIndex >= _path.Count)
            _reachedEnd = true;
    }
}
```

`Assets/Game/Scripts/Gameplay/Enemies/Enemy.cs`

```csharp
using UnityEngine;

[RequireComponent(typeof(EnemyMovement), typeof(EnemyHealth), typeof(SlowEffect))]
public class Enemy : MonoBehaviour
{
    public EnemyConfig Config => _config;
    public EnemyMovement Movement => _movement;
    public EnemyHealth Health => _health;
    public SlowEffect Slow => _slow;

    private EnemyMovement _movement;
    private EnemyHealth _health;
    private SlowEffect _slow;
    private EnemyConfig _config;

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _health = GetComponent<EnemyHealth>();
        _slow = GetComponent<SlowEffect>();
        _slow.Bind(_movement);
    }

    public void Init(EnemyConfig config, Path path)
    {
        _config = config;
        _health.Init(config.MaxHealth);
        _movement.Init(path, config.Speed);
        _slow.Reset();
    }
}
```

> Все существующие префабы врагов нужно доукомплектовать компонентом `SlowEffect` (добавить один раз в редакторе — `[RequireComponent]` не подтянет на уже существующие префабы автоматически).

---

## 8. TowerSlot — клик по занятому слоту

`Assets/Game/Scripts/Gameplay/Towers/TowerSlot.cs`

```csharp
using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TowerSlot : MonoBehaviour
{
    public event Action<TowerSlot> Clicked;
    public event Action<TowerSlot> TowerClicked;

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
        if (IsOccupied) TowerClicked?.Invoke(this);
        else Clicked?.Invoke(this);
    }
}
```

`BuildMenuPresenter.OnSlotClicked` остаётся прежним (он подписан только на `Clicked` — свободные слоты). Новый презентер `TowerInfoPresenter` подписывается на `TowerClicked`.

---

## 9. TowerUpgradeService

`Assets/Game/Scripts/Gameplay/Towers/TowerUpgradeService.cs`

```csharp
using UnityEngine;
using Zenject;

public class TowerUpgradeService
{
    private readonly Wallet _wallet;
    private readonly SignalBus _signalBus;

    public TowerUpgradeService(Wallet wallet, SignalBus signalBus)
    {
        _wallet = wallet;
        _signalBus = signalBus;
    }

    public bool TryUpgrade(Tower tower)
    {
        if (tower == null || !tower.CanUpgrade) return false;
        if (!_wallet.Spend(tower.NextUpgradeCost)) return false;

        tower.ApplyUpgrade();
        _signalBus.Fire(new TowerUpgradedSignal { Tower = tower, Level = tower.Level });
        return true;
    }

    public void Sell(Tower tower)
    {
        if (tower == null) return;
        var refund = tower.SellRefund;
        var slot = tower.GetComponentInParent<TowerSlot>();
        if (slot == null)
        {
            var found = Object.FindObjectsByType<TowerSlot>(FindObjectsSortMode.None);
            foreach (var s in found)
                if (s.Tower == tower) { slot = s; break; }
        }
        if (slot != null) slot.Detach();

        _wallet.Add(refund);
        _signalBus.Fire(new TowerSoldSignal { Tower = tower, Refund = refund });
        Object.Destroy(tower.gameObject);
    }
}
```

> Если `Tower` не является ребёнком `TowerSlot` (в текущей схеме он живёт в `TowerRoot`), `TowerSlot` хранит ссылку сам — поэтому fallback через `FindObjectsByType`. Альтернатива: добавить в `Tower` поле `_slot` и проставить в `TowerFactory.TryBuild`. Рекомендую второй путь — чище:

```csharp
// Tower.cs
public TowerSlot Slot => _slot;
private TowerSlot _slot;
public void AttachSlot(TowerSlot slot) => _slot = slot;

// TowerFactory.TryBuild (после tower.Init):
tower.AttachSlot(slot);

// TowerUpgradeService.Sell:
if (tower.Slot != null) tower.Slot.Detach();
```

Тогда весь поиск через сцену не нужен.

---

## 10. Сигнал TowerUpgradedSignal

`Assets/Game/Scripts/Core/Signals/GameplaySignals.cs` — добавить:

```csharp
public struct TowerUpgradedSignal { public Tower Tower; public int Level; }
```

`ProjectInstaller.InstallBindings`:

```csharp
Container.DeclareSignal<TowerUpgradedSignal>();
```

---

## 11. TowerInfoView

`Assets/Game/Scripts/UI/Views/TowerInfoView.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class TowerInfoView : DisplayableView
{
    public event Action UpgradeClicked;
    public event Action SellClicked;
    public event Action CloseClicked;

    [SerializeField] private Text _nameLabel;
    [SerializeField] private Text _levelLabel;
    [SerializeField] private Text _statsLabel;
    [SerializeField] private Text _upgradeCostLabel;
    [SerializeField] private Text _sellRefundLabel;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Button _sellButton;
    [SerializeField] private Button _closeButton;

    protected override void Awake()
    {
        base.Awake();
        _upgradeButton.onClick.AddListener(() => UpgradeClicked?.Invoke());
        _sellButton.onClick.AddListener(() => SellClicked?.Invoke());
        _closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        Hide();
    }

    public void Populate(Tower tower, int currentGold)
    {
        var cfg = tower.Config;
        _nameLabel.text = cfg.DisplayName;
        _levelLabel.text = $"Lv {tower.Level}/{cfg.MaxLevel}";
        _statsLabel.text = $"DMG {tower.EffectiveDamage}  RNG {tower.EffectiveRange:F1}  RATE {cfg.FireRate:F1}";
        _sellRefundLabel.text = $"+{tower.SellRefund}";

        if (tower.CanUpgrade)
        {
            _upgradeCostLabel.text = tower.NextUpgradeCost.ToString();
            _upgradeButton.interactable = currentGold >= tower.NextUpgradeCost;
            _upgradeButton.gameObject.SetActive(true);
        }
        else
        {
            _upgradeCostLabel.text = "MAX";
            _upgradeButton.interactable = false;
        }
    }
}
```

---

## 12. TowerInfoPresenter

`Assets/Game/Scripts/UI/Presenters/TowerInfoPresenter.cs`

```csharp
using System;
using Zenject;

public class TowerInfoPresenter : IInitializable, IDisposable
{
    private readonly TowerInfoView _view;
    private readonly LevelContext _levelContext;
    private readonly TowerUpgradeService _upgradeService;
    private readonly Wallet _wallet;
    private readonly SignalBus _signalBus;

    private Tower _activeTower;

    public TowerInfoPresenter(TowerInfoView view, LevelContext levelContext,
        TowerUpgradeService upgradeService, Wallet wallet, SignalBus signalBus)
    {
        _view = view;
        _levelContext = levelContext;
        _upgradeService = upgradeService;
        _wallet = wallet;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        foreach (var slot in _levelContext.Slots)
            slot.TowerClicked += OnTowerClicked;

        _view.UpgradeClicked += OnUpgrade;
        _view.SellClicked += OnSell;
        _view.CloseClicked += Close;

        _signalBus.Subscribe<GoldChangedSignal>(OnGoldChanged);
        _signalBus.Subscribe<TowerUpgradedSignal>(OnTowerUpgraded);

        _view.Hide();
    }

    public void Dispose()
    {
        foreach (var slot in _levelContext.Slots)
            slot.TowerClicked -= OnTowerClicked;

        _view.UpgradeClicked -= OnUpgrade;
        _view.SellClicked -= OnSell;
        _view.CloseClicked -= Close;

        _signalBus.TryUnsubscribe<GoldChangedSignal>(OnGoldChanged);
        _signalBus.TryUnsubscribe<TowerUpgradedSignal>(OnTowerUpgraded);
    }

    private void OnTowerClicked(TowerSlot slot)
    {
        _activeTower = slot.Tower;
        if (_activeTower == null) return;
        _view.Populate(_activeTower, _wallet.Current);
        _view.Show();
    }

    private void OnUpgrade()
    {
        if (_activeTower == null) return;
        if (_upgradeService.TryUpgrade(_activeTower))
            _view.Populate(_activeTower, _wallet.Current);
    }

    private void OnSell()
    {
        if (_activeTower == null) return;
        _upgradeService.Sell(_activeTower);
        Close();
    }

    private void OnGoldChanged(GoldChangedSignal s)
    {
        if (_view.IsVisible && _activeTower != null)
            _view.Populate(_activeTower, s.Current);
    }

    private void OnTowerUpgraded(TowerUpgradedSignal s)
    {
        if (_view.IsVisible && s.Tower == _activeTower)
            _view.Populate(_activeTower, _wallet.Current);
    }

    private void Close()
    {
        _activeTower = null;
        _view.Hide();
    }
}
```

---

## 13. LevelContext — поле TowerInfoView

`Assets/Game/Scripts/Gameplay/Level/LevelContext.cs` — добавить:

```csharp
[SerializeField] private TowerInfoView _towerInfoView;
public TowerInfoView TowerInfoView => _towerInfoView;
```

---

## 14. GameplayInstaller — новые биндинги

Добавить в `InstallBindings()` (после существующих):

```csharp
Container.Bind<TowerInfoView>().FromInstance(_levelContext.TowerInfoView).AsSingle();
Container.Bind<TowerUpgradeService>().AsSingle();
Container.BindInterfacesAndSelfTo<TowerInfoPresenter>().AsSingle().NonLazy();
```

---

## 15. TowerFactory — AttachSlot (опционально для чистоты)

`Assets/Game/Scripts/Gameplay/Towers/TowerFactory.cs` — после `tower.Init(config)` добавить `tower.AttachSlot(slot);` (если выбрали вариант со ссылкой на слот из §9).

---

## 16. Ассеты (SO + префабы)

1. **TowerConfig-ассеты** `Assets/Game/Configs/Towers/`:
   - `TowerConfig_Ballista.asset` — уже есть, пересохранить с `UpgradeMeshes = [middle-a, middle-b, middle-c]`, `SplashRadius = 0`, `SlowMultiplier = 1`, `SlowDuration = 0`, `MaxLevel = 3`, `Cost = 50`, `Damage = 10`, `Range = 6`, `FireRate = 1`.
   - `TowerConfig_Cannon.asset` — `Cost = 100`, `Damage = 25`, `Range = 5`, `FireRate = 0.6`, `SplashRadius = 1.5`, `SlowDuration = 0`.
   - `TowerConfig_Catapult.asset` — `Cost = 150`, `Damage = 40`, `Range = 7`, `FireRate = 0.3`, `SplashRadius = 2.5`, `SlowMultiplier = 0.5`, `SlowDuration = 2`.
   - `TowerConfig_Turret.asset` — `Cost = 120`, `Damage = 5`, `Range = 4`, `FireRate = 5`, `SplashRadius = 0`.

2. **Префабы башен** `Assets/Game/Content/Prefabs/Towers/` (каждый наследует `Tower` + `TowerAttack` + `TowerMeshSwitcher`):
   - `Tower_Ballista.prefab` — `tower-round-base + bottom-a + middle-a + roof-a + weapon-ballista`. MeshFilter `middle-a` назначен в `TowerMeshSwitcher._target`.
   - `Tower_Cannon.prefab` — `tower-square + weapon-cannon`, `TowerMeshSwitcher._target = null` (массив меша пустой).
   - `Tower_Catapult.prefab` — `tower-square + weapon-catapult`.
   - `Tower_Turret.prefab` — `tower-round-base + weapon-turret`.
   - На каждом — `_muzzle` Transform в дуле.

3. **Projectile-префабы** `Assets/Game/Content/Prefabs/Projectiles/`:
   - `Projectile_Arrow.prefab` — `weapon-ammo-arrow`.
   - `Projectile_CannonBall.prefab` — `weapon-ammo-cannonball`.
   - `Projectile_Boulder.prefab` — `weapon-ammo-boulder`.
   - `Projectile_Bullet.prefab` — `weapon-ammo-bullet`.

4. **`TowerCatalog.asset`** — добавить все 4 `TowerConfig`.

5. **`TowerInfoView.prefab`** `Assets/Game/Content/Prefabs/UI/`:
   - Panel с Name, Level, Stats, UpgradeCost, SellRefund, кнопки `Upgrade`, `Sell`, `Close`.
   - Корень: `CanvasGroup` + `TowerInfoView`.

6. **Сцена `Gameplay.unity`**:
   - Под `Canvas` добавить инстанс `TowerInfoView.prefab`.
   - В `LevelContext` инспекторе прокинуть `_towerInfoView`.
   - Префабы врагов — добавить `SlowEffect` через `Component → Add` (если `RequireComponent` не добавил автоматически при импорте).

---

## 17. Тест-план

1. Play → Level 1 → Gold 300. Поставить Ballista на первый слот, пройти 1-ю волну. Клик по башне → `TowerInfoView` показывает `Lv 1/3`, `DMG 10`, `RNG 6`, `Upgrade cost 75`, `Sell +35`.
2. Нажать **Upgrade** (золото ≥ 75). Уровень → 2, `DMG 12/13`, `RNG 6.6`, `Invested 125`, `Sell +88`. Next cost = 150. Mesh сменился на `middle-b` (для Ballista).
3. Повторный апгрейд → Lv 3, mesh `middle-c`, кнопка Upgrade выключена, лейбл `MAX`.
4. **Sell** — башня уничтожена, слот снова кликабелен, золото += 70% вложений, `TowerSoldSignal` в логе.
5. Ставлю **Cannon**, проверяю splash: несколько врагов в группе получают урон одновременно от одного снаряда (`AreaDamage.Apply`).
6. Ставлю **Catapult** — враги в зоне попадания замедляются на 2 секунды (EnemyMovement шагает в 2 раза медленнее — визуально проверяется).
7. Ставлю **Turret** — стреляет с fireRate = 5 (5 раз в секунду), малый радиус, одиночный урон.
8. Клик по пустому слоту всё ещё открывает `BuildMenuView`; клик по занятому — `TowerInfoView`, и оба меню не перекрываются.
9. Пройти уровень полностью → `LevelCompleteView` корректен. Повторный вход — башен нет, золото на старте.
10. Console — 0 `NullReferenceException`, подписки снимаются при переходе Menu ↔ Gameplay несколько раз.
11. Slow стакается по «сильнейшему»: два попадания Catapult подряд не делают врага быстрее, более долгий таймер сохраняется.

---

## Отклонения от плана

- **Tower.ApplyUpgrade()**: метод называется `ApplyUpgrade()`, не `Upgrade()`. Дополнительно добавлены `TowerSlot Slot`, `AttachSlot(TowerSlot)`, `OnTap()`.
- **TowerSlot.OnMouseDown**: в итерации 3 был `OnMouseDown` напрямую. В итерации 5 добавлен `OnTap()` публичный метод, `OnMouseDown() => OnTap()` как fallback. В итерации 7 `WorldTapRouter` вызывает `OnTap()` напрямую.
- **TowerUpgradeService.Sell**: финальная версия использует `tower.Slot.Detach()` через `Tower.Slot`, а не поиск через `FindObjectsByType`.
- **TowerFactory.TryBuild**: добавлен `tower.AttachSlot(slot)` после `tower.Init(config)`.

---

## Технические заметки

- **Effective stats кэшируются** в `TowerAttack._cachedDamage/_cachedRange` — пересчёт при `Init` и `RefreshStats` (после апгрейда). Это важно: `FindNearest` вызывается каждые 100 мс, неразумно каждый раз дёргать `_tower.EffectiveRange`.
- **Splash vs single**: при `SplashRadius > 0` одиночный урон по `_target` пропускается — весь урон идёт через `AreaDamage.Apply`, иначе цель получит двойной урон (сама + splash).
- **Slow стакается по максимуму**: логика в `SlowEffect.Apply` — выбираем меньший множитель и больший оставшийся таймер. Это естественно — два одинаковых Catapult'а не должны сокращать замедление.
- **Refund 70%** считается от `TotalInvested` (Cost + все GetUpgradeCost), а не только от Cost — это честно по GDD.
- **Mesh апгрейд** — только через смену `sharedMesh`. Никаких отдельных префабов/SetActive иерархий. Атлас `colormap.png` общий — draw call не добавляется.
- **TowerSlot.TowerClicked** — отдельное событие вместо проверки `IsOccupied` в `Clicked`, чтобы `BuildMenuPresenter` и `TowerInfoPresenter` были строго независимы и не конфликтовали по подпискам.
- **Никаких `FindObjectOfType`** в рантайме — `Tower.Slot` хранится напрямую, иначе итерация 5 ломает правило из §11 GDD.
- **`TowerUpgradeService` не `IInitializable`** — он stateless, вызывается презентером только на действия пользователя. `NonLazy` не нужен.
- **`SlowEffect` на всех существующих префабах** добавить вручную — `RequireComponent` подтягивается только при следующем `AddComponent<Enemy>()` в редакторе.
- **Range update**: после апгрейда `FindNearest` начнёт видеть дальше уже на следующем `ScanInterval` (≤100 мс) — допустимая задержка.
- **Apgrade-cost валидация**: `TowerUpgradeService.TryUpgrade` отказывает молча если золота не хватает; визуально кнопка неактивна — проверка дублируется в `TowerInfoView.Populate`.
