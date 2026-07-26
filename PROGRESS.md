# BoC4x — Session Checkpoint

**Saved:** July 25, 2026  
**Project:** Book of Concord 4X prototype (`C:\Users\stmit\BoC4x`)  
**Engine:** Unity 6000.5.x — scene `Assets/Scenes/SampleScene.unity`

---

## Latest — decision fork pass (July 26)

### Fork 1 — Decision 1: AI synod schism ✅

Rival synods (players 2–4) build Walther crisis pressure from city growth tension. When pressure breaks, a **nearby schismatic bloc splinters off** (new capital + units); the parent AI synod **survives weakened** (lost pop, one unit may defect).

### Prior session

1. **AI synod factions** — lobby players 2–4 spawn rival Lutheran synods
2. **Watchtower garrison**, **amphibious transport**, naval AI blockades

### Fork 2 — Decision 2: Mission House → frontier settler ✅

Mission House now unlocks **Train Frontier Settler** (replaces colonist). With one independent city, train a settler and press **F** on valid land to found a **second independent city** (Leipzig, etc.); settler becomes a missionary. Districts remain organic-only.

### Fork 3 — Decision 7: population sync ✅

Player faction population is now **derived from the sum of all synod city/district populations** each growth/migration/production/confessional phase. Losses and gains apply to cities first, then sync.

### Fork 4 — Decision 8: organic-only districts ✅

Removed **Found Hamlet** build, **Colonist** unit type, and all manual district founding paths. Districts spawn **only** from organic growth offers after food surplus.

### Fork 5 — Decision 14: AI synod personalities ✅

**Strasbourg** (evangelical): missionary-heavy production and preaching sieges. **Magdeburg** (garrison): soldier/siege focus. **Nuremberg** (humanist patrol): scouts + slingers. Shared `ManageCityProduction` also drives schismatic blocs.

### Remaining forks (in order)

### Fork 6 — Decision 17: asymmetric adherence rules ✅

**Spiritual track** keeps adherence gates (40%+ global, per-node minimums). **Secular track** can be researched at any adherence. All tech **bonuses stay dormant at ≤40%** adherence (potency scaling). Tech-granted adherence floors removed — adherence can reach **0%**.

### Remaining forks (in order)

### Fork 7 — Galley cargo UI + synod trade ✅

**Galley cargo panel** (bottom-right when galley selected): shows 0/2 slots, Select/Land per passenger; shore clicks land the selected soldier. **Synod trade stub:** Market districts (Market Hall or Dock) and coastal capitals with Dock/Market Hall within 4 hexes in the same cluster earn **+1 manuscript per link** each production phase.

### Diplomacy (rival synods) ✅

Press **D** for the diplomacy panel (lobby matches with 2+ players). Rival synods start **at war**. **Propose truce** costs 2 manuscripts for **10 turns** without synod-vs-synod combat or siege. **Declare war** breaks a truce. Schismatic blocs ignore diplomacy and stay always hostile.

---

*Update this file at the end of each major session.*
