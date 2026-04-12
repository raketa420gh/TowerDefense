# Bugfix: Console Errors & Warnings (2026-04-13)

## Проблема 1 — MissingReferenceException: CoroutineRunner destroyed

**Файл:** `Assets/Game/Scripts/Gameplay/Waves/WaveSpawner.cs:52`  
**Файл:** `Assets/Game/Scripts/Core/Services/CoroutineRunner.cs:10`

### Причина

При выходе из геймплейной сцены Unity уничтожает MonoBehaviour-компоненты раньше, чем Zenject вызывает `DisposableManager.Dispose()`. В итоге `WaveSpawner.Dispose()` → `Stop()` → `_runner.Stop(_routine)` → `StopCoroutine(routine)` на уже уничтоженном `CoroutineRunner`.

### Фикс

В `CoroutineRunner.Stop()` добавить Unity-null guard (`this != null` использует перегруженный оператор `==` Unity для проверки разрушенных объектов):

```csharp
public void Stop(Coroutine routine)
{
    if (routine != null && this) StopCoroutine(routine);
}
```

---

## Проблема 2 — Warnings: signal fired but no subscriptions

**Сигналы:** `LevelLoadedSignal`, `BaseHealthChangedSignal`, `GoldChangedSignal`, `WaveStartedSignal`, `EnemySpawnedSignal`

### Причина

Все сигналы объявлены в `ProjectInstaller` (корневой контейнер). Когда геймплейная сцена файрит сигнал через свой `SignalBus`, Zenject распространяет его вверх до `SignalDeclaration` родительского контейнера — там подписчиков нет, отсюда предупреждение. Сами подписчики (`HudPresenter` и др.) живут в sub-container сцены и работают корректно; предупреждение — ложная тревога на уровне propagation.

Исключение — `LevelLoadedSignal`: он файрится из проектного контейнера (`LoadLevelState`), но ни один класс не подписывается на него вообще. Сигнал объявлен, но орфанный.

### Фикс

В `ProjectInstaller.InstallBindings()` пометить все сигналы как `OptionalSubscriber()`:

```csharp
Container.DeclareSignal<LevelLoadedSignal>().OptionalSubscriber();
Container.DeclareSignal<EnemySpawnedSignal>().OptionalSubscriber();
Container.DeclareSignal<EnemyKilledSignal>().OptionalSubscriber();
Container.DeclareSignal<EnemyReachedBaseSignal>().OptionalSubscriber();
Container.DeclareSignal<BaseHealthChangedSignal>().OptionalSubscriber();
Container.DeclareSignal<BaseDestroyedSignal>().OptionalSubscriber();
Container.DeclareSignal<WaveStartedSignal>().OptionalSubscriber();
Container.DeclareSignal<WaveCompletedSignal>().OptionalSubscriber();
Container.DeclareSignal<AllWavesCompletedSignal>().OptionalSubscriber();
Container.DeclareSignal<WaveBreakStartedSignal>().OptionalSubscriber();
Container.DeclareSignal<LevelFailedSignal>().OptionalSubscriber();
Container.DeclareSignal<LevelCompletedSignal>().OptionalSubscriber();
Container.DeclareSignal<WaveEarlyStartRequestedSignal>().OptionalSubscriber();
Container.DeclareSignal<GoldChangedSignal>().OptionalSubscriber();
Container.DeclareSignal<TowerBuiltSignal>().OptionalSubscriber();
Container.DeclareSignal<TowerSoldSignal>().OptionalSubscriber();
Container.DeclareSignal<TowerUpgradedSignal>().OptionalSubscriber();
Container.DeclareSignal<ProjectileHitSignal>().OptionalSubscriber();
Container.DeclareSignal<PauseRequestedSignal>().OptionalSubscriber();
Container.DeclareSignal<PauseResumedSignal>().OptionalSubscriber();
Container.DeclareSignal<LevelStartRequestedSignal>().OptionalSubscriber();
```

Альтернатива — в `ZenjectSettings` выставить `RequireSubscriber = false` глобально, но точечный `OptionalSubscriber()` предпочтительнее.

---

## Порядок применения

1. `CoroutineRunner.cs` → `Stop()` с null guard (1 строка)
2. `ProjectInstaller.cs` → `OptionalSubscriber()` ко всем `DeclareSignal`

## Статус

- [x] Проблема 1 — `CoroutineRunner.Stop()` исправлен (2026-04-13)
- [x] Проблема 2 — все `DeclareSignal` помечены `OptionalSubscriber()` (2026-04-13)
- Компиляция успешна, консоль чистая.
