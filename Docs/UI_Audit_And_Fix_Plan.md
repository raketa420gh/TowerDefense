# UI Аудит и план фиксов

Дата: 2026-04-12  
Сцены: `Menu.unity`, `Gameplay.unity`, `Gameplay_L2..L5.unity`

---

## Результаты аудита

### Архитектурный контекст

Все View-компоненты наследуют `DisplayableView`, который управляет видимостью **исключительно через `CanvasGroup`** (`alpha/interactable/blocksRaycasts`). `SetActive` нигде не вызывается. Это означает: каждый Canvas-объект должен быть `activeSelf: true` в сцене — иначе `Show()` физически не отобразит содержимое.

---

## Критические баги

### BUG-01 — HudCanvas, LevelCompleteCanvas, LevelFailedCanvas: World Space вместо Screen Space Overlay

**Сцены:** все `Gameplay*.unity` (6 штук)

| Canvas | renderMode | CanvasScaler | sizeDelta |
|--------|-----------|-------------|----------|
| HudCanvas | 2 (World Space) | Constant Pixel Size | 100×100 |
| LevelCompleteCanvas | 2 (World Space) | Constant Pixel Size | 100×100 |
| LevelFailedCanvas | 2 (World Space) | Constant Pixel Size | 100×100 |

**Проблема:** Канвасы отрисовываются как 100×100-юнитовые карточки в 3D-мире у начала координат. На экране не видны или выглядят как крошечные объекты. CanvasScaler в режиме Constant Pixel Size не масштабируется под разрешение экрана.

**Фикс:**
- `Canvas.renderMode` → `0` (Screen Space Overlay)
- `CanvasScaler.uiScaleMode` → `1` (Scale With Screen Size)
- `CanvasScaler.referenceResolution` → `1920×1080`
- `CanvasScaler.matchWidthOrHeight` → `0.5`
- После переключения RectTransform обновится автоматически

---

### BUG-02 — PauseCanvas: activeSelf = false → Show() не работает

**Сцены:** все `Gameplay*.unity` (6 штук)

**Состояние в сцене:**
- `activeSelf: false`
- `localScale: (0, 0, 0)` (Canvas-driven scale не был проинициализирован)
- `CanvasGroup.alpha: 1` (но объект неактивен — не рендерится)

**Проблема:** `PausePresenter.OnPauseRequested()` вызывает `_view.Show()`, который устанавливает `CanvasGroup.alpha = 1`. Но так как GameObject неактивен, Canvas не рендерится. Пауза нажимается, `Time.timeScale = 0`, но меню паузы не появляется. Нажать Resume/Restart/BackToMenu невозможно — игра намертво зависает на паузе.

**Дополнительно:** в Unity `Awake()` не вызывается на неактивных объектах при загрузке сцены. Если `_canvasGroup` не был присвоен в инспекторе — NPE при `Initialize()`. В данном случае `_canvasGroup` присвоен корректно, но кнопки не подписаны на клики пока объект неактивен (хотя в данном случае это не критично т.к. кнопки рабочие, если объект будет активен).

**Фикс:**
- `PauseCanvas.activeSelf` → `true`
- CanvasGroup при старте будет установлен через `PausePresenter.Initialize()` → `_view.Hide()` → alpha=0

---

### BUG-03 — _canvasGroup не назначен в инспекторе (4 View)

**Сцены:** все `Gameplay*.unity`

| View | Canvas | canvasGroup в инспекторе |
|------|--------|--------------------------|
| HudView | HudCanvas | `null` |
| LevelCompleteView | LevelCompleteCanvas | `null` |
| LevelFailedView | LevelFailedCanvas | `null` |
| TowerInfoView | TowerInfoCanvas | `null` |

**Проблема:** `DisplayableView.Awake()` имеет фолбэк `GetComponent<CanvasGroup>()`, который сработает при активном объекте. Однако это хрупкое решение: если в иерархии появится лишний CanvasGroup (например, на дочернем элементе), или порядок инициализации нарушится — Show/Hide молча не сработают.

**Фикс:** явно назначить `_canvasGroup` в инспекторе для каждого из четырёх View.

---

## Средние баги

### BUG-04 — BuildMenu и TowerInfoCanvas: одинаковый sortingOrder = 10

**Сцены:** все `Gameplay*.unity`

При одновременном отображении (теоретически невозможно по логике Presenter-ов, но не гарантировано Canvas-системой) порядок отрисовки не определён. BuildMenu открывается при клике на пустой слот, TowerInfo — при клике на башню. Если через рейкаст или баг оба окажутся видимы — z-порядок undefined.

**Фикс:** `TowerInfoCanvas.sortingOrder` → `20`

---

### BUG-05 — Несогласованный matchWidthOrHeight у CanvasScaler

| Canvas | uiScaleMode | referenceResolution | matchWidthOrHeight |
|--------|------------|--------------------|--------------------|
| Menu/Canvas | Scale With Screen Size | 1920×1080 | **0.5** ✓ |
| PauseCanvas | Scale With Screen Size | 1920×1080 | **0.5** ✓ |
| BuildMenu | Scale With Screen Size | 1920×1080 | **0.0** — только ширина |
| TowerInfoCanvas | Scale With Screen Size | 1920×1080 | **0.0** — только ширина |
| HudCanvas | (после фикса BUG-01) | — | → нужно **0.5** |
| LevelCompleteCanvas | (после фикса BUG-01) | — | → нужно **0.5** |
| LevelFailedCanvas | (после фикса BUG-01) | — | → нужно **0.5** |

При `matchWidthOrHeight: 0` (match width) на портретном экране (1536×2048 по данным редактора) UI будет сильно масштабирован в ширину и сжат по высоте — кнопки могут выйти за экран или наложиться.

**Фикс:** установить `matchWidthOrHeight: 0.5` для BuildMenu и TowerInfoCanvas.

---

### BUG-06 — LevelCompleteCanvas: m_PresetInfoIsWorld = true на CanvasScaler

Редакторный артефакт — говорит редактору считать пресет World Space. После переключения renderMode на Overlay нужно убедиться, что это значение сброшено (Unity делает это автоматически при переключении через инспектор).

---

## Проверено — OK

| Элемент | Статус |
|---------|--------|
| Menu Canvas: renderMode Overlay, Scale With Screen Size 1920×1080, match 0.5 | ✓ |
| PauseCanvas: renderMode Overlay, sortingOrder 50, Scale With Screen Size 1920×1080 | ✓ |
| BuildMenu: renderMode Overlay, sortingOrder 10, CanvasGroup + View настроены | ✓ |
| TowerInfoCanvas: renderMode Overlay, sortingOrder 10, CanvasGroup + View настроены | ✓ |
| DisplayableView.Hide() в Awake всех View (LevelCompleteView, LevelFailedView, PauseView) | ✓ |
| HudPresenter.Hide() при AllWavesCompleted и LevelFailed | ✓ |
| BuildMenuPresenter.Close() при LevelFailed и AllWavesCompleted | ✓ |
| TowerInfoPresenter.Close() при LevelFailed и AllWavesCompleted | ✓ |
| PausePresenter.ForceHide() при LevelFailed | ✓ |
| WorldTapRouter: EventSystem.IsPointerOverGameObject() блокирует тапы под UI | ✓ |

---

## План фиксов (приоритетный порядок)

### P0 — Обязательно до тестирования

| # | Что | Где | Действие |
|---|-----|-----|---------|
| F1 | HudCanvas renderMode | Gameplay + L2..L5 | Screen Space Overlay, CanvasScaler Scale With Screen Size 1920×1080 match 0.5 |
| F2 | LevelCompleteCanvas renderMode | Gameplay + L2..L5 | то же |
| F3 | LevelFailedCanvas renderMode | Gameplay + L2..L5 | то же |
| F4 | PauseCanvas activeSelf | Gameplay + L2..L5 | true; CanvasGroup начальное состояние — alpha:0, interactable:false, blocksRaycasts:false |

### P1 — Качество / надёжность

| # | Что | Где | Действие |
|---|-----|-----|---------|
| F5 | _canvasGroup назначить явно | HudView, LevelCompleteView, LevelFailedView, TowerInfoView | назначить CanvasGroup в поле инспектора |
| F6 | TowerInfoCanvas sortingOrder | Gameplay + L2..L5 | 20 вместо 10 |
| F7 | BuildMenu matchWidthOrHeight | Gameplay + L2..L5 | 0.5 |
| F8 | TowerInfoCanvas matchWidthOrHeight | Gameplay + L2..L5 | 0.5 |

### P2 — После фиксов: дополнительная проверка

- Убедиться, что все дочерние элементы HudCanvas (GoldLabel, BaseHpLabel, WaveLabel, PauseButton, EarlyStartButton) имеют корректные якоря (stretch или фиксированные углы экрана)
- Убедиться, что LevelCompleteCanvas и LevelFailedCanvas растянуты на весь экран и перекрывают игровую камеру (добавить полупрозрачный фоновый Image если нет)
- Проверить Gameplay_L2 .. Gameplay_L5 — применить F1–F8 к каждой сцене

---

## Затронутые файлы

```
Assets/Game/Scenes/Gameplay.unity
Assets/Game/Scenes/Gameplay_L2.unity
Assets/Game/Scenes/Gameplay_L3.unity
Assets/Game/Scenes/Gameplay_L4.unity
Assets/Game/Scenes/Gameplay_L5.unity
```

Скрипты изменять не требуется — все баги на уровне сцены.
