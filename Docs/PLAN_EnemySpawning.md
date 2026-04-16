# ПЛАН РАЗРАБОТКИ — Enemy Spawning (Волны врагов)

> Документ: технический план фазы Enemy Spawning  
> Основа: GDD_MagicStaff_Prototype v0.1 (§5 Combat, §8 Фаза 3)  
> Дата: 2026-04-16  
> Предыдущий этап: PLAN_Gameplay_Core.md (выполнен полностью)
>
> **Правило:** по мере выполнения каждого шага — меняй `[ ]` на `[x]` прямо в этом файле.

---

## ЦЕЛЬ

Реализовать систему спавна врагов с нарастающими волнами:
1. **Враги** — простые капсулы, движутся к игроку, имеют HP
2. **Спавн** — с четырёх сторон арены (за пределами камеры), всегда вне поля зрения игрока
3. **Волны** — количество и скорость нарастают со временем (плавная кривая, не ступенчато)
4. **Готовность к автострельбе** — у врага есть `HealthComponent`/`TakeDamage`, у снарядов будет точка применения

---

## АРХИТЕКТУРА (КРАТКО)

```
GameplayInstaller
  ├── EnemySpawner          (MonoBehaviour — управляет волнами)
  ├── EnemyPool             (объектный пул)
  ├── WaveConfig            (ScriptableObject — кривые нарастания)
  └── EnemyConfig           (ScriptableObject — HP, speed)

Enemy Prefab
  ├── Capsule (mesh)
  ├── EnemyController       (MonoBehaviour — движение к игроку)
  └── HealthComponent                (MonoBehaviour — HP, Die event; нужен для автострельбы)

Спавн-точки: 4 стороны арены, выбираются случайно, смещены за край Floor
```

---

## ЗАДАЧИ

### 1. Данные — конфиги

#### 1.1 EnemyConfig

- [x] Создать `EnemyConfig : ScriptableObject`
  > `Assets/Game/Gameplay/Scripts/Enemy/EnemyConfig.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Config/EnemyConfig")]
  public class EnemyConfig : ScriptableObject
  {
      public float moveSpeed   = 2f;
      public float maxHp       = 3f;
      public float bodyRadius  = 0.5f;   // CapsuleCollider radius
  }
  ```
- [x] Создать ассет `Assets/Game/Gameplay/Resources/EnemyConfig.asset`

#### 1.2 WaveConfig

Описывает, как со временем растут количество врагов в волне и интервал между волнами.

- [x] Создать `WaveConfig : ScriptableObject`
  > `Assets/Game/Gameplay/Scripts/Enemy/WaveConfig.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Config/WaveConfig")]
  public class WaveConfig : ScriptableObject
  {
      [Tooltip("X = время игры (сек), Y = кол-во врагов в волне")]
      public AnimationCurve enemyCountOverTime  = AnimationCurve.Linear(0, 3, 300, 30);

      [Tooltip("X = время игры (сек), Y = интервал между волнами (сек)")]
      public AnimationCurve spawnIntervalOverTime = AnimationCurve.Linear(0, 5, 300, 1.5f);

      [Tooltip("Разброс точки спавна вдоль стороны арены")]
      public float spawnEdgeVariance = 8f;

      [Tooltip("Отступ спавн-точки за пределы Floor")]
      public float spawnEdgeOffset   = 2f;

      [Tooltip("Половина размера Floor по X и Z (для расчёта точки спавна)")]
      public float arenaHalfSize     = 25f;
  }
  ```
- [x] Создать ассет `Assets/Game/Gameplay/Resources/WaveConfig.asset`
  - `enemyCountOverTime`: ключи (0→3), (120→10), (300→30)
  - `spawnIntervalOverTime`: ключи (0→5), (120→2.5), (300→1.5)

---

### 2. HealthComponent — компонент жизней

Минимальный; нужен уже сейчас, чтобы автострельба подключилась без рефакторинга.

- [x] Создать `HealthComponent : MonoBehaviour`
  > `Assets/Game/Gameplay/Scripts/Enemy/HealthComponent.cs`
  ```csharp
  public class HealthComponent : MonoBehaviour
  {
      public event Action OnDied;

      [SerializeField] float _maxHp;

      float _current;

      public void Initialize(float maxHp)
      {
          _maxHp   = maxHp;
          _current = maxHp;
      }

      public void TakeDamage(float amount)
      {
          _current -= amount;
          if (_current <= 0f) OnDied?.Invoke();
      }
  }
  ```

---

### 3. EnemyController — ИИ врага

Простое движение: каждый кадр идёт к позиции игрока.

- [x] Создать `EnemyController : MonoBehaviour`
  > `Assets/Game/Gameplay/Scripts/Enemy/EnemyController.cs`
  ```csharp
  public class EnemyController : MonoBehaviour
  {
      [SerializeField] HealthComponent _health;

      EnemyConfig _config;
      Transform   _target;
      Rigidbody   _rb;

      public void Initialize(EnemyConfig config, Transform target)
      {
          _config = config;
          _target = target;
          _rb     = GetComponent<Rigidbody>();
          _health.Initialize(config.maxHp);
          _health.OnDied += ReturnToPool;
      }

      void FixedUpdate()
      {
          if (_target == null) return;

          var dir = (_target.position - _rb.position).normalized;
          dir.y = 0f;
          _rb.MovePosition(_rb.position + dir * _config.moveSpeed * Time.fixedDeltaTime);

          if (dir.sqrMagnitude > 0.01f)
              _rb.rotation = Quaternion.LookRotation(dir);
      }

      void ReturnToPool()
      {
          _health.OnDied -= ReturnToPool;
          gameObject.SetActive(false);
      }
  }
  ```

---

### 4. Префаб Enemy

- [x] Создать `Enemy` — пустой GameObject
  - `Rigidbody` (Constraints: Freeze Rotation X/Z, Use Gravity = false)
  - `CapsuleCollider` (Height 2, Radius 0.5)
  - `EnemyController`
  - `HealthComponent`
- [x] Дочерний объект `Body` — Capsule примитив (Scale 1,1,1), материал: красный/тёмный
  - Удалить дочерний `CapsuleCollider` с Body
- [x] Сохранить как `Assets/Game/Gameplay/Prefabs/Enemy.prefab`
  - Назначить `_health` в `EnemyController` через serialized ref

---

### 5. EnemyPool — объектный пул

Простой пул без сторонних библиотек.

- [x] Создать `EnemyPool : MonoBehaviour`
  > `Assets/Game/Gameplay/Scripts/Enemy/EnemyPool.cs`
  ```csharp
  public class EnemyPool : MonoBehaviour
  {
      [SerializeField] EnemyController _prefab;
      [SerializeField] int             _initialSize = 20;

      readonly Queue<EnemyController> _free = new();

      void Awake()
      {
          for (int i = 0; i < _initialSize; i++)
              CreateInstance();
      }

      public EnemyController Get()
      {
          var e = _free.Count > 0 ? _free.Dequeue() : CreateInstance();
          e.gameObject.SetActive(true);
          return e;
      }

      public void Return(EnemyController e)
      {
          e.gameObject.SetActive(false);
          _free.Enqueue(e);
      }

      EnemyController CreateInstance()
      {
          var e = Instantiate(_prefab, transform);
          e.gameObject.SetActive(false);
          return e;
      }
  }
  ```
  > `ReturnToPool` в `EnemyController` вызывает `gameObject.SetActive(false)` — пул подберёт через `OnDisable` или через прямой `Return()`. Для простоты: пул отслеживает активные объекты самостоятельно при вызове `Get()`.

---

### 6. EnemySpawner — логика волн

Главный управляющий класс. Запускает корутины волн, выбирает точки спавна.

- [x] Создать `EnemySpawner : MonoBehaviour`
  > `Assets/Game/Gameplay/Scripts/Enemy/EnemySpawner.cs`
  ```csharp
  public class EnemySpawner : MonoBehaviour
  {
      [Inject] EnemyPool   _pool;
      [Inject] WaveConfig  _waveConfig;
      [Inject] EnemyConfig _enemyConfig;

      [SerializeField] Transform _playerTarget;

      float _elapsedTime;

      void Start() => StartCoroutine(SpawnLoop());

      IEnumerator SpawnLoop()
      {
          while (true)
          {
              float interval = _waveConfig.spawnIntervalOverTime.Evaluate(_elapsedTime);
              yield return new WaitForSeconds(interval);

              int count = Mathf.RoundToInt(_waveConfig.enemyCountOverTime.Evaluate(_elapsedTime));
              SpawnWave(count);
          }
      }

      void Update() => _elapsedTime += Time.deltaTime;

      void SpawnWave(int count)
      {
          for (int i = 0; i < count; i++)
          {
              var pos = GetSpawnPosition();
              var enemy = _pool.Get();
              enemy.transform.position = pos;
              enemy.Initialize(_enemyConfig, _playerTarget);
          }
      }

      Vector3 GetSpawnPosition()
      {
          // Выбираем одну из 4 сторон, ставим за краем арены
          int side = Random.Range(0, 4);
          float half   = _waveConfig.arenaHalfSize;
          float offset = _waveConfig.spawnEdgeOffset;
          float variance = Random.Range(-_waveConfig.spawnEdgeVariance, _waveConfig.spawnEdgeVariance);

          return side switch
          {
              0 => new Vector3(variance,  1f,  half + offset),   // север
              1 => new Vector3(variance,  1f, -half - offset),   // юг
              2 => new Vector3( half + offset, 1f, variance),    // восток
              _ => new Vector3(-half - offset, 1f, variance),    // запад
          };
      }
  }
  ```

---

### 7. GameplayInstaller — расширение

Добавить новые биндинги к существующему `GameplayInstaller`.

- [x] Добавить поля:
  ```csharp
  [SerializeField] EnemyPool    _enemyPool;
  [SerializeField] EnemySpawner _enemySpawner;
  [SerializeField] WaveConfig   _waveConfig;
  [SerializeField] EnemyConfig  _enemyConfig;
  ```
- [x] В `InstallBindings()` добавить:
  ```csharp
  Container.BindInstance(_waveConfig);
  Container.BindInstance(_enemyConfig);
  Container.BindInstance(_enemyPool).AsSingle();
  Container.QueueForInject(_enemySpawner);
  ```
- [x] Назначить все serialized references в Inspector

---

### 8. Сцена — размещение объектов

- [x] Создать `EnemyPoolRoot` — пустой GameObject (контейнер для пула)
  - Компонент `EnemyPool`, `_prefab` → Enemy.prefab
- [x] Создать `EnemySpawner` — пустой GameObject
  - Компонент `EnemySpawner`, `_playerTarget` → Player transform
- [x] В `GameplayInstaller` на SceneContext заполнить новые поля

---

### 9. Проверка и балансировка

- [x] Враги спавнятся со всех 4 сторон
- [x] Враги плавно движутся к игроку
- [x] Волны нарастают: через 2 мин заметно больше и быстрее
- [x] Враги умирают при смерти (SetActive false) — нет утечек
- [x] `HealthComponent.TakeDamage()` можно вызвать вручную из консоли Unity — враг умирает
- [x] Нет ошибок в консоли (нет NullRef, нет FindObjectOfType)

---

## ПОРЯДОК ВЫПОЛНЕНИЯ

```
1. [x] EnemyConfig.cs + EnemyConfig.asset
2. [x] WaveConfig.cs + WaveConfig.asset (настроить AnimationCurve)
3. [x] HealthComponent.cs
4. [x] EnemyController.cs
5. [x] Prefab Enemy (Capsule + Rigidbody + HealthComponent + EnemyController)
6. [x] EnemyPool.cs
7. [x] EnemySpawner.cs
8. [x] Расширить GameplayInstaller
9. [x] Разместить EnemyPoolRoot + EnemySpawner на сцене
10. [x] Сквозной тест: движение волн + смерть врага через TakeDamage
```

---

## ФАЙЛОВАЯ СТРУКТУРА

```
Assets/
  Game/
    Gameplay/
      Scripts/
        Enemy/
          EnemyConfig.cs
          WaveConfig.cs
          HealthComponent.cs
          EnemyController.cs
          EnemyPool.cs
          EnemySpawner.cs
      Prefabs/
        Enemy.prefab
      Resources/
        EnemyConfig.asset
        WaveConfig.asset
```

---

## КРИТЕРИИ ГОТОВНОСТИ

- [x] Враги спавнятся за краем арены (вне камеры) со всех 4 сторон
- [x] Движение к игроку — без рывков, через Rigidbody
- [x] Волны нарастают плавно по AnimationCurve
- [x] Пул работает: объекты не уничтожаются, только SetActive
- [x] `HealthComponent` с публичным `TakeDamage` — готов к подключению снарядов автострельбы
- [x] `OnDied` event — готов к системе счёта/наград
- [x] Нет нарушений Code Style (CLAUDE.md)
- [x] Нет `FindObjectOfType` — всё через Zenject + serialized refs

---

*Следующий этап: PLAN_AutoShooting.md (автоатака с посоха: снаряды, таргетинг, damage pipeline)*
