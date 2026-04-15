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

