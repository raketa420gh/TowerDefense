# Kenney UI Pack — Справочник ассетов

**Путь:** `Assets/Game/Content/kenney_ui-pack/`  
**Лицензия:** CC0 (Public Domain)  
**Всего файлов:** ~1315

---

## Структура каталогов

```
kenney_ui-pack/
├── Font/          ← 2 шрифта TTF
├── PNG/           ← растровые спрайты (Default + Double разрешения)
│   ├── Blue/
│   ├── Green/
│   ├── Grey/
│   ├── Red/
│   ├── Yellow/
│   └── Extra/     ← дополнительные элементы
├── Sounds/        ← 6 звуков UI
└── Vector/        ← SVG-версии (те же имена, те же папки)
```

---

## Шрифты

| Файл | Назначение |
|------|-----------|
| `Font/Kenney Future.ttf` | Основной — широкий, для заголовков |
| `Font/Kenney Future Narrow.ttf` | Компактный — для кнопок, HUD |

---

## Цветовые темы

Каждая тема содержит **одинаковый набор** спрайтов в двух разрешениях:
- `Default/` — стандартное разрешение
- `Double/` — двойное разрешение (для ретина/высокий DPI)

| Тема | Папка | Применение |
|------|-------|-----------|
| Синяя | `PNG/Blue/` | Основной акцент, активные элементы |
| Зелёная | `PNG/Green/` | Подтверждение, успех, старт |
| Серая | `PNG/Grey/` | Неактивные/disabled элементы |
| Красная | `PNG/Red/` | Опасность, закрыть, враг |
| Жёлтая | `PNG/Yellow/` | Предупреждение, золото, звёзды |

---

## Категории спрайтов (82 файла на тему)

### Кнопки — `button_*`

Три формы × стили normal / depth (с объёмом):

| Форма | Пример имени |
|-------|-------------|
| Прямоугольная | `button_rectangle_flat.png` |
| Круглая | `button_round_depth_gloss.png` |
| Квадратная | `button_square_gradient.png` |

**Стили отделки:**
- `flat` — однотонная заливка
- `gloss` — блик сверху
- `gradient` — градиент
- `line` — с контурной линией
- `border` — только рамка
- `depth_*` — объёмная версия каждого стиля выше

> Кнопки — **9-slice sprites** (растягиваются). Настрой `Border` в импорте Unity.

---

### Чекбоксы — `check_*`

| Файл | Описание |
|------|---------|
| `check_square_color.png` | Полный чекбокс (фон + галочка) |
| `check_square_color_checkmark.png` | Только галочка |
| `check_square_color_cross.png` | Только крестик |
| `check_square_color_square.png` | Только фон |
| `check_square_grey_*` | Серая (неактивная) версия |
| `check_round_color.png` | Круглый радиобаттон |
| `check_round_grey.png` | Серый радиобаттон |
| `check_round_grey_circle.png` | Кружок (фон) |
| `check_round_round_circle.png` | Кружок выбранный |

---

### Слайдеры — `slide_*`

| Файл | Описание |
|------|---------|
| `slide_hangle.png` | Ручка слайдера (handle) |
| `slide_horizontal_color.png` | Горизонтальный трек, цветной |
| `slide_horizontal_color_section.png` | Секция трека |
| `slide_horizontal_color_section_wide.png` | Широкая секция |
| `slide_horizontal_grey_*` | Серые версии |
| `slide_vertical_*` | Вертикальные версии (те же варианты) |

---

### Иконки — `icon_*`

| Файл | Описание |
|------|---------|
| `icon_checkmark.png` | Галочка |
| `icon_cross.png` | Крестик |
| `icon_circle.png` | Круг |
| `icon_square.png` | Квадрат |
| `icon_outline_checkmark.png` | Галочка (контур) |
| `icon_outline_cross.png` | Крестик (контур) |
| `icon_outline_circle.png` | Круг (контур) |
| `icon_outline_square.png` | Квадрат (контур) |

---

### Стрелки — `arrow_*`

| Суффикс | Описание |
|---------|---------|
| `arrow_basic_{e/n/s/w}.png` | Простая стрелка (Восток/Север/Юг/Запад) |
| `arrow_basic_{dir}_small.png` | Маленькая версия |
| `arrow_decorative_{dir}.png` | Декоративная (с хвостом) |
| `arrow_decorative_{dir}_small.png` | Маленькая декоративная |

---

### Звёзды — `star_*`

| Файл | Описание |
|------|---------|
| `star.png` | Заполненная звезда |
| `star_outline.png` | Контур звезды |
| `star_outline_depth.png` | Контур с тенью |

> Использовать для рейтинга (1–3 звезды за уровень).

---

## Extra — дополнительные элементы

**Путь:** `PNG/Extra/Default/` и `PNG/Extra/Double/`

| Файл | Описание |
|------|---------|
| `divider.png` | Горизонтальный разделитель |
| `divider_edges.png` | Разделитель с закруглёнными краями |
| `input_rectangle.png` | Поле ввода прямоугольное |
| `input_square.png` | Поле ввода квадратное |
| `input_outline_rectangle.png` | Поле ввода — только рамка |
| `input_outline_square.png` | Поле ввода квадратное — только рамка |
| `button_rectangle_depth_line.png` | Кнопки line без цветной темы |
| `button_round_depth_line.png` | — |
| `button_square_depth_line.png` | — |
| `button_rectangle_line.png` | — |
| `button_round_line.png` | — |
| `button_square_line.png` | — |
| `icon_arrow_down_dark.png` | Стрелка вниз тёмная |
| `icon_arrow_down_light.png` | Стрелка вниз светлая |
| `icon_arrow_down_outline.png` | Стрелка вниз контур |
| `icon_arrow_up_dark.png` | Стрелка вверх тёмная |
| `icon_arrow_up_light.png` | Стрелка вверх светлая |
| `icon_arrow_up_outline.png` | Стрелка вверх контур |
| `icon_play_dark.png` | Иконка Play тёмная |
| `icon_play_light.png` | Иконка Play светлая |
| `icon_play_outline.png` | Иконка Play контур |
| `icon_repeat_dark.png` | Иконка Повтор тёмная |
| `icon_repeat_light.png` | Иконка Повтор светлая |
| `icon_repeat_outline.png` | Иконка Повтор контур |

---

## Звуки

**Путь:** `Sounds/`

| Файл | Описание | Применение |
|------|---------|-----------|
| `click-a.ogg` | Клик (вариант A) | Кнопки подтверждения |
| `click-b.ogg` | Клик (вариант B) | Кнопки отмены/закрыть |
| `switch-a.ogg` | Переключение (A) | Toggle, чекбоксы ON |
| `switch-b.ogg` | Переключение (B) | Toggle, чекбоксы OFF |
| `tap-a.ogg` | Тап (A) | Лёгкие нажатия, hover |
| `tap-b.ogg` | Тап (B) | Лёгкие нажатия, иконки |

---

## SVG-версии

**Путь:** `Vector/{Blue|Green|Grey|Red|Yellow}/`

Идентичные имена файлов, что и в `PNG/`. Используй только если нужна векторная масштабируемость (Unity не импортирует SVG нативно без пакета).

---

## Рекомендации по использованию в Tower Defense

| UI элемент | Рекомендуемый ассет |
|-----------|-------------------|
| Кнопка «Старт/Play» | `Green/button_rectangle_depth_flat` + `Extra/icon_play_light` |
| Кнопка «Пауза» | `Blue/button_square_depth_flat` |
| Кнопка «Закрыть/Exit» | `Red/button_round_depth_flat` + `Blue/icon_cross` |
| Кнопка «Повтор» | `Yellow/button_rectangle_depth_flat` + `Extra/icon_repeat_light` |
| Кнопка неактивная | `Grey/button_rectangle_depth_flat` |
| Чекбокс настроек | `Blue/check_square_color` (комплект из 4 частей) |
| Слайдер громкости | `Blue/slide_horizontal_color` + `slide_hangle` |
| Звёзды завершения уровня | `Yellow/star` + `Grey/star_outline` |
| Разделители в меню | `Extra/divider` или `Extra/divider_edges` |
| Заголовки | Шрифт `Kenney Future.ttf` |
| Текст кнопок | Шрифт `Kenney Future Narrow.ttf` |

---

## Импорт в Unity

1. **Кнопки** — Texture Type: `Sprite (2D and UI)`, Mesh Type: `Full Rect`, включить `Generate Mip Maps: OFF`. Настроить **Border** для 9-slice.
2. **Иконки/звёзды** — Texture Type: `Sprite`, Mesh Type: `Tight`.
3. **Шрифты** — использовать через `TextMeshPro` (создать TMP Font Asset из `.ttf`).
4. **Звуки** — импортируются как `.ogg`, Load Type: `Decompress On Load` (они короткие).
