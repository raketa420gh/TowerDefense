# Bugfix: UI — видимость панелей + верстка LevelFailed

**Дата:** 2026-04-12  
**Ветка:** main

---

## Проблема 1 — Панели не скрывались при завершении уровня

### Симптом
При переходе в `LevelFailedState` или `LevelCompleteState` на экране оставались видимыми:
- `HudView` (статус, золото, кнопка паузы)
- `BuildMenuView` (если было открыто меню постройки)
- `TowerInfoView` (если была открыта инфо-панель башни)
- `PauseView` (если игрок поставил паузу в момент уничтожения базы)

### Причина
Ни один из презентеров не реагировал на `LevelFailedSignal` и `AllWavesCompletedSignal`. Состояния `LevelFailedState` / `LevelCompleteState` только показывали свои панели, не скрывая игровой UI.

### Исправление

**`HudPresenter.cs`**
- Добавлена подписка на `LevelFailedSignal` → `_view.Hide()`
- `OnAllWavesCompleted()` дополнен вызовом `_view.Hide()` (ранее только скрывал кнопку раннего старта)

**`BuildMenuPresenter.cs`**
- Добавлена подписка на `LevelFailedSignal` → `Close()`
- Добавлена подписка на `AllWavesCompletedSignal` → `Close()`

**`TowerInfoPresenter.cs`**
- Добавлена подписка на `LevelFailedSignal` → `Close()`
- Добавлена подписка на `AllWavesCompletedSignal` → `Close()`

**`PausePresenter.cs`**
- Добавлена подписка на `LevelFailedSignal` → `ForceHide()` (сбрасывает `Time.timeScale = 1f` и скрывает панель)

Во всех случаях отписка добавлена симметрично в `Dispose()`.

---

## Проблема 2 — Неверная верстка LevelFailed панели

### Симптом
Элементы панели LevelFailed имели некорректные позиции/размеры:
- `TitleLabel` — пустой текст `""`
- Кнопки (RetryButton, MenuButton) расположены слишком близко к центру экрана (y = −40), асимметрично относительно заголовка

Сравнение с LevelComplete (эталон):

| Элемент | LevelFailed (до) | LevelComplete | LevelFailed (после) |
|---------|-----------------|---------------|---------------------|
| TitleLabel pos | (0, 100) | (0, 120) | **(0, 120)** |
| TitleLabel size | 600×80 | 600×60 | **(600×60)** |
| TitleLabel text | `""` | динамический | **"Уровень провален"** |
| RetryButton pos | (−150, −40) | — | **(−150, −120)** |
| MenuButton pos | (150, −40) | — | **(150, −120)** |

### Дополнительно: баг LevelCompleteView._starIcons

`LevelCompleteView._starIcons` содержал массив из трёх `null` — ссылки на `Star1`/`Star2`/`Star3` не были назначены в Inspector. Вызов `_starIcons[i].SetActive()` внутри `Populate()` приводил бы к `NullReferenceException` при первой победе.

Исправление: ссылки назначены через `SerializedObject` во всех сценах.

### Затронутые сцены
Все изменения применены к каждой геймплейной сцене:
- `Assets/Game/Scenes/Gameplay.unity`
- `Assets/Game/Scenes/Gameplay_L2.unity`
- `Assets/Game/Scenes/Gameplay_L3.unity`
- `Assets/Game/Scenes/Gameplay_L4.unity`
- `Assets/Game/Scenes/Gameplay_L5.unity`

---

## Изменённые файлы

| Файл | Тип изменения |
|------|--------------|
| `Assets/Game/Scripts/UI/Presenters/HudPresenter.cs` | код |
| `Assets/Game/Scripts/UI/Presenters/BuildMenuPresenter.cs` | код |
| `Assets/Game/Scripts/UI/Presenters/TowerInfoPresenter.cs` | код |
| `Assets/Game/Scripts/UI/Presenters/PausePresenter.cs` | код |
| `Assets/Game/Scenes/Gameplay.unity` | сцена |
| `Assets/Game/Scenes/Gameplay_L2.unity` | сцена |
| `Assets/Game/Scenes/Gameplay_L3.unity` | сцена |
| `Assets/Game/Scenes/Gameplay_L4.unity` | сцена |
| `Assets/Game/Scenes/Gameplay_L5.unity` | сцена |
