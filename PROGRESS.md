# BoC4x — Session Checkpoint

**Saved:** July 28, 2026 (playtest aborted — turn skip bug; fix committed)  
**Project:** Book of Concord 4X prototype (`C:\Users\stmit\BoC4x`)  
**Engine:** Unity 6000.5.x — scene `Assets/Scenes/SampleScene.unity`  
**Latest commit:** turn/unit-cycle fix after schism (stale `FinishAiTurn`); compile fixes (`EraBranchRules` Linq, capital food `out`)

---

## Resume here (next session)

1. Open **`Assets/Scenes/SampleScene.unity`** → wait for recompile, then Play (solo · Grand · Archipelago · Full canon).
2. **Smoke-test the turn fix first (post-schism):** End Turn several times after a schism — turn counter must advance **one** per round; banner should show `Orders X/Y of Z` when scout + missionary both need orders. Console logs `Turn advanced N → N+1`.
3. **Test Runner:** Window → General → Test Runner → **EditMode** → **BoC4x.Editor.Tests** → Run All.
4. **Playtest order:** Phase **B** (Sustainable Wittenberg + districts) → **G** retest block → **C** antinomian / third schism.
5. Mark pass/fail rows in the checklist below.

**Note:** Empty Test Runner was fixed by `BoC4x.asmdef` + `BoC4x.Editor.Tests.asmdef`. Unity AI `NoSubscription` console spam is harmless (ignore or remove AI Assistant package).

**Priority unverified (coded, needs editor):**

| Area | What to check |
|------|----------------|
| **Turn / unit cycle (Jul 28 fix)** | No 4→6 / 8→10 skips after schism; both units in order queue |
| **Three research queues** | **T** → Doctrine / Culture / Secular tabs; Q/E cycles tabs |
| **Sustainable Wittenberg** | Found pop **15**; food preview; +6 urban food; 8-turn deficit grace — playtest saw odd pop/food after schism (recheck) |
| **Emphasis / forks** | Partial/deepened/full reception; study colloquy; Augsburg → Law +8% |
| **Phase G** | Most rows still open |

**No save/load** — Play mode resets on recompile; use a fresh match for verification.

---

## This session (Jul 28) — implemented

- **Turn skip fix:** `SimpleAI.FinishAiTurn` ignores stale Invokes on player turn; cancel stacked Invokes; schedule with expected faction/bloc guard.
- **Unit cycle:** Banner `Orders X/Y of Z`; no raw `EndTurn` fallback when phased end-turn is blocked; missionary still needs orders when preach-ready at 0 MP.
- **Schism peel:** `ConvertToSchismaticBloc` re-registers unit on schismatic faction list.
- **Compile:** `EraBranchRules` + `using System.Linq`; capital founding food assigns `foodSurplus` before early return.
- **Playtest aborted** mid Phase B after schism — turn counter and unit cycle were broken; not a clean pass/fail session.

**Key files:** `SimpleAI.cs`, `PlayerUnitCycle.cs`, `TurnManager.cs`, `EndTurnPhaseController.cs`, `Unit.cs`, `FirstSteps.cs`, `EraBranchRules.cs`, `CityGrowthSystem.cs`.

**Still not expanded:** Full Decision 16 era forks (only Augsburg↔Gutenberg, Mission↔Bach).

---

## Current playthrough


| Setting     | Value                                        |
| ----------- | -------------------------------------------- |
| Players     | **1** (solo — no rival synods, no diplomacy) |
| Map size    | **Grand** (80×52)                            |
| Coasts      | **Archipelago**                              |
| Heresy pack | **Full canon** (up to 3 schismatic blocs)    |
| Capital     | **Coastal** (for naval testing)              |


**Resume playtest track:** Smoke-test turn fix → Phase **B** then **G** — not a single in-progress save.

**Unity startup:** Hub → **BoC4x** (`6000.5.2f1`) → double-click **`Assets/Scenes/SampleScene.unity`** before Play. A blank **Untitled** scene (Main Camera only) is not the game.

**Previously confirmed:** Phase A; G1 core gates; nomadic founding; asymmetric tech; legalism + schism spawn.  
**Automated (EditMode):** `EmphasisGateTests` (6) — run full test list in **Resume here** on return.

**Phase log:** Report pass/fail per row below.

---



## Playtest checklist

Mark `[x]` as you go. Suggested order: **A → B → C → D → E**, then **F** in a separate lobby match.

### Phase A — Re-verify fixes (do first)


|      | Test                       | Pass criteria                                                                             |
| ---- | -------------------------- | ----------------------------------------------------------------------------------------- |
| [ x] | **Crisis loop (~turn 12)** | End Turn → crisis card → pick option → End Turn once → turn advances (no same card again) |
| [x ] | **Crisis card UI**         | Buttons appear and respond; no console `CrisisCardPanel.Show failed`                      |
| [x ] | **Scout survey counter**   | Only your scout counts; cap shows **10/10** max                                           |
| [x ] | **Multi-turn march**       | Move beyond 1 turn of MP → End Turn → unit continues; click unit tile to cancel           |
| [x ] | **Phased end turn**        | Growth → Migration → Production → Confessional runs **once** per turn                     |




### Phase B — Core solo loop (turns 1–25)


|      | Test                          | Pass criteria                                                            |
| ---- | ----------------------------- | ------------------------------------------------------------------------ |
| [x ] | **Nomadic founding**          | Preach → survey 10 hexes → bind catechism → **F** on valid coastal site  |
| [ ]  | **Organic districts only**    | District offer after food surplus — no manual hamlet/colonist founding   |
| [ ]  | **District specialty picker** | Seminary / Garrison / Market / Scholastic; district builds only its list |
| [ ]  | **Population sync**           | Dashboard faction pop = sum of city + district pops after phases         |
| [ ]  | **Sustainable Wittenberg**    | Found capital → pop **15**; placement hover shows **~food / mouths**; capital **+6** urban food; **8-turn** deficit grace; confessional pop + only when food surplus > 0 |
| [ ]  | **G appeal overlay**          | **G** toggles growth heatmap in synod territory                          |
| [ ]  | **Clergy roster**             | **R** — slot limits, pastor cap per parish church, chaplain assignments  |
| [ x] | **Asymmetric tech**           | Secular tree at low adherence; bonuses dormant until **>40%** adherence  |
| [ ]  | **Legacy slots**              | Fame milestones → legacy picker (replace, not stack)                     |




### Phase C — Crisis & schism (Full canon)


|      | Test                       | Pass criteria                                                   |
| ---- | -------------------------- | --------------------------------------------------------------- |
| [x ] | **Legalism crisis**        | Card blocks End Turn; resolve or schism with correct heresy     |
| [ ]  | **Antinomian crisis**      | Same — no re-queue while crisis active                          |
| [x ] | **Doctrinal drift stages** | Drift cards at pressure stages; recovery vs schism paths        |
| [x ] | **Schism spawn**           | Bloc gets capital + units; parent weakened; bloc always hostile |
| [x ] | **Second schism**          | Different heresy profile from first                             |
| [ ]  | **Third schism cap**       | Third bloc spawns; no fourth (max 3 concurrent)                 |
| [ ]  | **Siege + preaching**      | Martial + clergy erode loyalty; no instant capture              |




### Phase D — Naval & archipelago

**Prereq:** Research **Frontier Mission** (`MissionarySending`).


|     | Test                        | Pass criteria                                                                                |
| --- | --------------------------- | -------------------------------------------------------------------------------------------- |
| [ ] | **Coastal Market district** | Organic **Market** on hex touching **naval coast** (not necessarily capital)                 |
| [ ] | **Build Dock**              | **C** on that district → Build Dock (3 mss, 2 turns) — coast only                            |
| [ ] | **Coastal Galley**          | After Dock → Build Coastal Galley (3 mss, 2 turns)                                           |
| [ ] | **Galley movement**         | Shore + navigable water; blocked on deep impassable water                                    |
| [ ] | **Galley cargo panel**      | Select galley → bottom-right **0/2** slots                                                   |
| [ ] | **Embark / land troops**    | Load soldier; select passenger; click shore to land                                          |
| [ ] | **Coastal Patrol**          | Optional: train from Market district                                                         |
| [ ] | **Synod trade**             | Two hubs (Market Hall or Dock) within **4 hexes**, same cluster → **+1 mss/link**/production |


**Dock reminder:** Capital need not be coastal — use an organic **Market** district on the coast.

### Phase E — Expansion & mid-game


|     | Test                      | Pass criteria                                                                        |
| --- | ------------------------- | ------------------------------------------------------------------------------------ |
| [ ] | **Mission House**         | Build at capital or Market district                                                  |
| [ ] | **Frontier settler**      | With **1** independent city → train settler → **F** for second city (6+ hex spacing) |
| [ ] | **Settler → missionary**  | After founding, settler converts; no duplicate in field                              |
| [ ] | **City production queue** | **C** → start/cancel; phased production resolves                                     |
| [ ] | **Unit upgrades**         | Missionary→Pastor, Soldier→Defender, etc. on city hex (**U**)                        |
| [ ] | **Art era shift**         | Tier 3 or 5 tech → visual era transition                                             |




### Phase G — Emphasis systems (Tier 2 + related)

**Scope:** Match-gated confessional/culture emphasis (`MatchHistory`, `EmphasisGateRules`, `Tier2EmphasisManager`), card UX, secondary colloquies, tech-panel hints, synod brief. Synodical (Tier 4), pastoral briefings, and schism saturation included as related checks.

**Prereq:** `Assets/Scenes/SampleScene.unity` open · solo · Full canon heresy pack.

**Retest later (Jul 27 — thematic + fork pass, not yet playtested):**

| Area | What to verify |
|------|----------------|
| **G5 Augsburg emphasis** | Pick Augsburg → Walther dashboard **Law** drift ~**+8%** faster (not soldier +3) |
| **G8/G9 copy** | **T** panel uses *partial reception*, *deepened reception*, *full reception* (not raw “50% potency”) |
| **G9 study colloquy (2B)** | After integration, start deferred sibling → pays **research + 4 mss** study colloquy (tier 2); unlock log shows **deepened reception** |
| **G9 dual path (1C)** | Complete **both** siblings in a fork (e.g. Augsburg + Gutenberg) → deferred path shows **full reception** on dashboard / unlock log |
| **G9 integration cards** | Body mentions study colloquy + both-paths → full reception |
| **G6 dashboard** | Era-path line shows reception tier labels after integration / study / dual complete |

**Automated (run first):** `Window → General → Test Runner → EditMode → EmphasisGateTests` — all 6 pass. Also run **`EraBranchRulesTests`** (fork potency + study colloquy cost).

#### G1 — Confessional emphasis gates (Smalcald vs Augsburg)

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [x] | **Formula always** | Unlock **Confessional Emphasis** before any schism | Card shows **Internal — Formula** only; body notes external paths can wait |
| [x] | **No external without schism** | Same as above | No Augsburg or Smalcald buttons |
| [ ] | **Schism, no contact** | First schism, no scout sight / no schismatic fight yet | Formula only; hint text mentions scout (Augsburg) and battle (Smalcald) |
| [x] | **Augsburg — scout** | Move scout until a **schismatic unit** is visible **or** their **capital hex is explored** | **External — Augsburg** appears (on primary card if still pending, or on colloquy later) |
| [x] | **Smalcald — schismatic combat only** | Attack/defend vs schismatic with soldier or missionary | **External — Smalcald** appears; **generic non-schismatic combat does not unlock Smalcald** |
| [x] | **Both externals** | Scout contact, then fight same bloc | Both Augsburg and Smalcald on card |
| [ ] | **Scout contact persists** | Register contact, then lose sight of unit | Augsburg still offered (contact is match history, not line-of-sight) |

#### G2 — Culture emphasis gates (Chorale vs Gerhardt)

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [ ] | **Chorale always** | Unlock **Confessions Culture Emphasis** before any combat | **Chorale — liturgical order** only |
| [ ] | **Gerhardt — any combat** | After **any** player battle (not schismatic-specific) | **Gerhardt — cross & comfort** appears on culture card |
| [ ] | **Pre-combat hint** | Culture card before first fight | Body explains hymnody stays ordered until war teaches the cross |

#### G3 — Choice cards & End Turn

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [ ] | **Blocks End Turn** | Emphasis card open → click End Turn | Turn does **not** advance until a button is picked |
| [ ] | **Esc — confessional** | **Esc** on confessional card | Defaults to **Formula (internal)**; End Turn works |
| [ ] | **Esc — culture** | **Esc** on culture card | Defaults to **Chorale**; End Turn works |
| [ ] | **Re-open after dismiss** | End Turn with pending emphasis (card failed to show) | `EnsurePendingChoicesVisible` re-shows card |
| [ ] | **Tech panel re-open** | Open confession tech panel (**T**) while emphasis still pending | Card appears from panel refresh |

#### G4 — Secondary colloquy (half potency, 3 mss)

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [x] | **Confessional colloquy trigger** | Pick primary (e.g. Formula) → complete **Large Catechism** | **Confessional Colloquy** offers paths you **did not** pick (gated by current scout/combat state) |
| [ ] | **Colloquy cost** | Pick secondary with **≥3 mss** | −3 mss; banner shows secondary at **50%** potency |
| [ ] | **Colloquy broke** | Pick secondary with **<3 mss** | Choice postponed; banner says need 3 mss |
| [ ] | **Not now** | Dismiss colloquy with **Not now** | Primary unchanged; no secondary applied |
| [ ] | **Culture colloquy trigger** | Pick Chorale → complete **Chorale Tradition** or **Paul Gerhardt** | **Culture Colloquy** offers Gerhardt if you had combat and didn't pick it primary |

#### G5 — Modifiers & tech tree (emphasis ≠ lockout)

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [ ] | **Formula bonus** | Pick Formula | Antinomian guard active in modifiers |
| [ ] | **Augsburg bonus** | Pick Augsburg | Civic restraint (Law) grows ~**+8%** faster on Walther line |
| [ ] | **Smalcald bonus** | Pick Smalcald | Wilderness preaching +1 mss where applicable |
| [ ] | **Chorale bonus** | Pick Chorale | Settlement adherence decay −20% |
| [ ] | **Gerhardt bonus** | Pick Gerhardt | +5 spiritual comfort per turn |
| [ ] | **Tech tree open** | After any emphasis pick | **Formula of Concord**, **Augsburg Confession**, **Smalcald Articles** (and culture techs) still researchable in tree |
| [ ] | **Tech detail hints** | Select Confessional / Culture Emphasis in **T** panel | Hint text matches scout/combat gates, secondary, and integration unlocks |
| [ ] | **Document vs emphasis split** | Unlock **Augsburg Confession** without Augsburg emphasis | Doc gives siege +1 at **partial reception**; Law +8% only from emphasis |
| [ ] | **Formula guard on emphasis only** | Formula emphasis, no Formula doc | Antinomian guard from emphasis; doc decay bonus separate |
| [ ] | **Wilderness mss cap** | Stack Smalcald + Linnaeus + Gutenberg + emphasis | Total wilderness mss bonus capped at **+3** |

#### G8 — Document / emphasis split (Knob 3)

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [ ] | **Tagline in tech panel** | Select Formula / Augsburg / Smalcald / Chorale / Gerhardt in **T** | Shows *Emphasis is how we live; confessions are what we bind.* |
| [ ] | **50% doc without emphasis** | Research confession doc before matching emphasis | Numeric doc bonuses at half; **Legalism guard** on Gerhard doc stays full |
| [ ] | **Full doc with emphasis** | Take matching emphasis line | Document numeric bonuses at 100% |
| [ ] | **Iron reveal doc-only** | Unlock Augsburg Confession | Iron appears on map; independent of emphasis choice |

#### G9 — Era forks + integration recovery

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [ ] | **Confessions fork** | Unlock **Augsburg** (not Gutenberg) | **Gutenberg** shows × era-locked in **T** panel |
| [ ] | **Synodical culture fork** | Unlock **Missionary Sending** (not Bach) | **Liturgical Cantatas** era-locked |
| [ ] | **Integration reopens sibling** | Confessional integration after **Synodical Governance** | **Gutenberg** becomes `+` with **partial reception** hint |
| [ ] | **Study colloquy** | Start research on reopened sibling | **+4 mss** study colloquy (tier 2); unlock at **deepened reception (75%)** |
| [ ] | **Dual path full reception** | Unlock **both** Augsburg and Gutenberg | Dashboard / unlock log: deferred path at **full reception** |
| [ ] | **Tertiary emphasis** | After confessional integration | Third confessional emphasis card (e.g. Smalcald if Formula+Augsburg) |
| [ ] | **Colloquy defer** | **Not now** on colloquy | Card hidden 5 turns; body notes defer period |

**Active fork groups:** `Augsburg ↔ Gutenberg` (confessional track); `Missionary Sending ↔ Bach` (culture track).

#### G6 — UI & status

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [ ] | **Dashboard line** | After primary emphasis chosen | Dashboard shows `Emphasis — Confession: …` / `Culture: …` |
| [ ] | **Dashboard + secondary** | After secondary colloquy (e.g. Augsburg) | Line shows potency: `+ Augsburg (public) (50%, secondary)` |
| [ ] | **Integration potency** | Primary + secondary + **Synodical Governance** / **CTCR Reports** | Integration colloquy; secondary rises to **75%** |
| [ ] | **Y synod brief** | Press **Y** after choices | **WALTHER DIALECTIC** section includes tier-2 + synodical emphasis lines (primary + secondary) |
| [ ] | **Awaiting choice** | Card still open | Dashboard shows **Confessions emphasis — choose your path** |

#### G7 — Related emphasis systems (same session)

| | Test | How | Pass criteria |
|---|------|-----|---------------|
| [ ] | **Synodical (Tier 4)** | Unlock **Synodical Emphasis** | Walther vs Pieper card; Esc → Walther default |
| [ ] | **Synodical secondary** | Complete **Johann Gerhard** | Other path at **4 mss**, half potency |
| [ ] | **Pastoral briefing** | Play past turn 6 | Periodic Law/Gospel quote card; **Esc** defers (−2 adherence); blocks End Turn only while open |
| [ ] | **Schism saturation** | Force 4th schism while 3 blocs active | **Dissent without schism** overflow card (Colloquy / Feed / Purge) — not silent fail |

**Suggested solo path:** Found Wittenberg → tier-1 prereqs → **Confessional Emphasis** (Formula) → **Culture Emphasis** (Chorale) → push Walther drift → first schism → scout bloc → complete **Large Catechism** → Augsburg on colloquy → skirmish schismatic → Smalcald on colloquy if missed → late game **Mutual Conference** / **CTCR Reports** integration colloquies → verify dashboard potency lines and document 50% rule.

**Document vs emphasis (locked):** confession *documents* grant institutional bonuses (decay, siege, cantor comfort, etc.); *emphasis cards* grant posture bonuses (guards, Law/Gospel drift, wilderness mss, settlement decay). Unmatched docs run at partial reception until matching emphasis is adopted.

**Gate rules (locked design):**

| Path | Gate |
|------|------|
| Formula (internal) | Always |
| Augsburg | Active schism + scout contact (visible schismatic unit or explored schismatic capital) |
| Smalcald | Active schism + player combat vs schismatic bloc |
| Chorale | Always |
| Gerhardt | Any player combat |

---

### Phase F — Optional second match (2–4 players)


|     | Test                | Pass criteria                                                 |
| --- | ------------------- | ------------------------------------------------------------- |
| [ ] | **Rival synods**    | Strasbourg / Magdeburg / Nuremberg spawn with soldier + scout |
| [ ] | **Diplomacy panel** | **D** — rivals start at war                                   |
| [ ] | **Propose truce**   | 2 mss → 10-turn peace                                         |
| [ ] | **Declare war**     | Breaks truce                                                  |
| [ ] | **AI synod schism** | Rival schisms under Walther pressure                          |
| [ ] | **AI naval**        | Coastal AI builds Dock + galley                               |


Schismatic blocs **ignore diplomacy** in all cases.

### Skip for solo archipelago run

- Diplomacy / truce / declare war  
- Rival synod personalities and AI-vs-AI combat  
- Cross-player trade (stub is same-player cluster links only)



### Victory path (long horizon)

- 95%+ adherence for 5 turns, **or** CTCR + Nagel + Global Lutheran Fellowship, **or** 75 confessional fame

---



## Implementation ledger (decision forks)


| Fork | Topic                                   | Status             |
| ---- | --------------------------------------- | ------------------ |
| 1    | AI synod schism (players 2–4)           | ✅                  |
| 2    | Mission House → frontier settler        | ✅                  |
| 3    | Population sync (faction ← cities)      | ✅                  |
| 4    | Organic-only districts                  | ✅                  |
| 5    | AI synod personalities                  | ✅                  |
| 6    | Asymmetric adherence / secular research | ✅                  |
| 7    | Galley cargo UI + synod trade stub      | ✅                  |
| —    | Diplomacy panel (rival synods)          | ✅                  |
| —    | Crisis end-turn loop fix                | ✅ (needs playtest) |
| —    | Document / emphasis split (Knob 3)      | ✅ (needs playtest) |
| —    | Integration colloquy (Knob 1)         | ✅ (needs playtest) |
| —    | End Turn block banner + unit queue fix  | ✅ (needs playtest) |
| —    | Era forks + integration sibling @ 50% | ✅ (needs playtest) |
| —    | Tertiary confessional emphasis @ integration | ✅ (needs playtest) |
| —    | Confessional UI vocabulary + reception tiers | ✅ (needs playtest) |
| —    | Study colloquy @ fork research start (2B) | ✅ (needs playtest) |
| —    | Dual-path full reception (1C) | ✅ (needs playtest) |
| —    | Augsburg emphasis → Law +8% (3C) | ✅ (needs playtest) |
| —    | Sustainable Wittenberg (founding food) | ✅ (needs playtest) |
| —    | Three research queues (Doctrine / Culture / Secular) | ✅ (needs playtest) |
| —    | Post-schism turn skip / unit cycle (FinishAiTurn) | ✅ (needs retest) |


---

*Update this file at the end of each major session.*