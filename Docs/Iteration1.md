# Итерация 1 — Меню и выбор уровня

**Цель:** Рабочее главное меню и экран выбора уровней. Игрок видит 5 слотов (заблокированы кроме 1-го), нажимает на доступный → отправляется сигнал → `LoadLevelState` грузит сцену `Gameplay.unity` (пока пустая заглушка с возвратом в меню через 1 сек). Прогресс сохраняется.

---

## Содержание

1. Структура новых файлов
2. Сигналы
3. Core: новые состояния (`LoadLevel`, `Gameplay`)
4. UI: `DisplayableView`, `MainMenuView`, `LevelSelectView`, `LevelButton`
5. Presenters / SceneInstaller
6. Конфиг уровней (минимальный SO)
7. Правки `ProjectInstaller`, `GameLoopStateMachine`, `MainMenuState`
8. Unity-сцены и prefabs
9. Чек-лист прогресса

---

## 1. Структура файлов

```
Assets/Game/Scripts/
  Core/
    GameLoop/States/
      LoadLevelState.cs          ← new
      GameplayState.cs           ← new (stub)
    Signals/
      LevelStartRequestedSignal.cs ← new
      LevelLoadedSignal.cs         ← new
  Configs/
    LevelCatalog.cs              ← new (SO со списком id → sceneName)
    LevelDefinition.cs           ← new
  UI/
    Views/
      DisplayableView.cs         ← new
      MainMenuView.cs            ← new
      LevelSelectView.cs         ← new
      LevelButton.cs             ← new
    Presenters/
      MainMenuPresenter.cs       ← new
      LevelSelectPresenter.cs    ← new
  Bootstrap/
    MenuInstaller.cs             ← new (SceneContext Menu)
    GameplayInstaller.cs         ← new (SceneContext Gameplay, заглушка)
Assets/Game/Scenes/
  Menu.unity                     ← new
  Gameplay.unity                 ← new (переименовать SampleScene или рядом)
Assets/Game/Settings/
  LevelCatalog.asset             ← new
```

Для меты итерации 0: `SampleScene.unity` остаётся Bootstrap-сценой (там SceneContext, который подгружает ProjectContext). Из неё `InitializeState` после загрузки прогресса сразу уходит в `MainMenuState`, который попросит `SceneLoader` загрузить `Menu.unity`.

---

## 2. Сигналы

### `Core/Signals/LevelStartRequestedSignal.cs`
```csharp
public class LevelStartRequestedSignal
{
    public int LevelId;
}
```

### `Core/Signals/LevelLoadedSignal.cs`
```csharp
public class LevelLoadedSignal
{
    public int LevelId;
}
```

Биндинг сигналов — в `ProjectInstaller` (см. §7).

---

## 3. Новые состояния GameLoop

### `Core/GameLoop/States/LoadLevelState.cs`
```csharp
using UnityEngine;
using Zenject;

public class LoadLevelState : GameLoopState
{
    private readonly SceneLoader _sceneLoader;
    private readonly SignalBus _signalBus;
    private readonly LevelCatalog _catalog;

    private int _pendingLevelId;

    public LoadLevelState(GameLoopStateMachine sm, SceneLoader sceneLoader, SignalBus signalBus, LevelCatalog catalog)
        : base(sm)
    {
        _sceneLoader = sceneLoader;
        _signalBus = signalBus;
        _catalog = catalog;
    }

    public override void OnStateRegistered()
    {
        _signalBus.Subscribe<LevelStartRequestedSignal>(OnLevelStartRequested);
    }

    public override void OnStateActivated()
    {
        Debug.Log($"[LoadLevelState] loading level {_pendingLevelId}");
        var def = _catalog.Get(_pendingLevelId);
        _sceneLoader.LoadScene(def.SceneName, () =>
        {
            _signalBus.Fire(new LevelLoadedSignal { LevelId = _pendingLevelId });
            StateMachine.SetState(GameLoopStateMachine.State.Gameplay);
        });
    }

    public override void OnStateDisabled() { }

    private void OnLevelStartRequested(LevelStartRequestedSignal signal)
    {
        _pendingLevelId = signal.LevelId;
        StateMachine.SetState(GameLoopStateMachine.State.LoadLevel);
    }
}
```

### `Core/GameLoop/States/GameplayState.cs` (заглушка)
```csharp
using UnityEngine;

public class GameplayState : GameLoopState
{
    private readonly SceneLoader _sceneLoader;
    private float _autoReturnTime;

    public GameplayState(GameLoopStateMachine sm, SceneLoader sceneLoader) : base(sm)
    {
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        Debug.Log("[GameplayState] activated (stub — auto-return in 1s)");
        _autoReturnTime = Time.time + 1f;
    }

    public override void Update()
    {
        if (Time.time >= _autoReturnTime)
            StateMachine.SetState(GameLoopStateMachine.State.MainMenu);
    }

    public override void OnStateDisabled() { }
}
```

> В итерации 2 заглушка заменится реальным геймплеем.

---

## 4. UI-классы

### `UI/Views/DisplayableView.cs`
```csharp
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class DisplayableView : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup _canvasGroup;

    public bool IsVisible => _canvasGroup.alpha > 0.99f;

    protected virtual void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual void Show()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    public virtual void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
}
```

### `UI/Views/MainMenuView.cs`
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : DisplayableView
{
    public event Action PlayClicked;

    [SerializeField]
    private Button _playButton;

    [SerializeField]
    private Button _quitButton;

    protected override void Awake()
    {
        base.Awake();
        _playButton.onClick.AddListener(() => PlayClicked?.Invoke());
        _quitButton.onClick.AddListener(Application.Quit);
    }
}
```

### `UI/Views/LevelButton.cs`
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public event Action<int> Clicked;

    [SerializeField]
    private int _levelId;

    [SerializeField]
    private Button _button;

    [SerializeField]
    private GameObject _lockIcon;

    [SerializeField]
    private GameObject[] _stars;

    [SerializeField]
    private Text _label;

    public int LevelId => _levelId;

    private void Awake()
    {
        _button.onClick.AddListener(() => Clicked?.Invoke(_levelId));
    }

    public void Bind(bool unlocked, int stars)
    {
        _button.interactable = unlocked;
        _lockIcon.SetActive(!unlocked);
        _label.text = _levelId.ToString();
        for (int i = 0; i < _stars.Length; i++)
            _stars[i].SetActive(i < stars);
    }
}
```

### `UI/Views/LevelSelectView.cs`
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectView : DisplayableView
{
    public event Action<int> LevelSelected;
    public event Action BackClicked;

    [SerializeField]
    private LevelButton[] _buttons;

    [SerializeField]
    private Button _backButton;

    protected override void Awake()
    {
        base.Awake();
        foreach (var btn in _buttons)
            btn.Clicked += id => LevelSelected?.Invoke(id);
        _backButton.onClick.AddListener(() => BackClicked?.Invoke());
    }

    public void Refresh(PlayerProgress progress)
    {
        foreach (var btn in _buttons)
        {
            bool unlocked = btn.LevelId <= progress.UnlockedLevel;
            btn.Bind(unlocked, progress.GetStars(btn.LevelId));
        }
    }
}
```

---

## 5. Presenters + SceneInstaller

### `UI/Presenters/MainMenuPresenter.cs`
```csharp
using Zenject;

public class MainMenuPresenter : IInitializable, System.IDisposable
{
    private readonly MainMenuView _mainMenu;
    private readonly LevelSelectView _levelSelect;
    private readonly PlayerProgress _progress;
    private readonly SignalBus _signalBus;

    public MainMenuPresenter(MainMenuView mainMenu, LevelSelectView levelSelect,
        PlayerProgress progress, SignalBus signalBus)
    {
        _mainMenu = mainMenu;
        _levelSelect = levelSelect;
        _progress = progress;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        _mainMenu.PlayClicked += OnPlay;
        _levelSelect.BackClicked += OnBack;
        _levelSelect.LevelSelected += OnLevelSelected;

        _mainMenu.Show();
        _levelSelect.Hide();
    }

    public void Dispose()
    {
        _mainMenu.PlayClicked -= OnPlay;
        _levelSelect.BackClicked -= OnBack;
        _levelSelect.LevelSelected -= OnLevelSelected;
    }

    private void OnPlay()
    {
        _levelSelect.Refresh(_progress);
        _mainMenu.Hide();
        _levelSelect.Show();
    }

    private void OnBack()
    {
        _levelSelect.Hide();
        _mainMenu.Show();
    }

    private void OnLevelSelected(int levelId)
    {
        if (levelId > _progress.UnlockedLevel)
            return;
        _signalBus.Fire(new LevelStartRequestedSignal { LevelId = levelId });
    }
}
```

### `Bootstrap/MenuInstaller.cs`
```csharp
using UnityEngine;
using Zenject;

public class MenuInstaller : MonoInstaller
{
    [SerializeField]
    private MainMenuView _mainMenuView;

    [SerializeField]
    private LevelSelectView _levelSelectView;

    public override void InstallBindings()
    {
        Container.Bind<MainMenuView>().FromInstance(_mainMenuView).AsSingle();
        Container.Bind<LevelSelectView>().FromInstance(_levelSelectView).AsSingle();
        Container.BindInterfacesAndSelfTo<MainMenuPresenter>().AsSingle().NonLazy();
    }
}
```

### `Bootstrap/GameplayInstaller.cs` (пустая сейчас)
```csharp
using Zenject;

public class GameplayInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // в итерации 2+ сюда появятся Wallet, WaveSpawner, EnemyFactory и т.д.
    }
}
```

---

## 6. Конфиг уровней

### `Configs/LevelDefinition.cs`
```csharp
using System;

[Serializable]
public class LevelDefinition
{
    public int Id;
    public string DisplayName;
    public string SceneName;
}
```

### `Configs/LevelCatalog.cs`
```csharp
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "TD/LevelCatalog", fileName = "LevelCatalog")]
public class LevelCatalog : ScriptableObject
{
    [SerializeField]
    private LevelDefinition[] _levels;

    public LevelDefinition[] Levels => _levels;

    public LevelDefinition Get(int id) => _levels.First(l => l.Id == id);
}
```

**Asset:** `Assets/Game/Settings/LevelCatalog.asset` — заполнить 5 записей, пока все c `SceneName = "Gameplay"`.

---

## 7. Правки существующих файлов

### `Bootstrap/ProjectInstaller.cs`
```csharp
using UnityEngine;
using Zenject;

[CreateAssetMenu(menuName = "Installers/ProjectInstaller", fileName = "ProjectInstaller")]
public class ProjectInstaller : ScriptableObjectInstaller<ProjectInstaller>
{
    [SerializeField]
    private LevelCatalog _levelCatalog;

    public override void InstallBindings()
    {
        SignalBusInstaller.Install(Container);

        Container.Bind<LevelCatalog>().FromInstance(_levelCatalog).AsSingle();
        Container.Bind<PersistenceService>().AsSingle();
        Container.Bind<SceneLoader>().AsSingle();
        Container.Bind<PlayerProgress>().AsSingle();
        Container.Bind<GameLoopStateMachine>().AsSingle();

        Container.DeclareSignal<LevelStartRequestedSignal>();
        Container.DeclareSignal<LevelLoadedSignal>();
    }
}
```

### `Core/GameLoop/GameLoopStateMachine.cs` — регистрация новых состояний
```csharp
protected override void RegisterStates()
{
    RegisterState(_container.Instantiate<InitializeState>(new object[] { this }), State.Initialize);
    RegisterState(_container.Instantiate<MainMenuState>(new object[] { this }), State.MainMenu);
    RegisterState(_container.Instantiate<LoadLevelState>(new object[] { this }), State.LoadLevel);
    RegisterState(_container.Instantiate<GameplayState>(new object[] { this }), State.Gameplay);
}
```

### `Core/GameLoop/States/MainMenuState.cs` — грузим сцену меню
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuState : GameLoopState
{
    private const string MenuSceneName = "Menu";

    private readonly SceneLoader _sceneLoader;

    public MainMenuState(GameLoopStateMachine sm, SceneLoader sceneLoader) : base(sm)
    {
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        Debug.Log("[MainMenuState] activated");
        if (SceneManager.GetActiveScene().name != MenuSceneName)
            _sceneLoader.LoadScene(MenuSceneName);
    }

    public override void OnStateDisabled() { }
}
```

> `MenuInstaller` на `SceneContext` инстанцирует `MainMenuPresenter` автоматически после загрузки сцены — презентер подключается к уже существующему `SignalBus` в ProjectContext.

---

## 8. Unity-сцены и prefabs (через UnityMCP)

1. **Build Settings:** добавить `SampleScene` (index 0), `Menu` (1), `Gameplay` (2).
2. **Menu.unity:**
   - `SceneContext` + ссылка на `MenuInstaller` GO.
   - Canvas (Screen Space Overlay, Landscape Reference 1920×1080).
   - GO `MainMenu` (CanvasGroup) с `MainMenuView`, кнопки Play/Quit.
   - GO `LevelSelect` (CanvasGroup) с `LevelSelectView`, 5 префабов `LevelButton` (id 1..5), кнопка Back.
   - `MenuInstaller` GO — поля указывают на оба View.
3. **Gameplay.unity:** пустая сцена с Camera + Directional Light + `SceneContext` + `GameplayInstaller`.
4. **LevelCatalog.asset:** 5 записей, `SceneName="Gameplay"`.
5. **ProjectInstaller.asset:** привязать `_levelCatalog`.

---

## 9. Прогресс

| Шаг | Статус |
|-----|--------|
| Сигналы `LevelStartRequestedSignal`, `LevelLoadedSignal` | ✅ |
| `LoadLevelState`, `GameplayState` | ✅ |
| `GameLoopStateMachine.RegisterStates` обновлён | ✅ |
| `MainMenuState` грузит `Menu.unity` | ✅ |
| `DisplayableView` | ✅ |
| `MainMenuView` / `LevelSelectView` / `LevelButton` | ✅ |
| `MainMenuPresenter` | ✅ |
| `MenuInstaller`, `GameplayInstaller` | ✅ |
| `LevelDefinition` + `LevelCatalog` + `.asset` | ✅ |
| `ProjectInstaller` обновлён (catalog + signal declarations) | ✅ |
| Сцены `Menu.unity`, `Gameplay.unity` собраны | ✅ |
| Сцены добавлены в Build Settings | ✅ |
| Компиляция без ошибок (`read_console`) | ✅ |
| Runtime smoke: Sample → Menu → LevelSelect → (1) → Gameplay → Menu | ✅ Init→MainMenu→LoadLevel(1)→Gameplay (авто-возврат — в ручной сессии) |
| Progress save: после авто-возврата второй уровень остаётся залочен (stars=0) | ⬜ (нужен ручной прогон с FixedUpdate-фреймами) |

---

## 10. Тест-критерий итерации

1. Запуск из `SampleScene`:
   `Construct → Initialize → MainMenu (грузит Menu.unity) → Presenter.Show MainMenuView`.
2. Клик «Играть» → показывается `LevelSelectView`, кнопка 1 интерактивна, 2–5 залочены.
3. Клик по 1 → `LevelStartRequestedSignal` → `LoadLevelState` → `Gameplay.unity` → `GameplayState` (через 1с возврат).
4. После возврата `MainMenuState` снова загружает `Menu`, меню отображается корректно.
5. Никаких `NullReferenceException`, никаких утечек подписок (повторный цикл Play→Back→Play работает без ошибок).

---

## 11. Заметки / gotchas

- `MainMenuPresenter` создаётся через `NonLazy()` в `MenuInstaller` — чтобы `Initialize()` вызвался автоматом при старте сцены Menu.
- `SignalBus` берётся из `ProjectContext` (родительский контейнер) — декларацию сигналов держим в `ProjectInstaller`, чтобы они переживали смену сцен.
- `SceneLoader.LoadScene` сейчас использует `LoadSceneMode.Single` — после перехода Unity уничтожает прежний SceneContext, так что `MainMenuPresenter` с его подписками тоже уничтожается, и мы не течём.
- `LoadLevelState.OnStateRegistered` подписан один раз на `LevelStartRequestedSignal` — это валидно, так как состояние живёт всё время работы SM.
- `GameplayState` — временная заглушка итерации 1. В итерации 2 уйдёт логика запуска волн.
- Если в итерации 2 появится `Back to menu` из геймплея — презентер `HudView` будет сам фаерить сигнал возврата, а не лезть в SM напрямую.
