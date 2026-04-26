# Enemy System Architecture

## Концепция

Система построена на **компонентной архитектуре** — каждая механика врага реализована как
отдельный `MonoBehaviour`-компонент (`IEnemyBehaviour`), который добавляется на префаб.
`EnemyController` — тонкий оркестратор; он не знает о конкретных механиках,
только запускает и останавливает поведения.

```
Prefab: EnemyShooter
 ├── EnemyController        ← оркестратор, смерть, XP
 ├── HealthComponent        ← здоровье, OnDied event
 ├── ChaseMovementBehaviour ← движение к цели
 └── RangedAttackBehaviour  ← остановиться и стрелять
```

---

## Интерфейс IEnemyBehaviour

```csharp
public interface IEnemyBehaviour
{
    void Initialize(EnemyBehaviourContext ctx);
    void OnActivated();    // вызывается при инициализации из пула
    void OnDeactivated();  // вызывается перед возвратом в пул
}
```

Три метода покрывают полный жизненный цикл в пуле объектов:
- `Initialize` — получает зависимости через `EnemyBehaviourContext`
- `OnActivated` — старт таймеров, подписка на события
- `OnDeactivated` — отписка, сброс состояния

### EnemyBehaviourBase

Абстрактный базовый класс, хранит `Ctx` и даёт виртуальные методы с пустой реализацией.
Конкретные поведения наследуют от него и переопределяют только нужное.

---

## EnemyBehaviourContext

```csharp
public struct EnemyBehaviourContext
{
    public EnemyConfig          Config;
    public Transform            Target;          // трансформ игрока
    public Rigidbody            Rb;
    public EnemyPool            OwnerPool;
    public IPlayerHealthService PlayerHealth;    // для нанесения урона игроку
    public EnemyProjectilePool  ProjectilePool;  // null для немелее-врагов
}
```

Передаётся `struct`-ом (не классом) — нет аллокаций в heap при передаче. Поведения
получают контекст через `EnemyController.Initialize`, который сам получает зависимости
от `EnemySpawner`.

---

## EnemyController — оркестратор (GRASP: Controller)

```
Initialize(config, target, pool, playerHealth, projectilePool?)
  → health.Initialize
  → foreach behaviours: Initialize(ctx), OnActivated()
  → ApplyColor()

HandleDied (private)
  → foreach behaviours: OnDeactivated()
  → OnKilled?.Invoke(xp)        ← ExperienceService подписан
  → pool.Return(this)
```

`EnemyController` не содержит логики движения, атаки или взрыва.
Он только управляет lifecycle.

---

## Каталог поведений

| Класс | Конфиг | Описание |
|-------|--------|----------|
| `ChaseMovementBehaviour` | `EnemyConfig.moveSpeed` | Преследует игрока через Rigidbody.MovePosition. Поддерживает `PauseChase()` / `ResumeChase()` для совместной работы с атакующими поведениями |
| `RangedAttackBehaviour` | `RangedAttackConfig` | Конечный автомат: Inactive → Aiming → Firing. При входе в `attackRange` останавливает Chase, прицеливается `aimDuration` секунд, затем стреляет снарядами через `EnemyProjectilePool` |
| `ProximityExplosionBehaviour` | `ExplosionConfig` | При сближении на `triggerRadius` запускает отсчёт `countdownDuration`. По окончании — AoE урон игроку (если в `blastRadius`), затем самоуничтожение через `HealthComponent.TakeDamage(∞)` |

---

## Конфиги ScriptableObject

Каждый тип поведения со своими числами имеет собственный SO-конфиг.
Конфиг лежит `[SerializeField]`-полем на компоненте поведения (не в `EnemyConfig`).

```
EnemyConfig          — базовые параметры: moveSpeed, maxHp, xpReward, color
RangedAttackConfig   — attackRange, aimDuration, fireInterval, projectileSpeed, projectileDamage
ExplosionConfig      — triggerRadius, blastRadius, damage, countdownDuration
```

**Как добавить новое поведение:**
1. Создать `XxxConfig : ScriptableObject` (если нужны числа)
2. Создать `XxxBehaviour : EnemyBehaviourBase`
3. Добавить компонент на нужный префаб
4. Привязать конфиг в Inspector

Не нужно трогать `EnemyController`, `EnemySpawner`, `GameplayInstaller`.

---

## Пулы

```
EnemyPool          — один экземпляр на тип врага (5 шт. в сцене)
EnemyProjectilePool — один общий пул снарядов для всех стреляющих врагов
ProjectilePool      — пул снарядов игрока (без изменений)
```

`EnemyPool` принимает любой `EnemyController`-префаб → типизация через префаб, не через код.

`EnemyProjectilePool` получает `IPlayerHealthService` через Zenject injection и
передаёт его в `EnemyProjectileController.SetContext()` при создании экземпляра.
Это означает, что пулу не нужно ничего знать о конкретных поведениях врагов.

---

## Спавнер — мультитипная поддержка

`EnemySpawner` несёт `[SerializeField] EnemySpawnEntry[] _enemies` — массив записей вида:

```csharp
[Serializable]
public struct EnemySpawnEntry
{
    public EnemyPool   Pool;
    public EnemyConfig Config;
    [Range(0f, 1f)]
    public float       Weight;
}
```

Тайминг волн определяется `WaveConfig` (AnimationCurve). Тип врага — взвешенным
случайным выбором из массива. Это позволяет настраивать баланс без изменения кода.

---

## SOLID

| Принцип | Применение |
|---------|-----------|
| **SRP** | `ChaseMovementBehaviour` — только движение. `HealthComponent` — только здоровье. `EnemyController` — только lifecycle |
| **OCP** | Новый тип врага = новый `EnemyBehaviourBase`-наследник + новый префаб. Существующий код не меняется |
| **LSP** | Любой `EnemyBehaviourBase` можно подставить вместо `IEnemyBehaviour` — контракт соблюдён |
| **ISP** | `IEnemyBehaviour` минимален (3 метода). Конкретные поведения добавляют публичные методы только при необходимости межкомпонентного взаимодействия (`PauseChase`) |
| **DIP** | `EnemyController` зависит от `IEnemyBehaviour[]`, не от конкретных классов. `EnemySpawner` зависит от `IPlayerHealthService`, не от `PlayerHealthService` |

---

## GRASP

| Паттерн | Применение |
|---------|-----------|
| **Controller** | `EnemyController` — GRASP Controller для событий жизненного цикла врага (смерть, инициализация) |
| **Creator** | `EnemyPool` создаёт `EnemyController`. `EnemyProjectilePool` создаёт `EnemyProjectileController`. Создатель агрегирует/использует созданный объект |
| **Information Expert** | `HealthComponent` знает о текущем HP (только он). `RangedAttackBehaviour` знает о дальности атаки. `EnemySpawner` знает веса и тайминг |
| **High Cohesion** | Каждый компонент отвечает за строго одну задачу. `ProximityExplosionBehaviour` — только взрыв, ничего кроме |
| **Low Coupling** | Поведения общаются через `EnemyBehaviourContext` (struct-интерфейс), не через прямые ссылки друг на друга. Исключение: `RangedAttackBehaviour` → `ChaseMovementBehaviour` (на том же GameObject — это допустимо) |

---

## Добавление нового типа врага — чеклист

1. [ ] Создать `XxxConfig.asset` (если нужны параметры поведения)
2. [ ] Создать класс `XxxBehaviour : EnemyBehaviourBase`
3. [ ] Создать префаб (сфера + нужный цвет)
4. [ ] Добавить на префаб: `EnemyController`, `HealthComponent`, `ChaseMovementBehaviour`, `XxxBehaviour`
5. [ ] Прикрепить конфиг в Inspector компонента
6. [ ] Создать `EnemyPool`-объект в сцене с этим префабом
7. [ ] Добавить новую запись в `EnemySpawner._enemies` с нужным весом
