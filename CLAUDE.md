# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working Style

- Think in English, answer in Russian
- Treat the user as a senior Unity/C# expert — no hand-holding
- Terse responses; give actual code, not high-level descriptions
- Fewer lines of code = better
- Do not stop until task is complete
- When fixing bugs: explain the problem in plain Russian, make minimal changes

## Code Style

- Public events and properties come first, then private
- Method order: Unity methods → public → private → EventHandlers
- After `[SerializeField]` (and other attributes), next text on a new line
- No `public Var { get; private set; }` — always `public Var => _var;` + setter method
- Files 
- SOLID, OOP, GRASP, KISS
- All toggleable Views must inherit from `DisplayableView` (`Show()` / `Hide()` methods)

## Project Overview

Tower Defense game built in **Unity 6 (6000.2.13f1)** with **Universal Render Pipeline (URP)**. The project uses dependency injection via Zenject for architecture.

## Key Dependencies

| Plugin | Version | Purpose |
|--------|---------|---------|
| Zenject | 9.2.0 | Dependency Injection (`Assets/Plugins/Zenject/`) |
| Odin Inspector (Sirenix) | — | Enhanced Unity Inspector (`Assets/Plugins/Sirenix/`) |
| DOTween (Demigiant) | — | Tweening/animation (`Assets/Plugins/Demigiant/`) |
| Unity Input System | 1.14.2 | Input handling |
| AI Navigation (NavMesh) | 2.0.9 | Enemy pathfinding |
| URP | 17.2.0 | Render pipeline |

## Architecture

The project uses **Zenject** for DI. Key architectural patterns to follow:
- `MonoInstaller` / `ScriptableObjectInstaller` for binding registrations
- `IInitializable`, `ITickable`, `IDisposable` Zenject interfaces instead of Unity lifecycle methods where possible
- `EntryPoint.cs` is the game's bootstrapper (`Assets/Game/Scripts/EntryPoint.cs`)

## Project Structure

```
Assets/
  Game/
    Scripts/       ← all game code goes here
    Scenes/        ← SampleScene.unity
    Content/       ← art assets (kenney_tower-defense-kit)
    Settings/      ← URP and other Unity settings assets
  Plugins/
    Zenject/       ← DI framework
    Sirenix/       ← Odin Inspector assemblies
    Demigiant/     ← DOTween
  Resources/       ← Unity Resources-loaded assets
```

## Working with Unity via MCP

This project has **UnityMCP** connected. Use MCP tools to interact with the editor:
- Check `read_console` after any script changes to verify compilation succeeded before proceeding
- Use `manage_scene` to query/save scenes; use `manage_gameobject` to add/modify objects
- Poll `editor_state` resource `isCompiling` field to wait for domain reload after script edits
- Always check `mcpforunity://custom-tools` resource for project-specific tools

## Game Loop Mechanics

**Win condition:** All enemies in all waves are killed → `AllWavesCompletedSignal` → `LevelComplete`.

**Lose condition:** ANY enemy reaches the base → `EnemyReachedBaseSignal` → immediate `LevelFailed`.  
(Also: base HP hits 0 via `BaseDestroyedSignal` → `LevelFailed`, but in practice `EnemyReachedBaseSignal` fires first.)

Both subscriptions live in `GameplayState.OnStateRegistered()` (`Assets/Game/Scripts/Core/GameLoop/States/GameplayState.cs`).

Signal flow for lose:  
`EnemyMovement._reachedEnd=true` → `EnemyBaseDamager.Update` → `EnemyReachedBaseSignal` → `GameplayState.OnEnemyReachedBase` → `LevelFailed`

## Running Tests

Tests use Unity Test Framework (`com.unity.test-framework` 1.6.0). Run via Unity Editor: **Window → General → Test Runner**, or via MCP tool `run_tests`.