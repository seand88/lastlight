# Game Loop Spec (Source of Truth)

_Last updated: 2026-03-06 (America/Los_Angeles)_

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
- **All active abilities come from equipment slots.** *Inspired by Albion*
- **Skills never add new buttons.** Skills only add: modifiers, triggers, conversions, scaling, constraints, and AI/targeting changes. *Inspired by UO*
- **Proc City** Your abilities proc things and can be greatly improved by skills. That means something as mundane as your primary generator ability can end up having 5 extra projectiles of different damage types that each apply signature debuffs. *Inspired by Grim Dawn*

### Game Loop
- Loadout for equipment and consumables; lost on death
- Easy, Medium, Hard, Dangerous, and Impossible dungeon / spawn tiers; instanced; multiplayer; ties well into risk vs. reward
- Loot chests drop on ground; can't open until end; requires gold looted during run to unlock at end
- Loot chests come with a price; higher Tier chests have harmful debuffs (e.g. -1 HP)
- Loot chests will disappear quickly so players must decide quickly if they want to accept the risk
- Level up **Equipment** each run using gold; the dungeon instance gets harder so you'll need it
- Several minibosses; after minibosses you fight main boss

There are several systems for customization of character:
1. **Equipment** - Core abilities derived from equipment. Level these up during runs using gold dropped from monsters. Equipment is ephemoral, you can lose it when you die. Regardless, you'll need to level up equipment during each run to keep pace with the monster difficulty scaling.
2. **Skills** - These are permanent alterations to your character. The currency is XP. You do not lose XP or skills if you die. There is a cap to how many skills you can have.
3. **Streaks** - These are perks earned after a successful dungeon run. You unlock them using XP. This is more of an endgame system after your skills are leveled. These are ephemoral as well. You lose them when you die and your streak ends.
4. **Cosmetic Items** - Robes, capes, offhands like torches. These are sourced from: TBD
5. **Toolbelt** - These are consumable items that you load into your toolbelt during the loadout phase. Examples are scrolls for party buffs, potions for big heals, bandages for sustained heals, totems for utility, smokebomb for rogues, etc. Some of these consumables work better if you have a harmonzied skill like **Healing** or **Shamanism**. But generally every characater can use every consumable - they just aren't very useful.

---

## 2) System Responsibilities (what goes where, and why)
This is a **class-less** game: archetypes emerge from loadouts. To keep the system readable and expandable, each system has a job.

### Equipment (verbs)
Currency to upgrade: **Gold**.
- **Weapons:** own **Auto + Special**, baseline bullet feel, and **Tier 2 / Tier 4 choice perks** bullet upgrades (patterns/lanes/pierce/homing/explosions).
- **Helmets:** own the **Defensive** verbs (summon/ward/decoy/mark/heal).
- **Boots:** own the **Mobility** verbs (dash/blink/phase/leap and movement survival).
- **Body Armor:** owns passive build loops (stealth cadence, wards, overcharge, leech).
- **Gloves:** proc to help support a build

Non-weapon abilities are classified as `Utility`. This includes the passive, proc, and active abilities of Helmet, Body Armor, Gloves, and Boots. This is an important distinction because of the negative status effects as follows:

- `Silence` prevents usage of `Utility` passives, actives, and procs
- `Disarm` prevents usage of `Weapon` abilities and `Contact` damage

### Skills (grammar, no new buttons)
Currency to upgrade: **XP**.
- Skills modify existing verbs via **tags**: add statuses, conversions (e.g., Frost Attunement), triggers/procs, AI behavior changes, allows more summons, stronger totems, more projectiles, etc.
- Skills should not replace weapon identity. They **enhance themes** (DoT builds, necro, frost, shock).

### Consumables and Food (loadout tools)
- Consumables are **tiered (T1–T5)**, but they are chosen in your loadout **before** the run.
- Consumables **do not upgrade during the run**.
- Effectiveness for some consumables is determined by corresponding skill (Healing for bandages, Shamanism for totems, Inscription for scrolls).
- **Food is a consumable** that occupies a Toolbelt slot and provides a **5-minute buff** (variants: increased XP gain, HP regen, Mana regen, Gold loot). Food upgrades are TBD.

### Streaks
Currency to upgrade: **XP**.
- An end game system that will be the XP sink.
- You lose all perks on death.

---

## 3) Player Loadout and Buttons

### Equipment

Exactly **4 actives** on the bar:
1) **Weapon Auto Attack**  Projectiles. Delivery, type and shape depend on weapon. Generates mana.
2) **Weapon Special Attack** No cooldown. Uses mana.
3) **Helmet Utility** Cooldown. Uses mana.
4) **Boots Mobility** Cooldown. Uses mana.

Plus:
- **Body Armor:** passive only (no button).
- **Gloves:** proc-only (no button).
- **Loadout lock:** Equipped gear cannot be swapped during a dungeon run.

### Toolbelt

Starts with three slots. Can potentially be upgraded somehow. Use these slots to place consumable items as part of loadout. One use per item. You can bring stacks, e.g. 20 bandages.


---

## 4) Resources
### XP
- **XP is gained on monster kill.**
- XP is spent to upgrade skills from **Tier 1 → Tier 5**.
- XP persists between runs
- XP is spent on **Skills**, specifically **Skill Points**

This table shows the cost of **Skill** perks
| Tier | Point cost | 
|---|---|
| I | 1 |
| II | 1 | 
| III | 2 | 
| IV | 2 | 
| V | 3 |

The **Skill Point** cost to level all 5 perks on a skill is 9. Players are allowed to spend a total of 45 **Skill Points** across all skills. This means they can get all 5 perks in a total of 5 **Skills**. 

Cost of **Skill Points** increases with each purchase. Formula TBD.

| Point | XP Cost |
|---|---|
| 1 | 250 |
| 2 | 500 |
| 3 | 1,000 |
| 4 | 2,500 |
| 5 | 5,000 |
| 6 | 7,500 |
| 7 | 10,000 |
| 8 | 15,000 |
| ... | ... |
| 45 | 1,950,000 |

### Mana (the generator/spender loop)
- **Auto Attack generates Mana** (the bullets are the generator).
- **Add-on projectiles** (e.g., “every 5th shot” skill add-ons) **do not generate Mana**.
- **Passive Mana regen exists via Food**, so you can recover while hiding/running/stealthed with nothing to shoot.
- **Mana is consumed by other abilities** (Weapon Special / Helmet Utility / Boots Mobility) and by procs **only if the ability explicitly says so**.
- **Mana potions** are toolbelt consumables and restore Mana on cooldown.

### Gold (run-only)
- **Gold is earned during the dungeon run** (drops, chests, room rewards).
- Gold is spent on **in-run equipment upgrades** (Tier 1–5) and **Weapon Mastery**.
- The upgrade screen is accessible at any time (for now).
- **Gold does not persist** between runs; leftover gold disappears when the run ends.

### Hit Points
- **HP regen** can come from Food, equipment passives, scrolls, totems and skills.
- **Instant/active healing** comes from Health Potions, Helmet Abilities, Glove Procs, Totems, Bandages, and Skills.


---


## 5) Damage and Status Effects
### Damage types (tags-only)
Damage types are **tags**, not resist tables. 

| Damage Type Tag | Signature Debuff Tags |
|---|---|
| `Physical` | `Bleed`, `Diseased` |
| `Poison` | `Poisoned` |
| `Frost` | `Chilled` |
| `Fire` | `Burning` |
| `Shock` | `Conduit` |


### Negative Status Effects

Status effects follow **per-status stacking rules** (stack, refresh, or unique).

**Conduit**
- Shock hits can apply `Conduit` which lasts **4s**
- `Conduit` can be applied by **any `Direct` `Shock` damage caused by the player**
- While `Conduit` is active, any **`Direct` `Shock` damage caused by the player** to the afflicted target causes a **guaranteed jump** to **1** nearby enemy for **full Shock damage**.
- **`Conduit` jumps cannot apply `Conduit`.**
- Chain depth: **1 hop** baseline (the guaranteed jump).

**Poisoned** (`Poison`, `DoT`)
- Ticks `Poison` damage every second for **8s**.
- Stacks: none.
- Players: can refresh Poisoned on monsters (no stacking).
- Monsters: cannot refresh Poisoned on players (only applies if not already Poisoned).
- While active: blocks **all HP healing/restoration** (including heals, HP regen, and leech/life-steal).
- Does not block: Mana regen or Mana restoration (including Mana potions/refunds).
- Does not block: **Wards**.
- Cleanses: Cure Potion, cleanse effects from items, or expiration.

**Diseased** (`Physical`, `DoT`, `Spread`)
- Ticks `Physical` damage every second for **12s**
- Stacks: up to **5 times** baseline.
- On reapply: add a stack (if under cap) and refresh duration to **12s**.
- Scaling: more stacks = more tick damage.
- Spread: on death, spreads **1 stack** to nearby enemies.
- (Necromancy increases max stacks above 5 for **player-applied** Diseased; exact cap/tier TBD.)

**Bleeding** (`Physical` DoT + vulnerability + Rupture)
- Ticks `Physical` damage every second for **10s**
- Stacks: up to **10**.
- Duration per stack: **10s**.
- Refresh: stacks do **not** refresh on reapply (pressure mechanic).
- At 10 stacks your bleed disappears, unless you have the **Rupture** perk in the **Serration** skill, which causes a big phsyical damage tick equivelant to the sum of all ticks that ocurred.
- **Hemmorhage** Perk adds: Vulnerability: +2% **Physical damage taken** per stack (max +20%).
- **Improved Rupture** can increase max stacks to **20 (40% damage bonus, 20 second stack duration)** .

**Burning** (`Fire`, `Delayed`, `AoE`)
- Duration: **3s**.
- No periodic damage (not a DoT) baseline.
- If the target dies while Burning is active, trigger `AoE` `Fire` damage to enemies within a range of **2**
- **Fire Magic** T5 modifies `Burning` so it is treated as a `DoT` with a single tick at expiration, enabling DoT scaling and DoT cashout; it also causes detonation on expiration.

**Slow**
- Lasts **2s**
- Does not stack; reapply refreshes.
- -25% move speed 
- -25% attack speed
- -25% projectile speed

**Hexed**
- +10% `Magic` damage taken for **6s**. Does not stack; reapply refreshes.

**Chilled**
- increases `Frost` damage taken by 2% per stack
- Stacks up to **5** times
- Lasts **3s**
- Can be refreshed
- If the target is also `Stunned`, `Frost` damage increases damage by 50%

**Stunned**
- Cannot perform any action including movement and attacks
- Can receive healing, but cannot initiate healing while stunned
- Variable length duration, generally between **1-3s**
- Cannot stack

**Rooted**
- Stuck in place, cannot move, but can continue to use abilities

**Disarm**
- An enemy, player, or summoned creature **cannot deal `Contact` damage or use any `Weapon` ability damage** while `Disarmed`. Generally lasts **1-3s**.

**Silence**
- Stops use of `Utility` abilities for duration (boots, glove proc, helmet, chest passive). Generally silence is **1-3s**.

### Positive Status Effects

**HoT**
A `HoT` is a Heal Over Time spell. A short beneficial buff that heals every **1s** for a variable duration (generally **2-8s**). There are many sources for `HoT` buffs such as **Scrolls**, **Utility** abilities, **Skills**, etc.
- Can NOT stack
- Heals every **1s**
- Can be refreshed

**Wards**
- **Wards** absorb **ALL Magical damage** (non-Physical).
- Generated by the Warding skill and certain items.
- Wards are not affected by healing/regen blocks.
- Each stack of Ward can absorb 1 point of damage. Default limit is 20 stacks.
- Ward stacks do not disappear unless the character takes damage.

**Stealth**
- The player or monster is invisible.
- Damage taken is reduced by **90%**
- Performing any action will remove `Stealth`
- Allows for `Ambush`, which increases damage of an attack greatly
- There are many ways to gain the `Stealth` buff, including **Smoke Bombs**, **Equipment** `Utility` abilities, and **Skills**
- Generally lasts between **5-20s**


### Damage delivery (separate from damage type)
Damage type (Physical vs Magical subtypes) is separate from *how* damage is delivered.
- **Projectile:** bullets/arrows/bolts/knives
- **Beam:** sustained or tick-based ray
- **AoE:** ground zones, explosions, cones
- **Contact:** collision impacts at range **0**. All weapons (and enemies) have a baseline **Contact Damage** value and **Contact Damage Rate** (how often contact damage can be dealt while in contact).

---

## 6) Tags (the glue that makes skills work)
Every ability has a small tag set so skills can hook consistently.

### Required tag dimensions
- **Weapon tags:** `Bow`, `Crossbow`, `Dagger`, `Ritual Dagger`, `Spellbook`, `Staff`, `Spellblade`, `Sword`, `Axe`
- **Delivery tags:** `Projectile`, `Beam`, `AoE`, `Contact`
- **Mobility tags:** `Dash`, `Blink`, `Leap`, `Phase`
- **Summon:** `Summon`
- **Sigil/Trap:** `Sigil`, `Trap` (behavior tags, not delivery)
- **Behavior tags:** `DoT`, `Pierce`, `Homing`, `Channel`, `Burst`, `Slow`, `Stealth`, `Ward`, `Ambush`, `Focus`, `Mark`, `Dodge`, `Stagger`, `Interrupt`, `Conduit`, `Bleeding`, `Spread`, `Affliction`, `Burning`, `Poisoned`, `Chilled`, `Stunned`, `Hexed`, `Diseased`, `Disarm`, `Silence`, `Consume`, `Weapon`, `Utility`, `Delayed`, `HoT`
- **Damage tags:** `Physical`, `Poison`, `Frost`, `Fire`, `Shock`
- **Category tags:** `Physical` (Physical damage), `Magic` (all Non-Physical damage), `Elemental` (Frost, Fire and Shock damage)

### Ability structure (implementation model)
Each ability is assembled from:
- **Delivery** (projectile/beam/etc.)
- **Tags**
- **Knobs** (mana cost, cooldown, duration, radius, tick rate, pierce, homing, etc.)

---

## 7) Tooltip / UI Style (IMPORTANT)
Tooltips should prioritize **readable gameplay info**:

### 7.1) Abilities
- Ability Icon
- Ability name
- **Mana cost**
- **Cooldown**
- **Tags**
- Short description
- Tier perks (T1 / T2 / T3 / T4 / T5) - Cost, Activated

### 7.2) Weapons

### 7.3) Armor (Gloves, Boots, Helmet, Body Armor)

### 7.4) Consumables


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


---

## 13) Streaks
This is an end-game system that redirects XP to spend on perks. These perks remain active until you die. They become progressively more expensive. 
- Currency is **XP**
- Lost on death
- must spend 3 points in each tier to move to the next tier.

| Tier | Cost | Option 1 | Option 2 | Option 3 |
|---|---|---|---|---|
| I | 1000/3,500/10,000 | Your healing received from alls ources is increased by 5/10/15% | Dodging or Warding a projectile has a 5/10/15% chance to reflect it back to the attacker | 5/10/15% chance every second to generate a ward that absorbs 10 damage; stacks witgh other Ward sources | 
| II | 25,000/40,000/50,000 | Reduces the cooldown of helmet ability by 10/20/30% | Reduces the cooldown of boot ability by 10/20/30% | Reduces the cooldown of glove proc by 10/20/30% |
| III | TODO| TODO | TODO | TODO |
| IV | TODO | TODO | TODO | TODO |
| V | TODO| TODO | TODO | TODO |

---

## 14) Crafting (TODO)

If we want player crafting the general rule is: You can buy T1 and T2 resources from vendors, but T3+ needs to be player crafted. We're talking potions, bandages, food, totems, scrolls.

Ideally armor would be crafted as well. This means that there should be resource crates inside of dungeon instead of just loot chests

- **Cooking** Gives buffs to food and ability to craft foods
- **Alchemy** Gives buffs to potion consumption and ability to craft potions
- **Inscription** Gives buffs to using scrolls and ability to craft scrolls
- **Shamanism** Stronger totems, ability to craft totems

---

## 15) Implementation Notes (so this stays buildable)
- Keep tag lists small (2–6 core tags per ability).
- Skills should hook via tags (e.g., `Projectile`, `DoT`, `Stealth`, `Summon`, `Magic`).
- Summons are core-build viable: Summon skills must reach “primary damage” viability.
- Optional later balance valve: multiple damage tags can apply, but only the top 2 elemental skills apply at full strength (others reduced).