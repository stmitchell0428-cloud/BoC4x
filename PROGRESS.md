# BoC4x — Session Checkpoint

**Saved:** August 3, 2026 — local + agent trees merged to master  
**Project:** Book of Concord 4X prototype (`C:\Users\stmit\BoC4x`)  
**Engine:** Unity 6000.5.x — scene `Assets/Scenes/SampleScene.unity`

---

## Resume here

**Lobby:** solo · Grand · Archipelago · Full canon · coastal capital · fame win **120**.  
**No save/load** — recompile / Exit Play **wipes the match**. Fresh lobby match only.

**Keep local ↔ GitHub aligned:** see **`SYNC.md`**.

1. Hub → **BoC4x** → `Assets/Scenes/SampleScene.unity` → wait for clean recompile → Play.
2. Work **`PLAYTEST-AUDIT-BATCH.md`** — tightening batch priority first, then shipped-fixes regression.

### Shipped Aug 3 — local post-playtest

- **Naval feel pass** — Explorer no Ocean; Galley/Deep-Sea water-only; coastal navigable depth **3**; water hover/tint tiers
- **Left HUD readability** — semi-opaque backing on dashboard stat rows + `TerrainInfoPanel`
- **Synod Brief** — CITY YIELDS no longer false-empty when districts use ` - ` in text
- **Clergy roster (R)** — `ClergyRosterPanel` bootstrapped in `HexGridMap.EnsureGameSystems`
- **Galley cargo panel** — raised above End Turn; "Ship cargo" for deep-sea
- **Combat** — melee counter-damage scales with defender strength / fight left; no chip on overkill
- **Schism** — new crises prefer unused heresy; Walther schism warnings suppressed at 3-bloc cap (Union strife line)

### Shipped Aug 3 — agent tightening batch (merged)

- **PlayerCapital:** schismatic win on capital capture (not hardcoded Wittenberg).
- **Population grace:** 2 turns at pop 0; critical banner at ≤3.
- **Synod Brief:** military witness + emphasis gates; brief diplomacy + colloquy truce.
- **HUD:** BUILD / SCRIPTURE labels; unified left column; modal stack on banner.
- **End Turn:** pastoral + district auto-defer; crisis/emphasis/narrative/liturgical/testimony still block.
- **Appeal flash** on district offers; **AI** 12 actions/turn cap.
- **Salvation history intro:** "In the Beginning" card (Confess / Pastor / Study hexameron).

### ⏳ later

1. **Schism saturated cards** — verify colloquy / feed dissent / purge appear reliably at cap in play
2. **AI naval build** — rivals wharf, soldiers, galleys
3. **Siege / armory UI copy** — cluster armory must not unlock local siege messaging
4. **Unit XP / veterancy** — XP per encounter → atk/def growth; tier names + optional upgrade branches (see `PLAYTEST-AUDIT-BATCH.md`)

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

**Resume playtest track:** **`PLAYTEST-AUDIT-BATCH.md`** (tightening priority → regression).

**Victory reference (code):** 100% adherence × 5 turns · CTCR+Nagel+GLF trio · **120** fame · pop 0 for 2-turn grace · schismatic capital capture after schism.

---

## Implementation ledger (decision forks)

| Fork | Topic                                      | Status                        |
| ---- | ------------------------------------------ | ----------------------------- |
| 1    | AI synod schism (players 2–4)              | ✅                             |
| 2    | Mission House → frontier settler           | ✅                             |
| 3    | Population sync (faction ← cities)         | ✅                             |
| 4    | Organic-only districts                     | ✅                             |
| 5    | AI synod personalities                     | ✅                             |
| 6    | Asymmetric adherence / secular research    | ✅                             |
| 7    | Galley cargo UI + synod trade stub         | ✅                             |
| —    | Diplomacy panel (rival synods)             | ✅                             |
| —    | Crisis end-turn loop fix                   | ✅ (needs playtest)           |
| —    | Design audit tightening batch              | ✅ merged (needs playtest)    |
| —    | PlayerCapital + population grace           | ✅ merged (needs playtest)    |
| —    | BUILD/SCRIPTURE HUD + End Turn defer       | ✅ merged (needs playtest)    |
| —    | Synod Brief diplomacy + military witness   | ✅ merged (needs playtest)    |
| —    | AI turn action budget (12/turn)            | ✅ merged (needs playtest)    |
| —    | Salvation history intro card               | ✅ merged (needs playtest)    |
| —    | Naval feel / combat retaliation / R roster | ✅ local (needs playtest)     |

---

*Update this file at the end of each major session.*
