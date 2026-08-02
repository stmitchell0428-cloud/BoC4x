# BoC4x — Session Checkpoint

**Saved:** August 2, 2026 — naval split + church-year events + testimony colloquies shipped  
**Project:** Book of Concord 4X prototype (`C:\Users\stmit\BoC4x`)  
**Engine:** Unity 6000.5.x — scene `Assets/Scenes/SampleScene.unity`

---

## Resume here

**Lobby:** solo · Grand · Archipelago · Full canon · coastal capital.  
**No save/load** — recompile / Exit Play **wipes the match**. Fresh lobby match only.

1. Hub → **BoC4x** → `Assets/Scenes/SampleScene.unity` → wait for clean recompile → Play.
2. Smoke **New this restart** first, then pick up **⏳ later** schism items if time.

### New this restart — verify

|     | Focus | What to verify |
| --- | ----- | -------------- |
| [ ] | **Salvation history** | Turns 1–~15: sparse narrative cards (Creation → Sinai → Nativity → Passion → Easter) |
| [ ] | **Ascension pivot** | Ascension card → dashboard switches to **Church Year**; LSB calendar active |
| [ ] | **Reformation beats** | After Ascension: 1517 / Augsburg / Formula narrative cards unlock Luther & confessional commemorations |
| [ ] | **Quiet turns** | Most turns advance narrative day (+18) with **no** card |
| [ ] | **Early wharf chain** | Tier 2 **River Trade & Wharves** → Wharf at coastal **capital** → Fishing Post → Coastal Patrol / Explorer |
| [ ] | **Late war dock** | Tier 3 **Naval Warfare** (after Chemnitz + wharf) → War Dock → Galley |
| [ ] | **Deep sea** | Tier 4 **Open-Ocean Navigation** (after Guericke) → Deep-Sea Ship on open ocean |
| [ ] | **Feast-day cards** | Principal feast / martyr in turn window → spawn → decision card next turn |
| [ ] | **Martyr briefings** | Martyr in 28-day window → Stephen/Polycarp/Ignatius pastoral (no spawn required) |
| [ ] | **Bugenhagen window** | Apr 20 commemoration turn → +1 food at coastal wharf cities |
| [ ] | **Smalcald Catalog** | Unlock Smalcald Articles → Catalog colloquy on **next turn start** |
| [ ] | **Patristic colloquies** | Chemnitz + Gerhard unlocks → testimony cards next turn; Library colloquy → patristic pool after resolve |

### ⏳ later (from Jul 31 playtest)

1. **Schism variety** — avoid duplicate Libertine blocs; diversify crisis heresy picks.
2. **Walther crisis line at schism cap** — no `Crisis: antinomian schism` when 3 blocs already up.

### Passed (prior playtests) — spot-check if odd

District occupancy, schism rebalance, fame 120 / no tribute fame, dual production, specialty picker, art era, map wrap, loyalty recovery, Confessions emphasis.

---

## Shipped Aug 2

**Narrative chronology (Tier A)**
- `MatchNarrativeChronology` — salvation-history narrative day clock (+18/turn default); variable jumps on event resolve
- `NarrativeEventDatabase` — 10 scripted beats (Creation → Ascension → 1517/Augsburg/Formula)
- Ascension activates **Church Year**; principal feasts unlock; reformers/martyrs added progressively via event unlocks
- `NarrativeEventManager` — multi-option cards (2–3 choices, mixed Law/Gospel/adherence/fame/mss)
- Liturgical feast cards **only after** Church Year phase

**Naval split**
- Tier 2 secular **River Trade & Wharves** (`CoastalWharves`, Bugenhagen): Wharf (12 prod), Fishing Post (8 prod, +2 food), Coastal Patrol, Coastal Explorer
- Wharf/fishing/patrol/explorer at **any coastal city** (capital OK); not Market-district-exclusive
- Tier 3 secular **Naval Warfare** (Chemnitz figure; requires wharf + Chemnitz): War Dock → Galley; coastal naval +1 move
- Tier 4 secular **Open-Ocean Navigation** (Guericke figure): Deep-Sea Ship; explorer +1 sight, galley/deep-sea +1 move
- **Frontier Mission** — missionaries/settlers only; no war dock, no Bach fork lock

**Church year & testimony**
- Feast spawn uses **28-day turn window** (highest priority: principal > martyr > biblical > festival)
- Decision card **next turn** after spawn; martyr briefings decoupled from spawn
- `ChoiceCardBlocking` — unified card mutual exclusion; colloquies deferred to turn start
- `TestimonyColloquyManager` — Smalcald/Chemnitz/Gerhard + Library; patristic pool after library colloquy resolved
- Bugenhagen Apr 20 window: +1 food at coastal wharf cities

### Explore much later

- Wonders grant fame; more district specialties; Church Year window copy; Decision 16 era forks; map wrap deeper fix

---

## Lobby defaults

| Setting     | Value                          |
| ----------- | ------------------------------ |
| Players     | **1** (solo)                   |
| Map size    | **Grand** (80×52)              |
| Coasts      | **Archipelago**                |
| Heresy pack | **Full canon** (up to 3 blocs) |
| Capital     | **Coastal**                    |
| Fame win    | **120**                        |
