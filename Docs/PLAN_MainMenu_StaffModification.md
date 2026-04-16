# ПЛАН РАЗРАБОТКИ — MainMenu + StaffModification

> Документ: технический план фазы UI/Меню  
> Основа: GDD_MagicStaff_Prototype v0.1 (§3, §8 Фаза 3, §9)  
> Дата: 2026-04-16  

---

## ЦЕЛЬ

Реализовать:
1. **Главное меню** — две кнопки: `В ПОДЗЕМЕЛЬЕ` и `ПОСОХ`
2. **Переход на сцену Gameplay** по кнопке `В ПОДЗЕМЕЛЬЕ`
3. **Окно модификации посоха** — открывается поверх меню (отдельный `DisplayableView`), позволяет менять детали посоха в 5 слотах

---

## АРХИТЕКТУРА (КРАТКО)

```
ProjectContext
  └── ProjectInstaller
        ├── MetaProgressionService   (живёт между сценами)
        └── StaffLoadoutService      (текущий набор деталей, живёт между сценами)

MainMenu Scene
  └── MainMenuInstaller
        ├── MainMenuController
        └── StaffModificationController

Views (наследуют DisplayableView)
  ├── MainMenuView
  └── StaffModificationView
        └── StaffSlotView × 5
```

---

## ЗАДАЧИ

### 0. ScriptableObject-схема деталей посоха

Нужна до UI — без данных нечего отображать.

- [ ] **`StaffPartSO`** — одна деталь посоха
  ```
  string partName
  StaffSlot slot          // enum: Artifact, TopCap, Grip, Shaft, BottomCap
  Sprite icon
  StatModifier[] modifiers // struct: StatType stat, float value
  string description
  ```
- [ ] **`StaffLoadoutSO`** — набор деталей для одного забега (5 слотов)
  ```
  StaffPartSO artifact
  StaffPartSO topCap
  StaffPartSO grip
  StaffPartSO shaft
  StaffPartSO bottomCap
  ```
- [ ] Создать папку `Resources/StaffParts/` и заполнить 2–3 варианта на каждый слот
- [ ] Создать `Resources/DefaultLoadout.asset` — ссылки на базовые детали

---

### 1. ProjectContext — `StaffLoadoutService`

Сервис живёт между сценами, хранит текущий выбранный loadout игрока.

```csharp
public class StaffLoadoutService
{
    public StaffLoadoutSO ActiveLoadout => _loadout;

    public void SetPart(StaffSlot slot, StaffPartSO part) { ... }
    // сохранение через PlayerPrefs (IDs деталей) в SetPart
}
```

- [ ] Создать `StaffLoadoutService`
- [ ] Забиндить в `ProjectInstaller` как `AsSingle`
- [ ] При старте: загрузить сохранённый loadout или взять `DefaultLoadout`

---

### 2. Сцена MainMenu

- [ ] Создать сцену `Assets/Scenes/MainMenu.unity`
- [ ] Добавить `Canvas` (Screen Space — Overlay, CanvasScaler: 1080×1920, Match Width or Height 0.5)
- [ ] Создать `MainMenuInstaller : MonoInstaller` и повесить на SceneContext
- [ ] Добавить сцену в Build Settings (index 0)
- [ ] Gameplay-сцена — index 1

---

### 3. `MainMenuView : DisplayableView`

Отвечает только за UI-события, не знает о сценах.

```csharp
public class MainMenuView : DisplayableView
{
    public event Action OnPlayClicked;
    public event Action OnStaffClicked;

    [SerializeField]
    Button _playButton;
    [SerializeField]
    Button _staffButton;

    void Awake()
    {
        _playButton.onClick.AddListener(() => OnPlayClicked?.Invoke());
        _staffButton.onClick.AddListener(() => OnStaffClicked?.Invoke());
    }
}
```

- [ ] Создать prefab `MainMenuCanvas`
- [ ] Разместить две кнопки (см. §3 GDD)
- [ ] Имплементировать `MainMenuView`

---

### 4. `MainMenuController`

Реагирует на события View, управляет переходом сцены и видимостью StaffModificationView.

```csharp
public class MainMenuController : IInitializable, IDisposable
{
    readonly MainMenuView _menuView;
    readonly StaffModificationView _staffView;
    readonly ISceneLoader _sceneLoader;

    [Inject]
    public MainMenuController(MainMenuView menuView,
                              StaffModificationView staffView,
                              ISceneLoader sceneLoader) { ... }

    public void Initialize()
    {
        _menuView.OnPlayClicked  += HandlePlay;
        _menuView.OnStaffClicked += HandleStaff;
    }

    void HandlePlay()  => _sceneLoader.Load("Gameplay");
    void HandleStaff() => _staffView.Show();

    public void Dispose()
    {
        _menuView.OnPlayClicked  -= HandlePlay;
        _menuView.OnStaffClicked -= HandleStaff;
    }
}
```

- [ ] Создать `ISceneLoader` интерфейс + `SceneLoader` реализация (через `SceneManager.LoadScene`)
- [ ] Забиндить `MainMenuController` и `SceneLoader` в `MainMenuInstaller`

---

### 5. `StaffSlotView`

Один виджет слота — иконка детали + название + кнопка выбора.

```csharp
public class StaffSlotView : MonoBehaviour
{
    public event Action<StaffSlot> OnSlotClicked;

    [SerializeField] StaffSlot _slot;
    [SerializeField] Image _icon;
    [SerializeField] TMP_Text _label;
    [SerializeField] Button _button;

    void Awake() => _button.onClick.AddListener(() => OnSlotClicked?.Invoke(_slot));

    public void Render(StaffPartSO part)
    {
        _icon.sprite = part.icon;
        _label.text  = part.partName;
    }
}
```

- [ ] Создать prefab `StaffSlotWidget`
- [ ] Разместить 5 штук вертикально в `StaffModificationView`

---

### 6. `StaffModificationView : DisplayableView`

Отображает 5 слотов. При клике на слот — открывает `PartPickerView`.

```csharp
public class StaffModificationView : DisplayableView
{
    public event Action<StaffSlot> OnSlotClicked;
    public event Action OnClosed;

    [SerializeField] StaffSlotView[] _slots; // 5 штук, назначены в Inspector
    [SerializeField] Button _closeButton;

    void Awake()
    {
        foreach (var slot in _slots)
            slot.OnSlotClicked += s => OnSlotClicked?.Invoke(s);
        _closeButton.onClick.AddListener(() => OnClosed?.Invoke());
    }

    public void RenderLoadout(StaffLoadoutSO loadout)
    {
        foreach (var slotView in _slots)
            slotView.Render(loadout.GetPart(slotView.Slot));
    }
}
```

- [ ] Создать prefab `StaffModificationCanvas` (отдельный Canvas поверх меню, по умолчанию — `Hide()`)
- [ ] Реализовать `Show()` / `Hide()` через `gameObject.SetActive`

---

### 7. `PartPickerView : DisplayableView`

Список доступных деталей для выбранного слота.

```csharp
public class PartPickerView : DisplayableView
{
    public event Action<StaffPartSO> OnPartSelected;

    [SerializeField] Transform _listRoot;
    [SerializeField] PartPickerItemView _itemPrefab;

    public void Render(StaffPartSO[] availableParts)
    {
        // очистить _listRoot, заспавнить _itemPrefab для каждой детали
    }
}
```

- [ ] `PartPickerItemView` — кнопка с иконкой и названием, кидает `OnSelected`
- [ ] Список деталей на слот пока = все `StaffPartSO` с нужным `slot` из `Resources/StaffParts/`

---

### 8. `StaffModificationController`

```csharp
public class StaffModificationController : IInitializable, IDisposable
{
    readonly StaffModificationView _modView;
    readonly PartPickerView        _pickerView;
    readonly StaffLoadoutService   _loadoutService;

    StaffSlot _pendingSlot;

    public void Initialize()
    {
        _modView.OnSlotClicked += OpenPicker;
        _modView.OnClosed      += () => _modView.Hide();
        _pickerView.OnPartSelected += SelectPart;
    }

    void OpenPicker(StaffSlot slot)
    {
        _pendingSlot = slot;
        var parts = LoadPartsForSlot(slot); // Resources.LoadAll<StaffPartSO>
        _pickerView.Render(parts);
        _pickerView.Show();
    }

    void SelectPart(StaffPartSO part)
    {
        _loadoutService.SetPart(_pendingSlot, part);
        _modView.RenderLoadout(_loadoutService.ActiveLoadout);
        _pickerView.Hide();
    }

    StaffPartSO[] LoadPartsForSlot(StaffSlot slot) =>
        Resources.LoadAll<StaffPartSO>("StaffParts")
                 .Where(p => p.slot == slot)
                 .ToArray();

    public void Dispose() { /* отписки */ }
}
```

- [ ] Забиндить в `MainMenuInstaller`

---

### 9. `MainMenuInstaller`

```csharp
public class MainMenuInstaller : MonoInstaller
{
    [SerializeField] MainMenuView _menuView;
    [SerializeField] StaffModificationView _staffView;
    [SerializeField] PartPickerView _pickerView;

    public override void InstallBindings()
    {
        Container.BindInstance(_menuView);
        Container.BindInstance(_staffView);
        Container.BindInstance(_pickerView);

        Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
        Container.BindInterfacesAndSelfTo<MainMenuController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<StaffModificationController>().AsSingle().NonLazy();
    }
}
```

---

## ПОРЯДОК ВЫПОЛНЕНИЯ

```
1. StaffPartSO + StaffSlot enum + StaffLoadoutSO           (данные)
2. StaffLoadoutService + ProjectInstaller bind             (сервис)
3. MainMenu сцена + Canvas + Build Settings                (сцена)
4. MainMenuView prefab + скрипт                            (view)
5. ISceneLoader + SceneLoader                              (навигация)
6. StaffModificationView prefab + StaffSlotView × 5       (view)
7. PartPickerView prefab + PartPickerItemView              (view)
8. StaffModificationController                             (логика)
9. MainMenuController                                      (логика)
10. MainMenuInstaller — всё вместе                         (DI)
11. Ручной тест: меню → посох → смена детали → в игру     (QA)
```

---

## КРИТЕРИИ ГОТОВНОСТИ

- [ ] Кнопка `В ПОДЗЕМЕЛЬЕ` → загружает сцену `Gameplay`
- [ ] Кнопка `ПОСОХ` → открывает `StaffModificationView` поверх меню
- [ ] В окне посоха отображается текущий loadout (5 слотов с иконками)
- [ ] Клик на слот → открывается `PartPickerView` с доступными деталями
- [ ] Выбор детали → слот обновляется, выбор сохраняется через `StaffLoadoutService`
- [ ] Кнопка закрытия → `StaffModificationView.Hide()`
- [ ] Никаких `FindObjectOfType` — все зависимости через Zenject
- [ ] Все View наследуют `DisplayableView`

---

## ФАЙЛОВАЯ СТРУКТУРА

```
Assets/
  Game/
    MainMenu/
      Scripts/
        Controllers/
          MainMenuController.cs
          StaffModificationController.cs
        Views/
          MainMenuView.cs
          StaffModificationView.cs
          StaffSlotView.cs
          PartPickerView.cs
          PartPickerItemView.cs
        Services/
          ISceneLoader.cs
          SceneLoader.cs
      Prefabs/
        MainMenuCanvas.prefab
        StaffModificationCanvas.prefab
        StaffSlotWidget.prefab
        PartPickerPanel.prefab
        PartPickerItem.prefab
      Installers/
        MainMenuInstaller.cs
    Staff/
      ScriptableObjects/
        StaffPartSO.cs
        StaffLoadoutSO.cs
        Enum/
          StaffSlot.cs
          StatType.cs
        StaffLoadoutService.cs
      Resources/
        StaffParts/       ← .asset файлы деталей
        DefaultLoadout.asset
```

---

*Конец плана. Следующий этап: ПЛАН_Gameplay_Core.md*
