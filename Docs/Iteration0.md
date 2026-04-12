# Итерация 0 — Bootstrap

**Цель:** Подготовить каркас приложения — Zenject-контейнер, GameLoopStateMachine с состояниями `Initialize → MainMenu`, базовые сервисы. Проект должен запускаться без ошибок, в консоли — логи активации состояний.

---

## План

### 1. Core/StateMachine/ — скопировать из Dramacore
- [x] `BaseState.cs`
- [x] `State.cs`
- [x] `StateMachineController.cs`

### 2. Core/GameLoop/ — машина состояний игры
- [x] `GameLoopStateMachine.cs` — наследник `StateMachineController<GameRoot, State>`, enum `State`
- [x] `GameLoopState.cs` — базовый класс состояний (`State<GameLoopStateMachine>`)
- [x] `States/InitializeState.cs` — грузит PlayerProgress, переходит в MainMenu
- [x] `States/MainMenuState.cs` — заглушка (лог активации)

### 3. Core/Services/
- [x] `PersistenceService.cs` — сохранение/загрузка JSON в `Application.persistentDataPath`
- [x] `SceneLoader.cs` — асинхронная загрузка сцен

### 4. Meta/Progress/
- [x] `SaveData.cs` — POCO для сериализации
- [x] `PlayerProgress.cs` — unlocked level, звёзды, методы Load/Save

### 5. Bootstrap/
- [x] `GameRoot.cs` — MonoBehaviour в ProjectContext, `[Inject]` зависимости
- [x] `ProjectInstaller.cs` — `ScriptableObjectInstaller`, биндит `SignalBus`, `PersistenceService`, `PlayerProgress`, `SceneLoader`, `GameLoopStateMachine`
- [x] `EntryPoint.cs` — переписать: `GameRoot` на старте инициализирует SM

### 6. Unity-сцена / ассеты (сделано через UnityMCP)
- [x] `Assets/Game/Settings/ProjectInstaller.asset` (SO)
- [x] `Assets/Resources/ProjectContext.prefab` — на корне `ProjectContext`, дочерний GO `GameRoot`, installer привязан
- [x] `SceneContext` добавлен в `SampleScene` (триггер загрузки ProjectContext)
- [x] Проверка: в логах — `[GameRoot] Construct → Start → InitializeState activated → MainMenuState activated`

---

## Прогресс

| Шаг | Статус |
|-----|--------|
| Скопированы StateMachine-базы | ✅ |
| GameLoopStateMachine + состояния | ✅ |
| Services (Persistence, SceneLoader) | ✅ |
| PlayerProgress + SaveData | ✅ |
| ProjectInstaller + GameRoot + EntryPoint | ✅ |
| Unity-ассеты (ProjectContext prefab, installer asset) | ✅ через MCP |
| SceneContext добавлен в SampleScene | ✅ |
| Компиляция без ошибок (read_console) | ✅ |
| Запуск — лог переходов состояний | ✅ Construct → Start → Initialize → MainMenu |

---

## Заметки
- `EntryPoint.cs` был пустышкой — переписан в MonoBehaviour `GameRoot`, сам `EntryPoint` очищен.
- `PersistenceService` синхронный (Json + File.IO) — для прототипа достаточно.
- Сигналы пока не объявляем — появятся в итерации 1+.
- **Важный gotcha:** `GameRoot` должен быть **дочерним** GameObject внутри ProjectContext-префаба, а не на корне. На корне Zenject не инжектит (ProjectContext исключает себя из `GetInjectableMonoBehaviours`, и через GetComponent тоже не ловит соседний компонент).
- Для того чтобы Zenject инстанцировал ProjectContext-префаб из `Resources`, сцена должна содержать `SceneContext` — иначе ничего не тригерит автолоад.
- Поле ProjectContext для привязки installer'а — `_scriptableObjectInstallers` (приватное, но видно через `SerializedObject`).
