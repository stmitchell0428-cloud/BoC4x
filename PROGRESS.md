# BoC4x — Session checkpoint

**Updated:** August 8, 2026  
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

1. Exit Play → recompile → Play → **fresh lobby match**.  
2. Work [`PLAYTEST-AUDIT-BATCH.md`](PLAYTEST-AUDIT-BATCH.md) top to bottom.  
3. Design backlog when ready: [`GUIDED-AUDIT-PASS.md`](GUIDED-AUDIT-PASS.md) from node **#1**.

### Last session (Aug 8)

**Playtest:** Solo coastal Grand → win. Notes: early districts, missing move rings, galley lake spawn, lake wharf, clergy UX, explorer portage, restraint/Loci copy.

**Shipped after:**
- District pacing: unlock after **8** turns since founding, streak **2**, max **6** districts, longer defer/decline cooldown
- Move rings on **Explored** tiles; selecting a unit no longer fights appeal overlay for rings
- Wharf/Dock/Galley/Deep-sea need **ocean**; fishing/explorer still allow lakes
- Naval spawn prefers **ocean**; ships may path through **friendly** city/garrison hexes
- Explorer: no free ocean↔lake portage across shared coastal land
- Clergy roster: chaplain section + hints
- Card/legacy copy: Civic Restraint (Law); Gerhard’s Loci Guard mechanics spelled out

### Parked (larger design)

- Modal stack `[N panels open]` — low value  
- Unit XP / veterancy  
- Guided audit P0+ + virtue track  
- **Rivers in map gen** — real river links for explorer (terrain options + generation)  
- Clergy stack with military/trade (cargo slots); LCMS church/seminary/hospital name picker  
- **Missionary home-church** rework (no free-floating Field)  
- New district specialties (harbor/mercy/press/mission house) beyond ceiling raise  

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
