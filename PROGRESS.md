# BoC4x — Session Checkpoint

**Saved:** August 3, 2026 — post-playtest batch (naval feel, HUD, combat, brief/roster fixes)  
**Project:** Book of Concord 4X prototype (`C:\Users\stmit\BoC4x`)  
**Engine:** Unity 6000.5.x — scene `Assets/Scenes/SampleScene.unity`

---

## Resume here

**Lobby:** solo · Grand · Archipelago · Full canon · coastal capital.  
**No save/load** — recompile / Exit Play **wipes the match**. Fresh lobby match only.

1. Hub → **BoC4x** → `Assets/Scenes/SampleScene.unity` → wait for clean recompile → Play.
2. Use `PLAYTEST-AUDIT-BATCH.md` **Shipped fixes — verify next run** for smoke pass.

### Shipped Aug 3 post-playtest

- **Naval feel pass** — Explorer no Ocean; Galley/Deep-Sea water-only; coastal navigable depth **3**; water hover/tint tiers
- **Left HUD readability** — semi-opaque backing on dashboard stat rows + `TerrainInfoPanel`
- **Synod Brief** — CITY YIELDS no longer false-empty when districts use ` - ` in text
- **Clergy roster (R)** — `ClergyRosterPanel` bootstrapped in `HexGridMap.EnsureGameSystems`
- **Galley cargo panel** — raised above End Turn; “Ship cargo” for deep-sea
- **Combat** — melee counter-damage scales with defender strength / fight left; no chip on overkill
- **Schism** — new crises prefer unused heresy; Walther schism warnings suppressed at 3-bloc cap (Union strife line)

### ⏳ later

1. **Schism saturated cards** — verify colloquy / feed dissent / purge appear reliably at cap in play
2. **AI naval build** — rivals wharf, soldiers, galleys
3. **Siege / armory UI copy** — cluster armory must not unlock local siege messaging
4. **Unit XP / veterancy** — XP per encounter → atk/def growth; tier names + optional upgrade branches (see `PLAYTEST-AUDIT-BATCH.md` § Deferred design)

### Passed (prior playtests) — spot-check if odd

District occupancy, schism rebalance, fame 120, dual production, specialty picker, art era, map wrap, loyalty recovery.

---

## Shipped Aug 2

**Narrative chronology (Tier A)**
- `MatchNarrativeChronology` — salvation-history narrative day clock (+18/turn default); variable jumps on event resolve
- `NarrativeEventDatabase` — 10 scripted beats (Creation → Ascension → 1517/Augsburg/Formula)
- Ascension activates **Church Year**; principal feasts unlock; reformers/martyrs added progressively via event unlocks
- `NarrativeEventManager` — multi-option cards (2–3 choices, mixed Law/Gospel/adherence/fame/mss)
- Liturgical feast cards **only after** Church Year phase

**Naval split**
- Tier 2 secular **River Trade & Wharves** (`CoastalWharves`, Bugenhagen): Wharf (12 prod), Fishing Post (8 prod, +2 food), Coastal Explorer
- Wharf/fishing/explorer at **coastal capital** or **Market** district; capital needs no district for early scout boat
- Tier 3 secular **Naval Warfare** (Chemnitz figure; requires wharf + Chemnitz): War Dock + Galley at **coastal Garrison** district
- Tier 4 secular **Open-Ocean Navigation** (Guericke figure): Deep-Sea Ship; explorer +1 sight, galley/deep-sea +1 move
- **Frontier Mission** — missionaries/settlers only; no war dock, no Bach fork lock

**Church year & testimony**
- Feast spawn uses **28-day turn window** (highest priority: principal > martyr > biblical > festival)
- Decision card **next turn** after spawn; martyr briefings decoupled from spawn
- `ChoiceCardBlocking` — unified card mutual exclusion; colloquies deferred to turn start
- `TestimonyColloquyManager` — Smalcald/Chemnitz/Gerhard + Library; patristic pool after library colloquy resolved
- Bugenhagen Apr 20 window: +1 food at coastal wharf cities

### Explore much later

- Wonders grant fame; more district specialties; Church Year window copy; Decision 16 era forks; map wrap deeper fix; **unit veterancy / XP tiers**

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
