# BoC4x — Audit decision tree

**Purpose:** Technical companion to [`GUIDED-AUDIT-PASS.md`](GUIDED-AUDIT-PASS.md) — branch trees and file lists per node.  
**Not** the session bookmark ([`PROGRESS.md`](PROGRESS.md)) and **not** the playtest checklist ([`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md)).

**How to use:** Make picks in the guided pass. Use this file when you need files/branches. Do not skip **P0** before **P1**.

**Legend:** `[P0]` critical · `[P1]` logic gap · `[P2]` theme/polish · `[P3]` optional

---

## Sprint 0 — Save point

- [x] Narrative chronology Tier A shipped
- [x] Naval split, testimony colloquies, church-year fixes shipped
- [x] Aug 7 playtest fixes: district offer End Turn block (no auto-defer), city Home cycle, food baseline/granary, EditMode chronology bind
- [ ] Run guided nodes → [`GUIDED-AUDIT-PASS.md`](GUIDED-AUDIT-PASS.md) · virtue track V (Option C, partially decided)
---

## P0 — Trust (mechanics must match UI)

### 1. Adherence floors `[P0]`

```
CTCR / Preus / Chytraeus / Riojas advertise adherence floors
│
├─ A) Wire MinAdherenceFloor from ConfessionModifiers into FirstSteps.EffectiveMinAdherenceFloor (Recommended)
│     → Merge all unlocked tech + legacy modifiers; use Max() for floor
│
├─ B) Implement missing floors in ConfessionModifiers only (Preus 40, Chytraeus +12%, Riojas +5%)
│     → Then wire as in A
│
└─ C) Remove floor claims from EffectSummary text (no new mechanics)
```

**Files:** `FirstSteps.cs`, `ConfessionModifiers.cs`, `ConfessionResearchManager.cs`, `ConfessionTechDatabase.cs`

---

### 2. AI Lutheran synod production `[P0]`

```
TryStartAiBuild returns false for ALL LutheranSynod cities → AI rivals never build
│
├─ A) Allow builds when city.SynodPlayer != Player1 (Recommended)
│     → Schismatic blocs already use non-synod faction OR separate path
│
├─ B) New TryStartSynodAiBuild with same gates as player, simplified AI subset
│
└─ C) Document solo-only design; disable multi-synod lobby options
```

**Files:** `CityProduction.cs`, `SimpleAI.cs`, `SynodAiBootstrap.cs`, lobby UI

**Follow-up after A/B:**
```
AI can build — train what?
├─ A) Extend AI: archer, horseman, mission house, district buildings (Recommended)
└─ B) Minimal: soldiers, missionaries, wharf chain only
```

---

### 3. Walther / Pieper tech text vs modifiers `[P0]`

```
EffectSummary describes emphasis bonuses, not ForTech modifiers
│
├─ A) Rewrite EffectSummary to match actual tech modifiers (Recommended)
│     → Walther: "+8 preach comfort"; Pieper: "decay -10%"
│
├─ B) Move advertised effects into ForTech (drift halving on Walther tech, etc.)
│     → Risk double-dip with emphasis choices
│
└─ C) EffectSummary references emphasis: "Choose Walther emphasis for drift halving"
```

**Files:** `ConfessionTechDatabase.cs`, `ConfessionModifiers.cs`, `SynodicalEmphasisManager.cs`

---

## P1 — City & production logic

### 4. Capital vs district specialization `[P1]`

```
Capital can build district-exclusive items (dock, mission house, fortification, granary…)
│
├─ A) Restrict capital: only "metropolitan" list (cathedral, seminary, chapel, walls?) (Recommended)
│     → Districts remain specialty sites
│
├─ B) Keep capital bypass; rewrite district flavor as "bonus yields, not exclusivity"
│
└─ C) Hybrid: capital may build anything but at +20% prod cost vs district
```

**Files:** `HamletSpecialty.cs`, `CityProduction.cs`, `CityScreenPanel.cs`, `CityBuildDatabase.cs`

---

### 5. Siege engine + armory gate `[P1]`

```
TrainSiegeEngine requires local armory; armory only on Garrison district; AI tries at capital
│
├─ A) Cluster-wide gate: any player Garrison armory enables siege train anywhere (Recommended)
│
├─ B) Keep local gate; UI + AI only queue siege at Garrison district city
│
└─ C) Remove armory requirement; gate siege on Maxwell tech only
```

**Files:** `CityProduction.cs`, `SimpleAI.cs`, `CityBuildDatabase.cs`

---

### 6. Pastor tech split `[P1]`

```
TrainPastor → Walther; MissionaryToPastor upgrade → Large Catechism (earlier)
│
├─ A) Align both to Large Catechism (Recommended)
├─ B) Align both to Walther
└─ C) Keep split; document "ordination via upgrade vs seminary build"
```

**Files:** `CityBuildDatabase.cs`, `UnitUpgradeDatabase.cs`

---

### 7. Armory defender discount `[P1]`

```
BuildArmory promises −1 mss on SoldierToDefender — not implemented
│
├─ A) Implement in UnitUpgradeService when armory in cluster (Recommended)
└─ B) Remove promise from build description
```

**Files:** `UnitUpgradeService.cs` / upgrade path, `CityBuildDatabase.cs`

---

### 8. BuildSeminary placement `[P1]`

```
BuildSeminary capital-only; Scholastic district cannot build it
│
├─ A) Add BuildSeminary to Scholastic (and/or Seminary) district allowed list (Recommended)
├─ B) Keep capital-only; add Scholastic bonus to seminary built at capital
└─ C) Remove BuildSeminary; fold effect into BuildUniversity
```

**Files:** `HamletSpecialty.cs`, `CityBuildDatabase.cs`

---

### 9. Hospital / granary growth text `[P1]`

```
Building copy promises growth; code differs
│
├─ A) Match code to copy (granary growth chance, hospital reliable +1) (Recommended)
└─ B) Match copy to code (remove growth claims; cite food/housing only)
```

**Files:** `CityBuildDatabase.cs`, `CityGrowthSystem.cs`, `CityProduction.cs`

---

## P1 — Tech tree & builds

### 10. Gutenberg vs printing press `[P1]`

```
GutenbergPress era-fork tech; BuildPrintingPress requires JohannesKepler
│
├─ A) Printing press requires GutenbergPress (Kepler optional second bonus) (Recommended)
├─ B) Remove Gutenberg era fork; Kepler owns printing thematically ("mechanism")
└─ C) Two buildings: Gutenberg press (early) + Kepler upgrade
```

**Files:** `ConfessionTechDatabase.cs`, `CityBuildDatabase.cs`, `EraBranchRulesTests.cs`

---

### 11. Hidden tech modifiers in copy `[P1]`

```
CoastalWharves +1 mss; Guericke +2 siege; etc. not in EffectSummary
│
├─ A) Add to EffectSummary strings (Recommended)
└─ B) Leave hidden as "discovery" mechanics
```

**Files:** `ConfessionTechDatabase.cs`

---

### 12. Naval unit defense bonuses `[P1]`

```
SoldierAttackBonus applies to galleys; SoldierDefenseBonus does not
│
├─ A) Add CoastalNavalDefenseBonus or apply defense to naval combat units (Recommended)
├─ B) Document as intentional (glass cannon navy)
└─ C) Remove soldier attack from deep-sea/galley
```

**Files:** `ConfessionModifiers.cs`, `Unit.cs`

---

## P1 — Match flow & cards

### 13. Global narrative card queue `[P1]`

```
Turn-start: narrative, liturgical, pastoral, testimony race for one panel
│
├─ A) Single FIFO queue service; max one card per turn start (Recommended)
├─ B) Strict priority: Crisis(deferred) > Narrative > Feast > Briefing > Colloquy
└─ C) Keep race; document script execution order in HexGridMap bootstrap
```

**Files:** new `ChoiceCardQueue.cs` or extend `ChoiceCardBlocking.cs`, all `*Manager.cs` presenters

---

### 14. Emphasis panels in ChoiceCardBlocking `[P1]`

```
Synodical/Tier2 emphasis block End Turn but not event spawn
│
├─ A) Add to ChoiceCardBlocking (Recommended)
└─ B) Defer all turn-start events until emphasis resolved
```

**Files:** `ChoiceCardBlocking.cs`, `EndTurnPhaseController.cs`

---

### 15. Identity picker enforcement `[P1]`

```
IdentityPickerPanel not in end-turn block list
│
├─ A) Block End Turn until identity chosen at founding (Recommended)
└─ B) Auto-assign default identity if skipped
```

**Files:** `EndTurnPhaseController.cs`, `IdentityPickerPanel.cs`

---

### 16. Legalism / antinomian double-hit `[P1]`

```
ApplyConfessionalTurnLogic damages stats before crisis card
│
├─ A) Card choice only — remove pre-card stat hit (Recommended)
├─ B) Pre-card hit only — card is flavor/legacy
└─ C) Pre-card hit at 50% strength; card adds rest
```

**Files:** `FirstSteps.cs`, `CrisisManager.cs`

---

## P2 — Chronology & wins

### 17. Dual clocks (18 vs 28 days) `[P2]`

```
Narrative day +18/turn; church civil date +28/turn after Ascension
│
├─ A) Keep dual clocks; dashboard shows both explicitly (Recommended)
├─ B) Unify to 28 days for both after Ascension
└─ C) Unify to 18 days for both always
```

**Files:** `MatchNarrativeChronology.cs`, `ChurchYearCalendar.cs`, `ChurchYearFlavor.cs`

---

### 18. Victory path pacing `[P2]`

```
Fame 120 / adherence streak / doctrine trio finish at different turns; no capstone
│
├─ A) Formula narrative grants fame toward 120 + dashboard "witness" cue (Recommended)
│     → Partially superseded by Virtue V4: soft profile, no witness-fame gate
├─ B) Lower fame threshold to 100
├─ C) Doctrine trio triggers optional epilogue card only
└─ D) Leave as-is (sandbox wins)
```

**Virtue track V4 (decided):** fame 120 only; virtue tags + annual ticks are flavor/stats, legacy variants optional (V6).

**Files:** `NarrativeEventDatabase.cs`, `MatchController.cs`, `GameHUD.cs`, virtue dashboard (V)

---

### 19. Lobby heresy pack vs AI rivals `[P2]`

```
Heresy pack affects schisms; lobby opponents are orthodox AI synods
│
├─ A) Clarify lobby copy: "AI synod neighbors + internal schism from heresy pack" (Recommended)
├─ B) Add optional schismatic lobby opponents
└─ C) Heresy pack only affects crisis card variety, rename setting
```

**Files:** `MatchLobbyPanel.cs`, `README.md`

---

## P2 — Theme & content

### 20. Historical tier placement `[P2]`

```
Bach in Synodical era; Mendel in Orthodoxy; Guericke tier vs Newton prereq
│
├─ A) Copy-only: era labels become "game eras" not historical (Recommended short-term)
├─ B) Move figures to historically closer tiers (balance pass required)
└─ C) Add figure dates to tech panel tooltip; keep tiers for gameplay
```

**Files:** `ConfessionTechDatabase.cs`, `ConfessionTechPanel.cs`

---

### 21. Maxwell → siege engine `[P2]`

```
James Clerk Maxwell unlocks TrainSiegeEngine
│
├─ A) Retheme to "Artillery science" / Maxwell + Chytraeus prereq (Recommended)
├─ B) Move siege to earlier tech (ShepherdsSling branch); Maxwell buffs pressure only
└─ C) Keep; add flavor text linking siege to "ordered mechanics"
```

**Files:** `CityBuildDatabase.cs`, `ConfessionTechDatabase.cs`

---

### 22. Coastal patrol description `[P2]`

```
DB says "fast riders"; unit is near-shore naval
│
├─ A) Fix copy to "coastal scout boat" (Recommended)
└─ B) Rebrand unit as mounted coastal patrol on land only
```

**Files:** `CityBuildDatabase.cs`, `Unit.cs` tooltips

---

### 23. Library secular track vs name `[P2]`

```
BuildLibrary on secular production track; named Confessional Library
│
├─ A) Rename category to ConfessionalBuilding, keep secular track (Recommended)
├─ B) Move to confessional timer track
└─ C) Keep; clarify in city screen tooltip
```

**Files:** `CityBuildDatabase.cs`, `CityScreenPanel.cs`

---

### 24. Art era vs narrative phase `[P2]`

```
ArtEraVisualController uses tech tier, not narrative/chronology phase
│
├─ A) Gate art era transitions on narrative phase + tier (Recommended)
└─ B) Keep tier-only; accept visual/history drift
```

**Files:** `ArtEraVisualController.cs`, `ArtEraTransitionPanel.cs`

---

## P3 — Schism & saturation (from Jul 31 playtest)

### 25. Schism variety `[P3]`

```
Duplicate Libertine blocs; hardcoded Antinomian crisis picks
│
├─ A) Dedupe bloc registry by heresy type (Recommended)
├─ B) Weight crisis picks by unused heresy pack entries
└─ C) Leave until next playtest confirms pain
```

**Files:** `SchismManager.cs`, `CrisisManager.cs`, `SchismaticBlocRegistry.cs`

---

### 26. Walther crisis line at schism cap `[P3]`

```
Shows "Crisis: antinomian schism" when 3 blocs already full
│
├─ A) Suppress crisis type banner; show Union Strife / saturation only (Recommended)
└─ B) Redirect to overflow crisis card copy
```

**Files:** `CrisisManager.cs`, `GameHUD.cs`, `UnionStrifeManager.cs`

---

## P3 — Narrative chronology Tier B (future)

### 27. Expand narrative spine `[P3]`

```
Tier A = 10 events; Tier B = virtue + observance loop
│
├─ A) Virtue track V (Option C) — tags, first/annual feasts (Recommended — partially decided)
├─ B) Add post-Formula narrative beats only (Chemnitz, Gerhard cards)
└─ C) Ship Tier A only until playtest count ≥ 3 full matches
```

**Virtue V (decided):** Option C framework, five tags, first-then-annual feasts, same tags on narrative. See **V** section.

**Files:** `NarrativeEventDatabase.cs`, `MatchNarrativeChronology.cs`, virtue/observance files (V)

---

### 28. Movable Easter / computus `[P3]`

```
LSB fixed dates only; Easter season approximated
│
├─ A) Implement Western computus for Easter / Pentecost movable feasts
└─ B) Keep fixed LSB 1-year calendar (current)
```

**Files:** `ChurchYearCalendar.cs`

---

## V — Virtue & observance (Option C, Aug 2026)

**Merged from design session.** Full walkthrough in [`GUIDED-AUDIT-PASS.md`](GUIDED-AUDIT-PASS.md#virtue--observance-v--option-c). Implement **after P0–P1** unless playtest forces earlier.

### V0. Framework `[DECIDED]`

```
Option C: grouped virtue tags + Law/Gospel primary axis
Win: fame 120 unchanged; soft bonuses only — no affinity-locked win path
```

### V1. Five tags `[DECIDED]`

First Table · Honor & Mercy · Fidelity & Truth · Neighbor & Contentment · Martyrial Witness  
Same tags on narrative events + church-year feasts/martyrs.

### V2. First encounter → annual commemoration `[DECIDED]`

```
First calendar hit → full choice card (Law/Gospel + tags)
Later years → annual tick B (passive stat from first choice; no card)
Martyrs and feasts share pipeline
```

**Gap vs code today:** feasts spawn once per match; no annual return tick yet.

### V5. Annual tick strength `[DECIDED — B]`

```
Moderate: annual tick follows first-choice lean; martyr feasts +1 fame every 3rd annual tick
Dashboard one-liner, no card on return years
```

### V6. Legacy trait variants `[DECIDED — A]`

```
Dominant virtue tag tints legacy name + soft modifier at fame 25/55 (existing SynodLegacyTraitId slots)
```

**Files:** `VirtueProfile` (new), `FeastObservanceRegistry` (new), `LiturgicalEventManager.cs`, `NarrativeEventManager.cs`, `SynodLegacyManager.cs`, `FirstSteps.cs`

**Overlaps:** #18 victory pacing (V4 decided: soft) · #27 narrative Tier B (virtue system is the expansion)

---

## Recommended traversal order

```
P0: 1 → 2 → 3
P1: 2-follow-up → 4 → 5 → 6 → 7 → 10 → 13 → 14 → 15 → 16
P1: 8 → 9 → 11 → 12
P2: 17 → 18 → 19 → 20–24 as time allows
P3: 25 → 26 → 27 → 28
V:  (after P1) V5 → V6 → implement V0–V4
```

**Estimated passes:** 3–4 focused sessions (P0 one session, P1 two sessions, P2/P3 optional).

---

## After implementing a sprint

Smoke-test with [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md). Record design picks in [`GUIDED-AUDIT-PASS.md`](GUIDED-AUDIT-PASS.md) (or mark branches here with `→ DECIDED: branch X`).

---

*Generated Aug 2026 from full-game audit. Doc roles cleaned Aug 7, 2026.*
