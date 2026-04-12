# Tower Defense — Game Design Document

**Платформа:** Android (мобильная, портрет/ландшафт — **ландшафт**)
**Движок:** Unity 6 (6000.2.13f1), URP 17.2.0
**DI:** Zenject 9.2.0
**Ассеты:** `kenney_tower-defense-kit` (CC0)
**Цель:** рабочий прототип Tower Defense — меню + 5 уровней прогрессии.

---

## 1. Концепция

Классический Tower Defense: враги (UFO из Kenney-пака) идут по заранее заданному пути от точки спавна к базе игрока. Игрок ставит башни на обозначенные слоты, тратит золото, прокачивает оборону, удерживает волны. Цель уровня — пережить все волны; проигрыш — если здоровье базы упадёт до 0.

### 1.1 Уникальные точки
- Модульные башни Kenney (4 типа оружия: ballista, cannon, catapult, turret).
- Прогрессия сложности по 5 уровням: новые враги, ветвление путей, ограничения.
- Быстрые раунды (1.5–4 минуты) для мобильного формата.

### 1.2 Целевая аудитория
Казуальные мобильные игроки, фанаты TD (Bloons TD, Kingdom Rush).

---

## 2. Геймплей — core loop

```
Меню → Выбор уровня → Подготовка → Волна → Перерыв → … → Победа/Поражение → Награда → Меню
```

### 2.1 Механики (MVP)
1. **Путь (Path)** — waypoints + NavMesh для врагов.
2. **Спавн волн (WaveSpawner)** — ScriptableObject с таймингом/составом.
3. **Башни (Tower)**:
   - Размещение в заранее заданных слотах (`TowerSlot`).
   - Стоимость в золоте.
   - Атака: радиус, урон, скорострельность, тип снаряда.
   - Апгрейд до уровня 3.
   - Продажа (возврат 70%).
4. **Враги (Enemy)**:
   - HP, скорость, награда, урон базе.
   - Движение по пути (NavMeshAgent либо waypoint-lerp).
   - Смерть → золото игроку.
5. **Снаряды (Projectile)** — полёт к цели, урон при попадании.
6. **Экономика**: стартовое золото, награда за убийство, награда за волну.
7. **База (PlayerBase)** — HP, проигрыш при 0.
8. **Прогресс** — разблокировка следующего уровня, звёзды (0–3) по сохранённым HP.

### 2.2 Out of scope (для прототипа)
- Донат/магазин/реклама.
- Мета-прокачка, руны, герои.
- Онлайн, лидерборды.
- Локализация (только русский/английский hardcoded).
- Звук/музыка (добавим позже, заглушки SFX).

---

## 3. Прогрессия — 5 уровней

| # | Название | Путь | Волн | Враги | Золото | База HP | Новое |
|---|----------|------|------|-------|--------|---------|-------|
| 1 | Тренировка | 1 прямой | 5 | ufo-a | 300 | 20 | Tutorial, 1 тип башни (ballista) |
| 2 | Поворот | 1 с поворотами | 7 | a, b | 350 | 20 | +cannon, апгрейды |
| 3 | Развилка | split путь | 8 | a, b, c | 400 | 18 | 2 пути одновременно |
| 4 | Ущелье | Узкий длинный | 10 | a, b, c, d | 400 | 15 | +catapult (AoE), быстрые враги |
| 5 | Финал | Сложный + развилки | 12 | все + босс | 450 | 12 | +turret, босс-волна |

**Звёзды:**
- 3⭐ — 100% HP
- 2⭐ — ≥50% HP
- 1⭐ — прохождение

---

## 4. Башни

| Башня | Урон | Скорострельность | Радиус | Цена | Особенность |
|-------|------|------------------|--------|------|-------------|
| Ballista (arrow) | средний | средняя | большой | 50 | single-target, базовая |
| Cannon (cannonball) | высокий | низкая | средний | 100 | splash-урон |
| Catapult (boulder) | очень высокий | очень низкая | большой | 150 | AoE, замедление |
| Turret (bullet) | низкий | очень высокая | малый | 120 | anti-fast |

**Апгрейд** (3 уровня): +25% урон, +10% радиус, цена = base × 1.5 × level.
**Визуал апгрейда:** смена mesh на варианты a/b/c из Kenney-пака (`tower-round-middle-a/b/c`).

---

## 5. Враги

| Враг | HP | Скорость | Урон базе | Награда | Заметки |
|------|----|---------:|-----------|---------|---------|
| ufo-a | 50 | 2.0 | 1 | 10 | базовый |
| ufo-b | 120 | 1.5 | 1 | 20 | «танк» |
| ufo-c | 40 | 3.5 | 1 | 15 | быстрый |
| ufo-d | 200 | 1.0 | 2 | 35 | тяжёлый |
| boss (ufo-d scaled) | 2000 | 0.8 | 10 | 200 | уникальная волна L5 |

---

## 6. Архитектура — Unity Way + Zenject

### 6.1 Принципы
- **Zenject installers** вместо синглтонов.
- **`IInitializable` / `ITickable` / `IDisposable`** вместо Awake/Update/OnDestroy где можно.
- **ScriptableObject** для конфигов (TowerConfig, EnemyConfig, WaveConfig, LevelConfig).
- **Signals** (Zenject SignalBus) — как основной способ событийной связи между системами.
- **Pooling** — снаряды, враги (через `MemoryPool<T>`).
- **SOLID, KISS**: каждый класс — одна ответственность. Views отделены от логики.
- **DisplayableView** — базовый класс UI, `Show()` / `Hide()`.

### 6.2 Структура папок

```
Assets/Game/Scripts/
  Bootstrap/          ← EntryPoint, ProjectInstaller, SceneInstaller
  Core/
    StateMachine/     ← BaseState, State<T>, StateMachineController (копия из Dramacore)
    GameLoop/         ← GameLoopStateMachine, GameLoopState, состояния
    Signals/          ← все Zenject-сигналы
    Services/         ← PersistenceService, SceneLoader, AudioStub
  Configs/            ← SO-конфиги (Level/Tower/Enemy/Wave)
  Gameplay/
    Level/            ← Level, Path, TowerSlot, PlayerBase
    Enemies/          ← Enemy, EnemyFactory, EnemyMovement, EnemyHealth
    Towers/           ← Tower, TowerFactory, TowerAttack, TowerUpgrader
    Projectiles/      ← Projectile, ProjectilePool
    Waves/            ← WaveSpawner, WaveController
    Economy/          ← Wallet, RewardService
  UI/
    Views/            ← DisplayableView, MainMenuView, LevelSelectView, HudView, PauseView, WinLoseView
    Presenters/       ← связь Signals → Views
  Meta/
    Progress/         ← PlayerProgress, SaveData
```

### 6.3 Zenject-контейнеры
- **ProjectContext** — `PersistenceService`, `PlayerProgress`, `SignalBus`, `SceneLoader`, `GameLoopStateMachine`.
- **SceneContext (Menu)** — `MainMenuPresenter`, `LevelSelectPresenter`.
- **SceneContext (Gameplay)** — `Level`, `Wallet`, `WaveSpawner`, `EnemyFactory`, `TowerFactory`, `ProjectilePool`, HUD-презентеры.

### 6.4 Game State Machine (аналог Dramacore)

```
Initialize → MainMenu → LoadLevel → Gameplay → LevelComplete → MainMenu
                                          ↓
                                       LevelFailed → MainMenu
                                       Pause (overlay)
```

**States (enum):**
```csharp
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
```

Наследуются от `GameLoopState : State<GameLoopStateMachine>`. Базовые классы `BaseState`, `State<T>`, `StateMachineController<K,T>` копируются из Dramacore (`D:/Game Development/Dramacore/Assets/Scripts/StateMachine/`).

`GameLoopStateMachine : StateMachineController<GameRoot, GameLoopStateMachine.State>` — где `GameRoot : MonoBehaviour` висит в ProjectContext-сцене и держит ссылки на сервисы, к которым обращаются state'ы через `Parent`.

### 6.5 Сигналы (ключевые)
- `EnemySpawnedSignal(Enemy)`, `EnemyKilledSignal(Enemy, int reward)`, `EnemyReachedBaseSignal(int damage)`
- `WaveStartedSignal(int index)`, `WaveCompletedSignal(int index)`, `AllWavesCompletedSignal()`
- `TowerBuiltSignal(Tower)`, `TowerUpgradedSignal(Tower)`, `TowerSoldSignal(Tower)`
- `GoldChangedSignal(int)`, `BaseHealthChangedSignal(int,int)`
- `LevelStartRequestedSignal(int levelId)`, `LevelCompletedSignal(int stars)`, `LevelFailedSignal()`

---

## 7. Сохранения

`PlayerProgress` (JSON через `PersistenceService` в `Application.persistentDataPath`):
```json
{
  "unlockedLevel": 1,
  "levelStars": { "1": 3, "2": 2, ... }
}
```

---

## 8. Ассеты — маппинг на геймплей

| Нужно | Kenney-модель |
|-------|---------------|
| Тайлы карты | tile, tile-straight, tile-corner-*, tile-split, tile-crossing |
| Спавн | tile-spawn, tile-spawn-round |
| База | wood-structure-high |
| Декор | detail-tree, detail-rocks, detail-crystal |
| Слот башни | selection-a (подсветка) + tile-round-base |
| Ballista | tower-round-base + bottom-a + middle-a + roof-a + weapon-ballista |
| Cannon | tower-square + weapon-cannon |
| Catapult | tower-square + weapon-catapult |
| Turret | tower-round + weapon-turret |
| Апгрейды | смена middle-a/b/c |
| Снаряды | weapon-ammo-arrow/cannonball/boulder/bullet |
| Враги | enemy-ufo-a/b/c/d |
| Босс | enemy-ufo-d scale 2.5 |

Все модели используют общий атлас `colormap.png` → 1 draw call (включить GPU Instancing на материале).

---

## 9. Пошаговый план разработки (итеративный, каждая итерация — тестируемая фича)

> После каждой итерации — ручной тест в Editor + запуск на Android APK.

### ИТЕРАЦИЯ 0 — Bootstrap (0.5 дня)
**Задачи:**
1. Скопировать `BaseState.cs`, `State.cs`, `StateMachineController.cs` из Dramacore в `Core/StateMachine/`.
2. Создать `GameRoot` (MonoBehaviour), `GameLoopStateMachine`, `GameLoopState`.
3. Создать `ProjectInstaller` (Zenject), забиндить `SignalBus`, `PersistenceService`, `SceneLoader`, `GameLoopStateMachine`, `PlayerProgress`.
4. Переписать `EntryPoint.cs` — запускает `GameLoopStateMachine.Initialise(gameRoot, State.Initialize)`.
5. Создать пустые `InitializeState`, `MainMenuState`.

**Тест:** запуск — логи `OnStateActivated` Initialize → MainMenu. Без ошибок компиляции.

---

### ИТЕРАЦИЯ 1 — Меню и выбор уровня (1 день)
**Задачи:**
1. Сцена `Menu.unity`, SceneContext с `MenuInstaller`.
2. `DisplayableView` — базовый класс (Show/Hide + CanvasGroup).
3. `MainMenuView` — кнопка «Играть» → открывает `LevelSelectView`.
4. `LevelSelectView` — 5 кнопок уровней, замок для недоступных, отображение звёзд.
5. `LevelButton` реагирует на `PlayerProgress`.
6. По нажатию — `LevelStartRequestedSignal`, состояние → `LoadLevel`.
7. `LoadLevelState` грузит сцену `Gameplay.unity` (пока пустую).

**Тест:** меню → выбор уровня → загрузка пустой сцены геймплея → возврат в меню.

---

### ИТЕРАЦИЯ 2 — Уровень, путь, враг (1.5 дня)
**Задачи:**
1. `LevelConfig` (SO) — ссылка на prefab-уровня, список wave'ов, стартовое золото, HP базы.
2. Собрать 1-й уровень как **prefab** из тайлов Kenney (прямой путь).
3. `Path` — массив `Transform[] waypoints`.
4. `Enemy` — `EnemyConfig` (SO: HP, speed, reward, damage, prefab).
5. `EnemyMovement` — движение по waypoints (без NavMesh для MVP, lerp).
6. `EnemyHealth` — урон/смерть, сигналы.
7. `EnemyFactory` (Zenject factory с pool).
8. `PlayerBase` — HP, получает урон когда враг доходит, сигнал `LevelFailed` на 0 HP.
9. `GameplayState` — активирует `WaveSpawner`.
10. `WaveSpawner` — читает `WaveConfig`, спавнит одного врага.

**Тест:** в Gameplay-сцене спавнится враг, идёт по пути, дамажит базу, игра уходит в `LevelFailed`.

---

### ИТЕРАЦИЯ 3 — Башня, атака, снаряды (2 дня)
**Задачи:**
1. `TowerConfig` (SO: damage, range, fireRate, cost, projectilePrefab, towerPrefab).
2. `TowerSlot` — точка на карте, клик → `BuildMenu` (UI с башнями).
3. `Wallet` — баланс, `GoldChangedSignal`.
4. `TowerFactory` — спавнит башню на слоте, списывает золото.
5. `Tower` — хранит config, держит `TowerAttack`.
6. `TowerAttack` — `ITickable`, ищет ближайшего врага в радиусе, стреляет по cooldown.
7. `Projectile` + `ProjectilePool` — полёт к цели, урон.
8. Враг при смерти даёт золото (`Wallet.Add`).

**Тест:** поставил башню → враг убит → золото прибавилось.

---

### ИТЕРАЦИЯ 4 — Волны, экономика, HUD (1 день)
**Задачи:**
1. `WaveConfig` (SO): список `SubWave { enemyConfig, count, interval }` + delayBeforeNext.
2. `WaveSpawner` — корректный тайминг всех волн.
3. `WaveCompletedSignal` → награда + пауза 5 сек.
4. `AllWavesCompletedSignal` → `LevelComplete`.
5. `HudView` (DisplayableView): золото, HP базы, текущая волна, кнопка «Начать следующую волну раньше».
6. Вычисление звёзд по `BaseHealth`.

**Тест:** полное прохождение 1-го уровня → `LevelCompleteView` со звёздами → возврат в меню → 1-й уровень с ⭐, 2-й разблокирован.

---

### ИТЕРАЦИЯ 5 — Апгрейд/продажа башен, ещё типы башен (1 день)
**Задачи:**
1. `TowerUpgrader` — до уровня 3, смена mesh-варианта.
2. UI `TowerInfoView` — клик на башню: характеристики, upgrade, sell.
3. Добавить Cannon (splash), Catapult (AoE+slow), Turret (fast).
4. `AreaDamageComponent` — урон по радиусу от точки попадания.
5. `SlowEffect` — компонент на враге с таймером.

**Тест:** можно апгрейдить, продавать, все 4 башни работают по-разному.

---

### ИТЕРАЦИЯ 6 — Остальные 4 уровня + враги (1.5 дня)
**Задачи:**
1. Создать prefabs уровней 2–5 из Kenney-тайлов (повороты, split, длинный).
2. Создать `EnemyConfig` для ufo-b/c/d и boss.
3. Создать `WaveConfig`-ассеты согласно таблице прогрессии (раздел 3).
4. Создать `LevelConfig` 1..5.
5. Level 3 — обработка 2-х путей одновременно (`Path[]` в LevelConfig).
6. Level 5 — босс-волна (1 враг, большой HP).

**Тест:** пройти все 5 уровней подряд, проверить возрастающую сложность, звёзды сохраняются.

---

### ИТЕРАЦИЯ 7 — Pause, поражение, полишинг (1 день)
**Задачи:**
1. `PauseState` — overlay (Time.timeScale = 0), кнопки Resume/Restart/Menu.
2. `LevelFailedState` — view с Retry/Menu.
3. SFX заглушки (выстрел, смерть, победа) через AudioSource в префабах.
4. Camera — фиксированная ортографическая/перспективная сверху.
5. Touch-input (Unity Input System) — tap на слот/башню.
6. GPU Instancing на материале `colormap`.

**Тест:** пауза работает, перезапуск уровня работает, тач на Android.

---

### ИТЕРАЦИЯ 8 — Android-билд и финальный QA (0.5 дня)
**Задачи:**
1. Player Settings: package name, icon, портрет/ландшафт (**ландшафт**), minSDK 24.
2. Build APK, установить на устройство.
3. Прогнать все 5 уровней на мобилке, проверить тач, FPS.
4. Фиксы регрессий.

**Результат:** рабочий APK-прототип.

---

## 10. Критерии готовности MVP
- [ ] Запуск на Android без ошибок.
- [ ] Меню → выбор уровня → геймплей → возврат в меню.
- [ ] 5 уровней играбельны, усложнение ощущается.
- [ ] 4 типа башен, 4 типа врагов + босс.
- [ ] Апгрейд / продажа башен.
- [ ] Волны, экономика, HP базы.
- [ ] Звёзды и сохранение прогресса.
- [ ] Пауза, рестарт, поражение, победа.
- [ ] Стабильные 60 FPS на среднем Android.

---

## 11. Технические заметки

- **Поиск цели башней:** `Physics.OverlapSphere` раз в ~0.1с, слой `Enemies`.
- **Snapping врагов:** ближайший waypoint по пути, не ближайший по дистанции.
- **Pool'ы:** `Enemy`, `Projectile` — через `MemoryPool<Config, T>`.
- **Time.timeScale** управляется только `PauseState`.
- **Сигналы** отписываются в `OnStateDisabled` — строго, иначе утечки.
- **ScriptableObject'ы** конфигов складываются в `Assets/Game/Configs/{Levels,Towers,Enemies,Waves}`.
- **Никаких `FindObjectOfType`** — всё через Zenject.
- **Никаких синглтонов.**

---

## 12. Оценка трудоёмкости
Суммарно: ~9–10 рабочих дней на одного разработчика до playable MVP на Android.
