# Playtest checklist — merged sync (Aug 2026)

Use `- [ ]` → `- [x]` to check items (works in Cursor/VS Code preview and on GitHub).

**Before you start:** Exit Play → recompile → **new match from lobby** (no mid-run resume).

**Recommended setup:** Solo · Grand · Archipelago · Full canon · **coastal capital** · fame win **120**

**Build under test:** Merged local + agent tree — naval feel, HUD, combat, tightening batch (PlayerCapital, pop grace, End Turn defer, Synod Brief, salvation history intro).

---

## Pre-flight

- [ ] Scripts recompiled
- [ ] Fresh match started from lobby
- [ ] EditMode → **BoC4x.Editor.Tests** → Run All (spot-check)

---

## Priority — Tightening batch (verify first)

- [ ] **BUILD / SCRIPTURE HUD** — Upper-left queue panel shows **BUILD** and **SCRIPTURE** lines; turn banner says Build | Scripture
- [ ] **End Turn defer** — With **pastoral briefing** or **district offer** open, End Turn **advances** (auto-defers); crisis/emphasis/narrative/liturgical/testimony still **block**
- [ ] **Synod Brief CITY YIELDS** — After districts/hamlets exist, **Y** brief shows real yields — not “No city production yet” falsely
- [ ] **Military witness (Y + T)** — **Y** brief → Military witness + emphasis gates; **T** tech sidebar shows gate summary when no tech selected
- [ ] **Diplomacy in brief** — Lobby **2 players** → **Y** shows rival lines; **Colloquy truce** button spends 2 mss, 10-turn truce; **D** panel still works
- [ ] **PlayerCapital win** — Post-schism: schismatic capture of **your capital** ends match (banner names capital)
- [ ] **Population grace** — Drop synod pop to **0** → banner shows grace countdown; defeat only after **2 turns** at 0; warning at **≤3**
- [ ] **Left HUD column** — Dashboard rows sit on dark backing / column
- [ ] **Modal stack banner** — Open **Y**, **T**, or **C** → turn banner shows `[N panels open]`
- [ ] **District appeal flash** — District offer highlights hex + shows appeal score; **G** toggles full appeal map
- [ ] **AI turn budget** — 2-player lobby: AI rivals act but turns don’t stall on huge unit stacks
- [ ] **Salvation history intro** — “In the Beginning” card with Confess / Pastor / Study hexameron; Law/Gospel hint visible

---

## Shipped fixes — verify next run

### Core movement

- [ ] Scout / settler move at game start
- [ ] Move-range highlight readable (ring overlays)
- [ ] Coastal Explorer cannot walk inland
- [ ] Explorer cannot portage across peninsula
- [ ] Explorer hugs shoreline; mouth landing OK

### Economy & UX

- [ ] Parish Granary: +3 food/turn; no pop spike on complete
- [ ] Worked tiles tint + toggle
- [ ] Skip turn (J); H/J blocked when panels open
- [ ] **Synod Brief CITY YIELDS** shows production when districts exist
- [ ] **Clergy roster (R)** opens assignment panel
- [ ] **Left HUD** stat rows + terrain readout have dark backing
- [ ] **Galley/Ship cargo** panel above End Turn button

### Naval feel pass (Aug 3 post-playtest)

- [ ] Explorer **cannot** enter `Ocean` terrain (rivers/lakes/shore only)
- [ ] Galley + Deep-Sea **water only** — no land tile movement; troops disembark from adjacent water
- [ ] Coastal navigable band **3 hexes** from land (Archipelago + Normal)
- [ ] Water hover tiers: river/lake · coastal sea · open ocean
- [ ] Water tint: teal / mid blue / dark on map

### Naval build chain

- [ ] Coastal Patrol absent
- [ ] Explorer at coastal capital via Wharf chain
- [ ] War Dock + Galley at coastal Garrison
- [ ] Deep-Sea Ship at coastal Garrison with War Dock (Open-Ocean Navigation)
- [ ] Explorer sight **5**

### Military & combat

- [ ] Promote to Defender: 1 mss with local Armory; UI shows discount
- [ ] **Melee counter-damage:** scales with defender fight left; **no chip** on overkill (e.g. soldier vs 1 HP scout)

### Schism

- [ ] Drift/crisis before fame win (retuned batch)
- [ ] At 3 blocs: dashboard shows **Union strife**, not `Crisis: antinomian schism`
- [ ] New schism crises prefer **unused heresy** (no duplicate Libertine bloc)
- [ ] Saturated overflow cards (colloquy / feed dissent / purge)

---

## Mid–late match

- [ ] Formula bound → fame path cue on dashboard
- [ ] Fame **120** pacing readable
- [ ] Emphasis cards block End Turn

---

## Deferred design (not blocking this playtest)

### Unit experience / veterancy

**Request:** Units gain **experience per combat encounter**; XP feeds **attack/defense growth**. Optional **veterancy tiers** with upgrade paths.

**Files (when built):** `CombatSystem`, `Unit`, `UnitUpgradeService`, `TerrainInfoPanel`, possibly `CityScreenPanel`

---

## Victory / defeat reference (code truth)

**Synod win (any one):** 100% adherence × 5 turns · Tier-6 trio (CTCR + Nagel + GLF) · **120** fame

**Defeat:** army + cities wiped · pop 0 for **2 turns** · adherence 0%

**Schismatic win:** capture player **capital** after schism

---

## Report back

Note turn number, lobby settings, and pass/fail per row. File issues against failing rows with Console excerpt if any.

*Companion: `GUIDED-AUDIT-PASS.md` · `SYNC.md`*
