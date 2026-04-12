# UI Implementation Plan — Tower Defense (kenney_ui_pack, MVP Passive View)

## Статус реализации

| Шаг | Статус | Примечания |
|-----|--------|------------|
| 1 — Text → TextMeshProUGUI | ✅ DONE | HudView, LevelCompleteView, TowerInfoView, BuildMenuButton, LevelButton |
| 2 — TMP Font Assets | ✅ DONE | KenneyFuture_TMP.asset, KenneyFutureNarrow_TMP.asset (dynamic, SDFAA, 1024×1024) |
| 3 — 9-slice спрайты | ✅ DONE | Green/Red/Yellow/Grey/Blue rect + round + square (L=8 B=12 R=8 T=8) |
| 4 — Menu.unity Canvas | ✅ DONE | Background, Title, PlayButton/QuitButton стилизованы; LevelSelect с Panel+Title+5xLevelButton |
| 5 — Gameplay.unity Canvas | ✅ DONE | HUD+TopBar, BuildMenu, TowerInfo, LevelComplete, LevelFailed, PauseView стилизованы |
| 6 — LevelContext references | ✅ DONE | Все 6 полей заполнены в Gameplay.unity |
| 7 — GameplayUI Prefab | ✅ DONE | Assets/Game/Prefabs/UI/GameplayUI.prefab применён во всех 5 gameplay-сценах |



## Контекст

Все C# скрипты (View + Presenter) уже написаны и работают. Цель — реализовать визуальную часть: собрать Canvas-иерархии в сценах и стилизовать их спрайтами из `kenney_ui_pack`. Параллельно мигрируем все `UnityEngine.UI.Text` → `TextMeshProUGUI` для поддержки шрифтов Kenney Future.

---

## Шаг 1 — Миграция Text → TextMeshProUGUI (скрипты)

| Файл | Поля |
|------|------|
| `Assets/Game/Scripts/UI/Views/HudView.cs` | `_goldLabel`, `_baseHpLabel`, `_waveLabel`, `_breakTimerLabel` |
| `Assets/Game/Scripts/UI/Views/LevelCompleteView.cs` | `_titleLabel` |
| `Assets/Game/Scripts/UI/Views/TowerInfoView.cs` | `_nameLabel`, `_levelLabel`, `_statsLabel`, `_upgradeCostLabel`, `_sellRefundLabel` |
| `Assets/Game/Scripts/UI/Views/BuildMenuButton.cs` | `_costLabel` |
| `Assets/Game/Scripts/UI/Views/LevelButton.cs` | `_label` |

Изменение: добавить `using TMPro;`, тип `Text` → `TextMeshProUGUI`.  
После изменений — `read_console` для проверки компиляции.

---

## Шаг 2 — TMP Font Assets

Создать через **Window → TextMeshPro → Font Asset Creator**:

- `Kenney Future.ttf` → `Assets/Game/Content/kenney_ui-pack/Font/KenneyFuture_TMP.asset` (заголовки, 36pt+)
- `Kenney Future Narrow.ttf` → `Assets/Game/Content/kenney_ui-pack/Font/KenneyFutureNarrow_TMP.asset` (кнопки, HUD, 18–24pt)

---

## Шаг 3 — Настройка 9-Slice на спрайтах

Sprite Mode = **Sliced**, Border: L=8, R=8, T=8, B=12:

| Спрайт | Применение |
|--------|------------|
| `PNG/Green/Default/button_rectangle_depth_flat.png` | Play, Продолжить, Resume |
| `PNG/Red/Default/button_rectangle_depth_flat.png` | Выход, Закрыть |
| `PNG/Yellow/Default/button_rectangle_depth_flat.png` | Retry, Рестарт |
| `PNG/Grey/Default/button_rectangle_depth_flat.png` | Меню, неактивные |
| `PNG/Blue/Default/button_rectangle_depth_flat.png` | Кнопки HUD, Build |
| `PNG/Blue/Default/button_round_depth_flat.png` | Круглые Close/Pause |

---

## Шаг 4 — Menu.unity Canvas

Canvas: Screen Space – Overlay, CanvasScaler Scale With Screen Size 1920×1080.

```
UIRoot (Canvas)
├── MainMenuView (CanvasGroup)                    ← MainMenuView.cs
│   ├── Background (Image, #1A1A2E)
│   ├── Title (TMP "TOWER DEFENSE", KenneyFuture 64pt)
│   ├── PlayButton (green rect 9-slice, 320×80)
│   │   └── Label (TMP "ИГРАТЬ", KenneyFutureNarrow 28pt)
│   └── QuitButton (red rect 9-slice, 320×80)
│       └── Label (TMP "ВЫХОД")
└── LevelSelectView (CanvasGroup, hidden)         ← LevelSelectView.cs
    ├── Panel (grey rect 9-slice, 900×600)
    ├── Title (TMP "ВЫБОР УРОВНЯ", KenneyFuture 48pt)
    ├── LevelGrid (HorizontalLayoutGroup, spacing 20)
    │   └── LevelButton × 5
    └── BackButton (grey round, 80×80)
        └── Icon (arrow_basic_w_small)
```

**LevelButton prefab** (`Assets/Game/Prefabs/UI/LevelButton.prefab`):
```
Root (Button, 140×180)
├── Background (blue square 9-slice)
├── LevelLabel (TMP, KenneyFuture 36pt, centred)   ← _label
├── LockIcon (icon_cross_outline)                  ← _lockIcon
└── StarsRow (HorizontalLayoutGroup)
    ├── Star1 (yellow/star или grey/star_outline)  ← _stars[0]
    ├── Star2                                      ← _stars[1]
    └── Star3                                      ← _stars[2]
```

---

## Шаг 5 — Gameplay.unity (шаблон для всех 5 сцен)

Sort Order: HUD=0 · BuildMenu/TowerInfo=10 · Overlays=20 · Pause=30

### HudView (SO=0)
```
HudCanvas
└── HudView (CanvasGroup)                         ← HudView.cs
    ├── TopBar (HorizontalLayoutGroup, anchor top-stretch)
    │   ├── GoldPanel
    │   │   ├── CoinIcon (yellow/icon_circle)
    │   │   └── GoldLabel (TMP 24pt)              ← _goldLabel
    │   ├── WaveLabel (TMP 22pt)                   ← _waveLabel
    │   └── PauseButton (blue round, 64×64)        ← _pauseButton
    ├── BaseHpLabel (TMP, bottom-left)             ← _baseHpLabel
    └── BreakPanel (hidden by default)
        ├── BreakTimerLabel (TMP 32pt)             ← _breakTimerLabel
        └── EarlyStartButton (green rect)          ← _earlyStartButton
            └── Label (TMP "→ СТАРТ")
```

### BuildMenuView (SO=10, hidden)
```
BuildMenuCanvas
└── BuildMenuView (CanvasGroup, hidden)           ← BuildMenuView.cs
    ├── Panel (grey 9-slice, 320×500, anchor left)
    ├── CloseButton (red round, 48×48)            ← _closeButton
    │   └── Icon (icon_cross_light)
    └── ButtonRoot (VerticalLayoutGroup)          ← _root
        └── (BuildMenuButton создаётся динамически)
```

**BuildMenuButton prefab** (обновить существующий):
```
Root (Button, 280×80)
├── Background (blue rect 9-slice)
├── Icon (Image)                                  ← _icon
└── CostLabel (TMP "150")                         ← _costLabel
```

### TowerInfoView (SO=10, hidden)
```
TowerInfoCanvas
└── TowerInfoView (CanvasGroup, hidden)           ← TowerInfoView.cs
    └── Panel (grey 9-slice, 320×380, anchor right-bottom)
        ├── NameLabel (TMP KenneyFuture 28pt)     ← _nameLabel
        ├── LevelLabel (TMP 20pt)                 ← _levelLabel
        ├── StatsLabel (TMP 18pt)                 ← _statsLabel
        ├── Divider (Extra/divider)
        ├── UpgradeButton (green rect)
        │   └── CostLabel (TMP)                   ← _upgradeCostLabel
        ├── SellButton (red rect)
        │   └── RefundLabel (TMP)                 ← _sellRefundLabel
        └── CloseButton (grey round, 40×40)
```

### LevelCompleteView (SO=20, hidden)
```
LevelCompleteCanvas
└── LevelCompleteView (CanvasGroup, hidden)       ← LevelCompleteView.cs
    ├── Overlay (black Image, α=0.7, stretch)
    └── Panel (grey 9-slice, 500×400, centred)
        ├── TitleLabel (TMP KenneyFuture 36pt)    ← _titleLabel
        ├── StarsRow (HorizontalLayoutGroup)
        │   ├── Star1                             ← _starIcons[0]
        │   ├── Star2                             ← _starIcons[1]
        │   └── Star3                             ← _starIcons[2]
        └── ContinueButton (green rect, 280×70)
            └── Label (TMP "ПРОДОЛЖИТЬ")
```
Star активен = `yellow/star`, неактивен = `grey/star_outline`.

### LevelFailedView (SO=20, hidden)
```
LevelFailedCanvas
└── LevelFailedView (CanvasGroup, hidden)         ← LevelFailedView.cs
    ├── Overlay (black α=0.7)
    └── Panel (grey 9-slice, 400×300, centred)
        ├── Title (TMP "ПОРАЖЕНИЕ", KenneyFuture 42pt, красный)
        ├── RetryButton (yellow rect, 240×65)     ← _retryButton
        │   ├── Icon (icon_repeat_light)
        │   └── Label (TMP "ЕЩЁ РАЗ")
        └── MenuButton (grey rect, 240×65)        ← _menuButton
            ├── Icon (arrow_basic_w_small)
            └── Label (TMP "МЕНЮ")
```

### PauseView (SO=30, hidden)
```
PauseCanvas
└── PauseView (CanvasGroup, hidden)               ← PauseView.cs
    ├── Overlay (black α=0.5)
    └── Panel (grey 9-slice, 380×340, centred)
        ├── Title (TMP "ПАУЗА", KenneyFuture 42pt)
        ├── ResumeButton (green, 280×65)          ← _resumeButton
        ├── RestartButton (yellow, 280×65)        ← _restartButton
        └── MenuButton (grey, 280×65)             ← _menuButton
```

---

## Шаг 6 — Привязка к LevelContext

В каждой gameplay-сцене заполнить поля `LevelContext`:

| Поле | Объект сцены |
|------|--------------|
| `_buildMenu` | BuildMenuView |
| `_hud` | HudView |
| `_completeView` | LevelCompleteView |
| `_failedView` | LevelFailedView |
| `_towerInfoView` | TowerInfoView |
| `_pauseView` | PauseView |

---

## Шаг 7 — GameplayUI Prefab (Gameplay_L2..L5)

Сохранить всю UI-иерархию как `Assets/Game/Prefabs/UI/GameplayUI.prefab`.  
Применить Prefab во всех 5 gameplay-сценах, обновив ссылки в LevelContext.

---

## Верификация

| Сценарий | Ожидаемое поведение |
|----------|---------------------|
| Запуск → Main Menu | Видны кнопки Play/Выход |
| Play → LevelSelect | Панель выбора уровня, кнопки с замками/звёздами |
| Выбор уровня → Gameplay | HUD виден, золото/волна/HP отображаются |
| Клик по слоту → BuildMenu | Меню башен, доступность по золоту |
| Клик по башне → TowerInfo | Статы, кнопки апгрейда/продажи |
| Кнопка Pause → PauseView | Игра на паузе (timeScale=0), overlay поверх игры |
| Все волны → LevelComplete | Overlay, правильное число звёзд |
| Враг у базы → LevelFailed | Overlay, Retry/Menu |
