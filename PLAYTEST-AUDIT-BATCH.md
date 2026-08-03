# Playtest Audit — Aug 3, 2026 (Tightening Batch)

**Branch:** `cursor/tightening-batch-b1b6` · **PR:** #1  
**Scene:** `Assets/Scenes/SampleScene.unity` · **Unity:** 6000.5.x

Work this list **top to bottom** in a fresh solo match unless noted. Mark pass/fail inline.

---

## Before Play

- [ ] Pull / checkout `cursor/tightening-batch-b1b6` (or merge PR #1)
- [ ] EditMode → **BoC4x.Editor.Tests** → Run All
- [ ] Lobby: **solo (1 player)** · Standard or Grand · Archipelago if testing naval

**No save/load** — each Play starts from lobby. Loading screen should say matches are not saved.

---

## Priority — Tightening batch (new this session)

| | Focus | Pass criteria |
|---|--------|----------------|
| [ ] | **BUILD / SCRIPTURE HUD** | Upper-left queue panel shows **BUILD** and **SCRIPTURE** lines; turn banner says Build \| Scripture |
| [ ] | **End Turn defer** | With **pastoral briefing** or **district offer** open, End Turn **advances** (auto-defers); crisis/emphasis cards still **block** |
| [ ] | **Synod Brief CITY YIELDS** | After districts/hamlets exist, **Y** brief shows real yields — not “No city production yet” falsely |
| [ ] | **Military witness (Y + T)** | **Y** brief → Military witness + emphasis gates; **T** tech sidebar shows gate summary when no tech selected |
| [ ] | **Diplomacy in brief** | Lobby **2 players** → **Y** shows rival lines; **Colloquy truce** button spends 2 mss, 10-turn truce; **D** panel still works |
| [ ] | **PlayerCapital win** | Post-schism: schismatic capture of **your capital** ends match (banner names capital, not hardcoded string only) |
| [ ] | **Population grace** | Drop synod pop to **0** → banner shows grace countdown; defeat only after **2 turns** at 0; warning at **≤3** |
| [ ] | **Left HUD column** | Dashboard rows sit on unified dark column background |
| [ ] | **Modal stack banner** | Open **Y**, **T**, or **C** → turn banner shows `[N panels open]` |
| [ ] | **District appeal flash** | District offer highlights hex + shows appeal score; **G** toggles full appeal map |
| [ ] | **AI turn budget** | 2-player lobby: AI rivals act but turns don’t stall on huge unit stacks (spot-check mid-game) |

---

## Regression — Still verify from prior sessions

| | Focus | Pass criteria |
|---|--------|----------------|
| [ ] | **Church Year / WATCH** | Dashboard season updates; WATCH on principal feasts |
| [ ] | **Parish heal** | Wounded unit on own city hex → End Turn → +4 HP |
| [ ] | **Militia + Parish Walls** | Adjacent hostile → militia; walls block entry, siege adjacent |
| [ ] | **Crisis loop** | End Turn → crisis → pick → End Turn once → turn advances |
| [ ] | **Era fork UI** | **T** → fork badges / amber tint on open siblings |
| [ ] | **3rd schism saturation** | Honest overflow copy; union strife path |

---

## Known gaps / not in this branch

Log for a follow-up pass — do **not** block this playtest on these unless they break core flow:

| Item | Notes |
|------|--------|
| **R — clergy roster** | `ClergyRosterPanel` may not bootstrap in `HexGridMap` on this branch — verify **R** opens roster |
| **Combat counter-damage retune** | Attacker retaliation scaling by fight weight not merged here |
| **Naval feel pass** | Deep-sea/galley water-only rules, galley cargo layout vs End Turn — prior batch may be local-only |
| **Unit XP / veterancy** | Design logged only; no system yet |
| **Fame win threshold** | Code still **75** fame (README synced); raise to 120 is a balance pass, not this PR |

---

## Victory / defeat reference (code truth)

**Synod win (any one):** 100% adherence × 5 turns · Tier-6 trio (CTCR + Nagel + GLF) · 75 fame  

**Defeat:** army + cities wiped · pop 0 for 2 turns · adherence 0%  

**Schismatic win:** capture player **capital** after schism

---

## Report back

Note turn number, lobby settings, and pass/fail per row. File issues against failing rows with Console excerpt if any.
