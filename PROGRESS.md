# BoC4x — Session checkpoint

**Updated:** August 7, 2026 (evening)  
**Scene:** `Assets/Scenes/SampleScene.unity` · Unity 6000.5.x · branch `master`  
**No save/load** — Exit Play / recompile wipes the match.

---

## Doc map (one job each)

| File | Use for |
|------|---------|
| **This file (`PROGRESS.md`)** | Where you left off · lobby defaults · last session bullets |
| [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md) | **Next** playtest checklist |
| [`GUIDED-AUDIT-PASS.md`](GUIDED-AUDIT-PASS.md) | Design backlog — pick A/B/C per audit node |
| [`AUDIT-DECISION-TREE.md`](AUDIT-DECISION-TREE.md) | Tech detail for guided nodes |
| [`SYNC.md`](SYNC.md) | Local PC ↔ GitHub sync |

Do **not** paste full playtest results or audit node text into this file.

---

## Resume here (next session)

**Lobby:** solo · Grand · Archipelago · Full canon · coastal capital · fame **120**

1. Open scene → wait for recompile if needed → Play → **fresh lobby match**.  
2. Work [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md) top to bottom.  
3. Design backlog when ready: [`GUIDED-AUDIT-PASS.md`](GUIDED-AUDIT-PASS.md) from node **#1**.

### Last session (Aug 7)

**Playtest:** Solo coastal Grand → Tier 6 doctrine win. Softlock/HUD/clergy held. District offers never appeared while growing (auto-defer bug). Schism ~t64. Wanted city camera.

**Shipped after:**
- District offers **block** End Turn (no silent auto-defer); Esc = Not now; break-even + full housing can qualify
- Capital/granary food bump; Growth shows `food ±N (prod/cons)`
- **Home** cycles player cities
- EditMode: retaliation weight assert, chronology Instance bind (EditMode skips Awake), float potency assert — **all EditMode green**

### Parked

- Modal stack `[N panels open]` — low value  
- Unit XP / veterancy — design later  
- Guided audit P0+ + virtue track — see guided pass  
- Explorer water-only redesign — only if design wants galley-like rules  

---

## Lobby & victory (canonical)

| Setting     | Value            |
| ----------- | ---------------- |
| Players     | 1 (solo smoke)   |
| Map         | Grand · Archipelago |
| Heresy pack | Full canon       |
| Capital     | Coastal          |
| Fame win    | **120**          |

**Win:** 100% adherence × 5 · CTCR+Nagel+GLF · 120 fame  
**Lose:** wiped · pop 0 for 2 turns · adherence 0% · schism captures your capital  

---

*Update only the Resume / Last session / Parked sections after each work session.*
