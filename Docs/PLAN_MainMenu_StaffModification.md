# ПЛАН РАЗРАБОТКИ — MainMenu + StaffModification

> Документ: технический план фазы UI/Меню  
> Основа: GDD_MagicStaff_Prototype v0.1 (§3, §8 Фаза 3, §9)  
> Дата: 2026-04-16  
> Обновлён: 2026-04-16 — выполнены задачи 0–10 полностью  

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

- [x] **`StaffPartConfig`** — одна деталь посоха
  ```
  string partName
  StaffSlot slot          // enum: Artifact, TopCap, Grip, Shaft, BottomCap
  Sprite icon
  StatModifier[] modifiers // struct: StatType stat, float value
  string description
  ```
  > `Assets/Game/Staff/Scripts/ScriptableObjects/StaffPartConfig.cs`  
  > `Assets/Game/Staff/Scripts/Enums/StaffSlot.cs` + `StatType.cs`  
  > `StatModifier` struct — в том же файле, что и `StaffPartConfig`
- [x] **`StaffLoadoutConfig`** — набор деталей для одного забега (5 слотов)
  ```
  StaffPartConfig artifact
  StaffPartConfig topCap
  StaffPartConfig grip
  StaffPartConfig shaft
  StaffPartConfig bottomCap
  ```
  > `Assets/Game/Staff/Scripts/ScriptableObjects/StaffLoadoutConfig.cs`  
  > Методы: `GetPart(StaffSlot)`, `SetPart(StaffSlot, StaffPartConfig)`
- [x] Создать папку `Resources/StaffParts/` и заполнить 2–3 варианта на каждый слот
  > `Assets/Game/Staff/Resources/StaffParts/` — 15 ассетов (по 3 на каждый слот)  
  > Artifact: FireGem / IceCore / VoidShard  
  > TopCap: GoldCrown / SpikeTip / LensFocus  
  > Grip: LeatherWrap / MetalBand / RuneGrip  
  > Shaft: OakWood / IronCore / ElderWood  
  > BottomCap: RubberEnd / SteelTip / CrystalBase
- [x] Создать `Resources/DefaultLoadout.asset` — ссылки на базовые детали
  > `Assets/Game/Staff/Resources/DefaultLoadout.asset`  
  > Defaults: FireGem / GoldCrown / LeatherWrap / OakWood / RubberEnd

---

### 1. ProjectContext — `StaffLoadoutService`

Сервис живёт между сценами, хранит текущий выбранный loadout игрока.

```csharp
public class StaffLoadoutService
{
    public StaffLoadoutConfig ActiveLoadout => _loadout;

    public void SetPart(StaffSlot slot, StaffPartConfig part) { ... }
    // сохранение через PlayerPrefs (IDs деталей) в SetPart
}
```

- [x] Создать `StaffLoadoutService`
  > `Assets/Game/Staff/Scripts/Services/StaffLoadoutService.cs`  
  > Конструктор вызывает `LoadOrDefault()`: инстанциирует `DefaultLoadout`, затем перекрывает слоты из `PlayerPrefs`  
  > `SetPart` пишет в `PlayerPrefs` имя ассета (`part.name`)
- [x] Забиндить в `ProjectInstaller` как `AsSingle`
  > `Assets/Game/Scripts/Installers/ProjectInstaller.cs`  
  > `Container.Bind<StaffLoadoutService>().AsSingle();`
- [x] При старте: загрузить сохранённый loadout или взять `DefaultLoadout`

---

### 2. Сцена MainMenu

- [x] Создать сцену `Assets/Game/Scenes/MainMenu.unity`
- [x] Добавить `Canvas` (Screen Space — Overlay, CanvasScaler: 1080×1920, Match Width or Height 0.5)
- [x] Создать `MainMenuInstaller : MonoInstaller` (`Assets/Game/MainMenu/Installers/MainMenuInstaller.cs`) и повесить на SceneContext
- [x] Добавить `DisplayableView` базовый класс (`Assets/Game/Scripts/Views/DisplayableView.cs`)
- [x] Добавить сцену в Build Settings (index 0)
- [x] Gameplay-сцена — index 2 (index 1 занят старой Menu.unity)

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

- [x] Создать prefab `MainMenuCanvas`
  > `Assets/Game/MainMenu/Prefabs/MainMenuCanvas.prefab`  
  > Canvas (1080×1920, ScaleWithScreenSize, match 0.5) + Background + Buttons (VLG)
- [x] Разместить две кнопки (см. §3 GDD)
  > PlayButton ("В ПОДЗЕМЕЛЬЕ", зелёный) + StaffButton ("ПОСОХ", синий), TMP labels
- [x] Имплементировать `MainMenuView`
  > `Assets/Game/MainMenu/Scripts/Views/MainMenuView.cs`  
  > Компонент добавлен на prefab, `_playButton`/`_staffButton` привязаны через SerializedObject  
  > Prefab инстанциирован на сцене MainMenu

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

- [x] Создать `ISceneLoader` интерфейс + `SceneLoader` реализация (через `SceneManager.LoadScene`)
  > `Assets/Game/MainMenu/Scripts/Services/ISceneLoader.cs`  
  > `Assets/Game/MainMenu/Scripts/Services/SceneLoader.cs`
- [x] Создать `MainMenuController`
  > `Assets/Game/MainMenu/Scripts/Controllers/MainMenuController.cs`
- [x] Создать `StaffModificationView` (stub — полная реализация в задаче 6)
  > `Assets/Game/MainMenu/Scripts/Views/StaffModificationView.cs`
- [x] Забиндить `MainMenuController` и `SceneLoader` в `MainMenuInstaller`
  > `_menuView` + `_staffView` — `BindInstance`  
  > `ISceneLoader → SceneLoader` + `MainMenuController` — `NonLazy`

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

    public void Render(StaffPartConfig part)
    {
        _icon.sprite = part.icon;
        _label.text  = part.partName;
    }
}
```

- [x] Создать скрипт `StaffSlotView`
  > `Assets/Game/MainMenu/Scripts/Views/StaffSlotView.cs`  
  > `Slot` — публичный accessor; `_icon`, `_label`, `_button` — SerializeField
- [x] Обновить `StaffModificationView` до полной реализации
  > `Assets/Game/MainMenu/Scripts/Views/StaffModificationView.cs`  
  > `_slots[]`, `_closeButton`, `RenderLoadout(StaffLoadoutConfig)`
- [x] Создать prefab `StaffSlotWidget`
  > `Assets/Game/MainMenu/Prefabs/StaffSlotWidget.prefab`  
  > Image (bg) + Button + HorizontalLayoutGroup + Icon (Image 60×60) + Label (TMP) + StaffSlotView  
  > Все SerializeField-ссылки назначены в prefab
- [x] Создать prefab `StaffModificationCanvas` с 5 слотами вертикально
  > `Assets/Game/MainMenu/Prefabs/StaffModificationCanvas.prefab`  
  > Canvas (sortingOrder=10) + Panel (затемнение) + Content (VLG) + Title + 5×StaffSlotWidget + CloseButton  
  > `_slots[5]` и `_closeButton` назначены в `StaffModificationView`  
  > Размещён на сцене MainMenu (по умолчанию неактивен), `_staffView` привязан в `MainMenuInstaller`

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

    public void RenderLoadout(StaffLoadoutConfig loadout)
    {
        foreach (var slotView in _slots)
            slotView.Render(loadout.GetPart(slotView.Slot));
    }
}
```

- [x] Создать prefab `StaffModificationCanvas` (отдельный Canvas поверх меню, по умолчанию — `Hide()`)
  > `Assets/Game/MainMenu/Prefabs/StaffModificationCanvas.prefab` — `m_IsActive: 0`
- [x] Реализовать `Show()` / `Hide()` через `gameObject.SetActive`
  > `DisplayableView.Show()` / `Hide()` — уже реализованы в базовом классе

---

### 7. `PartPickerView : DisplayableView`

Список доступных деталей для выбранного слота.

```csharp
public class PartPickerView : DisplayableView
{
    public event Action<StaffPartConfig> OnPartSelected;

    [SerializeField] Transform _listRoot;
    [SerializeField] PartPickerItemView _itemPrefab;

    public void Render(StaffPartConfig[] availableParts)
    {
        // очистить _listRoot, заспавнить _itemPrefab для каждой детали
    }
}
```

- [x] `PartPickerItemView` — кнопка с иконкой и названием, кидает `OnSelected`
  > `Assets/Game/MainMenu/Scripts/Views/PartPickerItemView.cs`  
  > `_icon`, `_label`, `_button` — SerializeField; `Render(StaffPartConfig)` + `OnSelected` event
- [x] `PartPickerView : DisplayableView` — список деталей для выбранного слота
  > `Assets/Game/MainMenu/Scripts/Views/PartPickerView.cs`  
  > `Render(StaffPartConfig[])` — очищает `_listRoot`, спавнит `_itemPrefab`; `OnPartSelected` event
- [x] Создать prefab `PartPickerItem`
  > `Assets/Game/MainMenu/Prefabs/PartPickerItem.prefab`  
  > RectTransform (400×80) + Image (bg) + Button + HorizontalLayoutGroup + Icon (60×60) + Label (TMP) + PartPickerItemView  
  > Все SerializeField-ссылки назначены
- [x] Создать prefab `PartPickerPanel`
  > `Assets/Game/MainMenu/Prefabs/PartPickerPanel.prefab`  
  > Canvas (sortingOrder=20, ScaleWithScreenSize 1080×1920) + Dimmer + Content (VLG) + Title + ScrollView/Viewport/ListRoot (ContentSizeFitter) + CloseButton  
  > `_listRoot` и `_itemPrefab` назначены в `PartPickerView`  
  > Размещён на сцене MainMenu (по умолчанию неактивен)
- [x] `MainMenuInstaller` — добавлен `[SerializeField] PartPickerView _pickerView` + `BindInstance`  
  > `_pickerView` привязан к `PartPickerPanel` на сцене

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
        var parts = LoadPartsForSlot(slot); // Resources.LoadAll<StaffPartConfig>
        _pickerView.Render(parts);
        _pickerView.Show();
    }

    void SelectPart(StaffPartConfig part)
    {
        _loadoutService.SetPart(_pendingSlot, part);
        _modView.RenderLoadout(_loadoutService.ActiveLoadout);
        _pickerView.Hide();
    }

    StaffPartConfig[] LoadPartsForSlot(StaffSlot slot) =>
        Resources.LoadAll<StaffPartConfig>("StaffParts")
                 .Where(p => p.slot == slot)
                 .ToArray();

    public void Dispose() { /* отписки */ }
}
```

- [x] Создать `StaffModificationController`
  > `Assets/Game/MainMenu/Scripts/Controllers/StaffModificationController.cs`  
  > `Initialize()`: подписки + `RenderLoadout` при старте  
  > `OpenPicker`: `LoadPartsForSlot` через `Resources.LoadAll` + фильтр по `slot`, `_pickerView.Render + Show`  
  > `SelectPart`: `SetPart` → `RenderLoadout` → `_pickerView.Hide()`  
  > `Dispose()`: отписки от `OnSlotClicked`, `OnPartSelected`
- [x] Забиндить в `MainMenuInstaller`
  > `Container.BindInterfacesAndSelfTo<StaffModificationController>().AsSingle().NonLazy();`

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
1. ✅ StaffPartConfig + StaffSlot enum + StaffLoadoutConfig        (данные)
2. ✅ StaffLoadoutService + ProjectInstaller bind                  (сервис)
2a.✅ 15 StaffPartConfig.asset (3×5 слотов) + DefaultLoadout.asset (ассеты)
3. ✅ MainMenu сцена + Canvas + Build Settings                     (сцена)
4. ✅ MainMenuView prefab + скрипт                                 (view)
5. ✅ ISceneLoader + SceneLoader + MainMenuController              (навигация)
6. ✅ StaffModificationView (полная) + StaffSlotView × 5 prefabs  (view)
7. ✅ PartPickerView prefab + PartPickerItemView                   (view)
8. ✅ StaffModificationController                                  (логика)
9. ✅ MainMenuController                                           (логика)
10. ✅ MainMenuInstaller — всё вместе                              (DI)
11.   Ручной тест: меню → посох → смена детали → в игру          (QA)
```

---

## КРИТЕРИИ ГОТОВНОСТИ

- [x] Кнопка `В ПОДЗЕМЕЛЬЕ` → загружает сцену `Gameplay`
- [x] Кнопка `ПОСОХ` → открывает `StaffModificationView` поверх меню
- [x] В окне посоха отображается текущий loadout (5 слотов с иконками)
- [x] Клик на слот → открывается `PartPickerView` с доступными деталями
- [x] Выбор детали → слот обновляется, выбор сохраняется через `StaffLoadoutService`
- [x] Кнопка закрытия → `StaffModificationView.Hide()`
- [x] Никаких `FindObjectOfType` — все зависимости через Zenject
- [x] Все View наследуют `DisplayableView`

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
        StaffPartConfig.cs
        StaffLoadoutConfig.cs
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
