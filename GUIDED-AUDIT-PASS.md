# Guided audit pass — design backlog

**Purpose:** Decide *what to build* (A/B/C per node). **Not** the daily playtest list and **not** the session bookmark.

| Need | File |
|------|------|
| Sit down / lobby / last fixes | [`PROGRESS.md`](PROGRESS.md) |
| Smoke-test this build | [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md) |
| File lists & branch trees | [`AUDIT-DECISION-TREE.md`](AUDIT-DECISION-TREE.md) |

**Status:** In progress · **Resume at:** Node **#1** (P0)  
**Next playtest first:** [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md) (district offer / Home / Growth) — then return here for design picks.  
**Virtue track V:** Option C framework — see [Virtue & observance (V)](#virtue--observance-v--option-c). Implement **after P0–P1** unless a node overlaps (#18, #27).

---

## How to use

1. Work **top to bottom** — do not skip P0 before P1.
2. For each node: read **What's wrong** → pick **A / B / C** → write **Your pick**.
3. To code a node: *"Implement audit node #N with branch X."*
4. After a sprint of implementations, smoke with [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md).

**Traversal order:**  
P0: 1 → 2 → 3 → P1: 2-follow-up → 4 → 5 → 6 → 7 → 10 → 13 → 14 → 15 → 16 → 8 → 9 → 11 → 12 → P2: 17–24 → P3: 25–28 → **V** (after P1)

---

## Note on old draft code

An earlier session may have implemented branch A before picks were filled. If a messy diff remains: discard and decide fresh, or keep only what matches your picks. Nothing here is “done” until **Your pick** is filled and shipped.

---

## P0 — Trust (mechanics must match UI)

### #1 · Adherence floors `[P0]`

**What's wrong:** CTCR, Preus, Chytraeus, and Riojas tech text promise adherence floors, but `FirstSteps.EffectiveMinAdherenceFloor` returns hardcoded `0`.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Wire floors *(recommended)* | Merge unlocked tech modifiers; floor = max/base + bonuses from Preus (40), Chytraeus (+12), Riojas (+5), CTCR (50) |
| **B** | Modifiers only, then wire | Same as A but two-step: add values in `ConfessionModifiers` first |
| **C** | Honest copy only | Remove floor claims from tech UI; no new mechanic |

**Your pick:** ___  
**Notes:** ___

---

### #2 · AI Lutheran synod production `[P0]`

**What's wrong:** `CityProduction.TryStartAiBuild` returns false for **all** `LutheranSynod` cities, so AI rival synods never build.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Allow rival synods *(recommended)* | Build when `city.SynodPlayer != Player1` |
| **B** | Separate AI build path | New `TryStartSynodAiBuild` with player-like gates |
| **C** | Solo-only | Document design; hide/disable multi-synod lobby |

**Sub-choice (after A or B) — what should AI train?**

| | Choice | Meaning |
|---|--------|---------|
| **A** | Extended *(recommended)* | Archers, horsemen, mission house, districts + existing wharf/soldier logic |
| **B** | Minimal | Soldiers, missionaries, wharf chain only |

**Your pick (main):** ___  
**Your pick (sub):** ___  
**Notes:** ___

---

### #3 · Walther / Pieper tech text `[P0]`

**What's wrong:** EffectSummary says "drift halved" / "preach +10" but `ConfessionModifiers.ForTech` gives comfort/decay only; emphasis choices hold the drift effects.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Fix copy *(recommended)* | Walther: "+8 preach comfort"; Pieper: "decay −10%" |
| **B** | Fix mechanics | Move advertised effects into `ForTech` (risk double-dip with emphasis) |
| **C** | Cross-reference emphasis | Copy points to Synodical emphasis choice |

**Your pick:** ___  
**Notes:** ___

---

## P1 — City & production

### #4 · Capital vs district specialization `[P1]`

**What's wrong:** Capital can build district-exclusive items (dock, mission house, garrison buildings, etc.), weakening specialty identity.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Metropolitan allow-list *(recommended)* | Capital: chapel, cathedral, seminary, basic units, coastal wharf/fishing, walls, granary/hospital… Districts keep exclusives |
| **B** | No exclusivity | Districts = bonus yields, capital can build anything |
| **C** | Hybrid | Capital can build anything at +20% production cost |

**Your pick:** ___  
**Notes:** ___

---

### #5 · Siege engine + armory gate `[P1]`

**What's wrong:** Siege requires **local** armory; armory only on Garrison **district**; AI tries siege at capital.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Cluster armory *(recommended)* | Any armory in city cluster unlocks siege train anywhere in cluster |
| **B** | Local + smart UI/AI | Keep local gate; only queue siege at Garrison hex |
| **C** | Maxwell only | Drop armory requirement |

**Your pick:** ___  
**Notes:** ___

---

### #6 · Pastor tech split `[P1]`

**What's wrong:** `TrainPastor` requires Walther; `MissionaryToPastor` upgrade requires Large Catechism (earlier, different path).

| | Choice | Meaning |
|---|--------|---------|
| **A** | Both → Large Catechism *(recommended)* | Align ordination paths |
| **B** | Both → Walther | Later pastoral theology gate |
| **C** | Keep split | Document "upgrade vs seminary build" |

**Your pick:** ___  
**Notes:** ___

---

### #7 · Armory defender discount `[P1]`

**What's wrong:** Armory copy promises −1 manuscript on Soldier→Defender upgrade; not implemented.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Implement *(recommended)* | Discount when cluster has armory |
| **B** | Fix copy | Remove promise |

**Your pick:** ___  
**Notes:** ___

---

### #8 · BuildSeminary placement `[P1]`

**What's wrong:** Seminary build is capital-oriented; Scholastic district cannot build it.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Scholastic (+ Seminary) district *(recommended)* | Add to allowed list |
| **B** | Capital-only bonus | Scholastic district buffs capital seminary |
| **C** | Fold into University | Remove standalone seminary build |

**Your pick:** ___  
**Notes:** ___

---

### #9 · Hospital / granary growth `[P1]`

**What's wrong:** Building text promises pop growth; code uses 35% hospital roll and no granary growth tick.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Match code to copy *(recommended)* | Hospital +1/turn reliable; granary growth chance |
| **B** | Match copy to code | Remove growth claims from descriptions |

**Your pick:** ___  
**Notes:** ___

---

### #10 · Gutenberg vs printing press `[P1]`

**What's wrong:** `GutenbergPress` is the era-fork tech; `BuildPrintingPress` currently requires Kepler.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Gutenberg gates press *(recommended)* | Kepler stays separate (movement bonus) |
| **B** | Kepler owns printing | Remove Gutenberg fork from printing |
| **C** | Two buildings | Early Gutenberg press + Kepler upgrade |

**Your pick:** ___  
**Notes:** ___

---

### #11 · Hidden tech modifiers in copy `[P1]`

**What's wrong:** CoastalWharves (+1 settlement mss), Guericke (+2 siege), etc. exist in code but not EffectSummary.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Add to EffectSummary *(recommended)* | Transparent tech panel |
| **B** | Discovery mechanics | Leave hidden |

**Your pick:** ___  
**Notes:** ___

---

### #12 · Naval unit defense `[P1]`

**What's wrong:** `SoldierAttackBonus` applies to galleys/deep-sea; defense bonuses do not.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Naval defense bonus *(recommended)* | Galleys/deep-sea get defense from naval tech |
| **B** | Glass cannon navy | Document intentional |
| **C** | Remove naval attack | Strip soldier attack from naval units |

**Your pick:** ___  
**Notes:** ___

---

## P1 — Match flow & cards

### #13 · Global narrative card queue `[P1]`

**What's wrong:** At turn start, narrative / feast / briefing / colloquy can race for the same crisis card panel.

| | Choice | Meaning |
|---|--------|---------|
| **A** | One card per turn start *(recommended)* | FIFO queue service |
| **B** | Strict priority | Crisis > Narrative > Feast > Briefing > Colloquy |
| **C** | Keep race | Document bootstrap order |

**Your pick:** ___  
**Notes:** ___

---

### #14 · Emphasis panels in blocking `[P1]`

**What's wrong:** Synodical/Tier2 emphasis block End Turn but not other event spawn.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Add to ChoiceCardBlocking *(recommended)* | Same mutual exclusion as crisis/narrative |
| **B** | Defer all turn-start events | Until emphasis resolved |

**Your pick:** ___  
**Notes:** ___

---

### #15 · Identity picker enforcement `[P1]`

**What's wrong:** Identity picker not in end-turn block list — can end turn without choosing identity at founding.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Block End Turn *(recommended)* | Until identity chosen |
| **B** | Default identity | Auto-assign if skipped |

**Your pick:** ___  
**Notes:** ___

---

### #16 · Legalism / antinomian double-hit `[P1]`

**What's wrong:** `ApplyConfessionalTurnLogic` applies population/adherence damage **before** crisis card; player gets hit twice.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Card only *(recommended)* | Remove pre-card stat hit |
| **B** | Pre-card only | Card is flavor |
| **C** | Split | 50% pre-card + card adds rest |

**Your pick:** ___  
**Notes:** ___

---

## P2 — Chronology & wins

### #17 · Dual clocks `[P2]`

**What's wrong:** Narrative day (+18/turn) and church civil date (+28/turn after Ascension) — dashboard can confuse.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Keep both, show both *(recommended)* | Explicit dual-clock dashboard |
| **B** | Unify to 28 | After Ascension |
| **C** | Unify to 18 | Always |

**Your pick:** ___  
**Notes:** ___

---

### #18 · Victory path pacing `[P2]`

**What's wrong:** Fame 120 / adherence streak / doctrine trio finish at different times; no capstone link.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Formula + witness cue *(recommended)* | Narrative fame + dashboard hint toward 120 |
| **B** | Lower fame threshold | 100 |
| **C** | Epilogue card only | Doctrine trio triggers optional card |
| **D** | Sandbox | Leave as-is |

**Your pick:** ___  
**Notes:** ___

---

### #19 · Lobby heresy pack vs AI rivals `[P2]`

**What's wrong:** Heresy pack shapes internal schisms; lobby opponents are orthodox AI synods — easy to misread.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Clarify lobby copy *(recommended)* | "AI neighbors + internal schism pool" |
| **B** | Schismatic lobby opponents | Optional |
| **C** | Rename setting | Heresy pack = crisis variety only |

**Your pick:** ___  
**Notes:** ___

---

## P2 — Theme & content

### #20 · Historical tier placement `[P2]`

**What's wrong:** Bach in Synodical tier, Mendel in Orthodoxy, etc. — historical drift for gameplay tiers.

| | Choice | Meaning |
|---|--------|---------|
| **A** | "Game era" copy *(recommended short-term)* | Labels = gameplay eras, not strict history |
| **B** | Move figures | Balance pass |
| **C** | Tooltips with dates | Keep tiers; add figure lifespan in panel |

**Your pick:** **A** — column headers `Era IV · Synodical` (short names); game-era note **detail pane only** (no column subtitle, no panel blurb)  
**Notes:** Figure lifespans already in detail when present; no full **C** (dates on tree buttons).

---

### #21 · Maxwell → siege engine `[P2]`

**What's wrong:** James Clerk Maxwell unlocks medieval-style siege engines — thematic stretch.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Artillery science retheme *(recommended)* | Copy + optional Chytraeus prereq |
| **B** | Earlier siege tech | Maxwell buffs pressure only |
| **C** | Flavor only | Keep mechanic; add "ordered mechanics" text |

**Your pick:** **A** — artillery retheme; **Maxwell + Chytraeus** to train; unit stays **Siege Engine** (field artillery via upgrades later)  
**Notes:** ___

---

### #22 · Coastal patrol description `[P2]`

**What's wrong:** Build DB says "fast riders"; unit is near-shore **naval**.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Fix copy *(recommended)* | Coastal scout boat |
| **B** | Rebrand unit | Mounted coastal patrol, land-only |

**Your pick:** **A** — already matches code (coastal scout boat copy)  
**Notes:** No change needed.

---

### #23 · Library track vs name `[P2]`

**What's wrong:** "Confessional Library" on secular production track.

| | Choice | Meaning |
|---|--------|---------|
| **A** | ConfessionalBuilding category *(recommended)* | Keep secular prod track |
| **B** | Confessional timer track | Move to manuscript/turn track |
| **C** | Tooltip only | No category change |

**Your pick:** **A** — already matches code (`ConfessionalBuilding` + secular prod track)  
**Notes:** No change needed.

---

### #24 · Art era vs narrative phase `[P2]`

**What's wrong:** Visual era follows tech tier only — can show "modern" art during Salvation History.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Gate on narrative + tier *(recommended)* | e.g. cap woodcut until Church Year |
| **B** | Tier only | Accept visual/history drift |

**Your pick:** **A** — already matches code (Salvation History caps visual tier at 2 / woodcut)  
**Notes:** No change needed.

---

## P3 — Schism & saturation

### #25 · Schism variety `[P3]`

**What's wrong:** Duplicate Libertine blocs possible; crisis can re-pick active heresy types.

| | Choice | Meaning |
|---|--------|---------|
| **A′** | Same-flavor reinforcement *(your pick)* | Crisis keeps its heresy flavor; matching active bloc grows instead of spawning a duplicate or swapping flavor |
| **A** | Dedupe by skipping active types | *(superseded by A′)* |
| **B** | Weight by pack | Prefer unused heresy pack entries |
| **C** | Wait | Next playtest |

**Your pick:** **A′** — antinomian crisis → Libertine bloc grows if already on map; legalism → Pharisaic; no forced flavor swap  
**Notes:** `ResolveSchism` → `ReinforceExistingBloc`; overflow card only when a **new** flavor hits the 3-bloc cap.

---

### #26 · Crisis banner at schism cap `[P3]`

**What's wrong:** Dashboard still shows "Crisis: antinomian schism" when 3 blocs already full.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Suppress type banner *(recommended)* | Show Union Strife / saturation only |
| **B** | Overflow copy | Redirect crisis card text |

**Your pick:** **A** — already matches code (suppress legalism/antinomian banner at 3-bloc cap; Union Strife shows instead)  
**Notes:** No change needed.

---

### #27 · Expand narrative spine `[P3]`

**What's wrong:** Tier A = 10 events; room for Tier B (~25 events).

| | Choice | Meaning |
|---|--------|---------|
| **A** | More pre-Ascension events | Prophets, Pentecost, martyr unlocks |
| **B** | Post-Formula beats | Chemnitz, Gerhard as narrative cards |
| **C** | Tier A only *(current)* | Until ≥3 full playtest matches |

**Your pick:** **C** — Tier A only for now; feast/martyr depth via virtue track **V** after audit batch playtest  
**Notes:** Avoid double-building narrative spine + **V**.

---

### #28 · Movable Easter / computus `[P3]`

**What's wrong:** Fixed LSB 1-year calendar; Easter season approximated.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Western computus | Movable Easter / Pentecost |
| **B** | Fixed LSB *(current)* | Keep as-is |

**Your pick:** **B** — keep fixed LSB 1-year calendar; defer computus  
**Notes:** Revisit after Church Year playtests if movable Easter matters.

---

## Virtue & observance (V) — Option C

**Humankind-inspired layer (merged Aug 2026).** Virtue tags accumulate from choices; **Law/Gospel** stays primary. Fame 120 unchanged — virtues shape *how* you win, not *whether*.

### V0 · Framework `[DECIDED]`

| Decision | Pick |
|----------|------|
| Model | Option C — grouped virtue tags |
| Win path | Flavor/stats + soft bonuses; no affinity-locked win |
| Primary axis | Law/Gospel |
| Secondary | Five virtue tags on choices |

### V1 · Virtue tags (v1) `[DECIDED]`

| Tag | Theme |
|-----|-------|
| **First Table** | Worship, Word, prayer (Cmd 1–3) |
| **Honor & Mercy** | Authority & life (4–5) |
| **Fidelity & Truth** | Integrity, catechesis (6–8) |
| **Neighbor & Contentment** | Community (9–10) |
| **Martyrial Witness** | Costly fidelity |

One primary tag per choice; optional secondary on major beats. Same tags on **narrative + church-year** cards.

### V2 · Observance loop `[DECIDED]`

First calendar hit → full choice card. Every later year → **annual tick B** (passive stat, no card). Martyrs = same pipeline as feasts.

### V4 · Fame integration `[DECIDED]`

No witness-fame gate. Legacy trait **variants** by dominant tag at 25/55 (optional V6).

### V5 · Annual tick magnitudes `[DECIDED]`

| | Choice | Meaning |
|---|--------|---------|
| **A** | Minimal | +1 comfort or adherence per feast-year |
| **B** | Moderate *(recommended)* | Tick follows first choice lean; tiny martyr fame |
| **C** | Dashboard only | Flavor until playtest |

**Your pick:** **B** — annual tick mirrors **first-choice lean** on that observance:

| First-choice lean | Annual tick |
|-------------------|-------------|
| Law / First Table / Honor | +1 adherence or +1 Law |
| Gospel / Neighbor / Mercy | +1 spiritual comfort or +1 Gospel |
| Martyrial Witness (martyr feasts) | lean tick **+** +1 confessional fame every **3rd** annual tick |

Dashboard one-liner per tick (no card): e.g. `St. Stephen (annual) · Martyrial Witness +1 comfort`.

### V6 · Legacy variants `[DECIDED]`

| | Choice | Meaning |
|---|--------|---------|
| **A** | Top tag → variant at 25/55 *(recommended)* | Reuse legacy slots |
| **B** | Display-only until playtest |
| **C** | New virtue oath slot |

**Your pick:** **A** — at fame **25** and **55**, dominant virtue tag tints legacy trait **name + soft modifier** in existing `SynodLegacyTraitId` slots (no new win gate; fame 120 unchanged).

**Implement after audit batch playtest.** Cross-links: audit **#18**, **#27**.

### V7 · Independent AI synod research `[DEFERRED — much later]`

Rivals should run their **own** confession research (same starting unlocks as player; optional higher starting tier on harder difficulty later). Today all factions share `ConfessionResearchManager` — AI builds only when **player** has unlocked the tech.

**Your pick:** defer · implement after virtue track / multi-synod balance pass  
**Notes:** Logged from guided pass #2 (Aug 2026).

### Dashboard dual-clock labels `[DONE]`

Church-year line reads e.g. `Salvation day 540 · Church Year: Lent · Mar 12` (not buried suffix). Logged from guided pass **#17** (Aug 2026).

---

## Progress tracker

**Commit `42a83ba`** applied branch **A** for most audit nodes (pre–guided-pass batch). Legend: ✅ in commit · ⚠️ partial · ❌ not in commit · 📋 design only

| # | Topic | Branch A means | Current |
|---|-------|----------------|---------|
| 1 | Adherence floors | Wire `EffectiveMinAdherenceFloor` from tech modifiers (Preus 40, Chytraeus +12, Riojas +5, CTCR 50) | ✅ **A** |
| 2 | AI synod production | Allow `TryStartAiBuild` when `SynodPlayer != Player1` | ✅ **A** · *follow-up: independent AI research (later)* |
| 2b | AI train scope | Extend AI: archer, horseman, mission house, wharf/siege chain | ✅ **A** |
| 3 | Walther/Pieper copy | Rewrite EffectSummary to match modifiers (+8 preach comfort; decay −10%) | ✅ **A** confirmed |
| 4 | Capital vs district | Capital metropolitan allow-list; districts keep exclusives | ✅ **A** confirmed |
| 5 | Siege + armory | **Local** armory only; train siege at Garrison district (UI + AI) | ✅ **B** confirmed |
| 6 | Pastor tech | Both TrainPastor and upgrade → **Large Catechism**; pastor slot via parish church or seminary/cathedral | ✅ **A** confirmed |
| 7 | Armory discount | −1 mss Soldier→Defender when **local** city has armory | ✅ **A** confirmed |
| 8 | Seminary placement | **BuildSeminary** on Scholastic district (+ capital metro list) | ✅ **A** confirmed |
| 9 | Hospital/granary | Match code to copy: hospital +1/turn; granary 50% growth tick | ✅ **A** confirmed |
| 10 | Gutenberg vs press | Printing press requires **GutenbergPress** (not Kepler) | ✅ **A** confirmed |
| 11 | Hidden modifier copy | Expose wharves mss, Guericke siege, naval defense in EffectSummary | ✅ **A** confirmed |
| 12 | Naval defense | `CoastalNavalDefenseBonus` on galleys/deep-sea | ✅ **A** confirmed |
| 13 | Card queue | `ChoiceCardQueue`: max **one** turn-start choice card | ✅ **A** confirmed |
| 14 | Emphasis blocking | Synodical/Tier2 emphasis in `ChoiceCardBlocking` | ✅ **A** confirmed |
| 15 | Identity picker | Block End Turn while identity panel open | ✅ **A** confirmed |
| 16 | Double-hit crisis | Remove pre-card pop/adherence hit; crisis card only | ✅ **A** confirmed |
| 17 | Dual clocks | Explicit labels (`Salvation day N · Church Year: …`) | ✅ **A** confirmed |
| 18 | Victory pacing | Formula witness cue + expand with virtue dashboard (**V4** soft) | ✅ **A** confirmed · partial in commit |
| 19 | Lobby copy | Clarify AI synod neighbors + heresy pack schism pool | ✅ **A** confirmed |
| 20 | Historical tiers | **A**: `Era IV · Synodical` headers + detail-pane era line | ✅ **A** confirmed |
| 21 | Maxwell siege | **A**: Maxwell + Chytraeus gate; Siege Engine name kept | ✅ **A** confirmed |
| 22 | Coastal patrol copy | Fix to coastal scout **boat** (not riders) | ✅ **A** |
| 23 | Library category | **ConfessionalBuilding** category; keep secular prod track | ✅ **A** |
| 24 | Art era gating | Cap visual era during **Salvation History** phase | ✅ **A** confirmed |
| 25 | Schism dedupe | **A′**: same-flavor crisis reinforces existing bloc | ✅ **A′** confirmed |
| 26 | Crisis banner cap | **A**: suppress schism-type banner; Union Strife at cap | ✅ **A** confirmed |
| 27 | Narrative Tier B | **C** — Tier A only; **V** for feasts/martyrs later | ✅ **C** confirmed |
| 28 | Computus | **B** — fixed LSB calendar | ✅ **B** confirmed |
| V0–V4 | Virtue framework | Option C, 5 tags, first-then-annual, soft fame | 📋 **decided**; not coded |
| V5 | Annual tick strength | **B** — lean-following tick; martyr +1 fame / 3yr | ✅ **B** confirmed |
| V6 | Legacy variants | **A** — dominant tag tints legacy at 25/55 | ✅ **A** confirmed |

---

## After implementing a sprint

Smoke-test with [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md). Do not maintain a second checklist in this file.

| After | Spot-check |
|-------|------------|
| P0 | CTCR floor / AI rival builds / Walther copy honest |
| P1 | Siege + card queue + identity at founding |
| V | Feast choice → annual tick (when coded) |

---

## When you resume in chat

**Next session default:** playtest first via [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md), then say **"Continue guided audit pass from node #1"** when ready for design picks.

One node at a time: restate tradeoffs → you pick → optionally implement that node only.

*Saved Aug 2026 · doc roles cleaned Aug 7, 2026 · checkpoint Aug 7 evening.*
