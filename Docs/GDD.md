# Tower Defense — Game Design Document

**Платформа:** Android (ландшафт)
**Движок:** Unity 6 (6000.2.13f1), URP 17.2.0
**DI:** Zenject 9.2.0
**Ассеты:** `kenney_tower-defense-kit` (CC0)
**Цель:** 25 уровней с прогрессией сложности; сессии 3–6 минут.

---

## 1. Концепция

Классический Tower Defense: враги (UFO из Kenney-пака) идут по заранее заданному пути от точки спавна к базе. Игрок ставит башни на слоты, тратит золото, удерживает волны. Победа — пережить все волны; поражение — любой враг достигает базы.

### 1.1 Ключевые фичи
- 4 типа башен: ballista, cannon, catapult, turret.
- 5 типов врагов + босс.
- 25 уровней, плавный рост сложности.
- Разветвлённые пути со Split Path начиная с уровня 4; 3 пути — с уровня 19.
- Мобильные сессии 3–6 мин.

### 1.2 Целевая аудитория
Казуальные мобильные игроки, фанаты TD (Bloons TD, Kingdom Rush).

---

## 2. Core Loop

```
Меню → Выбор уровня → Подготовка → Волна → Перерыв → … → Победа/Поражение → Награда → Меню
```

**Механики:**
1. **Path** — waypoints + NavMesh.
2. **WaveSpawner** — читает WaveConfig SO. SubWaves внутри волны спавнятся **последовательно** (не параллельно). Перерыв стартует после гибели последнего врага волны.
3. **Башни** — слоты, цена, атака (range/damage/fireRate), апгрейд до уровня 3, продажа за 70%.
4. **Враги** — HP, speed, reward, baseDamage, движение по waypoints.
5. **Снаряды** — ProjectilePool, урон по попаданию.
6. **Экономика** — стартовое золото, kill reward, wave bonus.
7. **База** — HP, проигрыш при нуле.
8. **Прогресс** — разблокировка, звёзды 0–3 по оставшемуся % HP базы.

---

## 3. Башни

### 3.1 Базовые параметры (TowerConfig SO)

| Башня | Cost | Damage | Range | FireRate | DPS | SplashRadius | SlowMult | SlowDur |
|-------|------|--------|-------|----------|-----|-------------|---------|--------|
| Ballista | 50 | 15 | 5.0 | 1.0 | 15 | 0 | 1.0 | 0 |
| Cannon | 100 | 40 | 4.5 | 0.5 | 20 | 2.0 | 1.0 | 0 |
| Catapult | 150 | 60 | 6.0 | 0.33 | 20 | 3.0 | 0.6 | 2.0 |
| Turret | 120 | 8 | 3.5 | 2.5 | 20 | 0 | 1.0 | 0 |

### 3.2 Апгрейды

Формула (реализована в `TowerConfig.cs`):
- Damage(level) = `baseDamage × (1 + 0.25 × (level−1))`
- Range(level) = `baseRange × (1 + 0.10 × (level−1))`
- UpgradeCost(current) = `baseCost × 1.5 × currentLevel`

| Переход | Ballista | Cannon | Catapult | Turret |
|---------|----------|--------|---------|--------|
| 1→2 | 75g | 150g | 225g | 180g |
| 2→3 | 150g | 300g | 450g | 360g |

### 3.3 Разблокировка по уровням

| Уровень игры | Новая башня |
|-------------|------------|
| 1 | Ballista |
| 2 | Cannon |
| 4 | Catapult |
| 5 | Turret |

---

## 4. Враги

### 4.1 EnemyConfig SO

| ID | MaxHealth | Speed | BaseDamage | Reward | VisualScale |
|----|-----------|-------|-----------|--------|-------------|
| ufo-a | 50 | 2.0 | 1 | 10 | 1.0 |
| ufo-b | 120 | 1.5 | 1 | 20 | 1.0 |
| ufo-c | 40 | 3.5 | 1 | 15 | 1.0 |
| ufo-d | 200 | 1.0 | 2 | 35 | 1.0 |
| boss | 2000 | 0.8 | 10 | 200 | 2.5 |

---

## 5. Прогрессия — 25 уровней

### 5.1 Обзорная таблица (LevelConfig)

> **Тирование сессий:** Tier1 (L1–5) ≈3:00 · Tier2 (L6–10) ≈3:30 · Tier3 (L11–15) ≈4:30 · Tier4 (L16–20) ≈5:30 · Tier5 (L21–25) ≈6:00

| # | Название | Tier | Волн | Start Gold | Base HP | Слоты в префабе | Новинки |
|---|----------|------|------|-----------|---------|----------------|---------|
| 1 | Старт | 1 | 5 | 200 | 20 | 3 | Tutorial; только Ballista; прямой путь |
| 2 | Поворот | 1 | 6 | 220 | 20 | 3 | +Cannon; путь с поворотами |
| 3 | Рывок | 1 | 6 | 230 | 20 | 3 | ufo-c (быстрые) |
| 4 | Развилка | 1 | 7 | 240 | 18 | 4 | Split path (2 пути); +Catapult |
| 5 | Давление | 1 | 7 | 260 | 18 | 4 | +Turret; ufo-b |
| 6 | Ущелье | 2 | 8 | 260 | 18 | 4 | Узкий длинный путь; mix a/b/c |
| 7 | Лабиринт | 2 | 8 | 270 | 16 | 4 | Извилистый путь; первый ufo-d (L8) |
| 8 | Вихрь | 2 | 9 | 280 | 16 | 5 | 5 слотов; split; ufo-d регулярно |
| 9 | Осада | 2 | 9 | 290 | 16 | 5 | Концентрация ufo-d |
| 10 | Клыки | 2 | 10 | 300 | 15 | 5 | Мини-босс (1 boss в последней волне) |
| 11 | Нашествие | 3 | 10 | 300 | 15 | 5 | Быстрые рои ufo-c |
| 12 | Таран | 3 | 10 | 310 | 14 | 5 | ufo-b спам |
| 13 | Двойной удар | 3 | 11 | 320 | 14 | 6 | 6 слотов; 2 пути одновременно |
| 14 | Ловушка | 3 | 11 | 330 | 14 | 6 | Dense waves; короткие перерывы |
| 15 | Падший | 3 | 12 | 340 | 12 | 6 | Первый полноценный Boss |
| 16 | Легион | 4 | 12 | 340 | 12 | 6 | Крупные волны всех типов |
| 17 | Рой | 4 | 12 | 350 | 12 | 6 | Мегарои ufo-c |
| 18 | Броня | 4 | 13 | 360 | 10 | 7 | 7 слотов; ufo-d спам |
| 19 | Трёхпутье | 4 | 13 | 370 | 10 | 7 | 3 пути (PathIndex 0/1/2) |
| 20 | Командир | 4 | 13 | 380 | 10 | 7 | 2 босса в финальной волне |
| 21 | Элита | 5 | 13 | 390 | 8 | 7 | Элитный состав; boss+d |
| 22 | Прорыв | 5 | 14 | 400 | 8 | 8 | 8 слотов; double boss + swarm |
| 23 | Шторм | 5 | 14 | 410 | 8 | 8 | 3 пути; boss×2 |
| 24 | Апокалипсис | 5 | 15 | 430 | 6 | 8 | 3 пути; 3 босса в финале |
| 25 | Финал | 5 | 15 | 450 | 5 | 8 | Boss Rush (боссы с волны 10) |

### 5.2 Звёзды (StarCalculator)

| Звёзды | Условие |
|--------|---------|
| ⭐⭐⭐ | HP базы ≥ 100% |
| ⭐⭐ | HP базы ≥ 50% |
| ⭐ | Прохождение |

---

## 6. Детализация волн по уровням

### Нотация

```
[type×N@Xs pP] — subwave: тип врага × кол-во @ интервал сек. на пути P
Несколько subwave в одной волне = последовательный спавн (WaveSpawner)
DelayAfter — пауза ПОСЛЕ гибели последнего врага (сек.)
Reward — бонус золота за завершение волны
```

### Конфиги SO для заполнения

**Путь:** `Assets/Game/Configs/Waves/LevelXX_WaveYY.asset`
**Тип:** `WaveConfig`

Поля `SubWave`: `Enemy` (ссылка на EnemyConfig SO) · `Count` · `Interval` · `PathIndex`
Поля `WaveConfig`: `SubWaves[]` · `DelayAfter` · `Reward`

---

### Level 1 — Старт
**LevelConfig:** StartGold=200 | BaseHP=20 | Waves=5 | TowerSlots=3 (в префабе)

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×5@1.2s p0] | 8 | 20 |
| 2 | [a×8@1.0s p0] | 8 | 25 |
| 3 | [a×10@0.9s p0] | 7 | 28 |
| 4 | [a×12@0.8s p0] | 7 | 32 |
| 5 | [a×15@0.7s p0] | 5 | 50 |

---

### Level 2 — Поворот
**LevelConfig:** StartGold=220 | BaseHP=20 | Waves=6 | TowerSlots=3

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×6@1.1s p0] | 8 | 22 |
| 2 | [a×9@1.0s p0] | 8 | 26 |
| 3 | [a×11@0.9s p0] | 7 | 30 |
| 4 | [a×10@0.8s p0] | 7 | 32 |
| 5 | [a×12@0.8s p0] | 7 | 38 |
| 6 | [a×14@0.7s p0] | 5 | 55 |

---

### Level 3 — Рывок
**LevelConfig:** StartGold=230 | BaseHP=20 | Waves=6 | TowerSlots=3

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×8@1.0s p0] | 8 | 25 |
| 2 | [a×6@0.9s p0] · [c×4@0.8s p0] | 7 | 30 |
| 3 | [a×8@0.8s p0] · [c×5@0.7s p0] | 7 | 33 |
| 4 | [c×8@0.6s p0] | 7 | 35 |
| 5 | [a×10@0.8s p0] · [c×6@0.6s p0] | 7 | 40 |
| 6 | [a×8@0.7s p0] · [c×8@0.5s p0] | 5 | 60 |

---

### Level 4 — Развилка
**LevelConfig:** StartGold=240 | BaseHP=18 | Waves=7 | TowerSlots=4
> Первый split-path. PathIndex 0 и 1 — разные маршруты в префабе уровня.

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×8@1.0s p0] · [a×6@1.0s p1] | 7 | 30 |
| 2 | [a×8@0.9s p0] · [c×5@0.8s p1] | 7 | 35 |
| 3 | [a×10@0.8s p0] · [c×6@0.7s p1] | 7 | 38 |
| 4 | [a×8@0.8s p0] · [b×2@1.5s p1] | 7 | 40 |
| 5 | [a×10@0.7s p0] · [c×7@0.6s p1] | 7 | 45 |
| 6 | [b×3@1.3s p0] · [c×6@0.6s p1] | 6 | 50 |
| 7 | [a×10@0.7s p0] · [b×3@1.2s p1] · [c×5@0.5s p0] | 5 | 70 |

---

### Level 5 — Давление
**LevelConfig:** StartGold=260 | BaseHP=18 | Waves=7 | TowerSlots=4

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×10@0.9s p0] · [a×8@0.9s p1] | 7 | 32 |
| 2 | [a×8@0.8s p0] · [c×6@0.7s p1] | 7 | 36 |
| 3 | [b×3@1.3s p0] · [a×8@0.8s p1] | 7 | 40 |
| 4 | [a×10@0.7s p0] · [c×8@0.6s p1] | 7 | 44 |
| 5 | [b×4@1.2s p0] · [c×7@0.6s p1] | 7 | 48 |
| 6 | [a×10@0.7s p0] · [b×4@1.1s p1] | 7 | 52 |
| 7 | [b×5@1.0s p0] · [c×10@0.5s p1] | 5 | 80 |

---

### Level 6 — Ущелье
**LevelConfig:** StartGold=260 | BaseHP=18 | Waves=8 | TowerSlots=4

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×10@0.8s p0] | 7 | 30 |
| 2 | [a×8@0.8s p0] · [c×6@0.7s p0] | 7 | 35 |
| 3 | [b×3@1.2s p0] · [a×8@0.8s p0] | 7 | 40 |
| 4 | [c×10@0.6s p0] · [a×8@0.8s p0] | 6 | 44 |
| 5 | [b×4@1.1s p0] · [c×6@0.6s p0] | 6 | 48 |
| 6 | [a×12@0.7s p0] · [b×3@1.1s p0] | 6 | 52 |
| 7 | [c×12@0.5s p0] · [b×4@1.0s p0] | 6 | 56 |
| 8 | [b×5@1.0s p0] · [c×10@0.5s p0] · [a×10@0.7s p0] | 5 | 85 |

---

### Level 7 — Лабиринт
**LevelConfig:** StartGold=270 | BaseHP=16 | Waves=8 | TowerSlots=4

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×10@0.8s p0] · [c×5@0.7s p0] | 7 | 32 |
| 2 | [a×8@0.8s p0] · [b×3@1.2s p0] | 6 | 37 |
| 3 | [c×10@0.6s p0] · [a×8@0.8s p0] | 6 | 42 |
| 4 | [b×4@1.1s p0] · [c×6@0.6s p0] | 6 | 46 |
| 5 | [a×12@0.7s p0] · [c×8@0.6s p0] | 6 | 50 |
| 6 | [b×5@1.0s p0] · [a×10@0.7s p0] | 6 | 55 |
| 7 | [c×12@0.5s p0] · [b×4@1.0s p0] | 6 | 58 |
| 8 | [b×6@1.0s p0] · [c×10@0.5s p0] · [d×1@0s p0] | 5 | 90 |

---

### Level 8 — Вихрь
**LevelConfig:** StartGold=280 | BaseHP=16 | Waves=9 | TowerSlots=5

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×10@0.8s p0] · [a×8@0.8s p1] | 7 | 34 |
| 2 | [a×8@0.7s p0] · [c×7@0.6s p1] | 6 | 38 |
| 3 | [b×4@1.1s p0] · [c×8@0.6s p1] | 6 | 44 |
| 4 | [a×12@0.7s p0] · [b×4@1.0s p1] | 6 | 48 |
| 5 | [c×12@0.5s p0] · [b×5@1.0s p1] | 6 | 52 |
| 6 | [b×5@1.0s p0] · [a×12@0.7s p1] | 6 | 56 |
| 7 | [d×2@2.0s p0] · [c×10@0.5s p1] | 6 | 60 |
| 8 | [b×6@0.9s p0] · [d×2@1.8s p1] | 6 | 65 |
| 9 | [d×3@1.5s p0] · [c×12@0.5s p1] | 5 | 100 |

---

### Level 9 — Осада
**LevelConfig:** StartGold=290 | BaseHP=16 | Waves=9 | TowerSlots=5

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×12@0.8s p0] · [a×10@0.8s p1] | 6 | 35 |
| 2 | [a×10@0.7s p0] · [c×8@0.6s p1] | 6 | 40 |
| 3 | [b×4@1.1s p0] · [c×8@0.6s p1] | 6 | 45 |
| 4 | [d×2@2.0s p0] · [a×10@0.7s p1] | 6 | 50 |
| 5 | [c×12@0.5s p0] · [b×5@1.0s p1] | 6 | 54 |
| 6 | [d×2@1.8s p0] · [b×5@1.0s p1] | 6 | 58 |
| 7 | [b×6@0.9s p0] · [c×12@0.5s p1] | 6 | 62 |
| 8 | [d×3@1.5s p0] · [b×4@1.0s p1] | 6 | 66 |
| 9 | [d×3@1.3s p0] · [c×15@0.4s p1] | 5 | 100 |

---

### Level 10 — Клыки
**LevelConfig:** StartGold=300 | BaseHP=15 | Waves=10 | TowerSlots=5

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×12@0.8s p0] · [c×8@0.6s p1] | 6 | 36 |
| 2 | [b×4@1.1s p0] · [a×10@0.7s p1] | 6 | 42 |
| 3 | [c×12@0.5s p0] · [b×4@1.0s p1] | 6 | 47 |
| 4 | [d×2@1.8s p0] · [a×12@0.7s p1] | 6 | 52 |
| 5 | [b×6@0.9s p0] · [c×12@0.5s p1] | 6 | 57 |
| 6 | [d×3@1.5s p0] · [b×5@1.0s p1] | 6 | 62 |
| 7 | [a×15@0.6s p0] · [d×3@1.4s p1] | 6 | 67 |
| 8 | [b×6@0.9s p0] · [c×15@0.4s p1] | 6 | 72 |
| 9 | [d×4@1.2s p0] · [b×6@0.9s p1] | 5 | 80 |
| 10 | [d×2@1.5s p0] · [boss×1@0s p0] | 5 | 200 |

---

### Level 11 — Нашествие
**LevelConfig:** StartGold=300 | BaseHP=15 | Waves=10 | TowerSlots=5

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [c×10@0.7s p0] · [a×10@0.7s p1] | 6 | 36 |
| 2 | [b×5@1.0s p0] · [c×10@0.6s p1] | 6 | 44 |
| 3 | [a×12@0.7s p0] · [c×12@0.5s p1] | 5 | 50 |
| 4 | [b×5@1.0s p0] · [d×2@1.8s p1] | 5 | 55 |
| 5 | [c×15@0.5s p0] · [a×12@0.7s p1] | 5 | 60 |
| 6 | [d×3@1.5s p0] · [c×12@0.5s p1] | 5 | 65 |
| 7 | [b×7@0.9s p0] · [c×12@0.5s p1] | 5 | 70 |
| 8 | [d×3@1.3s p0] · [b×6@0.9s p1] | 5 | 75 |
| 9 | [c×20@0.4s p0] · [d×3@1.3s p1] | 5 | 80 |
| 10 | [d×4@1.2s p0] · [c×15@0.4s p1] | 5 | 110 |

---

### Level 12 — Таран
**LevelConfig:** StartGold=310 | BaseHP=14 | Waves=10 | TowerSlots=5

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [b×5@1.0s p0] · [a×12@0.7s p1] | 6 | 40 |
| 2 | [b×6@1.0s p0] · [c×10@0.6s p1] | 5 | 48 |
| 3 | [b×6@0.9s p0] · [d×2@1.8s p1] | 5 | 55 |
| 4 | [b×7@0.9s p0] · [c×12@0.5s p1] | 5 | 60 |
| 5 | [d×3@1.4s p0] · [b×6@0.9s p1] | 5 | 65 |
| 6 | [b×8@0.8s p0] · [d×3@1.3s p1] | 5 | 70 |
| 7 | [d×4@1.2s p0] · [b×7@0.8s p1] | 5 | 75 |
| 8 | [b×8@0.8s p0] · [c×15@0.4s p1] | 5 | 80 |
| 9 | [d×4@1.1s p0] · [b×8@0.8s p1] | 5 | 85 |
| 10 | [d×5@1.0s p0] · [b×8@0.7s p1] | 5 | 120 |

---

### Level 13 — Двойной удар
**LevelConfig:** StartGold=320 | BaseHP=14 | Waves=11 | TowerSlots=6

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×10@0.8s p0] · [a×10@0.8s p1] | 5 | 40 |
| 2 | [b×5@1.0s p0] · [c×10@0.6s p1] | 5 | 48 |
| 3 | [c×12@0.5s p0] · [b×5@1.0s p1] | 5 | 55 |
| 4 | [d×3@1.5s p0] · [a×12@0.7s p1] | 5 | 60 |
| 5 | [b×7@0.8s p0] · [c×12@0.5s p1] | 5 | 65 |
| 6 | [d×3@1.3s p0] · [b×6@0.9s p1] | 5 | 70 |
| 7 | [c×15@0.4s p0] · [d×3@1.3s p1] | 5 | 75 |
| 8 | [b×7@0.8s p0] · [d×4@1.2s p1] | 5 | 80 |
| 9 | [d×4@1.1s p0] · [c×15@0.4s p1] | 5 | 85 |
| 10 | [b×8@0.8s p0] · [d×4@1.1s p1] | 5 | 90 |
| 11 | [d×5@1.0s p0] · [d×5@1.0s p1] | 5 | 130 |

---

### Level 14 — Ловушка
**LevelConfig:** StartGold=330 | BaseHP=14 | Waves=11 | TowerSlots=6

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×12@0.7s p0] · [c×10@0.6s p1] | 5 | 42 |
| 2 | [b×6@0.9s p0] · [a×12@0.7s p1] | 5 | 50 |
| 3 | [c×12@0.5s p0] · [b×6@0.9s p1] | 5 | 58 |
| 4 | [d×3@1.4s p0] · [c×12@0.5s p1] | 5 | 64 |
| 5 | [b×8@0.8s p0] · [d×3@1.3s p1] | 4 | 70 |
| 6 | [c×15@0.4s p0] · [b×7@0.8s p1] | 4 | 75 |
| 7 | [d×4@1.2s p0] · [c×15@0.4s p1] | 4 | 80 |
| 8 | [b×8@0.7s p0] · [d×4@1.1s p1] | 4 | 85 |
| 9 | [d×5@1.0s p0] · [b×8@0.7s p1] | 4 | 90 |
| 10 | [c×20@0.4s p0] · [d×4@1.1s p1] | 4 | 95 |
| 11 | [d×5@1.0s p0] · [d×5@1.0s p1] | 4 | 140 |

---

### Level 15 — Падший
**LevelConfig:** StartGold=340 | BaseHP=12 | Waves=12 | TowerSlots=6
> Первый полноценный Boss. Волна 12 — соло-босс.

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×12@0.7s p0] · [c×10@0.6s p1] | 5 | 42 |
| 2 | [b×6@0.9s p0] · [c×10@0.6s p1] | 5 | 50 |
| 3 | [d×3@1.4s p0] · [b×6@0.9s p1] | 5 | 60 |
| 4 | [c×15@0.4s p0] · [d×3@1.3s p1] | 5 | 66 |
| 5 | [b×8@0.8s p0] · [a×12@0.7s p1] | 5 | 72 |
| 6 | [d×4@1.2s p0] · [c×15@0.4s p1] | 4 | 78 |
| 7 | [b×8@0.7s p0] · [d×4@1.1s p1] | 4 | 84 |
| 8 | [c×20@0.3s p0] · [b×8@0.7s p1] | 4 | 90 |
| 9 | [d×5@1.0s p0] · [c×15@0.4s p1] | 4 | 95 |
| 10 | [d×5@1.0s p0] · [d×5@1.0s p1] | 4 | 100 |
| 11 | [b×10@0.7s p0] · [d×5@0.9s p1] | 4 | 110 |
| 12 | [boss×1@0s p0] | 4 | 200 |

---

### Level 16 — Легион
**LevelConfig:** StartGold=340 | BaseHP=12 | Waves=12 | TowerSlots=6

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×15@0.6s p0] · [c×12@0.5s p1] | 5 | 45 |
| 2 | [b×7@0.8s p0] · [a×12@0.7s p1] | 5 | 55 |
| 3 | [c×15@0.5s p0] · [b×7@0.8s p1] | 4 | 62 |
| 4 | [d×4@1.3s p0] · [c×15@0.5s p1] | 4 | 70 |
| 5 | [b×8@0.8s p0] · [d×3@1.3s p1] | 4 | 76 |
| 6 | [c×18@0.4s p0] · [b×8@0.7s p1] | 4 | 82 |
| 7 | [d×4@1.2s p0] · [c×18@0.4s p1] | 4 | 88 |
| 8 | [b×9@0.7s p0] · [d×4@1.1s p1] | 4 | 94 |
| 9 | [d×5@1.0s p0] · [b×9@0.7s p1] | 4 | 100 |
| 10 | [c×20@0.3s p0] · [d×5@1.0s p1] | 4 | 106 |
| 11 | [d×6@0.9s p0] · [c×20@0.3s p1] | 4 | 112 |
| 12 | [boss×1@0s p0] · [d×4@1.2s p1] | 4 | 220 |

---

### Level 17 — Рой
**LevelConfig:** StartGold=350 | BaseHP=12 | Waves=12 | TowerSlots=6

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [c×15@0.5s p0] · [a×15@0.6s p1] | 5 | 48 |
| 2 | [c×15@0.5s p0] · [c×15@0.5s p1] | 4 | 58 |
| 3 | [b×7@0.8s p0] · [c×15@0.4s p1] | 4 | 65 |
| 4 | [c×20@0.4s p0] · [b×7@0.8s p1] | 4 | 72 |
| 5 | [d×4@1.2s p0] · [c×20@0.4s p1] | 4 | 80 |
| 6 | [c×20@0.3s p0] · [d×4@1.1s p1] | 4 | 86 |
| 7 | [b×9@0.7s p0] · [c×20@0.3s p1] | 4 | 92 |
| 8 | [d×5@1.0s p0] · [b×9@0.7s p1] | 4 | 98 |
| 9 | [c×25@0.3s p0] · [d×5@1.0s p1] | 4 | 104 |
| 10 | [d×5@0.9s p0] · [c×25@0.3s p1] | 4 | 110 |
| 11 | [b×10@0.7s p0] · [d×5@0.9s p1] | 4 | 116 |
| 12 | [boss×1@0s p0] · [c×20@0.3s p1] | 4 | 225 |

---

### Level 18 — Броня
**LevelConfig:** StartGold=360 | BaseHP=10 | Waves=13 | TowerSlots=7

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [d×3@1.5s p0] · [b×8@0.8s p1] | 5 | 55 |
| 2 | [b×8@0.8s p0] · [d×3@1.4s p1] | 4 | 64 |
| 3 | [d×4@1.3s p0] · [b×8@0.7s p1] | 4 | 72 |
| 4 | [b×9@0.7s p0] · [d×4@1.2s p1] | 4 | 80 |
| 5 | [d×5@1.1s p0] · [c×20@0.3s p1] | 4 | 88 |
| 6 | [c×20@0.3s p0] · [d×5@1.0s p1] | 4 | 95 |
| 7 | [d×5@1.0s p0] · [b×10@0.7s p1] | 4 | 102 |
| 8 | [b×10@0.7s p0] · [d×5@0.9s p1] | 4 | 108 |
| 9 | [d×6@0.9s p0] · [c×20@0.3s p1] | 4 | 114 |
| 10 | [c×20@0.3s p0] · [d×6@0.9s p1] | 4 | 120 |
| 11 | [d×6@0.9s p0] · [b×10@0.7s p1] | 4 | 126 |
| 12 | [boss×1@0s p0] · [d×5@1.0s p1] | 4 | 240 |
| 13 | [d×8@0.8s p0] · [d×8@0.8s p1] | 4 | 160 |

---

### Level 19 — Трёхпутье
**LevelConfig:** StartGold=370 | BaseHP=10 | Waves=13 | TowerSlots=7
> Три пути: PathIndex 0, 1, 2 в префабе уровня.

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [a×12@0.7s p0] · [b×6@0.9s p1] · [c×12@0.5s p2] | 5 | 55 |
| 2 | [c×12@0.5s p0] · [a×12@0.7s p1] · [b×7@0.8s p2] | 4 | 65 |
| 3 | [b×7@0.8s p0] · [c×15@0.5s p1] · [d×3@1.5s p2] | 4 | 75 |
| 4 | [d×3@1.4s p0] · [b×8@0.8s p1] · [c×15@0.4s p2] | 4 | 84 |
| 5 | [c×18@0.4s p0] · [d×4@1.2s p1] · [b×8@0.7s p2] | 4 | 92 |
| 6 | [d×4@1.2s p0] · [c×18@0.4s p1] · [b×9@0.7s p2] | 4 | 100 |
| 7 | [b×9@0.7s p0] · [d×5@1.0s p1] · [c×18@0.4s p2] | 4 | 108 |
| 8 | [d×5@1.0s p0] · [b×9@0.7s p1] · [d×4@1.2s p2] | 4 | 116 |
| 9 | [c×20@0.3s p0] · [d×5@1.0s p1] · [b×10@0.7s p2] | 4 | 124 |
| 10 | [d×6@0.9s p0] · [c×20@0.3s p1] · [d×5@1.0s p2] | 4 | 132 |
| 11 | [b×10@0.7s p0] · [d×6@0.9s p1] · [c×20@0.3s p2] | 4 | 140 |
| 12 | [boss×1@0s p0] · [d×6@0.9s p1] · [d×6@0.9s p2] | 4 | 260 |
| 13 | [d×8@0.8s p0] · [d×8@0.8s p1] · [c×25@0.3s p2] | 4 | 180 |

---

### Level 20 — Командир
**LevelConfig:** StartGold=380 | BaseHP=10 | Waves=13 | TowerSlots=7
> Финальная волна — два босса на разных путях.

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [b×8@0.8s p0] · [c×15@0.5s p1] | 5 | 58 |
| 2 | [d×4@1.3s p0] · [b×8@0.7s p1] | 4 | 70 |
| 3 | [c×18@0.4s p0] · [d×4@1.2s p1] | 4 | 80 |
| 4 | [b×9@0.7s p0] · [d×5@1.0s p1] | 4 | 90 |
| 5 | [d×5@1.0s p0] · [c×20@0.3s p1] | 4 | 100 |
| 6 | [c×20@0.3s p0] · [d×5@1.0s p1] | 4 | 110 |
| 7 | [d×6@0.9s p0] · [b×10@0.6s p1] | 4 | 120 |
| 8 | [b×10@0.6s p0] · [d×6@0.9s p1] | 4 | 130 |
| 9 | [c×25@0.3s p0] · [d×6@0.9s p1] | 4 | 140 |
| 10 | [d×7@0.8s p0] · [c×25@0.3s p1] | 4 | 150 |
| 11 | [b×12@0.6s p0] · [d×7@0.8s p1] | 4 | 160 |
| 12 | [d×7@0.8s p0] · [b×12@0.6s p1] | 4 | 170 |
| 13 | [boss×1@0s p0] · [boss×1@0s p1] | 4 | 400 |

---

### Level 21 — Элита
**LevelConfig:** StartGold=390 | BaseHP=8 | Waves=13 | TowerSlots=7

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [b×8@0.7s p0] · [c×18@0.4s p1] | 5 | 60 |
| 2 | [d×5@1.1s p0] · [b×9@0.7s p1] | 4 | 75 |
| 3 | [c×20@0.4s p0] · [d×5@1.0s p1] | 4 | 88 |
| 4 | [b×10@0.7s p0] · [d×6@0.9s p1] | 4 | 100 |
| 5 | [d×6@0.9s p0] · [c×22@0.3s p1] | 4 | 112 |
| 6 | [c×22@0.3s p0] · [d×6@0.9s p1] | 4 | 124 |
| 7 | [d×7@0.8s p0] · [b×12@0.6s p1] | 4 | 135 |
| 8 | [b×12@0.6s p0] · [d×7@0.8s p1] | 4 | 145 |
| 9 | [c×25@0.25s p0] · [d×7@0.8s p1] | 4 | 155 |
| 10 | [d×8@0.8s p0] · [c×25@0.25s p1] | 4 | 165 |
| 11 | [b×14@0.5s p0] · [d×8@0.7s p1] | 4 | 175 |
| 12 | [d×8@0.7s p0] · [b×14@0.5s p1] | 4 | 185 |
| 13 | [boss×1@0s p0] · [d×8@0.7s p1] | 4 | 280 |

---

### Level 22 — Прорыв
**LevelConfig:** StartGold=400 | BaseHP=8 | Waves=14 | TowerSlots=8

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [c×20@0.4s p0] · [b×8@0.7s p1] | 5 | 62 |
| 2 | [b×9@0.7s p0] · [c×20@0.4s p1] | 4 | 78 |
| 3 | [d×5@1.0s p0] · [c×22@0.35s p1] | 4 | 92 |
| 4 | [c×22@0.35s p0] · [d×5@1.0s p1] | 4 | 104 |
| 5 | [b×10@0.6s p0] · [d×6@0.9s p1] | 4 | 116 |
| 6 | [d×6@0.9s p0] · [b×10@0.6s p1] | 4 | 128 |
| 7 | [c×25@0.3s p0] · [d×7@0.8s p1] | 4 | 140 |
| 8 | [d×7@0.8s p0] · [c×25@0.3s p1] | 4 | 152 |
| 9 | [b×12@0.6s p0] · [d×8@0.7s p1] | 4 | 164 |
| 10 | [d×8@0.7s p0] · [b×12@0.6s p1] | 4 | 176 |
| 11 | [c×30@0.25s p0] · [d×8@0.7s p1] | 4 | 188 |
| 12 | [d×9@0.7s p0] · [c×30@0.25s p1] | 4 | 200 |
| 13 | [boss×1@0s p0] · [d×8@0.7s p1] | 4 | 280 |
| 14 | [boss×1@0s p0] · [boss×1@0s p1] | 4 | 400 |

---

### Level 23 — Шторм
**LevelConfig:** StartGold=410 | BaseHP=8 | Waves=14 | TowerSlots=8
> Три пути. SubWave на p2 спавнится после p1.

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [b×8@0.7s p0] · [c×18@0.4s p1] · [a×15@0.5s p2] | 5 | 65 |
| 2 | [c×18@0.4s p0] · [b×8@0.7s p1] · [d×4@1.2s p2] | 4 | 82 |
| 3 | [d×5@1.0s p0] · [c×20@0.35s p1] · [b×9@0.7s p2] | 4 | 98 |
| 4 | [b×9@0.7s p0] · [d×5@1.0s p1] · [c×20@0.35s p2] | 4 | 114 |
| 5 | [d×6@0.9s p0] · [b×10@0.6s p1] · [d×5@1.0s p2] | 4 | 128 |
| 6 | [c×22@0.3s p0] · [d×6@0.9s p1] · [b×10@0.6s p2] | 4 | 142 |
| 7 | [b×10@0.6s p0] · [c×22@0.3s p1] · [d×7@0.8s p2] | 4 | 155 |
| 8 | [d×7@0.8s p0] · [b×12@0.6s p1] · [c×25@0.3s p2] | 4 | 168 |
| 9 | [c×25@0.3s p0] · [d×8@0.7s p1] · [b×12@0.6s p2] | 4 | 180 |
| 10 | [d×8@0.7s p0] · [c×25@0.3s p1] · [d×7@0.8s p2] | 4 | 192 |
| 11 | [b×14@0.5s p0] · [d×8@0.7s p1] · [c×28@0.25s p2] | 4 | 204 |
| 12 | [d×9@0.7s p0] · [b×14@0.5s p1] · [d×8@0.7s p2] | 4 | 215 |
| 13 | [boss×1@0s p0] · [d×8@0.7s p1] · [c×30@0.25s p2] | 4 | 300 |
| 14 | [boss×1@0s p0] · [boss×1@0s p1] · [d×10@0.6s p2] | 4 | 450 |

---

### Level 24 — Апокалипсис
**LevelConfig:** StartGold=430 | BaseHP=6 | Waves=15 | TowerSlots=8
> Три пути. Боссы в волнах 11, 12, 13 — по одному на каждый путь. Волна 15 — тройной босс.

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [b×10@0.6s p0] · [c×20@0.4s p1] · [a×15@0.5s p2] | 5 | 65 |
| 2 | [c×20@0.4s p0] · [b×10@0.6s p1] · [d×5@1.0s p2] | 4 | 82 |
| 3 | [d×5@1.0s p0] · [c×22@0.35s p1] · [b×10@0.6s p2] | 4 | 98 |
| 4 | [b×10@0.6s p0] · [d×6@0.9s p1] · [c×22@0.35s p2] | 4 | 114 |
| 5 | [d×6@0.9s p0] · [b×12@0.6s p1] · [d×5@1.0s p2] | 4 | 128 |
| 6 | [c×25@0.3s p0] · [d×6@0.9s p1] · [b×12@0.6s p2] | 4 | 142 |
| 7 | [d×7@0.8s p0] · [c×25@0.3s p1] · [d×7@0.8s p2] | 4 | 156 |
| 8 | [b×12@0.6s p0] · [d×8@0.7s p1] · [c×28@0.25s p2] | 4 | 170 |
| 9 | [d×8@0.7s p0] · [b×14@0.5s p1] · [d×8@0.7s p2] | 4 | 184 |
| 10 | [c×30@0.25s p0] · [d×8@0.7s p1] · [b×14@0.5s p2] | 4 | 198 |
| 11 | [boss×1@0s p0] · [d×10@0.6s p1] · [c×30@0.25s p2] | 4 | 320 |
| 12 | [d×10@0.6s p0] · [boss×1@0s p1] · [d×10@0.6s p2] | 4 | 320 |
| 13 | [b×15@0.5s p0] · [d×10@0.6s p1] · [boss×1@0s p2] | 4 | 320 |
| 14 | [boss×1@0s p0] · [boss×1@0s p1] · [d×12@0.5s p2] | 4 | 450 |
| 15 | [boss×1@0s p0] · [boss×1@0s p1] · [boss×1@0s p2] | 4 | 600 |

---

### Level 25 — Финал (Boss Rush)
**LevelConfig:** StartGold=450 | BaseHP=5 | Waves=15 | TowerSlots=8
> Boss Rush: боссы с волны 10. Волна 15 — два босса на каждом пути с интервалом 10 сек.

| # | SubWaves | Delay | Reward |
|---|----------|-------|--------|
| 1 | [b×10@0.6s p0] · [c×20@0.35s p1] | 5 | 65 |
| 2 | [c×20@0.35s p0] · [b×10@0.6s p1] | 4 | 82 |
| 3 | [d×6@0.9s p0] · [b×10@0.6s p1] | 4 | 98 |
| 4 | [b×12@0.6s p0] · [d×6@0.9s p1] | 4 | 114 |
| 5 | [c×25@0.3s p0] · [d×8@0.7s p1] | 4 | 130 |
| 6 | [d×8@0.7s p0] · [c×25@0.3s p1] | 4 | 146 |
| 7 | [b×14@0.5s p0] · [d×8@0.7s p1] | 4 | 162 |
| 8 | [d×10@0.6s p0] · [b×14@0.5s p1] | 4 | 178 |
| 9 | [c×30@0.25s p0] · [d×10@0.6s p1] | 4 | 194 |
| 10 | [boss×1@0s p0] · [d×10@0.6s p1] | 3 | 260 |
| 11 | [boss×1@0s p0] · [boss×1@0s p1] | 3 | 400 |
| 12 | [d×12@0.5s p0] · [boss×1@0s p1] | 3 | 280 |
| 13 | [boss×1@0s p0] · [d×12@0.5s p1] | 3 | 280 |
| 14 | [boss×2@10s p0] · [boss×2@10s p1] | 3 | 800 |
| 15 | [boss×1@0s p0] · [boss×1@0s p1] | 3 | 600 |

---

## 7. Экономика — сводка по тирам

| Tier | Уровни | Start Gold | Типичный доход/уровень | Примерный budget башен |
|------|--------|-----------|----------------------|----------------------|
| 1 | 1–5 | 200–260 | 600–900g | 3–4 башни + 1–2 апгрейда |
| 2 | 6–10 | 260–300 | 900–1300g | 4–5 башен + апгрейды |
| 3 | 11–15 | 300–340 | 1200–1700g | 5–6 башен + full апгрейды |
| 4 | 16–20 | 340–380 | 1500–2200g | 6–7 башен + full апгрейды |
| 5 | 21–25 | 380–450 | 2000–3000g | 7–8 башен + full апгрейды |

**Правило «золотого окна»:** игрок должен успевать купить базовый набор башен до начала первой волны. Формула: `StartGold / 50 ≥ TowerSlots − 1` (то есть на стартовые деньги хватает заполнить все слоты кроме одного).

---

## 8. Архитектура

### 8.1 Принципы
- **Zenject installers** вместо синглтонов.
- **`IInitializable` / `ITickable` / `IDisposable`** вместо Awake/Update/OnDestroy где можно.
- **ScriptableObject** для конфигов (TowerConfig, EnemyConfig, WaveConfig, LevelConfig).
- **Signals** (Zenject SignalBus) — основной способ событийной связи.
- **Pooling** — снаряды, враги через `MemoryPool<T>`.
- **DisplayableView** — базовый класс UI, `Show()` / `Hide()`.

### 8.2 Структура папок

```
Assets/Game/Scripts/
  Bootstrap/          ← EntryPoint, ProjectInstaller, GameplayInstaller, MenuInstaller
  Core/
    StateMachine/     ← BaseState, State<T>, StateMachineController
    GameLoop/         ← GameLoopStateMachine, GameLoopState, States/
    Signals/          ← все Zenject-сигналы
    Services/         ← PersistenceService, SceneLoader, AudioService, InputReader
  Configs/            ← SO-конфиги: LevelConfig, LevelCatalog, TowerConfig, TowerCatalog,
                         EnemyConfig, WaveConfig, SfxConfig, LevelDefinition
  Gameplay/
    Level/            ← LevelContext, Path, TowerSlot, PlayerBase, StarCalculator, LevelResultService
    Enemies/          ← Enemy, EnemyFactory, EnemyMovement, EnemyHealth, EnemyBaseDamager, SlowEffect
    Towers/           ← Tower, TowerFactory, TowerAttack, TowerUpgradeService, TowerMeshSwitcher
    Projectiles/      ← Projectile, ProjectilePool, ProjectileImpact, AreaDamage
    Waves/            ← WaveSpawner
    Economy/          ← Wallet, RewardService
    Input/            ← WorldTapRouter
  UI/
    Views/            ← DisplayableView, MainMenuView, LevelSelectView, LevelButton, HudView,
                         BuildMenuView, BuildMenuButton, TowerInfoView, PauseView,
                         LevelCompleteView, LevelFailedView
    Presenters/       ← MainMenuPresenter, BuildMenuPresenter, TowerInfoPresenter,
                         HudPresenter, PausePresenter, LevelCompletePresenter, LevelFailedPresenter
  Meta/
    Progress/         ← PlayerProgress, SaveData
```

### 8.3 Zenject-контейнеры
- **ProjectContext** — `PersistenceService`, `PlayerProgress`, `SignalBus`, `SceneLoader`, `GameLoopStateMachine`.
- **SceneContext (Menu)** — `MainMenuPresenter`, `LevelSelectPresenter`.
- **SceneContext (Gameplay)** — `LevelContext`, `Wallet`, `WaveSpawner`, `EnemyFactory`, `TowerFactory`, `ProjectilePool`, HUD-презентеры.

### 8.4 Game State Machine

```
Initialize → MainMenu → LoadLevel → Gameplay → LevelComplete → MainMenu
                                         ↓
                                      LevelFailed → MainMenu
                                      Pause (overlay)
```

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

### 8.5 Сигналы (ключевые)

```
GameplaySignals.cs:
  WaveStartedSignal(Index, Total)
  WaveCompletedSignal(Index, Reward)
  WaveBreakStartedSignal(NextIndex, Seconds)
  WaveEarlyStartRequestedSignal
  AllWavesCompletedSignal
  EnemyKilledSignal(Enemy, Reward)
  EnemyReachedBaseSignal(Damage)
  BaseHealthChangedSignal(Current, Max)
  GoldChangedSignal(Amount)
  TowerBuiltSignal(Tower)
  TowerUpgradedSignal(Tower)
  TowerSoldSignal(Tower)
  LevelCompletedSignal(Stars)
  LevelFailedSignal

LevelStartRequestedSignal.cs: LevelStartRequestedSignal(LevelId)
LevelLoadedSignal.cs: LevelLoadedSignal
```

---

## 9. Сохранения

`PlayerProgress` (JSON через `PersistenceService`):
```json
{
  "unlockedLevel": 1,
  "levelStars": { "1": 3, "2": 2 }
}
```

---

## 10. Ассеты — маппинг на геймплей

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
| Босс | enemy-ufo-d, VisualScale=2.5 |

Все модели используют общий атлас `colormap.png` → 1 draw call (GPU Instancing на материале).

---

## 11. Технические заметки

- **Поиск цели башней:** `Physics.OverlapSphere` раз в ~0.1с, слой `Enemies`.
- **WaveSpawner:** SubWaves в одной волне — **последовательный** спавн. Перерыв DelayAfter стартует после гибели последнего врага, не после последнего спавна.
- **Split paths:** PathIndex в SubWave — индекс в массиве `IReadOnlyList<Path>`, который WaveSpawner получает через `Run(waves, paths)`. Число путей определяется префабом уровня (количество компонентов `Path`).
- **Pool'ы:** `Enemy`, `Projectile` — через `MemoryPool<Config, T>`.
- **Time.timeScale** управляется только `PauseState`.
- **Сигналы** отписываются в `OnStateDisabled` / `Dispose` — строго, иначе утечки.
- **SO-конфиги** складываются в `Assets/Game/Configs/{Levels,Towers,Enemies,Waves}`.
- **Никаких `FindObjectOfType`** — всё через Zenject.

---

## 12. Критерии готовности

- [ ] 25 уровней создано (LevelConfig + WaveConfig SO-ассеты + Level Prefab).
- [ ] Все 4 башни работают с правильными параметрами.
- [ ] Все 5 типов врагов (включая boss) в конфигах.
- [ ] Апгрейд / продажа башен.
- [ ] Split-path (2 пути) c L4; 3 пути c L19.
- [ ] Boss появляется на L10 (мини), L15, L20+.
- [ ] Волны, экономика, HP базы, звёзды сохраняются.
- [ ] Пауза, рестарт, поражение, победа.
- [ ] Запуск на Android без ошибок, 60 FPS на среднем устройстве.
