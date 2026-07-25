# Book of Concord — 4X Prototype

A hex-grid turn-based strategy prototype themed around the Lutheran confessional tradition.

## Quick Start

1. Open the project in **Unity 6000.5** (or your installed 6000.x editor).
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play** — the **match lobby** appears first.
4. Set seed, map size, player count, wrap style, heresy pack, and coastal density, then **Begin Match**.

### Match lobby

| Setting | Options |
|---------|---------|
| **Map seed** | `0` = random; any integer reproduces the layout |
| **Map size** | Compact (40×28), Standard (64×42), Grand (80×52) |
| **Players** | 1–4 (solo default; extra slots reserved for future AI) |
| **Map wrap** | Toroidal, Bounded, Cylindrical (E/W only) |
| **Heresy pack** | Full canon, Reformation core, Radical fringe |
| **Coasts & rivers** | Normal or Archipelago (more water — naval stub) |

Naval units are not built yet; **naval coast** tiles (shore, rivers, map-edge seas) are tagged on the map for future ship movement.

### Visual art eras

Confession tech tiers shift the whole match aesthetic:

| Highest unlocked tier | Visual era | Look |
|----------------------|------------|------|
| 1–2 | Woodcut & paper | Sepia terrain, grainy unit silhouettes |
| 3–4 | Stained glass | Jewel-tone map, dark outlines, pane segments |
| 5–6 | Modern confession | Clean gradients, cooler palette |

The turn banner and dashboard show the current art era. Unlocking tier 3 or 5 tech triggers a visible transition.

## How to Play

| Action | Input |
|--------|-------|
| Select unit | Left-click a **blue** unit (Lutheran Synod) |
| Next unit | **Tab** — cycle units needing orders |
| Move | Left-click a **cyan** highlighted hex |
| Attack | Left-click a **red** highlighted enemy (adjacent) |
| Preach | **Spacebar** — missionary (1 manuscript or 1 catechism for +4) or chaplain (free once/turn); nomadic settler preaches once before founding |
| Bind catechism | **B** — nomadic phase only (2 manuscripts → +1 bound catechism) |
| Found capital | **F** — nomadic settler on valid land after preach + scout survey + bound catechism |
| Pan map | **Middle-mouse drag** or **WASD / arrow keys** |
| Appeal overlay | **G** — toggle gold/lavender heatmap for district/growth potential in synod territory |
| Upgrade unit | **U** — on your city hex with tech + manuscripts (or **C** → Upgrades column) |
| Recenter camera | **Home** — snap back to selected unit |

### Tile yields, borders, and resources

Each land hex has **food / production / manuscript** yields from terrain plus optional **map resources**:

| Resource | Typical terrain | Bonus |
|----------|-----------------|-------|
| Wheat, Cattle | Pasture | +2 food |
| Grapes | Pasture, Hill | +1 food, +1 prod |
| Fish | Shore | +2 food |
| Timber | Forest | +2 prod |
| Stone | Hill, Wilderness | +2 prod |
| Iron | Hill, Forest | +3 prod |
| Coal | Hill, Forest | +2 prod |
| Gold | Hill | +2 mss/turn |

**City borders** grow through **culture** within a hard cap of **4 hexes** from the city center. Culture still controls how many tiles you fill inside that ring.

**City spacing:** independent cities must be at least **6 hexes** apart. **Districts** (hamlets) are not separate cities — they sit within **3 hexes** of their parent and share its borders.

**Placement advisor:** select your **settler** or **colonist** to highlight the top 3 founding sites (green). Hover any hex for an **Excellent / Good / Fair / Poor** rating based on center tile, **adjacent yields**, and workable lands within **4 hexes** (plus gold/manuscript bonuses).

### District specializations

When a district is founded, choose one specialty — the district can **only** build/train from that list:

| Specialty | Trains | Builds |
|-----------|--------|--------|
| **Seminary** | Missionary, Pastor, Deaconess, Cantor (+ Pastor→Chaplain upgrade) | Chapel, parish school/church, scriptorium, organ loft, orphanage, hospital |
| **Garrison** | Soldier, Slinger, Archer, Horseman (+ defender upgrade) | Barracks, archery range, stable, armory, fortifications, watchtower |
| **Market** | Colonist, Scout | Workshop, pottery, granary, market hall, mill, printing press, mission house |
| **Scholastic** | Missionary (+ pastor ordination upgrade) | Scriptorium, library, university, observatory, parish school |

Capitals remain generalists; district-only units (pastor, archer, etc.) train only at specialized districts.

**Manuscript sources on the map:**
- **Gold** resource on a hill: +2 mss/turn when worked (highlighted in gold on hover)
- **Wilderness / forest / hill / shore**: +1 mss/turn when your **missionary** ends the turn there (shown as "Missionary: +1 mss/turn" on hover)

Hover any hex to see yields, manuscript bonuses, resource, owner, and worked status.

| Action | Key |
|--------|-----|
| Confession tech trees | **T** — **Spiritual** (Doctrine & Culture) or **Secular** (Science & Civic) tabs |
| Switch research tree | **Q** / **E** or **[** / **]** while tech panel open |
| Start / change research | **T** → preview → **Start research** (one project per tree) |
| Cancel research | **T** → **Cancel research** |
| City screen | **C** (click your city, unit near city, or tabs if you own several) |
| Clergy roster | **R** — view slots, reassign clergy, set chaplain ministry (parish / escort / hospital) |
| Start city production | **C** → preview → **Start production** |
| Cancel city build | **C** → **Cancel build** |
| End turn | **End Turn** button — cycles units needing orders, then **phased resolution** (Growth → Migration → Production → Confessional) with Continue between each |

**Units & city shapes**

| Shape | Type | Notes |
|-------|------|-------|
| **Ring + cross** | Settler | Game-start only; preach + survey + bind catechism, then **F** founds Wittenberg |
| **Triangle (small)** | Scout | Fast mover; 4 hex sight |
| **Triangle (small)** | Coastal Patrol | Market on coast; land + navigable water; +1 move on shore/water |
| **Diamond** | Coastal Galley | Dock + coast; shore + navigable water only; strong attack |
| **Cross** | Missionary | Train at city; **upgrade to Pastor** on city hex (Large Catechism) |
| **Square (large)** | Defender | **Upgrade from Soldier** at city (Martin Chemnitz) |
| **Square** | Soldier | Combat; upgrade path to Defender |
| **Square (large)** | Siege Engine | Garrison + Armory + Maxwell; slow; stacks with Guericke/Maxwell/Armory bonuses |
| **Circle (small)** | Slinger | Ranged 2-hex attack (Shepherd's Sling tech) |
| **Triangle** | Chaplain | **Specialty pastor** — upgrade from Pastor (Walther Pastoral Theology); assign escort / hospital / parish via **R** |
| **Star** | Bishop | **Upgrade from Pastor** (Formula of Concord) — one per city |
| **Star (large)** | Archbishop | **Upgrade from Bishop** (Augsburg Confession) — one synod-wide when **2+ cities** |
| **Circle** | Cantor | Train at Seminary district (Chorale Tradition) |
| **House** | Colonist | **F** founds hamlet on valid land near synod cities; consumed on use |
| **Diamond** | Small city | Pop under 15 |
| **House** | Medium city | Pop 15–29 |
| **Circle** | Large city | Pop 30+ |
| **Star** | Capital | Wittenberg / Augsburg Dissent |

Blue = Lutheran Synod, red = Schismatic. Bottom-left panel lists shapes; hover hexes for terrain.

**Nomadic start:** You begin with a **settler** and **scout** — no city, no enemy. Before founding **Wittenberg**, complete three tasks:

1. **Preach** once — select the settler, press **Space** (costs 1 manuscript, or use a bound catechism).
2. **Survey** — move the scout across **10 unique hexes**.
3. **Bind a catechism** — press **B** (costs 2 manuscripts).

When all three are done, move the settler to good land and press **F** to found the capital. That settler becomes your missionary. A **confessional identity** picker opens (Missionary Sending, Magisterial, Pastoral Care) — permanent modifiers for the match.

**Schism:** There is **no Schismatic faction at start**. When Walther meters spike, **crisis cards** interrupt play — concede, debate, or discipline before a split. Each schism spawns a **historical heresy bloc** (Pharisaic Synod, Libertine Congregation, Augsburg Dissent, Schwärmer Circle, etc.) with its own capital, AI turns, and growth flavor. Up to **3 concurrent dissent blocs**; further crises can schism again with a different heresy. Surviving crises with tech guards earns **legacy traits** (3 active slots).

**City capture:** Cities have **loyalty** — martial units siege (stand on city hex) and clergy **preach** loyalty down before capture. Capitals and fortifications hold longer. Districts flip faster than capitals. **Hover a city** on the map for a loyalty mini-bar; with a unit selected, see projected siege/preach pressure per turn.

**Fog of war:** Scouts see 4 hexes, missionaries see 3, soldiers/chaplains/cities see 2. Unexplored hexes are dark until scouted.

**Wrap-around map:** Toroidal **64×42** grid (~2,700 hexes) — sized for four factions with room for 4–6 cities each and open land between clusters. Edges connect; spawns are randomized each match on land pockets (not water traps).

### Water

Ocean, lake, and river tiles are **bright blue** and impassable. Shore tiles grant +1 manuscript when your missionary ends turn there.

## Cities & Production

**C** opens the city on your selected unit's tile, or the nearest/capital city. With multiple cities, use the **tabs** at the top of the city screen. A **loyalty bar** shows synod hold (green → orange → red under enemy siege).

- **Production** and **Research** queues show in the upper-left HUD with turns remaining (Spiritual and Secular research lines).
- **Adherence win progress** appears in the adherence line and top banner when you approach 95%.
- **Two Kingdoms growth:** settlers migrate when **food surplus** (produced − consumed) is positive **and** **blended appeal** (secular × spiritual) is high enough. Secular appeal comes from granaries, markets, worked food tiles, and civic restraint; spiritual appeal from chapels, schools, adherence, and comfort. Imbalance triggers Walther tensions (legalism, antinomian drift, etc.).
- **Housing cap** limits population until you build parish infrastructure (chapel, granary, church, orphanage). **Workers** ≈ ⅓ of population — only one secular production project per free worker pool; short workers halve build speed.
- **Organic districts:** after **2+ turns of food surplus**, the game may offer a district site inside your borders (accept / defer / decline). Specialty is suggested from your building mix. **Colonists** can still force a district elsewhere with **F**.
- **Mission House chain:** build a **Mission House** (Wilhelm Loehe tech) to unlock **colonists cluster-wide**. At the city with the house: colonist and missionary training cost **1 fewer manuscript** and **1 fewer turn**; each house yields **+1 fame/turn**.
- **Siege engines:** Garrison districts with an **Armory** and **James Clerk Maxwell** research can train **Siege Engines** — slow but apply high loyalty pressure/turn (partially bypasses fortifications). **Otto von Guericke**, **Maxwell**, and a synod **Armory** further boost siege pressure.
- **Coastal patrol:** Market districts touching shore or naval coast can train **Coastal Patrol** after **Missionary Sending** — moves on land and **navigable water**; +1 move when starting on shore or water. **Deep ocean** beyond coastal range is impassable (wider range on Archipelago maps).
- **Dock and galley:** build a **Dock** at coastal Market districts, then train **Coastal Galleys** — warships restricted to shore and navigable water hexes. AI rivals on coast may blockade rivers and harbors.
- **Lobby rivals:** set **Players** to 2–4 in the match lobby to spawn 1–3 active schismatic blocs at game start (in addition to crisis schisms later).
- **Ordain Pastor** — upgrade missionary on city hex (Large Catechism); or train at capital after **Parish Church** / **Seminary** building, or at Seminary districts.
- **Commission Deaconess** at Seminary districts only (Large Catechism).
- **Train Cantor** at Seminary districts (Chorale Tradition).
- **Specialize Chaplain** — upgrade pastor on city hex (Walther Pastoral Theology); press **R** to assign **parish**, **military escort** (+atk/def, heal escort), or **hospital** (+healing on city hex, comfort).
- **Train Slinger** (2 mss) after **Shepherd's Sling** — 2-hex ranged skirmisher.
- **Pottery Workshop** (18 prod) after **Earthen Vessels**; **Parish Granary** (3 mss) after **Parish Granary** tech.

**Clergy model**

| Tier | Unit | Cap | How |
|------|------|-----|-----|
| Field | Missionary | — | Train anywhere |
| Congregation | **Pastor** | **1 per parish church** (church or cathedral in cluster) | Train or Missionary → Pastor |
| City | **Bishop** | **1 per independent city** | Pastor → Bishop |
| Synod | **Archbishop** | **1 when 2+ cities** | Bishop → Archbishop at city hex |
| Support | Deaconess, Cantor, Chaplain | Cluster slots (max 5) | Train / upgrade |

**Chaplain specialties** (via **R** roster panel):

| Assignment | Effect |
|------------|--------|
| **Parish** | Default specialty pastor — strong preach on roster hex |
| **Military escort** | Link to a soldier/slinger/archer/horseman/defender — +1 atk / +2 def while adjacent; heals escort each turn |
| **Hospital** | Requires Parish Hospital in cluster — heals units on city hex, +spiritual comfort |

**Support slots** expand via: capital base (1), Parish Church (+1), Seminary building (+1), each Seminary district (+1), population tiers (+1/+1), max 5 — for Deaconess, Cantor, and Chaplain only. Press **R** to view roster and reassign clergy.

**Parish bonus:** Installed clergy preaching on their roster city hex gain **+3 adherence** (+1 within 2 hexes). Bishops +1, Archbishops +2 on top.

**Episcopal passives** (bishop within 2 hexes of roster city; archbishop is synod-wide):

| Office | Passive |
|--------|---------|
| **Bishop** | Cluster pastors/chaplains/deaconesses/cantors: **+1 preach adherence**, **+2 siege preach**; **+2 comfort**/turn to synod; cantors **+2 hymn comfort** |
| **Archbishop** | All synod clergy: **+1 preach** and **+1 siege preach**; bishops amplify to **+2** cluster preach; **+3 comfort** and **+1 adherence**/turn synod-wide |

**Schismatic mirror:** Each schism spawns a heresy-flavored clergy unit (Bishop, Chaplain, Cantor, or Missionary) at the dissent capital.

**Unit upgrades** (stand unit on city hex, pay manuscripts, uses turn — **U** or city screen **Upgrades** column):

| Path | Tech | Cost |
|------|------|------|
| Missionary → Pastor | Large Catechism | 2 mss |
| Pastor → Chaplain | Walther Pastoral Theology | 2 mss (Seminary access) |
| Pastor → Bishop | Formula of Concord | 3 mss (Seminary access) |
| Bishop → Archbishop | Augsburg Confession | 4 mss (2+ synod cities, on city hex) |
| Soldier → Defender | Martin Chemnitz | 2 mss |

**Civic tech (traditional 4X):** **Earthen Vessels** (pottery), **Parish Granary**, **Shepherd's Sling** — secular/confessional tier 1–2.
- **Bind Catechism** (2 mss, 1 turn) — craft portable catechisms; preaching with one costs no manuscript and grants **+4 adherence**.
- **Found Hamlet** (2 mss, 2 turns) — spawns a tribute hamlet on an adjacent hex. Hamlets send manuscripts and fame each turn; they cannot build secular projects.

**Confessional buildings:** Scriptorium, Parish School, Chapel, **Seminary** (+research speed), **Cathedral** (capital only), **Hospital**, **Mission House**, **Fortifications**, **Orphanage**.

**Secular buildings:** Guild Workshop, Printing Press, Observatory, **Library**, **University** (+research speed).

**Tech tree:** 6 eras (Reformation → Global Witness), 3 tracks (Doctrine, Culture, Secular) — **47 technologies** with branching prerequisites. Tier 6 adds CTCR, Nagel, CPH, Heisenberg, and global fellowship.

**Confessional Fame** — preaching, chapels, founding, identity, hamlets. **75 fame** = synod victory. Legacy milestones at 25/55.

## Victory & Defeat

**Synod victory (any one):**

- Hold **95%+ adherence** for **5 consecutive player turns**
- Unlock **Robert Preus** + **James Clerk Maxwell**
- Reach **75 confessional fame**

**Defeat (always):**

- Destroy all synod units
- Population 0 or adherence 0%

**Schismatic victory (only after schism):**

- Capture **Wittenberg**

## Architecture

See `Assets/` — `HexGridMap.cs`, `FirstSteps.cs`, `Scripts/` for units, cities, tech, AI, fog, match flow.

**Developer checkpoint:** [`PROGRESS.md`](PROGRESS.md) — session state, playtest checklist, and prioritized next steps.

## Roadmap (later)

- Diplomacy and trade
- Siege / more unit types
- Art pass (replace procedural sprites)
