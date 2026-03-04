# Skills + Equipment Spec (Source of Truth)

_Last updated: 2026-03-03 (America/Los_Angeles)_

This doc is the **single source of truth** for the current design: loadout rules, tags, skills (tiers), equipment abilities (tiers), tooltip conventions, and consumables.

---

## 1) High-Concept
- **Genre:** Top-down 2D **bullet-hell** action RPG.
- **Session loop:** **Instanced dungeons** lasting **5–25 minutes**.
- **Aiming:** **Mouse aim** (movement via WASD).
- **PvP:** **None**.
- **Loot:** Pick up gold/chests during the run, but **open rewards only at the end if you survive** (no in-run inventory).

### Design pillar
**Gear gives the buttons (verbs). Skills mutate the buttons (grammar).**
- **All active abilities come from equipment slots.**
- **Skills never add new buttons.** Skills only add: modifiers, triggers, conversions, scaling, constraints, and AI/targeting changes.

---

## 2) System Responsibilities (what goes where, and why)
This is a **class-less** game: archetypes emerge from loadouts. To keep the system readable and expandable, each system has a job.

### Equipment (verbs)
- **Weapons:** own **Auto + Special**, baseline bullet feel, and **Tier 2 / Tier 4 choice perks** bullet upgrades (patterns/lanes/pierce/homing/explosions).
- **Helmets:** own the **Utility** verb (summon/ward/decoy/mark/heal utility).
- **Boots:** own the **Mobility** verb (dash/blink/phase/leap and movement survival).
- **Body Armor:** owns passive build loops (stealth cadence, wards, overcharge, leech). 
- **Gloves:** proc to help support a build

### Skills (grammar, no new buttons)
- Skills modify existing verbs via **tags**: add statuses, conversions (e.g., Frost Attunement), triggers/procs, AI behavior changes, allows more summons, stronger totems, more projectiles, etc.
- Skills should not replace weapon identity. They **enhance themes** (DoT builds, necro, frost, shock).

### Consumables and Food (loadout tools)
- **Toolbelt (5 slots):** cooldown consumables for emergency moments (heal now / mana now / stealth now / party buffs from inscription).
- **Food (separate pick):** long-duration **HP regen + Mana regen** in and out of combat (supports downtime, kiting, hiding).
- **Totems**: long cooldown stationary totems that provide some effect. Scales with **Shamanism**
- **Scrolls**: grant buffs to party. Scales with **Inscription**.


---

## 3) Player Loadout and Buttons
Exactly **4 actives** on the bar:
1) **Weapon Auto Attack**  Projectiles. Delivery, type and shape depend on weapon. Generates mana.
2) **Weapon Special Attack** Different shapes. Uses mana.
3) **Helmet Utility** Cooldown. Uses mana.
4) **Boots Mobility** Cooldown. Uses mana.

Plus:
- **Body Armor:** passive only (no button).
- **Gloves:** proc-only (no button).
- **Loadout lock:** Equipped gear cannot be swapped during a dungeon run.

---

## 4) Resources
### XP
- **XP is gained on monster kill.**
- XP is spent to upgrade skills from **Tier 1 → Tier 5**.

### Mana (the generator/spender loop)
- **Auto Attack generates Mana** (the bullets are the generator).
- **Passive Mana regen exists via Food**, so you can recover while hiding/running/stealthed with nothing to shoot.
- **Mana is consumed by other abilities** (Weapon Special / Helmet Utility / Boots Mobility) and by procs **only if the ability explicitly says so**.
- **Mana potions (Mana/Mana Potions)** are toolbelt consumables and restore Mana on cooldown.

### Gold (run-only)
- **Gold is earned during the dungeon run** (drops, chests, room rewards).
- Gold is spent on **in-run equipment upgrades** (Tier 1–5) and **Weapon Mastery**.
- The upgrade screen is accessible at any time (for now).
- **Gold does not persist** between runs; leftover gold disappears when the run ends.

### Hit Points
- **HP regen** can come from Food, equipment passives, scrolls, totems and skills.
- **Instant/active healing** comes from Health Potions, Helmet Abilities, Glove Procs, Totems, Bandages, and Skills.

---

## 5) Damage Types and Combat Categories and Combat Categories
### Damage types (tags-only)
Damage types are **tags**, not resist tables:
- **Physical**
- **Poison**
- **Frost**
- **Fire**
- **Shock**

**Outgoing damage buckets**
- **Physical** damage is its own bucket.
- **Magical** damage = everything else (**Poison, Frost, Fire, Shock**).

Status effects **stack** (for now).

**Shock status: Conduit**
- Shock hits can apply **Conduit** for **4s**.
- While Conduit is active, when the target is hit by **Shock** damage, that hit also arcs to a nearby enemy for **full Shock damage**.
- Conduit does **not** propagate by default (upgrades can add propagation).


### DoT notes
- **Poison** is always a **DoT** via the **Poisoned** status.
- **Fire** can apply **Burning** (a short DoT with on-death pop).
- **Bleeding (Bleeding)** is a **Physical**-typed DoT status.
- **Diseased** is a **Physical**-typed DoT status.

### Status effects (canonical)
Naming rules:
- **Damage types:** Physical, Poison, Frost, Fire, Shock
- **Statuses:** are separate from damage types and can bundle multiple effects.
- Abilities can apply multiple statuses.
- A **DoT** is a status that **ticks damage over time**.

**Poisoned** (Poison DoT)
- Ticks Poison damage over time.
- Duration: **8s**.
- Stacks: none.
- Players: cannot refresh (only applies if not already Poisoned).
- Monsters: reapply refreshes duration.
- While active: blocks **all healing** and blocks **all HP + Mana regen**.
- Does not block: **Wards**.
- Cleanses: Cure Potion, cleanse effects, or expiration.

**Diseased** (Physical DoT + spread)
- Ticks Physical damage over time.
- Duration: **12s** (refreshes on reapply).
- Stacks: up to **5**.
- On reapply: add a stack (if under cap) and refresh duration to 12s.
- Scaling: more stacks = more tick damage.
- Spread: on death, spreads **1 stack** to nearby enemies.

**Bleeding** (Physical DoT + vulnerability + Rupture)
- Ticks Physical damage over time.
- Stacks: up to **10**.
- Duration per stack: **10s**.
- Refresh: stacks do **not** refresh on reapply (pressure mechanic).
- Vulnerability: +2% **Physical damage taken** per stack (max +20%).
- Rupture: at 10 stacks, your next attack triggers **Rupture** (bonus Physical burst) and clears Bleeding.

**Burning** (Fire death/expiration mark)
- Duration: **3s**.
- No periodic damage (not a DoT).
- If the target dies **or Burning expires**, trigger a small AoE **Fire** explosion (does not apply Burning).

**Slow**
- -25% move speed for **2s**. Does not stack; reapply refreshes.

**Weakened**
- -20% attack speed for **3s**. Does not stack; reapply refreshes.

**Dampened**
- -30% projectile speed (projectiles fired by target) for **2s**. Does not stack; reapply refreshes.

**Hexed**
- +10% **Magical** damage taken for **6s**. Does not stack; reapply refreshes.

**Conduit**
- Duration: **4s**.
- While active, Shock hits arc **full Shock damage** to a nearby enemy.
- Chain depth: **1 hop** baseline.
- Propagation: arcs do **not** apply Conduit by default (upgrades can enable).

**Chilled / Frozen / Stunned** (Frost chain)
- **Chilled:** stacks to 3; lasts **4s** and refreshes on reapply. Chilled itself does nothing.
  - At 3 stacks: apply **Frozen (1s)** and **Stunned (1s)** simultaneously, then reset Chilled to 0 and cannot apply Chilled until Frozen ends.
- **Frozen:** +50% Frost damage taken for **1s**.
- **Stunned:** cannot move or act for **1s**.

**Disarm**
- Stops the **generator** (weapon auto) for **1s**.

**Silence**
- Stops the **weapon special** for **1s**.

### Wards
- **Wards** absorb **ALL Magical damage** (non-Physical).
- Generated by the Warding skill and certain items.
- Wards are not affected by healing/regen blocks.

### Damage delivery (separate from damage type)
Damage type (Physical vs Magical subtypes) is separate from *how* damage is delivered.
- **Projectile:** bullets/arrows/bolts/knives
- **Beam:** sustained or tick-based ray
- **AoE:** ground zones, explosions, cones
- **Contact:** touch/swipe impacts

**Defense rules:**
- **Dodge** only affects **Projectile** hits (bullets), regardless of damage type.
- **Warding** reduces **debuff duration and severity** (DoTs, slows, Conduit, Freeze).

---

## 6) Tags (the glue that makes skills work)
Every ability has a small tag set so skills can hook consistently.

### Required tag dimensions
- **Weapon tags:** `Bow`, `Crossbow`, `Dagger`, `Ritual Dagger`, `Spellbook`, `Staff`, `Spellblade`, `Sword`, `Axe`
- **Delivery tags:** `Projectile`, `Beam`, `AoE`, `Dash`, `Summon`, `Trap/Sigil`
- **Behavior tags:** `DoT`, `Pierce`, `Homing`, `Channel`, `Burst`, `Slow`, `Stealth`, `Ward`, `Ambush`, `Focus`, `Mark`, `Dodge`, `Stagger`, `Interrupt`, `Conduit`, `Bleeding`, `Spread`, `Affliction`, `Catalyst`, `Burning`, `Poisoned`, `Burning`, `Chilled`, `Frozen`, `Stunned`, `Hexed`, `Weakened`, `Dampened`, `Diseased`, `Disarm`, `Silence`, `Rupture`
- **Damage tags:** `Physical`, `Poison`, `Frost`, `Fire`, `Shock`
- **Category tags:** `Melee`, `Magic` (category of incoming damage for mitigation rules)


### Ability structure (implementation model)
Each ability is assembled from:
- **Delivery** (projectile/beam/etc.)
- **Tags**
- **Knobs** (mana cost, cooldown, duration, radius, tick rate, pierce count, homing strength, etc.)

---

## 7) Tooltip / UI Style (IMPORTANT)
Tooltips should prioritize **readable gameplay info**:
Show:
- Ability name
- **Mana cost**
- **Cooldown**
- **Tags**
- Short description
- Tier perks (T3 / T5) and synergy notes

Hide:
- General item stat blocks on most gear (for now).

**Exception: Body Armor**
- Body armor can show only these basic stats:
  - **Physical Damage Reduction**
  - **Mana Recovery Rate**

---

## 8) Gear Tiering (T1–T5)
### 8.1 In-run gear upgrades (Dota-style)
- Every dungeon run starts with all equipped gear at **Tier 1**.
- You earn **gold** during the run and spend it to upgrade **specific equipped items** one tier at a time.
- Upgrade UI: a single table screen with **rows = slots** (Weapon, Helmet, Body Armor, Gloves, Boots) and **columns = Tiers (T1–T5)**.
  - Unaffordable tiers are grayed out.
  - Affordable tiers are highlighted.
  - Purchased tiers show a distinct “owned” state.
- **Tier 2 and Tier 4** are **choice perks selected per run** when purchased.
- **Gold is run-only** (disappears after the run).
- **No between-run gear progression** (only XP/skills and cosmetics persist).

Equipment is Tier 1–5 (upgraded during the run):
- **T1–T2:** numeric scaling
- **T3:** first breakpoint perk (small mechanical change)
- **T4:** numeric scaling
- **T5:** signature perk (build-shaping)

---

## 9) Weapons (Autos are projectile by default)
Launch weapons (current set):
- **Bow, Crossbow, Dagger, Ritual Dagger, Spellbook, Staff, Spellblade, Sword, Axe**

Melee fantasy is preserved using projectile shape + range + cadence:
- **Dagger:** thrown knives/needles (short range, very fast).
- **Sword:** “slash wave” (wide, short-range projectile).
- **Axe:** thick shockwave / heavy hatchet projectile (slow, chunky, knockback).

---

# 10) Skills (Tier 1–5, no new buttons)

**Skill system constraints**
- ~**20 skills** at launch (more later).
- Player can **fully max 5** skills.
- **Tier 2 and Tier 4** often include a **choice node** (pick 1 of 2).

## 10.0 Elemental skill standard (Tier 1 rule)
- Tier 1 of elemental skills adds an extra elemental projectile to the generator **every 5th generator projectile**.
- Elemental add-on projectiles **do not generate Mana**.
- Elemental status apply chance scales with tier: **20% (T1)** → **30% (T3)** → **40% (T5)**.

## 10.1 Skills Table
> All “adds to auto attack” effects mean: the weapon auto **gains the damage tag** and an associated on-hit effect.

| Skill | Core tags it adds/uses | T1 Enablement | T2 Choice | T3 Numbers | T4 Choice | T5 Numbers |
|---|---|---|---|---|---|---|
| **Stealth** | `Stealth`, `Ambush`, `Defensive` | While **Stealthed**, take reduced **Projectile** damage (bullets). | **A)** *Ghost*: more Magic DR while stealthed **B)** *Assassin*: bigger ambush + execute vs low HP | **Ambush**: first hit out of Stealth deals big bonus damage. | **A)** *Shadow Refill*: ambush hit refunds Mana (ICD) **B)** *Chain Ambush*: ambush kill re-stealths (ICD) | Stealth lasts longer or has a short “grace” window before breaking. |
| **Poison** | Adds `Poison`, `Poisoned` | T1: generator fires a Poison dart every 5th shot. All direct Poison damage has **20%** chance to apply **Poisoned**. | **A)** +1 extra Poison add-on projectile (cadence improvement) **B)** *Venom Tap*: Every time you refresh a poison stack on an enemy, you instantly deal an extra tick of damage. | T3: Poison apply chance **30%**; Poison DoT damage increased (numbers). | **A)** TODO **B)** Chance apply poison to all enemies nearby target **C)** Chance to Summon Poison Elemental on enemy death (limit 1 poison elemental) | T5: Poison apply chance **40%**; Poison projectiles explode and can apply Poisoned to nearby enemies (chance). |
| **Frost Magic** | Adds `Frost`, `Chill`, `Slow`, `Freeze` | Auto projectiles apply **Chill** (slow). | **A)** *Shatter*: frozen targets explode (AoE) **B)** *Brittle*: frozen targets take increased damage | Chill stacks to **Freeze** (a brief stun). | **A)** *Ice Block*: auto ice-block at low HP (long CD) **B)** *Frost Attunement*: your **Physical direct damage**  becomes **Frost** **C)** Chance to Summon Frost Elemental on enemy death (limit 1 poison elemental) | Chill decays slower and freeze uptime improves. |
| **Fire Magic** | Adds `Fire`, `Burning` | T1: generator fires a Fire add-on projectile every 5th shot; **all Fire damage** has **20%** chance to apply **Burning**. | **A)** *Bigger Boom*: Burning explosion damage/radius increased **B)** *Long Fuse*: Burning duration increased | T3: Burning apply chance **30%**; numeric scaling (boom damage/radius or duration). | **A)** **Shrapnel:** Burning detonations apply **Weakened** (3s) to enemies hit. **B)** TODO **C)** Chance to Summon Fire Elemental on enemy death (limit 1 poison elemental) | T5: Burning apply chance **40%**; Burning detonates on **death and expiration** (AoE). Additional numeric scaling. |
| **Shock Magic** | Adds `Shock`, `Conduit` | T1: generator fires a Shock bolt every 5th shot; any Shock hit has **20%** chance to apply **Conduit (4s)**. Bolt can target any on-screen enemy, preferring Conduit targets. | **A)** *Conductor*: longer Conduit duration / range **B)** *Overload*: occasional extra mini Shock hit (ICD) | T3: Conduit apply chance **30%**; numeric scaling. | **A)** *Propagation*: Conduit arcs can apply Conduit (chance/ICD) **B)** *Fork*: arcs can hit an additional nearby enemy **C)** Chance to Summon Poison Elemental on enemy death (limit 1 poison elemental) | T5: Conduit apply chance **40%**; numeric scaling; optional deeper chain tuning. |
| **Necromancy** | `Diseased`, `DoT`, `Summon`, `Undead`, `Corpse` | Auto projectiles apply **Diseased** (stacking DoT). Enemies leave **corpses**. | **A)** *Swarm*: more weaker minions **B)** *Thrall*: fewer but elite undead | Weapon Special can consume a nearby corpse (if present) to raise an undead. +1 max minion. | **A)** *Corpse Casting*: Weapon Special consumes corpses to spawn temp undead **B)** All summoned elementals (through skills or abilities) become more powerful **Undead Wight** | Minions inherit some of your on-hit tags (reduced strength). |
| **Spirit Speak** | `Summon`, `AI`, `Inheritance` | Summons gain damage + HP. | **A)** *Link*: you heal from summon damage (small) **B)** *Command*: Weapon Special orders summons to focus your cursor target (behavior) | Summons gain attack/move speed; improved target selection. | **A)** *Twin Bond*: +1 companion limit for helmet summons **B)** *Aura*: summons emit aura tied to your build tags | Summons inherit more of your on-hit tags (still reduced). |
| **Healing** | `Healing`, `Bandage`, `Defensive`, `Trigger` | Bandages heal more. | **A)** *Emergency Wrap*: auto-bandage trigger at low HP (long CD) **B)** *Field Medic*: bandage grants move speed + small barrier | Bandage use time reduced or adds small HoT. | **A)** *Overheal Shield*: overheal becomes temporary shield **B)** *Second Wind*: after bandage, cooldown recovery briefly faster | Bandages cleanse minor debuff or reduce incoming DoT briefly. |
| **Warding** | `Warding`, `Ward`, `Shield`, `Defensive` | If you avoid damage for **X seconds**, begin generating **Ward** stacks (up to N). | **A)** *Bulwark*: higher max Ward stacks **B)** *Purity*: when a Ward stack breaks, cleanse **1 random** negative status (ICD **8s**) | Numeric scaling (stack size/rate/cap). | **A)** *Mirror Ward*: absorbed magic fires a retaliatory bolt (ICD) **B)** *Ward Surge*: Ward break grants brief barrier + speed (ICD) | Numeric scaling |
| **Dodge** | `Dodge`, `Evasion`, `Defensive`, `Physical`, `Contact` | Chance to dodge **Physical** damage and **Contact** hits. | **A)** *Duelist*: higher dodge while close **B)** *Footwork*: higher dodge while moving | Improves dodge chance. | **A)** *Perfect Step*: on dodge, gain brief move speed + Mana (ICD) **B)** *Ghost Frame*: first eligible hit each X seconds is dodged | Further scaling. |
| **Serration** | `Bleeding`, `DoT`, `Physical` | Your **Physical** direct hits apply **Bleeding** (bleed DoT). | **A)** *Cleave*: heavy hits splash 1 Bleeding stack to nearby enemies (ICD) **B)** *Deep Cuts*: higher Bleeding stack cap / faster stacking | Bleeding enemies **cannot regenerate** HP (or regen reduced to ~0). | **A)** *Rupture*: at max Bleeding stacks, next direct hit causes a small burst (ICD) **B)** *Crippling Bleed*: bleeding enemies are slowed slightly | Bleeding damage increased; applying Bleeding refreshes duration. |
| **Arms Lore** | `Status`, `Utility`, `Amplify` | Improve **non-damaging** status effects you apply (NOT DoTs). | **A)** *Control*: increase duration/magnitude of debuffs **B)** *Conduit*: improve Conduit effectiveness | Numeric scaling. | **A)** *Extended Control*: further duration scaling **B)** *Deep Hex*: increases Hex magnitude further | Capstone: up to **+150%** duration/magnitude for non-damaging statuses (e.g., 1s → 2.5s; Hex 10% → 25%; Frozen 50% → 125%). Also increases Conduit chain depth up to **5 hops**. |
| **Catalyst** | `DoT`, `Catalyst`, `Burst` | Enables/boosts **DoT Consumption** scaling: your DoT-cashout effects deal more damage. | **A)** *Efficient Reaction*: cashout refunds some Mana on elite hit (ICD) **B)** *Widening Circle*: larger cashout area | Increases cashout radius slightly and improves reliability. | **A)** *Afterburn*: cashout leaves a short lingering zone (tiny) **B)** *Execution*: cashout deals bonus damage to low-HP targets | Cashout damage scales more strongly with remaining DoT stacks/duration. |
| **Virulence** | `DoT`, `Spread`, `Affliction` | Your DoTs deal increased damage. | **A)** *Outbreak*: increased **spread chance** for your DoT spread triggers **B)** *Corrosion*: DoTs also apply a small “takes more damage” debuff (capped) | Your DoT duration/application improves. | **A)** *Pandemic*: on death with 2+ DoTs, spread one stack of each to nearby enemies (ICD) **B)** *Meltdown*: reaching max stacks triggers a small burst (ICD) | Higher DoT stack caps or faster stacking. |
| **Inscription** | `Scroll`, `Buff`, `Support` | Unlocks **Scroll** consumables on the Toolbelt; scroll buffs are stronger. | **A)** *Chorus*: scrolls also affect nearby allies more strongly **B)** *Quick Ink*: scroll cooldowns reduced further | Scroll duration increased and/or cooldown reduced. | **A)** *Grand Script*: scrolls affect the whole party in the instance **B)** *Runic Guard*: using a scroll grants a brief barrier to affected allies | Scroll effects scale higher (magnitude). |
| **Shamanism** | `Totem`, `Support`, `AoE` | Unlocks **Totem** consumables on the Toolbelt. | **A)** *Larger Radius*: Fill me in **B)** *More Boom Boom*: more damage | Totem duration increased and/or cooldown reduced. | **A)** *TODO*: todo  **B)** *TODO*: todo | Totem radius increased. |


---

# 11) Equipment Tables (Abilities + Tiers)

## 11.1 Weapons (by type)
Weapon identity is fixed per weapon type. Bullet behaviors are primarily expressed via tier progression.

### Weapon identity and bullet behavior (design notes)
- Weapons have **common patterns** and **unique behaviors** per type.
- **Tier 2 and Tier 4** provide **bullet upgrade choices** (A/B). These are **selected per run**:
  - This creates “class-like” identity through weapon mastery without locking players into classes.

### Bow
| Tier | Auto Attack bullet upgrade | Special (Volley) upgrade |
|---|---|---|
| **T1** | **Quick Shot:** single fast arrow projectile. | **Volley:** cone of arrows (baseline). |
| **T2 (Choice)** | **A) Heavy Arrow Cadence:** every 4th shot becomes a **large** arrow (+size, +damage, +Pierce 1). **B) Twin Lane:** every 3rd shot fires **2 parallel arrows**. | Volley gains +1 arrow **or** tighter cone (matches your chosen style). |
| **T3** | Faster projectile + slight damage increase. | Slightly lower CD or better mana efficiency. |
| **T4 (Choice)** | **A) Piercing Line:** autos gain **Pierce +1** (or heavy arrows gain +2 Pierce). **B) Split-on-Hit:** arrows split into 2 weaker arrows after first hit. | **A)** Volley arrows pierce once **B)** Volley gains a small impact pop on marked/center hit. |
| **T5** | Modest fire-rate increase. | Modest damage scaling. |

### Crossbow (ballistics weapon: pierce/homing/patterns live here)
| Tier | Auto Attack bullet upgrade | Special (Barrage) upgrade |
|---|---|---|
| **T1** | **Bolt Shot:** slower heavy bolt projectile. | **Barrage:** fire a short burst of bolts in lanes (baseline). |
| **T2 (Choice)** | **A) Piercing Bolts:** bolts gain **Pierce +1**. **B) Guided Bolts:** bolts gain mild **Homing** (prefers cursor-near targets). | **A)** Barrage gains an extra lane **B)** Barrage gains tighter lanes (more accurate). |
| **T3** | Increased bolt speed + reliability (less whiff). | Better mana efficiency or +1 bolt per barrage. |
| **T4 (Choice)** | **A) Explosive Tips:** bolts create a small impact detonation (low AoE). **B) Split Bolts:** bolts split into 2 weaker bolts after first hit. | **A)** Barrage bolts pierce once **B)** Barrage bolts gain homing (reduced strength). |
| **T5** | Modest fire-rate increase (“reload” improvement). | Modest scaling. |

### Dagger
| Tier | Auto Attack bullet upgrade | Special (Piercing Ambush) upgrade |
|---|---|---|
| **T1** | **Needle Toss:** short-range, very fast thrown knives (projectiles). | **Piercing Ambush:** short-range piercing strike directly forward. Deals **bonus damage** if you are at **full Mana** and you have **not damaged an enemy in 5s**. |
| **T2 (Choice)** | **A) Return Blades:** every 5th knife boomerangs back (can hit again). **B) Triple Burst:** autos fire in 3-shot micro-bursts (spiky proc behavior). | **A)** Piercing Ambush width slightly increased **B)** Piercing Ambush pierces +1 target. |
| **T3** | Slight fire-rate increase + minor range bump. | Slightly lower CD or improved mana efficiency. |
| **T4 (Choice)** | **A) Ambush Payload:** first hit after Stealth fires +2 extra knives (ambush shotgun). **B) Detonating Kunai:** knives create a tiny impact pop on hit (small AoE). | **A)** If Piercing Ambush kills, re-stealth briefly (ICD) **B)** If Piercing Ambush hits an elite, refund part of Mana (ICD). |
| **T5** | Better projectile speed + reliability. | Bonus damage scaling (still conditional). |


### Ritual Dagger (DoT cashout weapon)
| Tier | Auto Attack bullet upgrade | Special (Ritual Harvest) upgrade |
|---|---|---|
| **T1** | **Blight Dart:** fires **fewer** projectiles; each hit applies a **weak Diseased stack** on direct hit. *(Mana gain per hit should be higher to compensate.)* | **Ritual Harvest:** consume **all damage-over-time statuses** (Poisoned, Diseased, Bleeding) on enemies in an area, instantly dealing **all remaining scheduled tick damage** as direct damage, then removing those statuses. *(Does not consume Conduit.)* |
| **T2 (Choice)** | **A) Piercing Darts:** darts gain Pierce +1. **B) Heavy Darts:** bigger hitbox (fewer misses). | **A)** Harvest radius increased **B)** Harvest deals bonus vs elites. |
| **T3** | Slightly faster darts or better reliability. | Slightly larger radius or better mana efficiency. |
| **T4 (Choice)** | **A) Affliction Darts:** applying a DoT has higher spread chance (small). **B) Splitting Darts:** darts split after first hit (weaker). | **A)** Harvest leaves a short lingering blight zone (tiny) **B)** Harvest refunds some Mana on elite hit (ICD). |
| **T5** | Modest scaling. | Modest scaling. |

### Spellbook
| Tier | Auto Attack bullet upgrade | Special (Sigil Cast) upgrade |
|---|---|---|
| **T1** | **Arc Bolt:** medium speed projectile (inherits damage tags from skills). | **Sigil Cast:** place rune that detonates / triggers (baseline). |
| **T2 (Choice)** | **A) Chain Script:** bolts chain 1 time. **B) Rune Mark:** bolts leave a rune on hit (sigils detonate runes for bonus). | Sigil gains +1 charge **or** triggers faster. |
| **T3** | Faster bolt travel + slightly larger hitbox. | Slightly larger sigil radius or lower CD. |
| **T4 (Choice)** | **A) Tri-Lane Bolts:** every 4th bolt fires 3 lanes. **B) Detonation Glyph:** bolts detonate on impact (small AoE pop). | **A)** Sigil inherits dominant tag stronger **B)** Double-sigil (two smaller circles). |
| **T5** | Slight fire-rate increase. | Better mana efficiency. |

### Staff
| Tier | Auto Attack bullet upgrade | Special (Channel Beam) upgrade |
|---|---|---|
| **T1** | **Pulse Shot:** projectile bursts (e.g., 3 pulses). | **Channel Beam:** sustained beam (Mana/sec). |
| **T2 (Choice)** | **A) Piercing Pulses:** final pulse gains Pierce +1. **B) Wide Pulse:** pulses get bigger hitbox. | Beam gains ramp-up **or** allows slightly more move speed while channeling. |
| **T3** | More consistent pulses + modest scaling. | Slight DR while channeling **or** reduced mana drain. |
| **T4 (Choice)** | **A) Impact Nova Pulse:** final pulse creates tiny AoE on hit. **B) Twin Pulse:** bursts fire two parallel pulse lanes. | **A)** Release triggers Overload cone **B)** Beam applies status tags more reliably. |
| **T5** | Faster burst cycle (modest). | Better mana efficiency. |

### Spellblade (hybrid weapon)
| Tier | Auto Attack bullet upgrade | Special (Arc Slash) upgrade |
|---|---|---|
| **T1** | **Rune Shot:** projectile auto (bullet-game rule). | **Arc Slash:** AoE arc in front of caster (“lightning-themed”; Shock-tagged). |
| **T2 (Choice)** | **A) Dual Lane:** every 3rd shot fires 2 lanes. **B) Channel Stance:** auto converts into a short **Beam** (still generates Mana on hit ticks). | Arc Slash gains slightly longer reach **or** stronger hit confirm. |
| **T3** | Faster shots + slightly larger hitbox. | Better mana efficiency or slightly wider arc. |
| **T4 (Choice)** | **A) Impact Pop:** shots create a tiny detonation on hit. **B) Piercing Runes:** shots gain Pierce +1. | **A)** Arc Slash applies a brief Mark-like debuff (readable damage window) **B)** Arc Slash has a chance to refund a bit of Mana on elite hit (ICD). |
| **T5** | Modest scaling. | Modest scaling. |

> **Frost Attunement (Frost Magic T5)** treats your **direct damage** as Frost for tags (DoT ticks keep their own type). Spellblade Arc Slash becomes a Frost arc and can Freeze targets via the “Weapon Specials can Freeze” effect.

### Sword
| Tier | Auto Attack bullet upgrade | Special (Whirl Slash) upgrade |
|---|---|---|
| **T1** | **Slash Wave:** wide short-range wave projectile (melee fantasy). | **Whirl Slash:** AoE spin/wave that **Interrupts** enemies hit (stops casting). |
| **T2 (Choice)** | **A) Double Wave:** every 3rd attack fires two waves. **B) Crescent Arc:** wave gets bigger hitbox (shorter range). | Whirl gains a small pull-in **or** brief Guard on use. |
| **T3** | Slightly wider wave + modest scaling. | Slightly lower CD or improved duration. |
| **T4 (Choice)** | **A) Piercing Crescent:** waves gain Pierce +1. **B) Returning Wave:** waves return after max range. | **A)** Interrupt lasts slightly longer **B)** Hitting 2+ enemies grants Guard vs next Magic hit. |
| **T5** | Modest fire-rate increase (still slower than dagger). | Better mana efficiency. |

### Axe
| Tier | Auto Attack bullet upgrade | Special (Ground Slam) upgrade |
|---|---|---|
| **T1** | **Rending Shockwave:** slow thick projectile (heavy feel). | **Ground Slam:** AoE knockback; **inner radius Staggers** (brief stun). |
| **T2 (Choice)** | **A) Boomerang Hatchet:** every 4th auto becomes returning shockwave/hatchet. **B) Split Cleaver:** autos occasionally fire two lanes. | Slam leaves fissure line **or** stronger knockback. |
| **T3** | Faster travel + better reliability. | Slightly larger inner radius or lower CD. |
| **T4 (Choice)** | **A) Explosive Impact:** shockwaves create small impact detonation. **B) Crushing Wave:** shockwaves become thicker “bigger bullets”. | **A)** Aftershocks (2 pulses) **B)** Stagger affects slightly larger inner radius (careful tuning). |
| **T5** | Modest scaling (axe stays slower). | Better mana efficiency. |

## 11.2 Helmets (Utility actives)
Helmets provide the **Utility** button. Mana/CD numbers are placeholders.

| Helmet | Utility Ability (Mana / CD) | Tags | What it does |
|---|---|---|---|
| **Wizard Hat** | **Conjure Elemental** (E+CD) | `Summon`, `Utility` + element tag | Summon an elemental companion that attacks automatically and slightly prioritizes your cursor target. Necromancy can convert it into an **Undead Wight** (tag/behavior swap). |
| **Ward Crown** | **Ward Dome** (E+CD) | `Ward`, `Defensive`, `Magic` | Drop a small dome that reduces **Magic** damage and slows projectiles passing through. |
| **Mirror Helm** | **Mirror Window** (E+CD) | `Reflect`, `Defensive`, `Magic` | Brief reflect window that reflects a limited number of incoming projectiles. |
| **Aegis Half-Dome Helm** | **Projectile Bulwark** (E+CD) | `Reflect`, `Defensive`, `Projectile` | Create a 180° half-bubble shield in front of you for a short duration. **Reflects all incoming projectiles** that hit the shield back toward enemies. |
| **Decoy Mask** | **Throw Decoy** (E+CD) | `Utility`, `Taunt` | Toss a decoy that pulls aggro and draws bullets for a few seconds. |
| **Hunter Visor** | **Mark Target** (E+CD) | `Mark`, `Utility` | Mark the nearest/cursor target: you deal more damage to it; projectiles bias toward it. |
| **Medic Hood** | **Field Patch** (E+CD) | `Healing`, `Utility` | Instant small heal plus short HoT (or boosts your next bandage). Supports bandage builds without replacing them. |
| **Smoke Cowl** | **Vanish** (E+CD) | `Stealth`, `Utility` | Enter stealth instantly and gain brief **Magic** damage reduction to slip through bullet pressure. |
| **Grave Helm** | **Corpse Spark** (E+CD) | `Necro`, `Utility` | Consume/detonate nearby corpses for damage or raise 1 temporary skeleton (uses corpses). |
| **Frost Diadem** | **Ice Wall** (E+CD) | `Frost`, `Defensive` | Create a short-lived wall/zone that blocks or slows projectiles and chills enemies. |
| **Ember Circlet** | **Fireburst** (E+CD) | `Fire`, `AoE` | Small AoE detonation (at cursor or around you) that applies Burn. |


## 11.3 Boots (Mobility actives)
Boots provide the **Mobility** button. Mana/CD numbers are placeholders.

| Boots | Mobility Ability (Mana / CD) | Tags | What it does | Tier 3 perk | Tier 5 perk |
|---|---|---|---|---|---|
| **Phasewalker Boots** | **Blink** (E+CD) | `Blink`, `Utility` | Teleport a short distance in **movement direction**. Brief i-frames. | Leaves a brief afterimage that draws fire (mini-decoy). | Blink partially resets on elite hit (ICD). |
| **Shadowstep Greaves** | **Vanish Step** (E+CD) | `Dash`, `Stealth`, `Utility` | Short dash that grants instant **Stealth** briefly (breaks on attack). | Exiting stealth grants a small Ambush bonus. | Using Weapon Special while stealthed extends stealth briefly (ICD). |
| **Frosttrail Striders** | **Skate Dash** (E+CD) | `Dash`, `Frost`, `Chill` | Dash leaves an ice trail that chills enemies crossing it. | Trail lasts longer and stacks Chill faster. | Trail causes a mini-freeze pulse when a target reaches max Chill (ICD). |
| **Emberstride Boots** | **Cinder Leap** (E+CD) | `Leap`, `Fire`, `AoE` | Leap forward; landing creates a small fire burst that applies Burn. | Landing burst radius increased. | Landing on burning enemies triggers a small Ignition explosion (ICD). |
| **Wardrunner Boots** | **Phase Slip** (E+CD) | `Dash`, `Ward`, `Defensive`, `Magic` | Dash grants brief **Magic** damage reduction. | Grants 1 Ward stack after dash (if you have not taken damage recently). | Dash leaves a short Ward Field trail that slows projectiles. |
| **Pursuer Treads** | **Hunt Dash** (E+CD) | `Dash`, `Mark`, `Utility` | Dash and apply **Mark** to the nearest/cursor target on your next hit. | Marked targets take increased damage briefly (window). | If the marked target dies, reduce Mobility cooldown slightly (ICD). |
| **Marksman’s Anchors** | **Brace** (E+CD) | `Utility`, `Focus`, `Defensive` | Brief brace window: reduced move speed, rapid Focus stacking. | Focus stacks build faster during Brace. | Reaching max Focus during Brace refunds some Mana (ICD). |
| **Knightcharge Sabatons** | **Bull Rush** (E+CD) | `Dash`, `AoE`, `Knockback` | Longer dash that knocks back enemies to clear space. | End of dash creates a shockwave. | If you hit 2+ enemies, gain Guard vs the next Magic hit. |
| **Gravebound Boots** | **Grave Drift** (E+CD) | `Dash`, `Necro`, `Corpse` | Dash and “collect” nearby corpses for corpse skills. | Collected corpses reduce Weapon Special cooldown slightly (cap/ICD). | After dash, spawn a temporary bone wisp that attacks once per corpse collected. |
| **Beastcall Sandals** | **Relay Blink** (E+CD) | `Blink`, `Summon`, `Utility` | Blink and your companion relocates with you (keeps summons relevant in movement-heavy fights). | Companion gains brief attack speed after relay. | Relay blink heals you slightly based on recent summon damage (ICD). |


## 11.4 Gloves (proc-only slot)
Gloves have **no active button**.
- Proc model: **chance on Auto Attack hit** → if it triggers, it applies a **powerful temporary buff** for X seconds.
- Proc gating: **internal cooldown** + **Mana cost**.
  - If you don’t have enough Mana when the proc would trigger, it **fails** and still consumes the internal cooldown.
- Tiering is the same as other gear (upgraded in-run):
  - **T1:** baseline proc
  - **T2:** numeric scaling
  - **T3:** choice perk (pick 1)
  - **T4:** numeric scaling
  - **T5:** choice perk (pick 1)

Example proc families (placeholders):
- **Overdrive Gloves:** big fire-rate increase for 20s.
- **Frostweave Gloves:** your hits apply extra Chill and you gain brief damage reduction for 12s.
- **Ritual Gloves:** your DoTs tick faster for 15s.


### Sample Gloves (T1–T5)
Proc rules (applies to all gloves):
- Proc is **chance on Auto hit**.
- Proc has an **internal cooldown (ICD)**.
- Proc costs **Mana** to trigger.
- If you do not have enough Mana when it would trigger, the proc **fails** and still consumes the ICD.

#### Overdrive Gloves
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Overdrive:** 10% on-hit → **+60% Fire Rate** for 12s. Cost 30 Mana. ICD 30s. |
| **T2 (Choice)** | **A) Stable Overdrive:** lower bonus, **ICD -8s**. **B) Redline:** higher bonus (+95%), **Mana cost +15**. |
| **T3** | Stronger numbers (e.g., +75% Fire Rate, 14s). |
| **T4 (Choice)** | **A) Kill Switch:** during Overdrive, Weapon Special costs less Mana (or refunds on first use) (ICD). **B) Sustained Heat:** extend duration on kill (cap). |
| **T5** | Adds projectile feel boost (e.g., +proj speed) and/or duration scaling. |

#### Frostweave Gloves
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Frostweave:** 10% on-hit → 12s window: **+Chill application** and **reduced Projectile damage taken**. Cost 30 Mana. ICD 30s. |
| **T2 (Choice)** | **A) Deep Chill:** extra Chill stacks. **B) Crystal Guard:** stronger projectile DR + brief slow immunity. |
| **T3** | Stronger projectile DR and chill application. |
| **T4 (Choice)** | **A) Shiver Nova:** on proc, emit small Chill nova. **B) Ice Thread:** while active, Weapon Specials have increased chance to Freeze (ICD). |
| **T5** | Slightly longer window and/or stronger DR; improve freeze reliability slightly. |

#### Venomcrafters
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Toxic Surge:** 10% on-hit → 15s window: your hits apply **extra Poison stacks**. Cost 25 Mana. ICD 30s. |
| **T2 (Choice)** | **A) Caustic Spread:** higher Poison spread chance. **B) Viscous Slow:** stronger slow while Toxic Surge is active. |
| **T3** | Stronger Poison output (stacks/tick rate) during window. |
| **T4 (Choice)** | **A) Toxic Bloom:** poisoned kills create poison cloud (ICD). **B) Antivenom Loop:** heal from Poison ticks (cap). |
| **T5** | Longer window and higher Poison damage. |

#### Gravepulse Grips
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Gravepulse:** 10% on-hit → 12s window: apply **extra Diseased**; higher corpse drop chance. Cost 35 Mana. ICD 35s. |
| **T2 (Choice)** | **A) Corpse Magnet:** corpses pull toward you (QoL) during window. **B) Blight Power:** Diseased ticks harder during window. |
| **T3** | Stronger Diseased stacks and longer duration. |
| **T4 (Choice)** | **A) Harvest Ready:** DoT cashout (e.g., Ritual Harvest) deals bonus damage during window. **B) Wight Frenzy:** summons gain big atk speed during window. |
| **T5** | Also boosts minions slightly (atk speed/HP) and/or Diseased spread chance. |

#### Duelist Wraps
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Adrenal Spike:** 10% on-hit → 12s window: **+Physical damage** and **+move speed**. Cost 30 Mana. ICD 30s. |
| **T2 (Choice)** | **A) Bleeding Focus:** extra Bleeding stacks during window (Physical). **B) Riposte:** dodging a projectile during window grants brief damage boost (ICD). |
| **T3** | Higher Physical damage bonus and duration. |
| **T4 (Choice)** | **A) Executioner:** bonus damage vs low-HP targets during window. **B) Blade Storm:** on proc, fire a short forward blade burst (ICD). |
| **T5** | Adds “first hit each second deals bonus damage” (cap) or scales numbers. |


## 11.5 Body Armor (allowed basic stats + passives)
Body armor is **passive-only** (no button). Identity comes from **always-on effects** and **conditional passives**.

| Body Armor (Playstyle) | Passive / Always-on effect |
|---|---|
| **Stalker Jerkin** (Ranger/Stealth flow) | **Hit Counter Stealth:** after **X auto-projectile hits**, gain **Stealth** for **Y sec** (breaks on dealing damage). |
| **Sharpshooter Mantle** (Ranger/Marksman) | **Focus Harness:** each second you stand still grants **Focus** (stacking damage). Moving drops Focus. At max Focus, your next auto becomes a **Heavy Shot** (+damage, +size) (no pierce). |
| **Vanguard Plate** (Knight) | **Guard Rhythm:** every **N hits taken** (or every **X sec**), gain a brief **Guard** that reduces the next **Magic** hit. |
| **Quartermaster’s Cuirass** (Economy) | **Bulk Discount:** reduces gold cost of upgrading **other equipped items** (Weapon/Helmet/Boots/Gloves) by **10/20/30/40/50%** at T1–T5 (does not discount upgrading this chest piece). |
| **Bulwark Hauberk** (Knight/Reflect) | **Thorns Plating:** when hit by **Magic** damage, fire a small retaliatory bolt at the attacker (ICD). |
| **Aegis Robe** (Pure Mage) | **Arcane Ward:** after **3s** without taking damage, gain **Ward stacks (max 3)**. A stack reduces the next **Magic** hit. |
| **Reservoir Robe** (Pure Mage/Battery) | **Overcharge Reservoir:** while at **full Mana**, auto hits build **Overcharge** stacks that increase auto damage up to **+100%** (200% total). Spending Mana or dropping below full clears stacks (or rapidly decays). |
| **Mirrorweave Robe** (Pure Mage/Reflect) | **Mirror Stitch:** when a Ward stack absorbs damage, fire a small seeking shard back (tiny homing) (ICD). |
| **Beastbinder Cuirass** (Summoner) | **Leech Bond:** a % of **summon damage** heals you (steady sustain; cap per second). |
| **Caretaker Vestments** (Summoner/Defense) | **Shared Shield:** when your companion takes a large hit, you gain a brief barrier (ICD). |
| **Gravewrap Carapace** (Necro) | **Rot Leech:** your **Diseased** tick damage (Physical) heals you for a small amount (capped per second). |
| **Glacier Shroud** (Ice Mage) | **Deathless Ice:** when you would die, trigger **Ice Block** instead (short invuln/DR). Long cooldown. |


---

# 12) Consumables (Toolbelt)
Consumables are not part of the 4 core actives.

## 12.1 Toolbelt rules
- Player equips **three consumable items** in a **Toolbelt** loadout.
- Player also equips **one Food item** (separate from the three toolbelt slots).
- Loadout selections **cannot be changed later** (for the run / instance).
- Consumables are **cooldown-based** (not mana-based), simple and readable.
- **Consumables are tiered (T1–T5)**, but they are chosen in your loadout **before** the run.
- **Consumables do not upgrade during the run**.

## 12.2 Initial items
| Item | Effect | Notes / Tags |
|---|---|---|
| **Health Potion** | Instant heal (or fast heal-over-time). | `Consumable`, `Healing` |
| **Mana Potion (Mana Potion)** | Restore Mana instantly (or regen burst). | `Consumable`, `Mana` |
| **Bandages** | Healing item that scales strongly with the **Healing** skill. | `Consumable`, `Bandage`, `Healing` |
| **Smoke Bomb** | Instant **Stealth** + brief reposition window (optional small slow cloud). | `Consumable`, `Stealth`, `Utility` |
| **Food** | Grants **Health Regen** + **Mana Regen** in and out of combat for a long duration (often the whole run). | `Consumable`, `Food`, `Regen` |
| **Scroll: Mana Regen** | Grants **Mana Regen** to the whole party for a short duration. | `Consumable`, `Scroll`, `Mana`, scales with **Inscription** |
| **Scroll: Health Regen** | Grants **HP Regen** to the whole party for a short duration. | `Consumable`, `Scroll`, `Healing`, scales with **Inscription** |
| **Scroll: Max Health** | Increases **Max HP** for the whole party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |
| **Scroll: Magical Damage** | Increases **Magical damage** (all non-Physical types) dealt by the party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |
| **Scroll: Physical Damage** | Increases **Physical damage** dealt by the party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |
| **Totem: Frost** | Pulses **Frost damage** and has a chance to apply **chilled**. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |
| **Totem: Healing** | Heals nearby allies every pulse. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |
| **Totem: Fire** | Pulses **Fire damage** projectiles. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |
| **Totem: Fire** | Pulses **Shock damage** projectiles. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |
| **Totem: Fire** | Drops a totem that applies **ward**. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |

---

## 13) Implementation Notes (so this stays buildable)
- Keep tag lists small (2–6 core tags per ability).
- Skills should hook via tags (e.g., `Projectile`, `DoT`, `Stealth`, `Summon`, `Magic`).
- Summons are core-build viable: Summon skills must reach “primary damage” viability.
- Optional later balance valve: multiple damage tags can apply, but only the top 2 elemental skills apply at full strength (others reduced).
