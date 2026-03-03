# Skills + Equipment Spec (Source of Truth)

_Last updated: 2026-03-02 (America/Los_Angeles)_

This doc is the **single source of truth** for the current design: loadout rules, tags, skills (tiers), equipment abilities (tiers), tooltip conventions, and consumables.

---

## 1) High-Concept
- **Genre:** Top-down 2D **bullet-hell** action RPG.
- **Session loop:** **Instanced dungeons** lasting **5–25 minutes**.
- **Aiming:** **Mouse aim** (movement via WASD).
- **PvP:** **None**.
- **Gear:** Loadout style. Tiered. Replaceable.
- **Death:** Lose all items and ejected from spawn. Do not lose XP or blessed items (cosmetics)

### Design pillar
**Gear gives the buttons (verbs). Skills mutate the buttons (grammar).**
- **All active abilities come from equipment slots.**
- **Skills never add new buttons.** Skills only add: modifiers, triggers, conversions, scaling, constraints, and AI/targeting changes.

---

## 2) System Responsibilities (what goes where, and why)
This is a **class-less** game: archetypes emerge from loadouts. To keep the system readable and expandable, each system has a job.

### Equipment (verbs)
- **Weapons:** own **Auto + Special**, baseline bullet feel, and **Tier 3 / Tier 5 persistent mastery** bullet upgrades (patterns/lanes/pierce/homing/explosions).
- **Helmets:** own the **Utility** verb (summon/ward/decoy/mark/heal utility).
- **Boots:** own the **Mobility** verb (dash/blink/phase/leap and movement survival).
- **Body Armor:** owns passive build loops (stealth cadence, wards, overcharge, leech). Tooltips show only **Physical Damage Reduction** + **Energy Recovery Rate**.

### Skills (grammar, no new buttons)
- Skills modify existing verbs via **tags**: add statuses, conversions (e.g., Frost Attunement), triggers/procs, AI behavior changes.
- Skills should not replace weapon identity. They **enhance themes** (DoT builds, necro, frost, shock) rather than reinventing bullet patterns.

### Consumables and Food (loadout tools)
- **Toolbelt (3 slots):** cooldown consumables for emergency moments (heal now / energy now / stealth now / party buffs).
- **Food (separate pick):** long-duration **HP regen + Energy regen** in and out of combat (supports downtime, kiting, hiding).

### Cheat-sheet: where mechanics belong
- **Homing / pierce / lanes / big bullets / impact pops:** Weapon mastery (Tier 3 / Tier 5 per weapon type).
- **DoT damage + spread chance:** **Virulence**.
- **DoT cashout:** **Ritual Dagger** (verb) + **Catalyst** (scaling).
- **Damage-type conversion:** elemental capstones (Frost now; Fire later).
- **Hard CC (stun/freeze):** Frost stack→Freeze, and select weapon specials (Axe Stagger, Sword Interrupt).

---

## 3) Player Loadout and Buttons
Exactly **4 actives** on the bar:
1) **Weapon Auto Attack** (projectile by default; some weapon mastery options can convert it to a Beam)
2) **Weapon Special Attack**
3) **Helmet Utility**
4) **Boots Mobility**

Plus:
- **Body Armor:** passive only (no button).
- **Weapon equip:** only **one weapon** at a time; swapping exists but has a **cooldown**.

---

## 4) Resources
### XP
- **XP is gained on monster kill.**
- XP is spent to upgrade skills from **Tier 1 → Tier 5**.

### Energy (the generator/spender loop)
- **Auto Attack generates Energy** (the bullets are the generator).
  - Recommended: gain Energy **on-hit** (accuracy matters) with a small per-second cap so multi-hit weapons don’t break the economy.
  - If an Auto Attack is converted into a **Beam**, it generates Energy via **hit ticks** (same cap idea).
- **Passive Energy regen exists via Food**, so you can recover while hiding/running/stealthed with nothing to shoot.
- **Energy is consumed by other abilities** (Weapon Special / Helmet Utility / Boots Mobility) and by procs **only if the ability explicitly says so**.
- **Energy potions (Energy/Mana Potions)** are toolbelt consumables and restore Energy on cooldown.

### Hit Points
- **HP regen** can come from Food, equipment passives, and skills.
- **Instant/active healing** comes from Health Potions and Bandages (Bandages scale strongly with the Healing skill).

---

## 5) Damage Types and Combat Categories and Combat Categories
### Damage types (tags-only)
Damage types are **tags**, not resist tables:
- **Physical**
- **Poison**
- **Frost**
- **Fire**
- **Disease**
- **Shock**

**Outgoing damage buckets**
- **Physical** damage is its own bucket.
- **Magical** damage = everything else (**Poison, Frost, Fire, Disease, Shock**).

Status effects **stack** (for now).

**Shock status: Jolt**
- Shock hits apply **Jolt** stacks.
- At max Jolt stacks, the target **arcs** to nearby enemies (chain), dealing Shock damage and applying some Jolt.
- Shock can also have a small chance to **Interrupt** (stop casting), but does not hard-stun (Frost owns hard CC).
 If needed later, add a soft cap/diminishing returns.

### Damage delivery (separate from damage type)
Damage type (Physical vs Magical subtypes) is separate from *how* damage is delivered.
- **Projectile:** bullets/arrows/bolts/knives
- **Beam:** sustained or tick-based ray
- **AoE:** ground zones, explosions, cones
- **Contact:** touch/swipe impacts

**Defense rules:**
- **Dodge** only affects **Projectile** hits (bullets), regardless of damage type.
- **Warding** reduces **debuff duration and severity** (DoTs, slows, Jolt, Freeze).

---

## 6) Tags (the glue that makes skills work)
Every ability has a small tag set so skills can hook consistently.

### Required tag dimensions
- **Weapon tags:** `Bow`, `Crossbow`, `Dagger`, `Ritual Dagger`, `Spellbook`, `Staff`, `Spellblade`, `Sword`, `Axe`
- **Delivery tags:** `Projectile`, `Beam`, `AoE`, `Dash`, `Summon`, `Trap/Sigil`
- **Behavior tags:** `DoT`, `Pierce`, `Homing`, `Channel`, `Burst`, `Slow`, `Stealth`, `Ward`, `Ambush`, `Focus`, `Mark`, `Dodge`, `Stagger`, `Interrupt`, `Jolt`, `Hemorrhage`, `Spread`, `Affliction`, `Catalyst`
- **Damage tags:** `Physical`, `Poison`, `Frost`, `Fire`, `Disease`, `Shock`
- **Category tags:** `Melee`, `Magic` (category of incoming damage for mitigation rules)


### Ability structure (implementation model)
Each ability is assembled from:
- **Delivery** (projectile/beam/etc.)
- **Tags**
- **Knobs** (energy cost, cooldown, duration, radius, tick rate, pierce count, homing strength, etc.)

---

## 7) Tooltip / UI Style (IMPORTANT)
Tooltips should prioritize **readable gameplay info**:
Show:
- Ability name
- **Energy cost**
- **Cooldown**
- **Tags**
- Short description
- Tier perks (T3 / T5) and synergy notes

Hide:
- General item stat blocks on most gear (for now).

**Exception: Body Armor**
- Body armor can show only these basic stats:
  - **Physical Damage Reduction**
  - **Energy Recovery Rate**

---

## 8) Gear Tiering (T1–T5)
Equipment is Tier 1–5:
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
- **Tier 3 and Tier 5** often include a **choice node** (pick 1 of 2).

## 10.1 Skills Table
> All “adds to auto attack” effects mean: the weapon auto **gains the damage tag** and an associated on-hit effect.

| Skill | Core tags it adds/uses | Tier 1 | Tier 2 | Tier 3 (choice) | Tier 4 | Tier 5 (choice / capstone) |
|---|---|---|---|---|---|---|
| **Stealth** |  `Stealth`, `Ambush`, `Defensive` | While **Stealthed**, take reduced **Projectile** damage (bullets). | **Ambush**: first hit out of Stealth deals big bonus damage. | **A)** *Ghost*: more Magic DR while stealthed **B)** *Assassin*: bigger ambush + execute vs low HP | Stealth lasts longer or has a short “grace” window before breaking. | **A)** *Shadow Refill*: ambush hit refunds Energy (ICD) **B)** *Chain Ambush*: ambush kill re-stealths (ICD) |
| **Poison** | Adds `Poison`, `DoT`, `Slow` to autos | Auto projectiles apply **Poison** (DoT + slow). | Poison damage up; slow stronger. | **A)** *Neurotoxin*: poisoned enemies have reduced projectile/attack speed **B)** *Caustic*: poisoned enemies take increased DoT damage | Poison stacks higher or ticks faster. | **A)** *Toxic Bloom*: poisoned enemies create poison cloud on death **B)** *Venom Loop*: hitting poisoned enemies refunds small Energy (ICD) |
| **Frost Magic** | Adds `Frost`, `Chill`, `Slow`, `Freeze` | Auto projectiles apply **Chill** (slow). | Chill stacks to **Freeze** (a brief stun). | **A)** *Shatter*: frozen targets explode (AoE) **B)** *Brittle*: frozen targets take increased damage | Chill decays slower and freeze uptime improves. | **A)** *Ice Block*: auto ice-block at low HP (long CD) **B)** *Frost Attunement*: your **direct damage** is treated as **Frost** for tags; Weapon Specials have a chance to **Freeze** targets hit (ICD) |
| **Fire Magic** | Adds `Fire`, `Burn`, `DoT`, `Crit` | Auto projectiles apply **Burn** (DoT). | Burn damage up; vs burning targets you gain **+crit chance**. | **A)** *Wildfire*: burn spreads on death **B)** *Hot Streak*: crits extend/boost burn | Burn stacks higher or ticks faster. | **A)** *Ignition*: at high burn stacks, next hit causes small explosion **B)** *Phoenix Ward*: lethal hit prevention by consuming nearby burn stacks (long CD) |
| **Necromancy** | `Disease`, `DoT`, `Summon`, `Undead`, `Corpse` | Auto projectiles apply **Disease** (stacking DoT). Enemies leave **corpses**. | Weapon Special can consume a nearby corpse (if present) to raise an undead. +1 max minion. | **A)** *Swarm*: more weaker minions **B)** *Thrall*: fewer but elite undead | Minions inherit some of your on-hit tags (reduced strength). | **A)** *Corpse Casting*: Weapon Special consumes corpses to spawn temp undead **B)** *Hat Conversion*: Wizard Hat elemental becomes **Undead Wight** (tag/behavior swap) |
| **Spirit Speak** | `Summon`, `AI`, `Inheritance` | Summons gain damage + HP. | Summons gain attack/move speed; improved target selection. | **A)** *Link*: you heal from summon damage (small) **B)** *Command*: Weapon Special orders summons to focus your cursor target (behavior) | Summons inherit more of your on-hit tags (still reduced). | **A)** *Twin Bond*: +1 companion limit for helmet summons **B)** *Aura*: summons emit aura tied to your build tags |
| **Healing** | `Healing`, `Bandage`, `Defensive`, `Trigger` | Bandages heal more. | Bandage use time reduced or adds small HoT. | **A)** *Emergency Wrap*: auto-bandage trigger at low HP (long CD) **B)** *Field Medic*: bandage grants move speed + small barrier | Bandages cleanse minor debuff or reduce incoming DoT briefly. | **A)** *Overheal Shield*: overheal becomes temporary shield **B)** *Second Wind*: after bandage, cooldown recovery briefly faster |
| **Warding** | `Warding`, `Debuff`, `Tenacity` | Debuffs on you have reduced **duration** (slow/freeze/jolt/DoTs). | Debuffs on you have reduced **severity** (weaker slows, lower DoT ticks, slower Jolt stacking). | **A)** *Purity*: periodically cleanse 1 minor debuff (ICD) **B)** *Stoicism*: stronger freeze/stun duration reduction | When a debuff is cleansed/ends, gain brief move speed or a small barrier (ICD). | **A)** *Aegis*: first major debuff every X seconds is heavily reduced/negated **B)** *Ward Pulse*: using Helmet Utility grants a short anti-debuff window |
| **Dodge** | `Dodge`, `Evasion`, `Defensive`, `Projectile` | Gain a small chance to **fully avoid** an incoming **projectile** hit (bullets). | After using Boots Mobility, gain a brief **increased dodge window** vs projectiles. | **A)** *Nimble*: more dodge while moving **B)** *Calm*: dodge works while standing still (lower peak) | Successful dodge grants brief move speed and reduces slows slightly (small). | **A)** *Perfect Dodge*: dodges refund small Energy (ICD) **B)** *Ghost Frame*: first projectile that would hit you each X seconds is automatically dodged |
| **Serration** | `Hemorrhage`, `DoT`, `MartialWeapon` | Your **MartialWeapon** direct hits apply **Hemorrhage** (bleed DoT). | Bleeding enemies **cannot regenerate** HP (or regen reduced to ~0). | **A)** *Cleave*: heavy hits splash 1 Hemorrhage stack to nearby enemies (ICD) **B)** *Deep Cuts*: higher Hemorrhage stack cap / faster stacking | Hemorrhage damage increased; applying Hemorrhage refreshes duration. | **A)** *Rupture*: at max Hemorrhage stacks, next direct hit causes a small burst (ICD) **B)** *Crippling Bleed*: bleeding enemies are slowed slightly |
| **Catalyst** | `DoT`, `Catalyst`, `Burst` | Enables/boosts **DoT Consumption** scaling: your DoT-cashout effects deal more damage. | Increases cashout radius slightly and improves reliability. | **A)** *Efficient Reaction*: cashout refunds some Energy on elite hit (ICD) **B)** *Widening Circle*: larger cashout area | Cashout damage scales more strongly with remaining DoT stacks/duration. | **A)** *Afterburn*: cashout leaves a short lingering zone (tiny) **B)** *Execution*: cashout deals bonus damage to low-HP targets |
| **Virulence** | `DoT`, `Spread`, `Affliction` | Your DoTs deal increased damage. | Your DoT duration/application improves. | **A)** *Outbreak*: increased **spread chance** for your DoT spread triggers **B)** *Corrosion*: DoTs also apply a small “takes more damage” debuff (capped) | Higher DoT stack caps or faster stacking. | **A)** *Pandemic*: on death with 2+ DoTs, spread one stack of each to nearby enemies (ICD) **B)** *Meltdown*: reaching max stacks triggers a small burst (ICD) |
| **Inscription** | `Scroll`, `Buff`, `Support` | Unlocks **Scroll** consumables on the Toolbelt; scroll buffs are stronger. | Scroll duration increased and/or cooldown reduced. | **A)** *Chorus*: scrolls also affect nearby allies more strongly **B)** *Quick Ink*: scroll cooldowns reduced further | Scroll effects scale higher (magnitude). | **A)** *Grand Script*: scrolls affect the whole party in the instance **B)** *Runic Guard*: using a scroll grants a brief barrier to affected allies |


---

# 11) Equipment Tables (Abilities + Tiers)

## 11.1 Weapons (by type)
Weapon identity is fixed per weapon type. Bullet behaviors are primarily expressed via tier progression.

### Weapon identity and bullet behavior (design notes)
- Weapons have **common patterns** and **unique behaviors** per type.
- **Tier 3 and Tier 5** provide **bullet upgrade choices** (A/B). These are **persistent per weapon type**:
  - Your **Bow** stores its T3/T5 selections.
  - Equipping a different Bow later **keeps those selections** (e.g., old bow breaks, new bow inherits mastery choices).
- This creates “class-like” identity through weapon mastery without locking players into classes.

### Bow
| Tier | Auto Attack bullet upgrade | Special (Volley) upgrade |
|---|---|---|
| **T1** | **Quick Shot:** single fast arrow projectile. | **Volley:** cone of arrows (baseline). |
| **T2** | Faster projectile + slight damage increase. | Slightly lower CD or better energy efficiency. |
| **T3 (Choice)** | **A) Heavy Arrow Cadence:** every 4th shot becomes a **large** arrow (+size, +damage, +Pierce 1). **B) Twin Lane:** every 3rd shot fires **2 parallel arrows**. | Volley gains +1 arrow **or** tighter cone (matches your chosen style). |
| **T4** | Modest fire-rate increase. | Modest damage scaling. |
| **T5 (Choice)** | **A) Piercing Line:** autos gain **Pierce +1** (or heavy arrows gain +2 Pierce). **B) Split-on-Hit:** arrows split into 2 weaker arrows after first hit. | **A)** Volley arrows pierce once **B)** Volley gains a small impact pop on marked/center hit. |

### Crossbow (ballistics weapon: pierce/homing/patterns live here)
| Tier | Auto Attack bullet upgrade | Special (Barrage) upgrade |
|---|---|---|
| **T1** | **Bolt Shot:** slower heavy bolt projectile. | **Barrage:** fire a short burst of bolts in lanes (baseline). |
| **T2** | Increased bolt speed + reliability (less whiff). | Better energy efficiency or +1 bolt per barrage. |
| **T3 (Choice)** | **A) Piercing Bolts:** bolts gain **Pierce +1**. **B) Guided Bolts:** bolts gain mild **Homing** (prefers cursor-near targets). | **A)** Barrage gains an extra lane **B)** Barrage gains tighter lanes (more accurate). |
| **T4** | Modest fire-rate increase (“reload” improvement). | Modest scaling. |
| **T5 (Choice)** | **A) Explosive Tips:** bolts create a small impact detonation (low AoE). **B) Split Bolts:** bolts split into 2 weaker bolts after first hit. | **A)** Barrage bolts pierce once **B)** Barrage bolts gain homing (reduced strength). |

### Dagger
| Tier | Auto Attack bullet upgrade | Special (Piercing Ambush) upgrade |
|---|---|---|
| **T1** | **Needle Toss:** short-range, very fast thrown knives (projectiles). | **Piercing Ambush:** short-range piercing strike directly forward. Deals **bonus damage** if you are at **full Energy** and you have **not damaged an enemy in 5s**. |
| **T2** | Slight fire-rate increase + minor range bump. | Slightly lower CD or improved energy efficiency. |
| **T3 (Choice)** | **A) Return Blades:** every 5th knife boomerangs back (can hit again). **B) Triple Burst:** autos fire in 3-shot micro-bursts (spiky proc behavior). | **A)** Piercing Ambush width slightly increased **B)** Piercing Ambush pierces +1 target. |
| **T4** | Better projectile speed + reliability. | Bonus damage scaling (still conditional). |
| **T5 (Choice)** | **A) Ambush Payload:** first hit after Stealth fires +2 extra knives (ambush shotgun). **B) Detonating Kunai:** knives create a tiny impact pop on hit (small AoE). | **A)** If Piercing Ambush kills, re-stealth briefly (ICD) **B)** If Piercing Ambush hits an elite, refund part of Energy (ICD). |


### Ritual Dagger (DoT cashout weapon)
| Tier | Auto Attack bullet upgrade | Special (Ritual Harvest) upgrade |
|---|---|---|
| **T1** | **Blight Dart:** fires **fewer** projectiles; each hit applies a **weak Disease** on direct hit. *(Energy gain per hit should be higher to compensate.)* | **Ritual Harvest:** consume **DoT effects** (Poison/Burn/Disease/Hemorrhage) on enemies in an area, dealing direct damage based on remaining stacks/duration. *(Does not consume Jolt.)* |
| **T2** | Slightly faster darts or better reliability. | Slightly larger radius or better energy efficiency. |
| **T3 (Choice)** | **A) Piercing Darts:** darts gain Pierce +1. **B) Heavy Darts:** bigger hitbox (fewer misses). | **A)** Harvest radius increased **B)** Harvest deals bonus vs elites. |
| **T4** | Modest scaling. | Modest scaling. |
| **T5 (Choice)** | **A) Affliction Darts:** applying a DoT has higher spread chance (small). **B) Splitting Darts:** darts split after first hit (weaker). | **A)** Harvest leaves a short lingering blight zone (tiny) **B)** Harvest refunds some Energy on elite hit (ICD). |

### Spellbook
| Tier | Auto Attack bullet upgrade | Special (Sigil Cast) upgrade |
|---|---|---|
| **T1** | **Arc Bolt:** medium speed projectile (inherits damage tags from skills). | **Sigil Cast:** place rune that detonates / triggers (baseline). |
| **T2** | Faster bolt travel + slightly larger hitbox. | Slightly larger sigil radius or lower CD. |
| **T3 (Choice)** | **A) Chain Script:** bolts chain 1 time. **B) Rune Mark:** bolts leave a rune on hit (sigils detonate runes for bonus). | Sigil gains +1 charge **or** triggers faster. |
| **T4** | Slight fire-rate increase. | Better energy efficiency. |
| **T5 (Choice)** | **A) Tri-Lane Bolts:** every 4th bolt fires 3 lanes. **B) Detonation Glyph:** bolts detonate on impact (small AoE pop). | **A)** Sigil inherits dominant tag stronger **B)** Double-sigil (two smaller circles). |

### Staff
| Tier | Auto Attack bullet upgrade | Special (Channel Beam) upgrade |
|---|---|---|
| **T1** | **Pulse Shot:** projectile bursts (e.g., 3 pulses). | **Channel Beam:** sustained beam (Energy/sec). |
| **T2** | More consistent pulses + modest scaling. | Slight DR while channeling **or** reduced energy drain. |
| **T3 (Choice)** | **A) Piercing Pulses:** final pulse gains Pierce +1. **B) Wide Pulse:** pulses get bigger hitbox. | Beam gains ramp-up **or** allows slightly more move speed while channeling. |
| **T4** | Faster burst cycle (modest). | Better energy efficiency. |
| **T5 (Choice)** | **A) Impact Nova Pulse:** final pulse creates tiny AoE on hit. **B) Twin Pulse:** bursts fire two parallel pulse lanes. | **A)** Release triggers Overload cone **B)** Beam applies status tags more reliably. |

### Spellblade (hybrid weapon)
| Tier | Auto Attack bullet upgrade | Special (Arc Slash) upgrade |
|---|---|---|
| **T1** | **Rune Shot:** projectile auto (bullet-game rule). | **Arc Slash:** AoE arc in front of caster (“lightning-themed”; Shock-tagged). |
| **T2** | Faster shots + slightly larger hitbox. | Better energy efficiency or slightly wider arc. |
| **T3 (Choice)** | **A) Dual Lane:** every 3rd shot fires 2 lanes. **B) Channel Stance:** auto converts into a short **Beam** (still generates Energy on hit ticks). | Arc Slash gains slightly longer reach **or** stronger hit confirm. |
| **T4** | Modest scaling. | Modest scaling. |
| **T5 (Choice)** | **A) Impact Pop:** shots create a tiny detonation on hit. **B) Piercing Runes:** shots gain Pierce +1. | **A)** Arc Slash applies a brief Mark-like debuff (readable damage window) **B)** Arc Slash has a chance to refund a bit of Energy on elite hit (ICD). |

> **Frost Attunement (Frost Magic T5)** treats your **direct damage** as Frost for tags (DoT ticks keep their own type). Spellblade Arc Slash becomes a Frost arc and can Freeze targets via the “Weapon Specials can Freeze” effect.

### Sword
| Tier | Auto Attack bullet upgrade | Special (Whirl Slash) upgrade |
|---|---|---|
| **T1** | **Slash Wave:** wide short-range wave projectile (melee fantasy). | **Whirl Slash:** AoE spin/wave that **Interrupts** enemies hit (stops casting). |
| **T2** | Slightly wider wave + modest scaling. | Slightly lower CD or improved duration. |
| **T3 (Choice)** | **A) Double Wave:** every 3rd attack fires two waves. **B) Crescent Arc:** wave gets bigger hitbox (shorter range). | Whirl gains a small pull-in **or** brief Guard on use. |
| **T4** | Modest fire-rate increase (still slower than dagger). | Better energy efficiency. |
| **T5 (Choice)** | **A) Piercing Crescent:** waves gain Pierce +1. **B) Returning Wave:** waves return after max range. | **A)** Interrupt lasts slightly longer **B)** Hitting 2+ enemies grants Guard vs next Magic hit. |

### Axe
| Tier | Auto Attack bullet upgrade | Special (Ground Slam) upgrade |
|---|---|---|
| **T1** | **Rending Shockwave:** slow thick projectile (heavy feel). | **Ground Slam:** AoE knockback; **inner radius Staggers** (brief stun). |
| **T2** | Faster travel + better reliability. | Slightly larger inner radius or lower CD. |
| **T3 (Choice)** | **A) Boomerang Hatchet:** every 4th auto becomes returning shockwave/hatchet. **B) Split Cleaver:** autos occasionally fire two lanes. | Slam leaves fissure line **or** stronger knockback. |
| **T4** | Modest scaling (axe stays slower). | Better energy efficiency. |
| **T5 (Choice)** | **A) Explosive Impact:** shockwaves create small impact detonation. **B) Crushing Wave:** shockwaves become thicker “bigger bullets”. | **A)** Aftershocks (2 pulses) **B)** Stagger affects slightly larger inner radius (careful tuning). |

## 11.2 Helmets (Utility actives)
Helmets provide the **Utility** button. Energy/CD numbers are placeholders.

| Helmet | Utility Ability (Energy / CD) | Tags | What it does |
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
Boots provide the **Mobility** button. Energy/CD numbers are placeholders.

| Boots | Mobility Ability (Energy / CD) | Tags | What it does | Tier 3 perk | Tier 5 perk |
|---|---|---|---|---|---|
| **Phasewalker Boots** | **Blink** (E+CD) | `Blink`, `Utility` | Teleport a short distance in **movement direction**. Brief i-frames. | Leaves a brief afterimage that draws fire (mini-decoy). | Blink partially resets on elite hit (ICD). |
| **Shadowstep Greaves** | **Vanish Step** (E+CD) | `Dash`, `Stealth`, `Utility` | Short dash that grants instant **Stealth** briefly (breaks on attack). | Exiting stealth grants a small Ambush bonus. | Using Weapon Special while stealthed extends stealth briefly (ICD). |
| **Frosttrail Striders** | **Skate Dash** (E+CD) | `Dash`, `Frost`, `Chill` | Dash leaves an ice trail that chills enemies crossing it. | Trail lasts longer and stacks Chill faster. | Trail causes a mini-freeze pulse when a target reaches max Chill (ICD). |
| **Emberstride Boots** | **Cinder Leap** (E+CD) | `Leap`, `Fire`, `AoE` | Leap forward; landing creates a small fire burst that applies Burn. | Landing burst radius increased. | Landing on burning enemies triggers a small Ignition explosion (ICD). |
| **Wardrunner Boots** | **Phase Slip** (E+CD) | `Dash`, `Ward`, `Defensive`, `Magic` | Dash grants brief **Magic** damage reduction. | Grants 1 Ward stack after dash (if you have not taken damage recently). | Dash leaves a short Ward Field trail that slows projectiles. |
| **Pursuer Treads** | **Hunt Dash** (E+CD) | `Dash`, `Mark`, `Utility` | Dash and apply **Mark** to the nearest/cursor target on your next hit. | Marked targets take increased damage briefly (window). | If the marked target dies, reduce Mobility cooldown slightly (ICD). |
| **Marksman’s Anchors** | **Brace** (E+CD) | `Utility`, `Focus`, `Defensive` | Brief brace window: reduced move speed, rapid Focus stacking. | Focus stacks build faster during Brace. | Reaching max Focus during Brace refunds some Energy (ICD). |
| **Knightcharge Sabatons** | **Bull Rush** (E+CD) | `Dash`, `AoE`, `Knockback` | Longer dash that knocks back enemies to clear space. | End of dash creates a shockwave. | If you hit 2+ enemies, gain Guard vs the next Magic hit. |
| **Gravebound Boots** | **Grave Drift** (E+CD) | `Dash`, `Necro`, `Corpse` | Dash and “collect” nearby corpses for corpse skills. | Collected corpses reduce Weapon Special cooldown slightly (cap/ICD). | After dash, spawn a temporary bone wisp that attacks once per corpse collected. |
| **Beastcall Sandals** | **Relay Blink** (E+CD) | `Blink`, `Summon`, `Utility` | Blink and your companion relocates with you (keeps summons relevant in movement-heavy fights). | Companion gains brief attack speed after relay. | Relay blink heals you slightly based on recent summon damage (ICD). |


## 11.4 Body Armor (allowed basic stats + passives)
Body armor may show only:
- **Physical Damage Reduction**
- **Energy Recovery Rate**

Body armor is **passive-only** (no button). Identity comes from its proc/passive.

| Body Armor (Playstyle) | Basic stats shown | Passive / Proc (Always On) |
|---|---|---|
| **Stalker Jerkin** (Ranger/Stealth flow) | Physical Damage Reduction \| Energy Recovery Rate | **Hit Counter Stealth:** after **X auto-projectile hits**, gain **Stealth** for **Y sec** (breaks on dealing damage). |
| **Sharpshooter Mantle** (Ranger/Marksman) | Physical Damage Reduction \| Energy Recovery Rate | **Focus Harness:** each second you stand still grants **Focus** (stacking damage). Moving drops Focus. At max Focus, your next auto becomes a **Heavy Shot** (+damage, +size) (no pierce). |
| **Vanguard Plate** (Knight) | Physical Damage Reduction \| Energy Recovery Rate | **Guard Rhythm:** every **N hits taken** (or every **X sec**), gain a brief **Guard** that reduces the next **Magic** hit. |
| **Bulwark Hauberk** (Knight/Reflect) | Physical Damage Reduction \| Energy Recovery Rate | **Thorns Plating:** when hit by **Magic** damage, fire a small retaliatory bolt at the attacker (ICD). |
| **Aegis Robe** (Pure Mage) | Physical Damage Reduction \| Energy Recovery Rate | **Arcane Ward:** after **3s** without taking damage, gain **Ward stacks (max 3)**. A stack reduces the next **Magic** hit. |
| **Reservoir Robe** (Pure Mage/Battery) | Physical Damage Reduction \\| Energy Recovery Rate | **Overcharge Reservoir:** while at **full Energy**, auto hits build **Overcharge** stacks that increase auto damage up to **+100%** (200% total). Spending Energy or dropping below full clears stacks (or rapidly decays). |
| **Mirrorweave Robe** (Pure Mage/Reflect) | Physical Damage Reduction \| Energy Recovery Rate | **Mirror Stitch:** when a Ward stack absorbs damage, fire a small seeking shard back (tiny homing) (ICD). |
| **Beastbinder Cuirass** (Summoner) | Physical Damage Reduction \| Energy Recovery Rate | **Leech Bond:** a % of **summon damage** heals you (steady sustain; cap per second). |
| **Caretaker Vestments** (Summoner/Defense) | Physical Damage Reduction \| Energy Recovery Rate | **Shared Shield:** when your companion takes a large hit, you gain a brief barrier (ICD). |
| **Gravewrap Carapace** (Necro) | Physical Damage Reduction \| Energy Recovery Rate | **Rot Leech:** your **Disease** damage heals you for a small amount (capped per second). |
| **Glacier Shroud** (Ice Mage) | Physical Damage Reduction \| Energy Recovery Rate | **Deathless Ice:** when you would die, trigger **Ice Block** instead (short invuln/DR). Long cooldown. |


---

# 12) Consumables (Toolbelt)
Consumables are not part of the 4 core actives.

## 12.1 Toolbelt rules
- Player equips **three consumable items** in a **Toolbelt** loadout.
- Player also equips **one Food item** (separate from the three toolbelt slots).
- Loadout selections **cannot be changed later** (for the run / instance).
- Consumables are **cooldown-based** (not energy-based), simple and readable.
- **Consumables are tiered (T1–T5)**. Tier increases effectiveness and may add a breakpoint perk at **T3** and **T5**.

## 12.2 Initial items
| Item | Effect | Notes / Tags |
|---|---|---|
| **Health Potion** | Instant heal (or fast heal-over-time). | `Consumable`, `Healing` |
| **Energy Potion (Mana Potion)** | Restore Energy instantly (or regen burst). | `Consumable`, `Energy` |
| **Bandages** | Healing item that scales strongly with the **Healing** skill. | `Consumable`, `Bandage`, `Healing` |
| **Smoke Bomb** | Instant **Stealth** + brief reposition window (optional small slow cloud). | `Consumable`, `Stealth`, `Utility` |
| **Food** | Grants **Health Regen** + **Energy Regen** in and out of combat for a long duration (often the whole run). | `Consumable`, `Food`, `Regen` |
| **Scroll: Mana Regen** | Grants **Energy Regen** to the whole party for a short duration. | `Consumable`, `Scroll`, `Energy`, scales with **Inscription** |
| **Scroll: Health Regen** | Grants **HP Regen** to the whole party for a short duration. | `Consumable`, `Scroll`, `Healing`, scales with **Inscription** |
| **Scroll: Max Health** | Increases **Max HP** for the whole party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |
| **Scroll: Magical Damage** | Increases **Magical damage** (all non-Physical types) dealt by the party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |
| **Scroll: Physical Damage** | Increases **Physical damage** dealt by the party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |

> Health and Mana potions can be separate toolbelt items, or one “Potion” slot that is chosen as Health or Mana. For now, treat them as separate items to create real loadout decisions.

 can be separate toolbelt items, or one “Potion” slot that is chosen as Health or Mana. For now, treat them as separate items to create real loadout decisions.

---

## 13) Implementation Notes (so this stays buildable)
- Keep tag lists small (2–6 core tags per ability).
- Skills should hook via tags (e.g., `Projectile`, `DoT`, `Stealth`, `Summon`, `Magic`).
- Summons are core-build viable: Summon skills must reach “primary damage” viability.
- Optional later balance valve: multiple damage tags can apply, but only the top 2 elemental skills apply at full strength (others reduced).

