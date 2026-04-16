# ПЛАН РАЗРАБОТКИ — Floating Staff + Auto-Attack

> Дата: 2026-04-16  
> Предыдущий этап: PLAN_EnemySpawning.md (выполнен)  
>
> **Правило:** по мере выполнения каждого шага — меняй `[ ]` на `[x]` прямо в этом файле.

---

## ЦЕЛЬ

1. **Посох парит** рядом с игроком — не прикреплён жёстко, а слегка покачивается вверх-вниз (bob)  
2. **Следует за игроком** плавно (smooth-follow с offset)  
3. **Автоматически стреляет** в ближайшего врага в радиусе детекции  

---

## АРХИТЕКТУРА

```
StaffFloatingBehaviour   — bob + smooth follow за Player
StaffCombat              — детекция врагов + таймер выстрела + спавн снаряда
ProjectileController     — движение снаряда + урон при столкновении
ProjectilePool           — пул снарядов (переиспользование)
StaffCombatConfig        — данные: радиус, урон, скорострельность, скорость снаряда
```

Зависимости через Zenject: `StaffCombat` получает `Transform` игрока через инжект.  
`StaffFloatingBehaviour` и `StaffCombat` — `MonoBehaviour` на объекте `StaffVisual`.  
Инжект — только через `[Inject] public void Construct(...)`, не через поля.

---

## ЗАДАЧИ

### 1. StaffCombatConfig — данные боя

- [x] Создать `StaffCombatConfig : ScriptableObject`  
  > `Assets/Game/Gameplay/Scripts/Staff/StaffCombatConfig.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Config/StaffCombatConfig")]
  public class StaffCombatConfig : ScriptableObject
  {
      public float detectionRadius   = 8f;
      public float fireRate          = 1f;    // выстрелов в секунду
      public float projectileSpeed   = 12f;
      public float projectileDamage  = 1f;
      public float bobAmplitude      = 0.15f; // амплитуда покачивания (юниты)
      public float bobFrequency      = 1.5f;  // Гц
      public float followSmoothTime  = 0.12f; // время сглаживания позиции
      public Vector3 staffOffset     = new(1.2f, 1.0f, 0f); // offset от Player
  }
  ```
- [x] Создать ассет `Assets/Game/Gameplay/Resources/StaffCombatConfig.asset`

---

### 2. StaffFloatingBehaviour — bob + smooth-follow

Посох **не является дочерним** Player. Он — самостоятельный GameObject в сцене, который программно следует за игроком.

- [ ] Отвязать `StaffVisual` от Player (сделать root-level объектом сцены)
- [ ] Создать `StaffFloatingBehaviour : MonoBehaviour`  
  > `Assets/Game/Gameplay/Scripts/Staff/StaffFloatingBehaviour.cs`
  ```csharp
  public class StaffFloatingBehaviour : MonoBehaviour
  {
      StaffCombatConfig _config;
      Transform         _playerTransform;

      Vector3 _velocity;

      [Inject]
      public void Construct(StaffCombatConfig config, [Inject(Id = "PlayerTransform")] Transform playerTransform)
      {
          _config          = config;
          _playerTransform = playerTransform;
      }

      void Update()
      {
          var targetPos = _playerTransform.position
                        + _playerTransform.TransformDirection(_config.staffOffset)
                        + Vector3.up * Mathf.Sin(Time.time * _config.bobFrequency * Mathf.PI * 2f)
                                     * _config.bobAmplitude;

          transform.position = Vector3.SmoothDamp(
              transform.position, targetPos, ref _velocity, _config.followSmoothTime);
      }
  }
  ```
  > `_playerTransform` — не Player напрямую, а его `Transform`, забинденный как именованный токен  
  > Bob добавляется к `targetPos` через `Mathf.Sin` — не к локальной позиции, а к мировой

---

### 3. ProjectileController — снаряд

- [x] Создать `ProjectileController : MonoBehaviour`  
  > `Assets/Game/Gameplay/Scripts/Staff/ProjectileController.cs`
  ```csharp
  public class ProjectileController : MonoBehaviour
  {
      float   _speed;
      float   _damage;
      Vector3 _direction;

      public void Launch(Vector3 direction, float speed, float damage)
      {
          _direction = direction.normalized;
          _speed     = speed;
          _damage    = damage;
      }

      void Update() => transform.position += _direction * _speed * Time.deltaTime;

      void OnTriggerEnter(Collider other)
      {
          if (!other.TryGetComponent<HealthComponent>(out var hp)) return;
          hp.TakeDamage(_damage);
          gameObject.SetActive(false);
      }
  }
  ```
- [x] Создать prefab `Assets/Game/Gameplay/Prefabs/Projectile.prefab`
  - `SphereCollider` (IsTrigger = true, radius 0.15)
  - `MeshRenderer` (Sphere, светящийся материал — маджента/голубой)
  - Компонент `ProjectileController`
  - Слой: `Projectile` (не попадает под `Physics.OverlapSphere` детекции врагов)

---

### 4. ProjectilePool — пул снарядов

- [x] Создать `ProjectilePool : MonoBehaviour`  
  > `Assets/Game/Gameplay/Scripts/Staff/ProjectilePool.cs`
  ```csharp
  public class ProjectilePool : MonoBehaviour
  {
      [SerializeField] ProjectileController _prefab;
      [SerializeField] int                  _initialSize = 20;

      readonly Queue<ProjectileController> _pool = new();

      void Awake()
      {
          for (var i = 0; i < _initialSize; i++)
              CreateInstance();
      }

      public ProjectileController Get()
      {
          var p = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
          p.gameObject.SetActive(true);
          return p;
      }

      public void Return(ProjectileController p)
      {
          p.gameObject.SetActive(false);
          _pool.Enqueue(p);
      }

      ProjectileController CreateInstance()
      {
          var p = Instantiate(_prefab, transform);
          p.gameObject.SetActive(false);
          return p;
      }
  }
  ```

  > `ProjectileController` сам вызывает `Return` через пул — нужна ссылка на пул  
  > Добавить `public void SetPool(ProjectilePool pool)` в `ProjectileController`,  
  > вызвать `_pool.Return(this)` вместо `SetActive(false)` в `OnTriggerEnter`  
  > Также добавить авто-деактивацию по дальности (чтобы снаряды не летели вечно)

---

### 5. StaffCombat — детекция + стрельба

- [ ] Создать `StaffCombat : MonoBehaviour`  
  > `Assets/Game/Gameplay/Scripts/Staff/StaffCombat.cs`
  ```csharp
  public class StaffCombat : MonoBehaviour
  {
      StaffCombatConfig _config;
      ProjectilePool    _projectilePool;

      float _fireTimer;

      [Inject]
      public void Construct(StaffCombatConfig config, ProjectilePool projectilePool)
      {
          _config          = config;
          _projectilePool  = projectilePool;
      }

      void Update()
      {
          _fireTimer += Time.deltaTime;
          if (_fireTimer < 1f / _config.fireRate) return;

          var target = FindClosestEnemy();
          if (target == null) return;

          _fireTimer = 0f;
          Shoot(target);
      }

      Transform FindClosestEnemy()
      {
          var hits = Physics.OverlapSphere(transform.position, _config.detectionRadius, LayerMask.GetMask("Enemy"));
          Transform closest  = null;
          var       minDist  = float.MaxValue;

          foreach (var hit in hits)
          {
              var d = (hit.transform.position - transform.position).sqrMagnitude;
              if (d < minDist) { minDist = d; closest = hit.transform; }
          }
          return closest;
      }

      void Shoot(Transform target)
      {
          var dir       = (target.position - transform.position).normalized;
          var projectile = _projectilePool.Get();
          projectile.transform.position = transform.position + Vector3.up * 0.5f; // вылетает из верхушки посоха
          projectile.Launch(dir, _config.projectileSpeed, _config.projectileDamage);
      }

      void OnDrawGizmosSelected()
      {
          Gizmos.color = Color.cyan;
          Gizmos.DrawWireSphere(transform.position, _config.detectionRadius);
      }
  }
  ```

---

### 6. HealthComponent — добавить TakeDamage

Проверить, есть ли `TakeDamage(float)` в существующем `HealthComponent`.

- [x] Открыть `Assets/Game/Gameplay/Scripts/Enemy/HealthComponent.cs`
- [x] Добавить `public void TakeDamage(float amount)` если отсутствует

---

### 7. Настройка слоёв (Layers)

- [x] Добавить слой `Enemy` в Project Settings → Tags & Layers (если не добавлен)
- [x] Добавить слой `Projectile`
- [x] Назначить слой `Enemy` на Enemy prefab
- [x] Назначить слой `Projectile` на Projectile prefab
- [x] Physics Matrix: `Projectile` ↔ `Enemy` = ON, `Projectile` ↔ `Player` = OFF, `Projectile` ↔ `Projectile` = OFF

---

### 8. GameplayInstaller — регистрация новых зависимостей

- [x] Добавить в `GameplayInstaller`:
  ```csharp
  [SerializeField] StaffFloatingBehaviour _staffFloating;
  [SerializeField] StaffCombat            _staffCombat;
  [SerializeField] ProjectilePool         _projectilePool;
  [SerializeField] StaffCombatConfig      _staffCombatConfig;

  // в InstallBindings():
  Container.BindInstance(_staffCombatConfig);
  Container.BindInstance(_projectilePool).AsSingle();
  Container.BindInstance(_movement.transform).WithId("PlayerTransform");
  Container.QueueForInject(_staffFloating);
  Container.QueueForInject(_staffCombat);
  ```

---

### 9. Сборка сцены

- [x] Убрать `StaffVisual` из иерархии Player (перетащить в root сцены)
- [x] Добавить `StaffFloatingBehaviour` на `StaffVisual`
- [x] Добавить `StaffCombat` на `StaffVisual`
- [x] Создать `ProjectilePool` — пустой GameObject `ProjectilePool` в сцене
- [x] Назначить все serialized references в `GameplayInstaller`
- [x] Проверить слои на Enemy prefab и Projectile prefab

---

### 10. Проверка

- [x] Посох парит рядом с игроком (не прикреплён жёстко)
- [x] Посох плавно следует при движении (smooth-follow, нет рывков)
- [x] Посох покачивается вверх-вниз без остановки
- [x] Враг в радиусе → посох стреляет с заданной частотой
- [x] Снаряд летит в цель, наносит урон, враг умирает
- [x] Снаряд возвращается в пул (не остаётся в сцене)
- [x] Нет ошибок в консоли
- [x] Gizmo детекции виден в Scene View при выделении `StaffVisual`

---

## ПОРЯДОК ВЫПОЛНЕНИЯ

```
1. [x] StaffCombatConfig.cs + .asset
2. [x] HealthComponent — добавить TakeDamage
3. [x] Layers: Enemy + Projectile, Physics Matrix
4. [x] ProjectileController.cs + Prefab
5. [x] ProjectilePool.cs
6. [x] StaffFloatingBehaviour.cs
7. [x] StaffCombat.cs
8. [x] GameplayInstaller — расширить
9. [x] Сцена: отвязать StaffVisual, назначить компоненты
10. [x] Сквозной тест
```

---

## ФАЙЛОВАЯ СТРУКТУРА

```
Assets/
  Game/
    Gameplay/
      Scripts/
        Staff/
          StaffCombatConfig.cs
          StaffFloatingBehaviour.cs
          StaffCombat.cs
          ProjectileController.cs
          ProjectilePool.cs
      Prefabs/
        Projectile.prefab
      Resources/
        StaffCombatConfig.asset
```

---

## КРИТЕРИИ ГОТОВНОСТИ

- [x] `StaffVisual` — root-level GameObject, не child Player
- [x] Покачивание через `Mathf.Sin` в `Update`, не анимация
- [x] Smooth-follow через `Vector3.SmoothDamp`
- [x] Детекция через `Physics.OverlapSphere` по слою `Enemy`
- [x] Снаряды из пула, возврат в пул после попадания или выхода за дальность
- [x] Никаких `FindObjectOfType` — всё через Zenject
- [x] Нет hardcoded магических чисел — всё в `StaffCombatConfig`

---

*Следующий этап: PLAN_Gameplay_Progression.md (опыт, уровни, апгрейды посоха)*
