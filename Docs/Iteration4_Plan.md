# Итерация 4 — Волны, экономика, HUD, звёзды, полный цикл уровня

**Цель:** игрок проходит уровень 1 полностью: корректный тайминг всех волн → награда за волну + 5-секундный перерыв → кнопка «Начать волну раньше» → `AllWavesCompletedSignal` → расчёт звёзд по оставшимся HP базы → `LevelCompleteView` с анимацией звёзд → сохранение прогресса → возврат в меню, где уровень 1 помечен звёздами и уровень 2 разблокирован. HUD показывает золото, HP базы, номер текущей волны и кнопку Early-Start.

**Зависимости из итерации 3:** `Wallet`, `TowerFactory`, `BuildMenuPresenter`, `LevelContext`, `WaveSpawner` (сейчас запускает все волны без reward/skip), `PlayerBase`, `PlayerProgress`, сигналы `GoldChangedSignal` / `BaseHealthChangedSignal` / `WaveStartedSignal` / `WaveCompletedSignal` / `AllWavesCompletedSignal` / `LevelFailedSignal`.

---

## Прогресс

- [ ] 1. Новые сигналы и поля: `WaveRewardGrantedSignal`, `WaveBreakStartedSignal`, `LevelCompletedSignal(int stars)`, расширение `WaveStartedSignal` (Index, Total)
- [ ] 2. `WaveConfig` — поле `Reward`
- [ ] 3. `WaveSpawner` — награда за волну, перерыв, early-start, отсчёт
- [ ] 4. `RewardService` (IInitializable) — подписка на `WaveCompletedSignal` → `Wallet.Add`
- [ ] 5. `StarCalculator` (stateless) — расчёт звёзд по `HP/maxHP`
- [ ] 6. `LevelResultService` — копит финальный HP базы, сохраняет прогресс
- [ ] 7. `HudView : DisplayableView` (золото, HP, волна, кнопка Early-Start)
- [ ] 8. `HudPresenter : IInitializable, IDisposable`
- [ ] 9. `LevelCompleteView : DisplayableView` + `LevelCompletePresenter`
- [ ] 10. `LevelFailedView : DisplayableView` + `LevelFailedPresenter`
- [ ] 11. Расширение `LevelContext` — ссылки на `HudView`, `LevelCompleteView`, `LevelFailedView`
- [ ] 12. `GameplayInstaller` — биндинги новых сервисов и views
- [ ] 13. `ProjectInstaller` — `DeclareSignal` для новых сигналов
- [ ] 14. `LevelCompleteState` — ждёт клика на `LevelCompleteView`, не автотаймер
- [ ] 15. `LevelFailedState` — показ view, Retry/Menu
- [ ] 16. `LevelSelectView` — обновление звёзд после возврата из гемплея
- [ ] 17. Сцена `Gameplay.unity` — добавить Canvas Hud/Complete/Failed, наполнение префабов
- [ ] 18. `WaveConfig_01.asset` — выставить `Reward`
- [ ] 19. Ручной тест — полное прохождение уровня 1, звёзды, возврат, разблокировка

---

## 1. Сигналы

Дополнить `Assets/Game/Scripts/Core/Signals/GameplaySignals.cs`:

```csharp
public struct EnemySpawnedSignal { public Enemy Enemy; }
public struct EnemyKilledSignal { public Enemy Enemy; public int Reward; }
public struct EnemyReachedBaseSignal { public int Damage; }
public struct BaseHealthChangedSignal { public int Current; public int Max; }
public struct BaseDestroyedSignal { }

public struct WaveStartedSignal { public int Index; public int Total; }
public struct WaveCompletedSignal { public int Index; public int Reward; }
public struct WaveBreakStartedSignal { public int NextIndex; public float Seconds; }
public struct AllWavesCompletedSignal { }

public struct LevelFailedSignal { }
public struct LevelCompletedSignal { public int LevelId; public int Stars; }

public struct GoldChangedSignal { public int Current; }
public struct TowerBuiltSignal { public Tower Tower; }
public struct TowerSoldSignal { public Tower Tower; public int Refund; }
public struct ProjectileHitSignal { public Enemy Enemy; public int Damage; }

public struct WaveEarlyStartRequestedSignal { }
```

`ProjectInstaller` — добавить:
```csharp
Container.DeclareSignal<WaveBreakStartedSignal>();
Container.DeclareSignal<LevelCompletedSignal>();
Container.DeclareSignal<WaveEarlyStartRequestedSignal>();
```

---

## 2. WaveConfig — награда

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

    [SerializeField]
    private int _reward = 25;

    public IReadOnlyList<SubWave> SubWaves => _subWaves;
    public float DelayAfter => _delayAfter;
    public int Reward => _reward;
}
```

---

## 3. WaveSpawner — тайминг, награда, early-start

`Assets/Game/Scripts/Gameplay/Waves/WaveSpawner.cs`
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class WaveSpawner : IInitializable, System.IDisposable
{
    private readonly EnemyFactory _factory;
    private readonly SignalBus _signalBus;
    private readonly ICoroutineRunner _runner;

    private Coroutine _routine;
    private bool _skipBreak;
    private int _aliveEnemies;
    private int _pendingSpawns;

    public WaveSpawner(EnemyFactory factory, SignalBus signalBus, ICoroutineRunner runner)
    {
        _factory = factory;
        _signalBus = signalBus;
        _runner = runner;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<WaveEarlyStartRequestedSignal>(OnEarlyStartRequested);
        _signalBus.Subscribe<EnemySpawnedSignal>(OnEnemySpawned);
        _signalBus.Subscribe<EnemyKilledSignal>(OnEnemyRemoved);
        _signalBus.Subscribe<EnemyReachedBaseSignal>(OnEnemyRemovedByBase);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<WaveEarlyStartRequestedSignal>(OnEarlyStartRequested);
        _signalBus.TryUnsubscribe<EnemySpawnedSignal>(OnEnemySpawned);
        _signalBus.TryUnsubscribe<EnemyKilledSignal>(OnEnemyRemoved);
        _signalBus.TryUnsubscribe<EnemyReachedBaseSignal>(OnEnemyRemovedByBase);
    }

    public void Run(IReadOnlyList<WaveConfig> waves, Path path)
    {
        Stop();
        _aliveEnemies = 0;
        _pendingSpawns = 0;
        _routine = _runner.Run(RunRoutine(waves, path));
    }

    public void Stop()
    {
        if (_routine != null) _runner.StopRoutine(_routine);
        _routine = null;
        _skipBreak = false;
    }

    private IEnumerator RunRoutine(IReadOnlyList<WaveConfig> waves, Path path)
    {
        for (int i = 0; i < waves.Count; i++)
        {
            _signalBus.Fire(new WaveStartedSignal { Index = i, Total = waves.Count });

            var wave = waves[i];
            foreach (var sub in wave.SubWaves)
            {
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

            _signalBus.Fire(new WaveBreakStartedSignal
            {
                NextIndex = i + 1,
                Seconds = wave.DelayAfter,
            });

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

    private void OnEarlyStartRequested() => _skipBreak = true;

    private void OnEnemySpawned(EnemySpawnedSignal _)
    {
        _pendingSpawns = Mathf.Max(0, _pendingSpawns - 1);
        _aliveEnemies++;
    }

    private void OnEnemyRemoved(EnemyKilledSignal _) => _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
    private void OnEnemyRemovedByBase(EnemyReachedBaseSignal _) => _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);
}
```

> `EnemyFactory.Spawn` должен файрить `EnemySpawnedSignal` (если ещё не файрит — добавить там вызов `_signalBus.Fire(new EnemySpawnedSignal { Enemy = enemy })`). Это единая точка учёта живых врагов — по ней `WaveSpawner` знает, когда волна «дозачищена».

---

## 4. RewardService

`Assets/Game/Scripts/Gameplay/Economy/RewardService.cs`
```csharp
using System;
using Zenject;

public class RewardService : IInitializable, IDisposable
{
    private readonly SignalBus _signalBus;
    private readonly Wallet _wallet;

    public RewardService(SignalBus signalBus, Wallet wallet)
    {
        _signalBus = signalBus;
        _wallet = wallet;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<WaveCompletedSignal>(OnWaveCompleted);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<WaveCompletedSignal>(OnWaveCompleted);
    }

    private void OnWaveCompleted(WaveCompletedSignal signal)
    {
        if (signal.Reward > 0) _wallet.Add(signal.Reward);
    }
}
```

---

## 5. StarCalculator

`Assets/Game/Scripts/Gameplay/Level/StarCalculator.cs`
```csharp
public static class StarCalculator
{
    public static int Calculate(int currentHp, int maxHp)
    {
        if (currentHp <= 0) return 0;
        if (currentHp >= maxHp) return 3;
        return currentHp * 2 >= maxHp ? 2 : 1;
    }
}
```

---

## 6. LevelResultService

Отдельный сервис, чтобы не раздувать `GameplayState`. Копит текущий HP базы, знает `LevelId`, умеет выдать звёзды и сохранить прогресс.

`Assets/Game/Scripts/Gameplay/Level/LevelResultService.cs`
```csharp
using System;
using Zenject;

public class LevelResultService : IInitializable, IDisposable
{
    public int LastStars => _lastStars;

    private readonly SignalBus _signalBus;
    private readonly PlayerProgress _progress;
    private readonly LevelContext _levelContext;

    private int _currentHp;
    private int _maxHp;
    private int _lastStars;

    public LevelResultService(SignalBus signalBus, PlayerProgress progress, LevelContext levelContext)
    {
        _signalBus = signalBus;
        _progress = progress;
        _levelContext = levelContext;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<BaseHealthChangedSignal>(OnBaseHealthChanged);
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<BaseHealthChangedSignal>(OnBaseHealthChanged);
    }

    public int FinalizeVictory()
    {
        _lastStars = StarCalculator.Calculate(_currentHp, _maxHp);
        _progress.SetLevelResult(_levelContext.Config.Id, _lastStars);
        _signalBus.Fire(new LevelCompletedSignal
        {
            LevelId = _levelContext.Config.Id,
            Stars = _lastStars,
        });
        return _lastStars;
    }

    private void OnBaseHealthChanged(BaseHealthChangedSignal signal)
    {
        _currentHp = signal.Current;
        _maxHp = signal.Max;
    }
}
```

---

## 7. HudView

`Assets/Game/Scripts/UI/Views/HudView.cs`
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class HudView : DisplayableView
{
    public event Action EarlyStartClicked;

    [SerializeField] private Text _goldLabel;
    [SerializeField] private Text _baseHpLabel;
    [SerializeField] private Text _waveLabel;
    [SerializeField] private Text _breakTimerLabel;
    [SerializeField] private Button _earlyStartButton;

    protected override void Awake()
    {
        base.Awake();
        _earlyStartButton.onClick.AddListener(() => EarlyStartClicked?.Invoke());
        SetEarlyStartVisible(false);
    }

    public void SetGold(int value) => _goldLabel.text = value.ToString();

    public void SetBaseHp(int current, int max) =>
        _baseHpLabel.text = $"{current}/{max}";

    public void SetWave(int index, int total) =>
        _waveLabel.text = $"Wave {index + 1}/{total}";

    public void SetEarlyStartVisible(bool visible)
    {
        _earlyStartButton.gameObject.SetActive(visible);
        _breakTimerLabel.gameObject.SetActive(visible);
    }

    public void SetBreakTimer(float seconds) =>
        _breakTimerLabel.text = Mathf.CeilToInt(seconds).ToString();
}
```

---

## 8. HudPresenter

`Assets/Game/Scripts/UI/Presenters/HudPresenter.cs`
```csharp
using System;
using UnityEngine;
using Zenject;

public class HudPresenter : IInitializable, IDisposable, ITickable
{
    private readonly HudView _view;
    private readonly SignalBus _signalBus;
    private readonly Wallet _wallet;

    private int _totalWaves;
    private int _currentWaveIndex;
    private float _breakTimeLeft;
    private bool _breakActive;

    public HudPresenter(HudView view, SignalBus signalBus, Wallet wallet)
    {
        _view = view;
        _signalBus = signalBus;
        _wallet = wallet;
    }

    public void Initialize()
    {
        _view.EarlyStartClicked += OnEarlyStartClicked;
        _signalBus.Subscribe<GoldChangedSignal>(OnGoldChanged);
        _signalBus.Subscribe<BaseHealthChangedSignal>(OnBaseHealthChanged);
        _signalBus.Subscribe<WaveStartedSignal>(OnWaveStarted);
        _signalBus.Subscribe<WaveBreakStartedSignal>(OnWaveBreakStarted);
        _signalBus.Subscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);

        _view.SetGold(_wallet.Current);
        _view.Show();
    }

    public void Dispose()
    {
        _view.EarlyStartClicked -= OnEarlyStartClicked;
        _signalBus.TryUnsubscribe<GoldChangedSignal>(OnGoldChanged);
        _signalBus.TryUnsubscribe<BaseHealthChangedSignal>(OnBaseHealthChanged);
        _signalBus.TryUnsubscribe<WaveStartedSignal>(OnWaveStarted);
        _signalBus.TryUnsubscribe<WaveBreakStartedSignal>(OnWaveBreakStarted);
        _signalBus.TryUnsubscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
    }

    public void Tick()
    {
        if (!_breakActive) return;
        _breakTimeLeft -= Time.deltaTime;
        if (_breakTimeLeft < 0f) _breakTimeLeft = 0f;
        _view.SetBreakTimer(_breakTimeLeft);
    }

    private void OnGoldChanged(GoldChangedSignal s) => _view.SetGold(s.Current);
    private void OnBaseHealthChanged(BaseHealthChangedSignal s) => _view.SetBaseHp(s.Current, s.Max);

    private void OnWaveStarted(WaveStartedSignal s)
    {
        _totalWaves = s.Total;
        _currentWaveIndex = s.Index;
        _breakActive = false;
        _view.SetWave(s.Index, s.Total);
        _view.SetEarlyStartVisible(false);
    }

    private void OnWaveBreakStarted(WaveBreakStartedSignal s)
    {
        _breakActive = true;
        _breakTimeLeft = s.Seconds;
        _view.SetBreakTimer(_breakTimeLeft);
        _view.SetEarlyStartVisible(true);
    }

    private void OnAllWavesCompleted()
    {
        _breakActive = false;
        _view.SetEarlyStartVisible(false);
    }

    private void OnEarlyStartClicked()
    {
        if (!_breakActive) return;
        _breakActive = false;
        _view.SetEarlyStartVisible(false);
        _signalBus.Fire(new WaveEarlyStartRequestedSignal());
    }
}
```

---

## 9. LevelCompleteView + Presenter

`Assets/Game/Scripts/UI/Views/LevelCompleteView.cs`
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelCompleteView : DisplayableView
{
    public event Action Continue;

    [SerializeField] private Text _titleLabel;
    [SerializeField] private GameObject[] _starIcons;
    [SerializeField] private Button _continueButton;

    protected override void Awake()
    {
        base.Awake();
        _continueButton.onClick.AddListener(() => Continue?.Invoke());
        Hide();
    }

    public void Populate(string levelName, int stars)
    {
        _titleLabel.text = $"{levelName} — Победа";
        for (int i = 0; i < _starIcons.Length; i++)
            _starIcons[i].SetActive(i < stars);
    }
}
```

`Assets/Game/Scripts/UI/Presenters/LevelCompletePresenter.cs`
```csharp
using System;
using Zenject;

public class LevelCompletePresenter : IInitializable, IDisposable
{
    public event Action Continue;

    private readonly LevelCompleteView _view;

    public LevelCompletePresenter(LevelCompleteView view)
    {
        _view = view;
    }

    public void Initialize()
    {
        _view.Continue += OnContinue;
        _view.Hide();
    }

    public void Dispose()
    {
        _view.Continue -= OnContinue;
    }

    public void ShowResult(string levelName, int stars)
    {
        _view.Populate(levelName, stars);
        _view.Show();
    }

    public void Hide() => _view.Hide();

    private void OnContinue() => Continue?.Invoke();
}
```

---

## 10. LevelFailedView + Presenter

`Assets/Game/Scripts/UI/Views/LevelFailedView.cs`
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelFailedView : DisplayableView
{
    public event Action Retry;
    public event Action BackToMenu;

    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _menuButton;

    protected override void Awake()
    {
        base.Awake();
        _retryButton.onClick.AddListener(() => Retry?.Invoke());
        _menuButton.onClick.AddListener(() => BackToMenu?.Invoke());
        Hide();
    }
}
```

`Assets/Game/Scripts/UI/Presenters/LevelFailedPresenter.cs`
```csharp
using System;
using Zenject;

public class LevelFailedPresenter : IInitializable, IDisposable
{
    public event Action Retry;
    public event Action BackToMenu;

    private readonly LevelFailedView _view;

    public LevelFailedPresenter(LevelFailedView view)
    {
        _view = view;
    }

    public void Initialize()
    {
        _view.Retry += OnRetry;
        _view.BackToMenu += OnBackToMenu;
        _view.Hide();
    }

    public void Dispose()
    {
        _view.Retry -= OnRetry;
        _view.BackToMenu -= OnBackToMenu;
    }

    public void ShowResult() => _view.Show();
    public void Hide() => _view.Hide();

    private void OnRetry() => Retry?.Invoke();
    private void OnBackToMenu() => BackToMenu?.Invoke();
}
```

---

## 11. LevelContext — расширение

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

    [SerializeField] private HudView _hud;
    [SerializeField] private LevelCompleteView _completeView;
    [SerializeField] private LevelFailedView _failedView;

    public LevelConfig Config => _config;
    public Path Path => _path;
    public PlayerBase PlayerBase => _playerBase;
    public Transform EnemyRoot => _enemyRoot;
    public TowerSlot[] Slots => _slots;
    public Transform TowerRoot => _towerRoot;
    public Transform ProjectileRoot => _projectileRoot;
    public BuildMenuView BuildMenu => _buildMenu;
    public HudView Hud => _hud;
    public LevelCompleteView CompleteView => _completeView;
    public LevelFailedView FailedView => _failedView;
}
```

---

## 12. GameplayInstaller — новые биндинги

Добавить в `InstallBindings()` рядом с существующими:

```csharp
Container.Bind<HudView>().FromInstance(_levelContext.Hud).AsSingle();
Container.Bind<LevelCompleteView>().FromInstance(_levelContext.CompleteView).AsSingle();
Container.Bind<LevelFailedView>().FromInstance(_levelContext.FailedView).AsSingle();

Container.BindInterfacesAndSelfTo<WaveSpawner>().AsSingle();      // заменяет прежний Bind<WaveSpawner>
Container.BindInterfacesAndSelfTo<RewardService>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<LevelResultService>().AsSingle().NonLazy();

Container.BindInterfacesAndSelfTo<HudPresenter>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<LevelCompletePresenter>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<LevelFailedPresenter>().AsSingle().NonLazy();
```

> `WaveSpawner` теперь `IInitializable/IDisposable` — поэтому `BindInterfacesAndSelfTo`. Без этого подписки на early-start и alive-counter не поднимутся.

---

## 13. LevelCompleteState — ждёт клик

`Assets/Game/Scripts/Core/GameLoop/States/LevelCompleteState.cs`
```csharp
using UnityEngine;
using Zenject;

public class LevelCompleteState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SceneLoader _sceneLoader;

    private LevelCompletePresenter _presenter;

    public LevelCompleteState(GameLoopStateMachine stateMachine, DiContainer container, SceneLoader sceneLoader)
        : base(stateMachine)
    {
        _container = container;
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        var sceneContext = Object.FindFirstObjectByType<SceneContext>();
        var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

        var resultService = sceneContainer.Resolve<LevelResultService>();
        var levelContext = sceneContainer.Resolve<LevelContext>();
        _presenter = sceneContainer.Resolve<LevelCompletePresenter>();

        var stars = resultService.FinalizeVictory();

        _presenter.Continue += OnContinue;
        _presenter.ShowResult(levelContext.Config.DisplayName, stars);

        Debug.Log($"[LevelCompleteState] stars={stars}");
    }

    public override void Update() { }

    public override void OnStateDisabled()
    {
        if (_presenter != null) _presenter.Continue -= OnContinue;
        _presenter = null;
    }

    private void OnContinue()
    {
        _presenter.Continue -= OnContinue;
        _sceneLoader.LoadScene("Menu", () =>
            StateMachine.SetState(GameLoopStateMachine.State.MainMenu));
    }
}
```

---

## 14. LevelFailedState

`Assets/Game/Scripts/Core/GameLoop/States/LevelFailedState.cs`
```csharp
using UnityEngine;
using Zenject;

public class LevelFailedState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SceneLoader _sceneLoader;
    private readonly SignalBus _signalBus;

    private LevelFailedPresenter _presenter;
    private int _lastLevelId;

    public LevelFailedState(GameLoopStateMachine stateMachine, DiContainer container,
        SceneLoader sceneLoader, SignalBus signalBus) : base(stateMachine)
    {
        _container = container;
        _sceneLoader = sceneLoader;
        _signalBus = signalBus;
    }

    public override void OnStateActivated()
    {
        var sceneContext = Object.FindFirstObjectByType<SceneContext>();
        var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

        var levelContext = sceneContainer.Resolve<LevelContext>();
        _lastLevelId = levelContext.Config.Id;
        _presenter = sceneContainer.Resolve<LevelFailedPresenter>();

        _presenter.Retry += OnRetry;
        _presenter.BackToMenu += OnBackToMenu;
        _presenter.ShowResult();
    }

    public override void Update() { }

    public override void OnStateDisabled()
    {
        if (_presenter != null)
        {
            _presenter.Retry -= OnRetry;
            _presenter.BackToMenu -= OnBackToMenu;
        }
        _presenter = null;
    }

    private void OnRetry()
    {
        _signalBus.Fire(new LevelStartRequestedSignal { LevelId = _lastLevelId });
        StateMachine.SetState(GameLoopStateMachine.State.LoadLevel);
    }

    private void OnBackToMenu()
    {
        _sceneLoader.LoadScene("Menu", () =>
            StateMachine.SetState(GameLoopStateMachine.State.MainMenu));
    }
}
```

---

## 15. LevelSelectView — обновление звёзд

Если `LevelSelectView.Refresh()` вызывается только при `Show()`, всё работает из коробки (после возврата в меню `OnStateActivated` пересоздаёт view). Если нет — вручную вызвать `Refresh()` в `MainMenuState.OnStateActivated()` через `LevelSelectView`.

Убедиться, что `LevelButton.Bind(LevelConfig, int stars, bool unlocked)` корректно отображает звёзды (в итерации 1 уже есть).

---

## 16. LevelResult-сервис: когда fire'ить `LevelCompletedSignal`

Подход:
- `GameplayState.OnAllWavesCompleted` → `StateMachine.SetState(LevelComplete)` (как сейчас)
- `LevelCompleteState.OnStateActivated` резолвит `LevelResultService.FinalizeVictory()` → расчёт звёзд, сохранение, сигнал
- `LevelCompleteView` слушает клик «Continue», выходит в меню

Ни `GameplayState`, ни `LevelResultService` не лазят в `PlayerProgress` напрямую из других мест — одна точка записи.

---

## 17. Ассеты

1. `Assets/Game/Configs/Waves/WaveConfig_01_*.asset` — проставить `Reward = 25` на каждой (5 волн).
2. `Assets/Game/Configs/Levels/LevelConfig_01.asset`:
   - `StartingGold = 300`, `BaseHealth = 20`, `Waves = [W01_1..W01_5]`.
3. Префабы UI:
   - `HudView.prefab` — Canvas → Panel → Text(gold), Text(hp), Text(wave), Text(breakTimer), Button(EarlyStart). На корне — `CanvasGroup` + `HudView`.
   - `LevelCompleteView.prefab` — Panel с Title, 3 иконки звёзд (разные GO), Button(Continue). `CanvasGroup` + `LevelCompleteView`.
   - `LevelFailedView.prefab` — Panel с двумя Button (Retry/Menu). `CanvasGroup` + `LevelFailedView`.

---

## 18. Сцена Gameplay.unity

1. В корневом Canvas (Screen Space — Overlay) добавить три дочерних префаба: `HudView`, `LevelCompleteView`, `LevelFailedView`. Все стартуют с `alpha = 0` (их `Awake` → `Hide()`).
2. В `LevelContext` инспекторе прокинуть: `_hud`, `_completeView`, `_failedView`.
3. Проверить `Canvas` → `GraphicRaycaster` (есть по умолчанию) — иначе кнопки не кликаются.
4. Убедиться, что `BuildMenuView` и `HudView` не перекрывают друг друга по raycast (разные панели).

---

## 19. Тест-план

1. Play → Menu → Level 1 → сцена Gameplay загружена, `[GameplayState] activated`.
2. HUD виден: Gold 300, HP 20/20, Wave 1/5. Early-Start скрыт.
3. Враги волны 1 выходят; ставим Ballista, волна зачищается → лог `WaveCompletedSignal { Index=0, Reward=25 }` → Gold 325 (+25 reward + доля убитых).
4. Появляется Break-таймер (5s) + кнопка Early-Start.
5. Клик Early-Start → таймер уходит, сразу стартует волна 2 → `WaveStartedSignal { Index=1, Total=5 }`.
6. Дождаться таймера на волне 2 → следующая волна стартует сама.
7. Пройти все 5 волн без урона базе → `AllWavesCompletedSignal` → `LevelCompleteState` → `LevelCompleteView` с 3⭐, заголовок `Level 1 — Победа`.
8. Клик Continue → сцена Menu → `LevelSelectView`: Level 1 с 3⭐, Level 2 разблокирован.
9. Рестарт, пропустить одного врага (HP 19/20) → 2⭐. Пропустить ≥11 → 1⭐. Проверить `StarCalculator` формулой.
10. Рестарт, довести базу до 0 → `LevelFailedSignal` → `LevelFailedState` → `LevelFailedView`. Retry → заново тот же уровень. Menu → меню, прогресс не изменён.
11. Console — без `NullReferenceException`, без утечек подписок после перехода Menu ↔ Gameplay несколько раз.

---

## Технические заметки

- **Early-Start**: `WaveSpawner` слушает `WaveEarlyStartRequestedSignal` и выставляет `_skipBreak`. Кнопка HUD никогда не вызывает `WaveSpawner` напрямую — всё через SignalBus, чтобы HUD не знал о спавнере.
- **Alive-counter**: `WaveSpawner` ждёт зачистки через `EnemySpawned/Killed/ReachedBase` — иначе `WaveCompletedSignal` выстрелит раньше, чем последний враг реально умрёт, и reward засчитается до того, как игрок его дозачистил. `EnemyFactory.Spawn` обязательно файрит `EnemySpawnedSignal` — если это не так в итерации 2, добавить одной строкой.
- **StarCalculator**: пороги — 100% = 3, ≥50% = 2, >0 = 1, 0 = 0 (fail). В `FinalizeVictory` 0-звёзд не бывает, потому что FinalizeVictory вызывается только из `LevelCompleteState`; базовое HP уже > 0.
- **Save only on victory**: `PlayerProgress.SetLevelResult` идёт только через `LevelResultService.FinalizeVictory()`. Поражение ничего не записывает.
- **Сигналы от Project vs Scene scope**: `LevelCompletedSignal` и `LevelFailedSignal` объявлены в `ProjectInstaller`, fire'ятся из сцены, но слушать их на Menu-сцене не обязательно — `LevelSelectView.Refresh()` читает `PlayerProgress` при показе.
- **Time.timeScale**: в этой итерации не трогаем. Pause — итерация 7.
- **HudPresenter как ITickable**: Tick работает, пока SceneContext жив; на выходе из сцены `Dispose` снимает подписки. Если нужно скрыть break-timer при `AllWavesCompleted` — делается в `OnAllWavesCompleted`.
- **Отсутствующие поля `WaveStartedSignal.Total`**: все существующие читатели должны быть обновлены (в основном тесты/логи; если какой-то презентер ловит старую форму — перекомпиляция покажет).
- **`NonLazy` для RewardService/LevelResultService**: обязательно — иначе `IInitializable.Initialize()` не вызовется, и подписок не будет.
