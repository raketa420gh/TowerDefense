# Wave Progression Bug — Analysis & Fix Plan

## Symptom
After killing all enemies in wave 1, the wave counter never advances to wave 2.

---

## Root Cause #1 (Critical) — GameRoot._stateMachine is null

### Evidence
Console (Play Mode) is completely flooded with:
```
NullReferenceException: Object reference not set to an instance of an object
GameRoot.Update () (at Assets/Game/Scripts/Bootstrap/GameRoot.cs:23)
```
Line 23: `_stateMachine.ActiveState?.Update();`  
`_stateMachine` is **null** → `Construct(GameLoopStateMachine)` was never called by Zenject.

### Effect Chain
```
Construct() not called
  → _stateMachine = null
  → GameRoot.Start() throws (line 18)  → state machine never initialises
  → GameplayState.OnStateActivated() never called
  → WaveSpawner.Run() never called
  → no waves, no enemies, no wave progression
```

### Why Zenject skips injection
Zenject calls `ProjectContext.Awake()` → `GetInjectableMonoBehaviours()` →
`QueueForInject(gameRoot)` → `InstallBindings()` → `ResolveRoots()`.
If `InstallBindings()` or `ResolveRoots()` throws an unhandled exception,
injections queued before it are never applied.  
`TypeAnalyzer.HasInfo(GameRoot) = true` (verified), so the type is recognised.
The exact exception is swallowed before appearing in console — only NullRefs from
the un-injected `GameRoot` remain.

### Fix (GameRoot.cs)
Add null guards so the crash doesn't cascade every frame and the error is obvious:

```csharp
private void Start()
{
    if (_stateMachine == null)
    {
        Debug.LogError("[GameRoot] _stateMachine is null — Zenject injection failed. " +
                       "Check ProjectContext / ProjectInstaller for binding errors.");
        return;
    }
    _stateMachine.Initialise(this, GameLoopStateMachine.State.Initialize);
}

private void Update()
{
    _stateMachine?.ActiveState?.Update();
}
```

### Fix (ProjectInstaller.cs)
Bind `GameRoot` explicitly so it is guaranteed to be in the container and injected,
regardless of Zenject version edge-cases with `ProjectContext` hierarchy auto-injection:

```csharp
// Add after SignalBusInstaller.Install(Container):
Container.Bind<GameRoot>().FromComponentInHierarchy().AsSingle().NonLazy();
```

> `FromComponentInHierarchy()` searches the ProjectContext prefab hierarchy for the
> existing `GameRoot` component and uses it — no new object created.

---

## Root Cause #2 (Secondary) — Killed enemy can still reach base

### Code path
```
Frame N  : Tower projectile hits enemy near path end
           EnemyHealth.TakeDamage() → Died fires
           EnemyKilledSignal   → WaveSpawner._aliveEnemies--  (now 0)
           Object.Destroy(gameObject)  ← queued, not immediate

Frame N+1: EnemyMovement.Update() still runs (GO not yet destroyed)
           → _reachedEnd = true

Frame N+1: EnemyBaseDamager.Update()
           _applied=false, _movement.ReachedEnd=true  → enters body
           _base.ApplyDamage(...)
           EnemyReachedBaseSignal fired
           → WaveSpawner._aliveEnemies = Max(0, 0-1) = 0  (no extra damage to counter)
           → GameplayState.OnEnemyReachedBase() → OnBaseDestroyed()
             → if !_gameEnded → LevelFailed!
```
**Result:** Player kills an enemy near the finish line → game incorrectly transitions to
`LevelFailed` even though the enemy was killed.  
This also means `AllWavesCompletedSignal` can never fire (state changes to LevelFailed first).

### Fix (EnemyBaseDamager.cs)
Guard against applying damage if the enemy is already dead:

```csharp
private void Update()
{
    if (_applied || !_movement.ReachedEnd) return;
    if (_enemy.Health.IsDead) return;   // ← ADD THIS LINE
    _applied = true;
    _base.ApplyDamage(_enemy.Config.BaseDamage);
    _signalBus.Fire(new EnemyReachedBaseSignal());
    Destroy(gameObject);
}
```

---

## Root Cause #3 (UX) — Wave counter is silent during the break

After all enemies in wave N die, `WaveCompletedSignal` fires but `HudPresenter`
does NOT subscribe to it — the wave counter stays at "Wave N/X" for the entire
`delayAfter` period (6 s for level 1).  
The break timer and early-start button should make this clear, but if `_breakTimerLabel`
or `_earlyStartButton` are null in the scene, `HudView.SetEarlyStartVisible()` throws
and no feedback is shown.

### Fix — short-term
Verify all `[SerializeField]` references in `HudView` are assigned in every
`Gameplay*.unity` scene.  Check `_goldLabel`, `_baseHpLabel`, `_waveLabel`,
`_breakTimerLabel`, `_earlyStartButton`, `_pauseButton`.

### Fix — long-term (optional UX)
Subscribe `HudPresenter` to `WaveCompletedSignal` and immediately show something
like "Wave complete!" or grey out the wave counter to indicate a break:

```csharp
// HudPresenter.Initialize():
_signalBus.Subscribe<WaveCompletedSignal>(OnWaveCompleted);

// HudPresenter.Dispose():
_signalBus.TryUnsubscribe<WaveCompletedSignal>(OnWaveCompleted);

// Handler:
private void OnWaveCompleted(WaveCompletedSignal _)
{
    _view.SetEarlyStartVisible(false);   // will be re-shown by OnWaveBreakStarted
}
```

---

## Fix Priority

| # | File | Change | Priority | Status |
|---|------|---------|----------|--------|
| 1 | `GameRoot.cs` | null guard in `Start()` + `?.` in `Update()` | **Critical** | ✅ Done |
| 2 | `ProjectInstaller.cs` | explicit `GameRoot` binding | **Critical** | ✅ Done |
| 3 | `EnemyBaseDamager.cs` | `IsDead` check before applying base damage | **High** | ✅ Done |
| 4 | `Gameplay*.unity` scenes | verify all HudView references are assigned | **High** | ✅ Done (verified via MCP) |
| 5 | `HudPresenter.cs` | subscribe to `WaveCompletedSignal` | Medium (UX) | ✅ Done |

---

## Verification Steps

1. Press Play — no NullReferenceException in console.
2. `[GameRoot]` injection success log appears (add `Debug.Log` in `Construct()`).
3. `[InitializeState] activated` / `[MainMenuState] activated` / `[GameplayState] activated` appear.
4. Kill all wave 1 enemies → break timer appears, early-start button visible.
5. Wait 6 s (or press early start) → wave 2 starts, counter changes to "Wave 2/5".
6. Let enemy reach finish line while another is killed → game does NOT fail.
7. Kill all enemies in all waves → `AllWavesCompletedSignal` → `LevelComplete` screen.
