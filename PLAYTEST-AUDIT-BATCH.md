# Playtest checklist — audit batch (Aug 2026)

Use `**- [ ]**` → `**- [x]**` to check items (works in Cursor/VS Code preview and on GitHub).

**Before you start:** Exit Play → recompile → **new match from lobby** (no mid-run resume).

**Recommended setup:** Solo · Grand · Archipelago · Full canon · **coastal capital** · fame win 120

**Build under test:** Aug 3 post-playtest batch — naval feel pass, HUD panels, combat counter-damage, Synod Brief yields fix, clergy roster bootstrap, cargo panel layout, schism heresy variety.

---

## Pre-flight

- [x] Scripts recompiled
- [x] Fresh match started from lobby

---

## Shipped fixes — verify next run

### Core movement

- [x] Scout / settler move at game start
- [x] Move-range highlight readable (ring overlays)
- [x] Coastal Explorer cannot walk inland
- [x] Explorer cannot portage across peninsula
- [x] Explorer hugs shoreline; mouth landing OK

### Economy & UX

- [ ] Parish Granary: +3 food/turn; no pop spike on complete
- [x] Worked tiles tint + toggle
- [x] Skip turn (J); H/J blocked when panels open
- [x] **Synod Brief CITY YIELDS** shows production when districts exist (not false “No city production”)
- [x] **Clergy roster (R)** opens assignment panel
- [x] **Left HUD** stat rows + terrain readout have dark backing
- [x] **Galley/Ship cargo** panel above End Turn button

### Naval feel pass (Aug 3 post-playtest)

- [ ] Explorer **cannot** enter `Ocean` terrain (rivers/lakes/shore only)
- [ ] Galley + Deep-Sea **water only** — no land tile movement; troops disembark from adjacent water
- [ ] Coastal navigable band **3 hexes** from land (Archipelago + Normal)
- [ ] Water hover tiers: river/lake · coastal sea · open ocean
- [ ] Water tint: teal / mid blue / dark on map

### Naval build chain

- [x] Coastal Patrol absent
- [x] Explorer at coastal capital via Wharf chain
- [x] War Dock + Galley at coastal Garrison
- [ ] Deep-Sea Ship at coastal Garrison with War Dock (Open-Ocean Navigation)
- [x] Explorer sight **5**

### Military & combat

- [x] Promote to Defender: 1 mss with local Armory; UI shows discount
- [ ] **Melee counter-damage:** scales with defender fight left; **no chip** on overkill (e.g. soldier vs 1 HP scout)

### Schism

- [x] Drift/crisis before fame win (retuned batch)
- [ ] At 3 blocs: dashboard shows **Union strife**, not `Crisis: antinomian schism`
- [ ] New schism crises prefer **unused heresy** (no duplicate Libertine bloc)
- [ ] Saturated overflow cards (colloquy / feed dissent / purge)

---

## Mid–late match

- [x] Formula bound → fame path cue on dashboard
- [x] Fame 120 pacing readable
- [x] Emphasis cards block End Turn

---

## Run log — winning run (Aug 3)

**End condition:** Fame / synod victory (user confirmed win)

**Blockers found & fixed post-run:**

```
Synod Brief “No city production” false positive (district “ - ” in yield string)
Deep-Sea / Galley walking Naval coast land → water-only movement
Galley cargo panel covered End Turn
R key clergy roster not bootstrapped
Left HUD unreadable over explored map
Attacker took no reliable damage on melee → counter-damage retune
Duplicate heresy picks on new schism; Walther antinomian warning at 3 blocs
```

---

## Deferred design (not in build)

### Unit experience / veterancy (Aug 3 — save for later)

**Request:** Units gain **experience per combat encounter**; XP feeds **attack/defense growth** over time. Optional **veterancy tiers** (e.g. green → seasoned → veteran) with **upgrade paths** at thresholds — not just flat stat bumps.

**Intent:** Reward units that survive campaigns; make chip attacks and garrison duty matter; parallel existing promote-to-Defender / armory upgrades without replacing them.

**Open design:**

- XP sources: deal damage, take counter, kill blow, siege tick, preach-adjacent escort?
- Caps by unit type; decay on promote/transform?
- UI: XP bar on selection / city roster; tier name in unit line
- **Files (when built):** `CombatSystem`, `Unit`, `UnitUpgradeService`, `TerrainInfoPanel`, possibly `CityScreenPanel`

---

## Prior run notes — archived

```
Jul 31 – Aug 3 (fame 120 win)
— District food gate ~turn 34 on Grand/coastal
— Granary +3 food / no completion pop ✓ | worked-tile toggle ✓ | skip turn ✓
— Defender 1 mss UI fix; Coastal Patrol removed; naval build-site retune
— Schism pressure v1 retune; movement/highlight fixes

Aug 3 interrupted runs
— StepCost bug; move-highlight opacity; peninsula portage → shared-water rule
```

---

*Companion: `[GUIDED-AUDIT-PASS.md](GUIDED-AUDIT-PASS.md)`*
