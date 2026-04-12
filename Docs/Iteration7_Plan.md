# Итерация 7 — Pause, polish поражения, SFX, камера, touch-input, GPU Instancing

**Цель:** закрыть пункты из раздела 9 GDD «Итерация 7». Добавляем полноценное состояние **Pause** (overlay с Resume/Restart/Menu, `Time.timeScale = 0`), дошлифовываем **LevelFailedState** (корректная остановка спавна/восстановление timeScale при Retry), прикручиваем **SFX-заглушки** (выстрел / смерть врага / победа / поражение) через `AudioService`, фиксируем **камеру** в `LevelContext` (ортографическая, top-down, ландшафт), вводим **touch-input** через Unity Input System (`InputSystemUIInputModule` + world-raycaster для тапа по `TowerSlot` / `Tower`), включаем **GPU Instancing** на материале атласа Kenney `colormap` для одного draw call.

**Зависимости итерации 6:** `GameLoopStateMachine`, `GameplayState`, `LevelFailedState`, `LevelContext`, `WaveSpawner`, `HudView`, `DisplayableView`, `SignalBus`, `BuildMenuPresenter`, `TowerInfoPresenter`, `Enemy`, `Tower`.

---

## Прогресс

- [~] 1. `PauseState` не регистрируем — выбран overlay-вариант через `PausePresenter`
- [x] 2. Overlay pause в `PausePresenter` (`Time.timeScale` 0/1, Resume/Restart/Menu)
- [x] 3. `PauseRequestedSignal` / `PauseResumedSignal` — добавлены
- [x] 4. `PauseView : DisplayableView` — класс создан (prefab собирается в Unity вручную)
- [x] 5. `PausePresenter` — создан, биндится в `GameplayInstaller`
- [x] 6. `HudView` — поле `_pauseButton` + событие `PauseClicked`
- [x] 7. `HudPresenter` — `OnPauseClicked` → `PauseRequestedSignal`
- [x] 8. `LevelContext` — поля `PauseView` + `LevelCamera`
- [x] 9. `GameplayInstaller` — биндинг `PauseView` (InjectOptional) + `PausePresenter`
- [x] 10. `LevelFailedState` — `Time.timeScale = 1`, `WaveSpawner.Stop()`, очистка Enemy/Projectile
- [x] 11. `LevelCompleteState` — `Time.timeScale = 1` первой строкой
- [x] 12. `SfxConfig` (SO) + `AudioService` (IInitializable)
- [x] 13. `AudioService` — подписка на 5 сигналов
- [~] 14. `TowerAttack` отдельный сигнал стрельбы — не нужен, `ProjectileHitSignal` уже покрывает
- [x] 15. `ProjectInstaller` — биндинг `AudioService.NonLazy` + `SfxConfig` (InjectOptional)
- [x] 16. `LevelContext._levelCamera` — присвоена существующая `Main Camera` сцены (ортокамера — настройка перспективы остаётся за художником)
- [x] 17. `InputReader` — прямые `InputAction` (`<Pointer>/press` + `<Pointer>/position`)
- [x] 18. `WorldTapRouter` — Raycast по `TowerSlot` / `Tower`, проверка `IsPointerOverGameObject`
- [x] 19. `TowerSlot.OnTap()` / `Tower.OnTap()` — публичные методы
- [x] 20. `EventSystem` сцен — `InputSystemUIInputModule` (Gameplay_* уже были, Menu перекинут через MCP)
- [ ] 21. `colormap.mat` — ассет ещё не создан (Kenney FBX использует per-model материалы), отложено
- [ ] 22. Ручной тест — после сборки UI в Unity

---

## 1. GameLoopStateMachine — регистрация PauseState

`Assets/Game/Scripts/Core/GameLoop/GameLoopStateMachine.cs`

```csharp
using Zenject;

public class GameLoopStateMachine : StateMachineController<GameRoot, GameLoopStateMachine.State>
{
    public enum State
    {
        Initialize = 0,
        MainMenu = 1,
        LoadLevel = 2,
        Gameplay = 3,
        Pause = 4,
        LevelComplete = 5,
        LevelFailed = 6
    }

    private readonly DiContainer _container;

    public GameLoopStateMachine(DiContainer container) => _container = container;

    protected override void RegisterStates()
    {
        RegisterState(_container.Instantiate<InitializeState>(new object[] { this }), State.Initialize);
        RegisterState(_container.Instantiate<MainMenuState>(new object[] { this }), State.MainMenu);
        RegisterState(_container.Instantiate<LoadLevelState>(new object[] { this }), State.LoadLevel);
        RegisterState(_container.Instantiate<GameplayState>(new object[] { this }), State.Gameplay);
        RegisterState(_container.Instantiate<PauseState>(new object[] { this }), State.Pause);
        RegisterState(_container.Instantiate<LevelFailedState>(new object[] { this }), State.LevelFailed);
        RegisterState(_container.Instantiate<LevelCompleteState>(new object[] { this }), State.LevelComplete);
    }
}
```

---

## 2. Сигналы паузы

`Assets/Game/Scripts/Core/Signals/GameplaySignals.cs` — добавить:

```csharp
public struct PauseRequestedSignal { }
public struct PauseResumedSignal { }
```

---

## 3. PauseState

`Assets/Game/Scripts/Core/GameLoop/States/PauseState.cs`

```csharp
using UnityEngine;
using Zenject;

public class PauseState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;
    private readonly SceneLoader _sceneLoader;

    private PausePresenter _presenter;
    private int _lastLevelId;

    public PauseState(GameLoopStateMachine stateMachine, DiContainer container,
        SignalBus signalBus, SceneLoader sceneLoader) : base(stateMachine)
    {
        _container = container;
        _signalBus = signalBus;
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        Time.timeScale = 0f;

        var sceneContext = Object.FindFirstObjectByType<SceneContext>();
        var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

        var levelContext = sceneContainer.Resolve<LevelContext>();
        _lastLevelId = levelContext.Config.Id;

        _presenter = sceneContainer.Resolve<PausePresenter>();
        _presenter.Resume += OnResume;
        _presenter.Restart += OnRestart;
        _presenter.BackToMenu += OnBackToMenu;
        _presenter.Show();
    }

    public override void Update() { }

    public override void OnStateDisabled()
    {
        if (_presenter != null)
        {
            _presenter.Resume -= OnResume;
            _presenter.Restart -= OnRestart;
            _presenter.BackToMenu -= OnBackToMenu;
            _presenter.Hide();
        }
        _presenter = null;
        Time.timeScale = 1f;
    }

    private void OnResume()
    {
        _signalBus.Fire(new PauseResumedSignal());
        StateMachine.SetState(GameLoopStateMachine.State.Gameplay, activate: false);
    }

    private void OnRestart()
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

> Resume возвращает к `Gameplay` **без повторной активации** (`activate: false`), чтобы не перезапускать спавн волн. Если `StateMachineController` такого флага не поддерживает — используем Overlay-паттерн: `Pause` как отдельный флаг в `GameplayState`, а не как отдельное активное состояние. Проверить сигнатуру `SetState` в `Dramacore`-копии и выбрать один путь. Вариант с флагом показан в пункте 4.

### Вариант без переключения состояния (рекомендованный)

Если `StateMachineController.SetState` всегда дёргает `OnStateDisabled/OnStateActivated`, то паузу проще держать как **overlay, управляемый из `GameplayState`**, не переключая state:

```csharp
public class GameplayState : GameLoopState, ITickable
{
    private bool _isPaused;

    public override void OnStateRegistered()
    {
        _signalBus.Subscribe<BaseDestroyedSignal>(OnBaseDestroyed);
        _signalBus.Subscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
        _signalBus.Subscribe<PauseRequestedSignal>(OnPauseRequested);
        _signalBus.Subscribe<PauseResumedSignal>(OnPauseResumed);
    }

    private void OnPauseRequested()
    {
        if (_isPaused) return;
        _isPaused = true;
        Time.timeScale = 0f;
        _sceneContainer.Resolve<PausePresenter>().Show();
    }

    private void OnPauseResumed()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = 1f;
        _sceneContainer.Resolve<PausePresenter>().Hide();
    }
}
```

Restart/Menu из паузы — публикуют `LevelStartRequestedSignal` / грузят сцену меню, `Time.timeScale` сбрасывается через `Hide()`. **Выбирается этот путь**, т.к. минимально ломает существующий граф состояний. Пункт 1 (регистрация `PauseState`) можно пропустить; `State.Pause` остаётся в enum для истории, но никто в него не переключается.

---

## 4. PauseView

`Assets/Game/Scripts/UI/Views/PauseView.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseView : DisplayableView
{
    public event Action Resume;
    public event Action Restart;
    public event Action BackToMenu;

    [SerializeField]
    private Button _resumeButton;

    [SerializeField]
    private Button _restartButton;

    [SerializeField]
    private Button _menuButton;

    protected override void Awake()
    {
        base.Awake();
        _resumeButton.onClick.AddListener(() => Resume?.Invoke());
        _restartButton.onClick.AddListener(() => Restart?.Invoke());
        _menuButton.onClick.AddListener(() => BackToMenu?.Invoke());
        Hide();
    }
}
```

---

## 5. PausePresenter

`Assets/Game/Scripts/UI/Presenters/PausePresenter.cs`

```csharp
using System;
using Zenject;

public class PausePresenter : IInitializable, IDisposable
{
    public event Action Resume;
    public event Action Restart;
    public event Action BackToMenu;

    private readonly PauseView _view;
    private readonly SignalBus _signalBus;

    public PausePresenter(PauseView view, SignalBus signalBus)
    {
        _view = view;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _view.Resume += OnResume;
        _view.Restart += OnRestart;
        _view.BackToMenu += OnBackToMenu;
        _view.Hide();
    }

    public void Dispose()
    {
        _view.Resume -= OnResume;
        _view.Restart -= OnRestart;
        _view.BackToMenu -= OnBackToMenu;
    }

    public void Show() => _view.Show();
    public void Hide() => _view.Hide();

    private void OnResume()
    {
        _signalBus.Fire(new PauseResumedSignal());
        Resume?.Invoke();
    }

    private void OnRestart() => Restart?.Invoke();
    private void OnBackToMenu() => BackToMenu?.Invoke();
}
```

---

## 6. HudView — кнопка Pause

`Assets/Game/Scripts/UI/Views/HudView.cs` — добавить:

```csharp
public event Action PauseClicked;

[SerializeField]
private Button _pauseButton;

protected override void Awake()
{
    base.Awake();
    _earlyStartButton.onClick.AddListener(() => EarlyStartClicked?.Invoke());
    _pauseButton.onClick.AddListener(() => PauseClicked?.Invoke());
    SetEarlyStartVisible(false);
}
```

`HudPresenter.Initialize` — подписка:

```csharp
_view.PauseClicked += OnPauseClicked;
// ...
private void OnPauseClicked() => _signalBus.Fire(new PauseRequestedSignal());
```

и отписка в `Dispose`.

---

## 7. LevelContext — новые поля

`Assets/Game/Scripts/Gameplay/Level/LevelContext.cs` — добавить поля/геттеры:

```csharp
[SerializeField] private PauseView _pauseView;
[SerializeField] private Camera _levelCamera;

public PauseView PauseView => _pauseView;
public Camera LevelCamera => _levelCamera;
```

`GameplayInstaller.InstallBindings`:

```csharp
Container.Bind<PauseView>().FromInstance(_levelContext.PauseView).AsSingle();
Container.BindInterfacesAndSelfTo<PausePresenter>().AsSingle().NonLazy();
```

---

## 8. LevelFailedState — остановка мира перед Retry

`Assets/Game/Scripts/Core/GameLoop/States/LevelFailedState.cs` — добавить в `OnStateActivated` и `OnRetry`:

```csharp
public override void OnStateActivated()
{
    Time.timeScale = 1f; // сброс на случай ухода из паузы

    var sceneContext = Object.FindFirstObjectByType<SceneContext>();
    var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

    var levelContext = sceneContainer.Resolve<LevelContext>();
    _lastLevelId = levelContext.Config.Id;

    var spawner = sceneContainer.Resolve<WaveSpawner>();
    spawner.Stop();

    _presenter = sceneContainer.Resolve<LevelFailedPresenter>();
    _presenter.Retry += OnRetry;
    _presenter.BackToMenu += OnBackToMenu;
    _presenter.ShowResult();
}
```

Аналогично в `LevelCompleteState.OnStateActivated` — `Time.timeScale = 1f;` первой строкой.

`WaveSpawner.Stop()` должен также зачистить активные `Enemy` в `EnemyFactory` (если раньше этого не было — добавить метод `EnemyFactory.DespawnAll()` и вызывать его из `Stop`). Это же применимо к `ProjectilePool.DespawnAll()`.

---

## 9. AudioService + SfxConfig

`Assets/Game/Scripts/Configs/SfxConfig.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/SfxConfig", fileName = "SfxConfig")]
public class SfxConfig : ScriptableObject
{
    [SerializeField] private AudioClip _shot;
    [SerializeField] private AudioClip _enemyDeath;
    [SerializeField] private AudioClip _towerBuilt;
    [SerializeField] private AudioClip _levelWin;
    [SerializeField] private AudioClip _levelFail;

    public AudioClip Shot => _shot;
    public AudioClip EnemyDeath => _enemyDeath;
    public AudioClip TowerBuilt => _towerBuilt;
    public AudioClip LevelWin => _levelWin;
    public AudioClip LevelFail => _levelFail;
}
```

`Assets/Game/Scripts/Core/Services/AudioService.cs`

```csharp
using System;
using UnityEngine;
using Zenject;

public class AudioService : IInitializable, IDisposable
{
    private readonly SignalBus _signalBus;
    private readonly SfxConfig _config;
    private readonly AudioSource _source;

    public AudioService(SignalBus signalBus, SfxConfig config)
    {
        _signalBus = signalBus;
        _config = config;
        var go = new GameObject("[AudioService]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _source = go.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
    }

    public void Initialize()
    {
        _signalBus.Subscribe<ProjectileHitSignal>(_ => Play(_config.Shot));
        _signalBus.Subscribe<EnemyKilledSignal>(_ => Play(_config.EnemyDeath));
        _signalBus.Subscribe<TowerBuiltSignal>(_ => Play(_config.TowerBuilt));
        _signalBus.Subscribe<AllWavesCompletedSignal>(_ => Play(_config.LevelWin));
        _signalBus.Subscribe<LevelFailedSignal>(_ => Play(_config.LevelFail));
    }

    public void Dispose()
    {
        _signalBus.TryUnsubscribe<ProjectileHitSignal>(_ => { });
        // SignalBus автоматом отписывает по времени жизни контейнера — Dispose пуст
    }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        _source.PlayOneShot(clip);
    }
}
```

> Стрельба — раздельная от попадания: если `TowerAttack` публикует свой сигнал — лучше ловить `TowerShotSignal`. Если нет — вызов `AudioService.Play(config.Shot)` прямо из `TowerAttack.Fire()`. В прототипе достаточно `ProjectileHitSignal` как прокси для «выстрелил / попал».

`ProjectInstaller.InstallBindings` — добавить:

```csharp
var sfx = Resources.Load<SfxConfig>("SfxConfig");
Container.Bind<SfxConfig>().FromInstance(sfx).AsSingle();
Container.BindInterfacesAndSelfTo<AudioService>().AsSingle().NonLazy();
```

Ассет `Assets/Resources/SfxConfig.asset` — заглушки из любых свободных wav'ов (пусть пустые `AudioClip` в MVP).

---

## 10. Камера уровня

В каждом `Level_*.prefab` под `LevelContext._levelCamera` кладётся дочерняя `Camera`:
- `Orthographic = true`, `OrthographicSize = 7` (подгонка под размер поля),
- поворот `(55, 0, 0)`, позиция над базой с отступом,
- `ClearFlags = SolidColor`,
- `Culling Mask` без слоёв UI (UI на Canvas с `Screen Space — Overlay`),
- разрешение под ландшафт — проверка через `Game View` aspect `16:9 Landscape`.

В `GameplayState` (или `GameplayInstaller`) можно сделать `Camera.main` → `_levelContext.LevelCamera` через `tag = MainCamera` на префабе. Дефолтной камеры в сцене `Gameplay.unity` быть не должно, только UI-камера при необходимости.

---

## 11. Touch-input через Unity Input System

### 11.1 InputActions

Создать `Assets/Game/Input/GameInput.inputactions` с action map `Gameplay`:
- `Tap` — type `Button`, binding `<Pointer>/press`,
- `Point` — type `Value` (Vector2), binding `<Pointer>/position`.

Сгенерировать C# класс `GameInput` (toggle «Generate C# Class»).

### 11.2 InputReader

`Assets/Game/Scripts/Core/Services/InputReader.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class InputReader : IInitializable, IDisposable, ITickable
{
    public event Action<Vector2> Tap;

    private GameInput _input;

    public void Initialize()
    {
        _input = new GameInput();
        _input.Gameplay.Enable();
        _input.Gameplay.Tap.performed += OnTapPerformed;
    }

    public void Dispose()
    {
        _input.Gameplay.Tap.performed -= OnTapPerformed;
        _input.Gameplay.Disable();
        _input.Dispose();
    }

    public void Tick() { }

    private void OnTapPerformed(InputAction.CallbackContext ctx)
    {
        var pos = _input.Gameplay.Point.ReadValue<Vector2>();
        Tap?.Invoke(pos);
    }
}
```

### 11.3 WorldTapRouter

`Assets/Game/Scripts/Gameplay/Input/WorldTapRouter.cs`

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

public class WorldTapRouter : IInitializable, System.IDisposable
{
    private readonly InputReader _input;
    private readonly LevelContext _levelContext;
    private readonly LayerMask _mask;

    public WorldTapRouter(InputReader input, LevelContext levelContext)
    {
        _input = input;
        _levelContext = levelContext;
        _mask = LayerMask.GetMask("TowerSlot", "Tower");
    }

    public void Initialize() => _input.Tap += OnTap;
    public void Dispose() => _input.Tap -= OnTap;

    private void OnTap(Vector2 screenPos)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        var camera = _levelContext.LevelCamera;
        if (camera == null) return;

        var ray = camera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 200f, _mask)) return;

        if (hit.collider.TryGetComponent(out TowerSlot slot)) { slot.OnTap(); return; }
        if (hit.collider.GetComponentInParent<Tower>() is { } tower) tower.OnTap();
    }
}
```

`GameplayInstaller`:

```csharp
Container.BindInterfacesAndSelfTo<InputReader>().AsSingle().NonLazy();
Container.BindInterfacesAndSelfTo<WorldTapRouter>().AsSingle().NonLazy();
```

### 11.4 TowerSlot / Tower

Заменить `OnMouseDown` на публичный `OnTap()`:

```csharp
public class TowerSlot : MonoBehaviour
{
    public event System.Action<TowerSlot> Tapped;
    public void OnTap() => Tapped?.Invoke(this);
}
```

```csharp
public class Tower : MonoBehaviour
{
    public event System.Action<Tower> Tapped;
    public void OnTap() => Tapped?.Invoke(this);
}
```

`BuildMenuPresenter` / `TowerInfoPresenter` подписываются на `Tapped` вместо старых OnMouseDown-обработчиков.

На префабах слотов и башен должны быть коллайдеры на слоях `TowerSlot` / `Tower` (создать слои в Tags & Layers через `manage_editor` → `tags`).

### 11.5 EventSystem

В сцене `Gameplay.unity` (и `Menu.unity`) заменить `StandaloneInputModule` на `InputSystemUIInputModule`. Иначе Unity Input System выкинет предупреждение.

---

## 12. GPU Instancing

`Assets/Game/Content/kenney_tower-defense-kit/Materials/colormap.mat` (точный путь — см. память `ref_kenney_asset_pack.md`):

- Включить чекбокс `Enable GPU Instancing` (в `.mat` это `m_EnableInstancingVariants: 1`).
- Все меши, использующие этот материал, автоматически батчатся. Проверка в Frame Debugger: число draw call'ов на уровне ≤ 10.

Через `manage_material` (или правкой `.mat` текстом):

```yaml
m_EnableInstancingVariants: 1
```

---

## 13. Ручной тест

1. Запуск L1 → кнопка Pause на HUD → появляется `PauseView`, игра замирает (враги стоят, таймеры не тикают).
2. Resume → game time возобновляется, враги продолжают, таймер перерыва корректный.
3. Pause → Restart → уровень начинается заново с полным HP/gold, `Time.timeScale = 1`, старые враги уничтожены.
4. Pause → Menu → возврат в меню, `Time.timeScale = 1`, сцена `Menu.unity` загружена, повторный вход в уровень работает.
5. Падение HP базы до 0 → `LevelFailedView`. Retry → уровень начинается заново без зависаний/утечек.
6. Выстрел башни → слышен `Shot` SFX, смерть врага → `EnemyDeath`, победа → `LevelWin`, поражение → `LevelFail`.
7. Editor: тап мышью по слоту / башне работает через `WorldTapRouter`; кнопки UI работают через `InputSystemUIInputModule`.
8. APK: тап пальцем по слоту/башне, мульти-тап не ломает, UI отзывчив.
9. Frame Debugger (Editor) — атлас `colormap` батчится в 1–2 draw call'а благодаря GPU Instancing.
10. Прогон всех 5 уровней подряд (регрессия итерации 6) — никаких утечек живых врагов/снарядов между запусками, `PlayerProgress` сохраняет звёзды.

---

## 14. Контрольный список готовности итерации

- [ ] Pause overlay блокирует игру (`Time.timeScale = 0`), Resume/Restart/Menu работают
- [ ] `LevelFailedState` сбрасывает `Time.timeScale` и очищает врагов/снаряды перед Retry
- [ ] `AudioService` играет 5 SFX по сигналам, без ошибок при пустых `AudioClip`
- [ ] Камера в префабах уровней — ортографическая, ландшафт, top-down
- [ ] Unity Input System — `InputSystemUIInputModule` в сценах, `WorldTapRouter` ловит тап по слоям `TowerSlot`/`Tower`, UI-тач не проваливается в мир (`IsPointerOverGameObject`)
- [ ] `colormap.mat` — `Enable GPU Instancing = true`, draw call'ы уменьшились
- [ ] Компиляция без ошибок (`read_console`), APK собирается и запускается, все 5 уровней проходятся
