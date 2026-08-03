# Guided audit pass — saved for later

**Status:** In progress · **Resume at:** Node **#1** (P0)  
**Companion:** [`AUDIT-DECISION-TREE.md`](AUDIT-DECISION-TREE.md) (technical detail + file lists)

**Also decided (virtue track V):** Option C framework — see [Virtue & observance (V)](#virtue--observance-v--option-c) below. Implement **after P0–P1** unless a node explicitly overlaps (#18, #27).

---

## How to use this doc

1. Work **top to bottom** — do not skip P0 before P1.
2. For each node: read **What's wrong** → pick **A / B / C** (or sub-choice) → write your choice in **Your pick**.
3. When you finish a node and want it coded, tell the agent: *"Implement audit node #N with branch X."*
4. Playtest using the checklist at the bottom after each sprint.

**Traversal order (recommended):**  
P0: 1 → 2 → 3 → P1: 2-follow-up → 4 → 5 → 6 → 7 → 10 → 13 → 14 → 15 → 16 → 8 → 9 → 11 → 12 → P2: 17–24 → P3: 25–28 → **V: virtue implementation (after P1)**

---

## Note on uncommitted draft code

An earlier session **mistakenly implemented branch A for all nodes** before you chose. That diff may still be in your working tree. When you resume:

- **Option A:** `git checkout` / discard those changes and decide fresh from this doc.
- **Option B:** Review the diff node-by-node; keep what matches your picks, revert the rest.

This guided pass assumes **you** are choosing — nothing is decided until you fill in **Your pick**.

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

**Your pick:** ___  
**Notes:** ___

---

### #21 · Maxwell → siege engine `[P2]`

**What's wrong:** James Clerk Maxwell unlocks medieval-style siege engines — thematic stretch.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Artillery science retheme *(recommended)* | Copy + optional Chytraeus prereq |
| **B** | Earlier siege tech | Maxwell buffs pressure only |
| **C** | Flavor only | Keep mechanic; add "ordered mechanics" text |

**Your pick:** ___  
**Notes:** ___

---

### #22 · Coastal patrol description `[P2]`

**What's wrong:** Build DB says "fast riders"; unit is near-shore **naval**.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Fix copy *(recommended)* | Coastal scout boat |
| **B** | Rebrand unit | Mounted coastal patrol, land-only |

**Your pick:** ___  
**Notes:** ___

---

### #23 · Library track vs name `[P2]`

**What's wrong:** "Confessional Library" on secular production track.

| | Choice | Meaning |
|---|--------|---------|
| **A** | ConfessionalBuilding category *(recommended)* | Keep secular prod track |
| **B** | Confessional timer track | Move to manuscript/turn track |
| **C** | Tooltip only | No category change |

**Your pick:** ___  
**Notes:** ___

---

### #24 · Art era vs narrative phase `[P2]`

**What's wrong:** Visual era follows tech tier only — can show "modern" art during Salvation History.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Gate on narrative + tier *(recommended)* | e.g. cap woodcut until Church Year |
| **B** | Tier only | Accept visual/history drift |

**Your pick:** ___  
**Notes:** ___

---

## P3 — Schism & saturation

### #25 · Schism variety `[P3]`

**What's wrong:** Duplicate Libertine blocs possible; crisis can re-pick active heresy types.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Dedupe by heresy type *(recommended)* | Skip active types when allocating |
| **B** | Weight by pack | Prefer unused heresy pack entries |
| **C** | Wait | Next playtest |

**Your pick:** ___  
**Notes:** ___

---

### #26 · Crisis banner at schism cap `[P3]`

**What's wrong:** Dashboard still shows "Crisis: antinomian schism" when 3 blocs already full.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Suppress type banner *(recommended)* | Show Union Strife / saturation only |
| **B** | Overflow copy | Redirect crisis card text |

**Your pick:** ___  
**Notes:** ___

---

### #27 · Expand narrative spine `[P3]`

**What's wrong:** Tier A = 10 events; room for Tier B (~25 events).

| | Choice | Meaning |
|---|--------|---------|
| **A** | More pre-Ascension events | Prophets, Pentecost, martyr unlocks |
| **B** | Post-Formula beats | Chemnitz, Gerhard as narrative cards |
| **C** | Tier A only *(current)* | Until ≥3 full playtest matches |

**Your pick:** ___  
**Notes:** ___

---

### #28 · Movable Easter / computus `[P3]`

**What's wrong:** Fixed LSB 1-year calendar; Easter season approximated.

| | Choice | Meaning |
|---|--------|---------|
| **A** | Western computus | Movable Easter / Pentecost |
| **B** | Fixed LSB *(current)* | Keep as-is |

**Your pick:** ___  
**Notes:** ___

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

### V5 · Annual tick magnitudes `[OPEN]`

| | Choice | Meaning |
|---|--------|---------|
| **A** | Minimal | +1 comfort or adherence per feast-year |
| **B** | Moderate *(recommended)* | Tick follows first choice lean; tiny martyr fame |
| **C** | Dashboard only | Flavor until playtest |

**Your pick:** ___

### V6 · Legacy variants `[OPEN]`

| | Choice | Meaning |
|---|--------|---------|
| **A** | Top tag → variant at 25/55 *(recommended)* | Reuse legacy slots |
| **B** | Display-only until playtest |
| **C** | New virtue oath slot |

**Your pick:** ___

**Implement after P1.** Cross-links: audit **#18**, **#27**.

---

## Progress tracker

| # | Topic | Your pick | Implemented? |
|---|-------|-----------|--------------|
| 1 | Adherence floors | | |
| 2 | AI synod production | | |
| 2b | AI train scope | | |
| 3 | Walther/Pieper copy | | |
| 4 | Capital vs district | | |
| 5 | Siege + armory | | |
| 6 | Pastor tech | | |
| 7 | Armory discount | | |
| 8 | Seminary placement | | |
| 9 | Hospital/granary | | |
| 10 | Gutenberg vs press | | |
| 11 | Hidden modifier copy | | |
| 12 | Naval defense | | |
| 13 | Card queue | | |
| 14 | Emphasis blocking | | |
| 15 | Identity picker | | |
| 16 | Double-hit crisis | | |
| 17 | Dual clocks | | |
| 18 | Victory pacing | **V4: soft only** | |
| 19 | Lobby copy | | |
| 20 | Historical tiers | | |
| 21 | Maxwell siege | | |
| 22 | Coastal patrol copy | | |
| 23 | Library category | | |
| 24 | Art era gating | | |
| 25 | Schism dedupe | | |
| 26 | Crisis banner cap | | |
| 27 | Narrative Tier B | **→ Virtue track V** | |
| 28 | Computus | | |
| V0–V4 | Virtue framework | **Decided** | |
| V5 | Annual tick strength | | |
| V6 | Legacy variants | | |

---

## Playtest checklist (after each sprint)

| After | Verify |
|-------|--------|
| P0 | CTCR floor holds in crisis; AI rival builds wharf/soldier; Walther tech panel honest |
| P1 cities | Garrison siege works; capital/district rules feel intentional |
| P1 flow | ≤1 narrative card per turn; identity required at founding |
| P2 | Dashboard clocks clear; Formula/fame + virtue summary understood |
| V | First feast choice → annual tick; tags visible on dashboard |
| Full | Solo coastal Grand, fame 120, Ascension before turn 40, 0–3 schisms |

---

## When you resume in chat

Say: **"Continue guided audit pass from node #1"** (or **#N** if you've filled picks above).

We'll do one node at a time: I'll restate the tradeoffs, you pick, then optionally implement that node only.

*Saved Aug 2026.*
