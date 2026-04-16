# ПЛАН РАЗРАБОТКИ — Gameplay Core (Сцена + Персонаж + Движение)

> Документ: технический план фазы Gameplay Core  
> Основа: GDD_MagicStaff_Prototype v0.1 (§4, §8 Фаза 2, §9)  
> Дата: 2026-04-16  
> Предыдущий этап: PLAN_MainMenu_StaffModification.md (выполнен полностью)
>
> **Правило:** по мере выполнения каждого шага — меняй `[ ]` на `[x]` прямо в этом файле.

---

## ЦЕЛЬ

Реализовать минимально играбельный Gameplay-слой:
1. **Переход** из MainMenu на сцену Gameplay по кнопке `В ПОДЗЕМЕЛЬЕ`
2. **Арена** — плоский пол, точка спавна
3. **Персонаж** — 3D Capsule + посох из примитивов (5 частей по слотам)
4. **Движение** — top-down (вид сверху, чуть под углом), 360 градусов (свободное направление)
5. **UI управления** — виртуальный джойстик на экране

---

## АРХИТЕКТУРА (КРАТКО)

```
ProjectContext
  └── ProjectInstaller
        ├── StaffLoadoutService   (уже есть)
        └── (MetaProgressionService — позже)

Gameplay Scene
  └── GameplayInstaller : MonoInstaller
        ├── PlayerController
        └── MovementComponent

Views (наследуют DisplayableView)
  └── GameplayHudView
        └── VirtualJoystickView

GameObjects (сцена)
  ├── Floor (Plane/Quad)
  ├── SpawnPoint (Empty)
  ├── Player
  │     ├── Capsule (MeshRenderer)
  │     └── StaffVisual
  │           ├── BottomCap   (Cylinder/Sphere)
  │           ├── Shaft       (Cylinder)
  │           ├── Grip        (Cylinder, чуть толще)
  │           ├── TopCap      (Sphere/Cylinder)
  │           └── Artifact    (Sphere, светится)
  └── GameplayCamera (под углом ~55° по X)
```

---

## ЗАДАЧИ

### 1. Gameplay-сцена: базовая геометрия

Создать сцену, которая загружается по кнопке "В ПОДЗЕМЕЛЬЕ".

- [x] Создать сцену `Assets/Game/Scenes/Gameplay.unity`
- [x] Добавить в Build Settings (index 2, как указано в PLAN_MainMenu)
- [x] Добавить **DirectionalLight** (Rotation: 50°, -30°, 0°)
- [x] Создать **Floor** — `Plane` (Scale: 5, 1, 5 → 50×50 юнитов), материал серый, позиция (0,0,0)
- [x] Создать **SpawnPoint** — пустой GameObject, позиция (0, 0, 0), тег `SpawnPoint`
  > Тег нужен только для Editor-навигации, в коде — serialized reference, не Find

---

### 2. Камера — top-down с лёгким наклоном

Roguelike-вид: смотрит сверху с углом ~55°, следует за игроком.

- [x] Настроить **Main Camera**
  - Position: (0, 10, -7)
  - Rotation: (55, 0, 0)
  - Projection: Perspective, FOV 60
- [x] Создать скрипт `CameraFollow`
  > `Assets/Game/Gameplay/Scripts/Camera/CameraFollow.cs`
  ```csharp
  public class CameraFollow : MonoBehaviour
  {
      [SerializeField] Transform _target;
      [SerializeField] Vector3   _offset = new(0, 10, -7);

      void LateUpdate() => transform.position = _target.position + _offset;
  }
  ```
- [x] Повесить `CameraFollow` на Main Camera, `_target` → Player (через serialized ref на сцене)

---

### 3. Персонаж — Capsule + StaffVisual

Вся визуальная сборка — из примитивов Unity. Никаких ассетов.

#### 3.1 Capsule (тело персонажа)

- [x] Создать `Player` — пустой GameObject, позиция (0, 1, 0)
  - Добавить `Rigidbody` (Constraints: Freeze Rotation X/Z, Use Gravity = false)
  - Добавить `CapsuleCollider` (Height 2, Radius 0.5)
- [x] Дочерний объект `Body` — `Capsule` примитив (Scale: 1,1,1), позиция (0,0,0)
  - Удалить `CapsuleCollider` с дочернего (коллайдер только на родителе)
  - Материал: синий/фиолетовый (маг)

#### 3.2 StaffVisual (посох из примитивов)

Посох держится в правой руке. Родитель — `Player`, сдвинут вправо и вперёд.

- [x] Создать `StaffVisual` — пустой GameObject
  - Позиция относительно Player: (0.5, 0.8, 0.3)
  - Rotation: (10, 0, 0) — чуть наклонён вперёд

Структура StaffVisual (все дочерние):

| GameObject    | Примитив  | Local Position    | Local Scale       | Цвет материала |
|---------------|-----------|-------------------|-------------------|----------------|
| `BottomCap`   | Sphere    | (0, 0, 0)         | (0.15, 0.15, 0.15)| тёмно-серый    |
| `Shaft`       | Cylinder  | (0, 0.65, 0)      | (0.08, 0.6, 0.08) | коричневый     |
| `Grip`        | Cylinder  | (0, 1.1, 0)       | (0.12, 0.2, 0.12) | тёмно-коричневый|
| `TopCap`      | Cylinder  | (0, 1.45, 0)      | (0.1, 0.1, 0.1)   | золотой        |
| `Artifact`    | Sphere    | (0, 1.65, 0)      | (0.2, 0.2, 0.2)   | красный/светящийся|

- [x] Создать все 5 дочерних объектов с нужными материалами
- [x] Создать скрипт `StaffVisualBuilder`
  > `Assets/Game/Gameplay/Scripts/Player/StaffVisualBuilder.cs`  
  > Принимает `StaffLoadoutService` через `[Inject]`, по слотам меняет цвет материала Artifact/TopCap/Grip/Shaft/BottomCap в `Start()`
  ```csharp
  public class StaffVisualBuilder : MonoBehaviour
  {
      [SerializeField] Renderer _artifact;
      [SerializeField] Renderer _topCap;
      [SerializeField] Renderer _grip;
      [SerializeField] Renderer _shaft;
      [SerializeField] Renderer _bottomCap;

      [Inject] StaffLoadoutService _loadout;

      void Start() => ApplyLoadout();

      void ApplyLoadout()
      {
          // Каждый StaffPartConfig несёт цвет (или берём по имени слота дефолт)
          // Минимум: просто убеждаемся что сборка работает
      }
  }
  ```

---

### 4. Движение персонажа

Top-down movement без поворота камеры. Персонаж поворачивается в сторону движения.

#### 4.1 Данные

- [x] Создать `PlayerConfig : ScriptableObject`
  > `Assets/Game/Gameplay/Scripts/Player/PlayerConfig.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Config/PlayerConfig")]
  public class PlayerConfig : ScriptableObject
  {
      public float moveSpeed = 5f;
      public float rotationSpeed = 720f;
  }
  ```
- [x] Создать ассет `Assets/Game/Gameplay/Resources/PlayerConfig.asset`

#### 4.2 Компонент движения

- [x] Создать `MovementComponent : MonoBehaviour`
  > `Assets/Game/Gameplay/Scripts/Movement/MovementComponent.cs`
  ```csharp
  public class MovementComponent : MonoBehaviour
  {
      [Inject] PlayerConfig _config;

      Rigidbody _rb;
      Vector2   _inputDir;

      void Awake() => _rb = GetComponent<Rigidbody>();

      // Вызывается из VirtualJoystickView или любого другого источника ввода
      public void SetInput(Vector2 dir) => _inputDir = dir;

      void FixedUpdate()
      {
          var move = new Vector3(_inputDir.x, 0, _inputDir.y) * _config.moveSpeed;
          _rb.MovePosition(_rb.position + move * Time.fixedDeltaTime);

          if (move.sqrMagnitude > 0.01f)
          {
              var targetRot = Quaternion.LookRotation(move);
              _rb.rotation = Quaternion.RotateTowards(
                  _rb.rotation, targetRot, _config.rotationSpeed * Time.fixedDeltaTime);
          }
      }
  }
  ```

---

### 5. UI — виртуальный джойстик

Простой джойстик для мобильного управления. Никаких сторонних пакетов.

#### 5.1 VirtualJoystickView

- [x] Создать `VirtualJoystickView : DisplayableView`
  > `Assets/Game/Gameplay/Scripts/Views/VirtualJoystickView.cs`

  Логика:
  - `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler`
  - Фон джойстика + ручка (Handle)
  - Радиус = половина размера фона
  - Нормализованный вектор → `PlayerMovement.SetInput()`

  ```csharp
  public class VirtualJoystickView : DisplayableView,
      IPointerDownHandler, IDragHandler, IPointerUpHandler
  {
      public event Action<Vector2> OnInputChanged;

      [SerializeField] RectTransform _background;
      [SerializeField] RectTransform _handle;

      float  _radius;
      Vector2 _startPos;

      void Awake() => _radius = _background.sizeDelta.x * 0.5f;

      public void OnPointerDown(PointerEventData e)
      {
          _startPos = e.position;
          _background.position = e.position;
          _handle.anchoredPosition = Vector2.zero;
      }

      public void OnDrag(PointerEventData e)
      {
          var delta = e.position - _startPos;
          var clamped = Vector2.ClampMagnitude(delta, _radius);
          _handle.anchoredPosition = clamped;
          OnInputChanged?.Invoke(clamped / _radius);
      }

      public void OnPointerUp(PointerEventData e)
      {
          _handle.anchoredPosition = Vector2.zero;
          OnInputChanged?.Invoke(Vector2.zero);
      }
  }
  ```

#### 5.2 GameplayHudView

- [x] Создать `GameplayHudView : DisplayableView`
  > `Assets/Game/Gameplay/Scripts/Views/GameplayHudView.cs`
  ```csharp
  public class GameplayHudView : DisplayableView
  {
      public VirtualJoystickView Joystick => _joystick;

      [SerializeField] VirtualJoystickView _joystick;
  }
  ```

#### 5.3 Prefab GameplayHUD

- [x] Создать `Canvas` (Screen Space Overlay, ScaleWithScreenSize 1080×1920, Match 0.5)
- [x] Внутри — `JoystickArea` (нижний левый угол, якорь LowerLeft, ~250×250px)
  - Дочерний `Background` — Image (Circle/Square, полупрозрачный, 200×200)
  - Дочерний `Handle` — Image (Circle, непрозрачный, 80×80)
  - Компонент `VirtualJoystickView`: `_background` → Background, `_handle` → Handle
- [x] Повесить `GameplayHudView` на Canvas, `_joystick` → JoystickView
- [x] Сохранить как prefab `Assets/Game/Gameplay/Prefabs/GameplayHUD.prefab`

---

### 6. PlayerController — соединяет всё

- [x] Создать `PlayerController : IInitializable, IDisposable`
  > `Assets/Game/Gameplay/Scripts/Player/PlayerController.cs`
  ```csharp
  public class PlayerController : IInitializable, IDisposable
  {
      readonly MovementComponent _movement;
      readonly GameplayHudView   _hud;

      [Inject]
      public PlayerController(MovementComponent movement, GameplayHudView hud)
      {
          _movement = movement;
          _hud      = hud;
      }

      public void Initialize()
          => _hud.Joystick.OnInputChanged += _movement.SetInput;

      public void Dispose()
          => _hud.Joystick.OnInputChanged -= _movement.SetInput;
  }
  ```

---

### 7. GameplayInstaller

- [x] Создать `GameplayInstaller : MonoInstaller`
  > `Assets/Game/Gameplay/Installers/GameplayInstaller.cs`
  ```csharp
  public class GameplayInstaller : MonoInstaller
  {
      [SerializeField] MovementComponent _movement;
      [SerializeField] GameplayHudView   _hudView;
      [SerializeField] PlayerConfig      _playerConfig;

      public override void InstallBindings()
      {
          Container.BindInstance(_playerConfig);
          Container.BindInstance(_movement);
          Container.BindInstance(_hudView);

          Container.BindInterfacesAndSelfTo<PlayerController>().AsSingle().NonLazy();
      }
  }
  ```
- [x] Добавить `SceneContext` на сцену Gameplay, привязать `GameplayInstaller`
- [x] Назначить все serialized references в Inspector

---

### 8. Сборка и проверка

- [x] Сцена Gameplay открывается из MainMenu по кнопке "В ПОДЗЕМЕЛЬЕ"
- [x] Игрок появляется в точке (0,1,0)
- [x] Джойстик двигает персонажа во все стороны
- [x] Персонаж поворачивается лицом в направлении движения
- [x] Посох визуально прикреплён к правой руке
- [x] Камера следует за персонажем
- [x] Нет ошибок в консоли при переходе сцен

---

## ПОРЯДОК ВЫПОЛНЕНИЯ

```
1. [x] Gameplay-сцена: Floor + SpawnPoint + Light + Build Settings
2. [x] Камера: позиция + CameraFollow скрипт
3. [x] Player GameObject: Capsule + Rigidbody + Collider
4. [x] StaffVisual: 5 примитивов с материалами
5. [x] PlayerConfig.asset + MovementComponent скрипт
6. [x] VirtualJoystickView + GameplayHudView + префаб GameplayHUD
7. [x] PlayerController
8. [x] GameplayInstaller + SceneContext
9. [ ] StaffVisualBuilder (инжект StaffLoadoutService)
10. [x] Сквозной тест: MainMenu → Gameplay → движение
```

---

## ФАЙЛОВАЯ СТРУКТУРА

```
Assets/
  Game/
    Gameplay/
      Scripts/
        Camera/
          CameraFollow.cs
        Movement/
          MovementComponent.cs
        Player/
          PlayerConfig.cs
          PlayerController.cs
          StaffVisualBuilder.cs
        Views/
          GameplayHudView.cs
          VirtualJoystickView.cs
      Installers/
        GameplayInstaller.cs
      Prefabs/
        GameplayHUD.prefab
      Resources/
        PlayerConfig.asset
    Scenes/
      Gameplay.unity        ← новая
      MainMenu.unity        ← уже есть
```

---

## КРИТЕРИИ ГОТОВНОСТИ

- [ ] Нажатие "В ПОДЗЕМЕЛЬЕ" → загрузка Gameplay без ошибок
- [ ] Персонаж стоит на полу, не проваливается
- [ ] Джойстик управляет движением (360 градусов)
- [ ] Персонаж поворачивается в сторону движения
- [ ] Камера следует, не дёргается
- [ ] StaffVisual виден, 5 частей различимы
- [ ] Никаких `FindObjectOfType` — всё через Zenject

---

*Следующий этап: PLAN_Gameplay_Combat.md (автоатака, снаряды, враги)*
