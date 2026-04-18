# План реализации: HP Bar, урон, поражение, монеты

## Порядок реализации

### Блок A — Базовые типы

1. `PlayerConfig.cs` — добавить поля:
   ```csharp
   public float maxHp              = 100f;
   public float enemyContactDamage = 10f;
   public float damageCooldown     = 0.5f;
   ```

2. `Assets/Game/Scripts/Services/ICoinService.cs` — новый интерфейс:
   ```csharp
   public interface ICoinService
   {
       int Coins { get; }
       void AddCoins(int amount);
       event Action<int> OnCoinsChanged;
   }
   ```

3. `Assets/Game/Scripts/Services/CoinService.cs` — реализация через `PlayerPrefs` (ключ `"PlayerCoins"`)

4. `Assets/Game/Gameplay/Scripts/Player/IPlayerHealthService.cs`:
   ```csharp
   public interface IPlayerHealthService
   {
       float MaxHp        { get; }
       float CurrentHp    { get; }
       float NormalizedHp { get; }
       event Action<float> OnHpChanged;
       event Action        OnDied;
   }
   ```

5. `Assets/Game/Gameplay/Scripts/Player/PlayerHealthService.cs` — реализация `IPlayerHealthService`, `IInitializable`; `TakeDamage(float)` вызывает события

---

### Блок B — MonoBehaviour и Views

6. `Assets/Game/Gameplay/Scripts/Player/PlayerHitReceiver.cs` (MonoBehaviour)
   - `OnTriggerEnter` / `OnCollisionEnter` с тегом `"Enemy"`
   - cooldown через `Time.time` и `_config.damageCooldown`
   - вызывает `_healthService.TakeDamage(_config.enemyContactDamage)`

7. `Assets/Game/Gameplay/Scripts/Views/HpBarView.cs`
   - наследуется от `DisplayableView`
   - `[SerializeField] Slider _slider`
   - `void SetProgress(float normalized)`

8. `Assets/Game/Gameplay/Scripts/Views/DefeatView.cs`
   - наследуется от `DisplayableView`
   - `event Action OnContinueClicked`
   - `[SerializeField] Button _continueButton`

9. `Assets/Game/MainMenu/Scripts/Views/CoinCounterView.cs`
   - наследуется от `DisplayableView`
   - `[SerializeField] TMP_Text _label`
   - `void SetCoins(int amount)`

---

### Блок C — Контроллеры

10. `Assets/Game/Gameplay/Scripts/Player/HpHudController.cs` (`IInitializable`, `IDisposable`)
    - подписывается на `OnHpChanged`, обновляет `HpBarView.SetProgress`

11. `Assets/Game/Gameplay/Scripts/Player/DefeatController.cs` (`IInitializable`, `IDisposable`)
    - `OnDied` → `AddCoins(1)`, `Time.timeScale = 0f`, `_defeatView.Show()`
    - `OnContinueClicked` → `Time.timeScale = 1f`, `_sceneLoader.Load("MainMenu")`

12. `Assets/Game/MainMenu/Scripts/Controllers/CoinCounterController.cs` (`IInitializable`, `IDisposable`)
    - инициализирует `CoinCounterView` текущим значением монет
    - подписывается на `OnCoinsChanged`

---

### Блок D — DI / Zenject bindings

13. **`ISceneLoader` / `SceneLoader`** — перенести из namespace `MagicStaff.MainMenu` в общий (например `MagicStaff`) и переместить в `Assets/Game/Scripts/Services/`. Обновить `using` в `MainMenuController` и `MainMenuInstaller`.

14. **`ProjectInstaller.cs`** — добавить:
    ```csharp
    Container.Bind<ICoinService>().To<CoinService>().AsSingle();
    ```

15. **`GameplayInstaller.cs`** — добавить serialized поля:
    ```csharp
    [SerializeField] private HpBarView         _hpBarView;
    [SerializeField] private DefeatView        _defeatView;
    [SerializeField] private PlayerHitReceiver _playerHitReceiver;
    ```
    Добавить в `InstallBindings()`:
    ```csharp
    Container.BindInstance(_hpBarView);
    Container.BindInstance(_defeatView);
    Container.QueueForInject(_playerHitReceiver);

    Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
    Container.BindInterfacesAndSelfTo<PlayerHealthService>().AsSingle().NonLazy();
    Container.BindInterfacesAndSelfTo<HpHudController>().AsSingle().NonLazy();
    Container.BindInterfacesAndSelfTo<DefeatController>().AsSingle().NonLazy();
    ```

16. **`MainMenuInstaller.cs`** — добавить:
    ```csharp
    [SerializeField] private CoinCounterView _coinCounterView;
    // в InstallBindings():
    Container.BindInstance(_coinCounterView);
    Container.BindInterfacesAndSelfTo<CoinCounterController>().AsSingle().NonLazy();
    ```

---

### Блок E — Prefabs / Сцена

17. **`GameplayHUD.prefab`** — добавить `HpBar` (Slider) ниже ExperienceBar, короче по ширине (~70%), Fill красного цвета. Назначить компонент `HpBarView`.

18. **Canvas Gameplay** — добавить `DefeatPanel` (overlay с затемнением, текст "Defeat", кнопка "Continue"). Назначить компонент `DefeatView`.

19. **Player GameObject** — добавить компонент `PlayerHitReceiver`.

20. **Enemy.prefab** — убедиться, что выставлен тег `"Enemy"`.

21. **`GameplayInstaller` на сцене** — назначить serialized refs: `_hpBarView`, `_defeatView`, `_playerHitReceiver`.

22. **`MainMenuCanvas.prefab`** — добавить `CoinCounter` (TMP_Text) в верхней части Canvas. Назначить компонент `CoinCounterView`.

23. **`MainMenuInstaller` на сцене** — назначить serialized ref `_coinCounterView`.

---

## Таблица Zenject bindings

| Installer           | Binding                                   | Scope                    |
|---------------------|-------------------------------------------|--------------------------|
| `ProjectInstaller`  | `ICoinService → CoinService`              | AsSingle (Project level) |
| `GameplayInstaller` | `IPlayerHealthService → PlayerHealthService` | AsSingle, NonLazy     |
| `GameplayInstaller` | `HpBarView` (instance)                    | —                        |
| `GameplayInstaller` | `DefeatView` (instance)                   | —                        |
| `GameplayInstaller` | `PlayerHitReceiver` (QueueForInject)      | —                        |
| `GameplayInstaller` | `ISceneLoader → SceneLoader`              | AsSingle                 |
| `GameplayInstaller` | `HpHudController`                         | AsSingle, NonLazy        |
| `GameplayInstaller` | `DefeatController`                        | AsSingle, NonLazy        |
| `MainMenuInstaller` | `CoinCounterView` (instance)              | —                        |
| `MainMenuInstaller` | `CoinCounterController`                   | AsSingle, NonLazy        |

---

## Важные замечания

- **`ISceneLoader`**: сейчас в `MagicStaff.MainMenu` — `DefeatController` из Gameplay его тоже использует, поэтому нужен общий namespace.
- **Физика столкновений**: реализовать оба метода `OnTriggerEnter` и `OnCollisionEnter` в `PlayerHitReceiver` — тип коллайдера на враге нужно уточнить.
- **`Time.timeScale`**: сбросить в `1f` перед `Load("MainMenu")` в `DefeatController.HandleContinue()`.
- **`PlayerHealthService.Initialize()`**: инициализировать `_currentHp = _config.maxHp` в `Initialize()`, не в конструкторе (Zenject сначала инжектирует, потом вызывает `Initialize()`).
