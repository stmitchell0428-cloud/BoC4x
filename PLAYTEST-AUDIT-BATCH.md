# Playtest checklist — next session

**Purpose:** Verify Aug 7 evening fixes in a fresh match. Click `- [ ]` → `- [x]`.  
**Resume / lobby:** [`PROGRESS.md`](PROGRESS.md)  
**Design backlog (not this list):** [`GUIDED-AUDIT-PASS.md`](GUIDED-AUDIT-PASS.md)

**Before Play:** Exit Play → recompile → **new match from lobby** (no save/load).

**Lobby:** Solo · Grand · Archipelago · Full canon · coastal · fame **120**

**Pre-check:** EditMode → BoC4x.Editor.Tests → Run All should be green (was green end of Aug 7).

---

## Pre-flight

- [ ] Scripts recompiled
- [ ] Fresh match from lobby
- [ ] EditMode All still green (optional if no code since last green)

---

## Priority — Aug 7 fixes

- [ ] **District offer** — With `food +N` (or break-even + full housing) and good **G** sites, End Turn **stops** on Accept / Not now / Decline (Esc = Not now); panel is not silently swallowed
- [ ] Accept → specialty picker; Not now → Growth shows cooldown; then End Turn advances
- [ ] **Home** — jumps to capital; press again to cycle other cities
- [ ] **Growth line** — `food ±N (prod/cons)`; early coastal capital not instantly starved (granary / urban baseline)

---

## Core smoke

- [ ] Narrative card after early turns → pick → End Turn / Esc; no softlock; AI turns advance
- [ ] Left HUD readable, not oversized
- [ ] Clergy roster (**R**) aligned
- [ ] Coastal Explorer: navigable coastal sea OK; shore/naval-coast land OK (design); deep ocean blocked
- [ ] BUILD / SCRIPTURE labels; Tech (**T**); Synod Brief (**Y**)

---

## When time allows

- [ ] Galley water-only + cargo UI
- [ ] Schism readable; food after pop drop
- [ ] Diplomacy (lobby **2 players**)
- [ ] Reach a win path (fame / adherence / Tier 6) without softlock

---

## After the run

1. Note fails in chat (turn # + what you saw).  
2. Agent updates **Last session** in `PROGRESS.md` only.  
3. Reset checkboxes here for the following playtest.
