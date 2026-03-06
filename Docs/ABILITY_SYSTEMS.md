# Skills + Equipment Spec (Source of Truth)

_Last updated: 2026-03-05 (America/Los_Angeles)_

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
- **Helmets:** own the **Utility** verb (summon/ward/decoy/mark/heal utility).
- **Boots:** own the **Mobility** verb (dash/blink/phase/leap and movement survival).
- **Body Armor:** owns passive build loops (stealth cadence, wards, overcharge, leech).
- **Gloves:** proc to help support a build

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
Damage types are **tags**, not resist tables:
- **Physical**
- **Poison**
- **Frost**
- **Fire**
- **Shock**

**Outgoing damage buckets**
- **Physical** damage is its own bucket.
- **Magical** damage = everything else (**Poison, Frost, Fire, Shock**).

Status effects follow **per-status stacking rules** (stack, refresh, or unique).

**Conduit**
- Shock hits can apply **Conduit**
- Duration: **4s**.
- Conduit can be applied by **any direct Shock damage caused by the player** (see Direct damage definition).
- While Conduit is active, any **direct Shock damage caused by the player** to the afflicted target causes a **guaranteed jump** to **1** nearby enemy for **full Shock damage**.
- **Conduit jumps cannot apply Conduit.**
- Chain depth: **1 hop** baseline (the guaranteed jump).

**Poisoned** (Poison DoT)
- Ticks Poison damage over time.
- Duration: **8s**.
- Stacks: none.
- Players: can refresh Poisoned on monsters (no stacking).
- Monsters: cannot refresh Poisoned on players (only applies if not already Poisoned).
- While active: blocks **all HP healing/restoration** (including heals, HP regen, and leech/life-steal).
- Does not block: Mana regen or Mana restoration (including Mana potions/refunds).
- Does not block: **Wards**.
- Cleanses: Cure Potion, cleanse effects, or expiration.

**Diseased** (Physical DoT + spread)
- Ticks Physical damage over time.
- Duration: **12s** (refreshes on reapply).
- Stacks: up to **5** baseline.
- On reapply: add a stack (if under cap) and refresh duration to **12s**.
- Scaling: more stacks = more tick damage.
- Spread: on death, spreads **1 stack** to nearby enemies.
- (Necromancy increases max stacks above 5 for **player-applied** Diseased; exact cap/tier TBD.)

**Bleeding** (Physical DoT + vulnerability + Rupture)
- Ticks Physical damage over time.
- Stacks: up to **10**.
- Duration per stack: **10s**.
- Refresh: stacks do **not** refresh on reapply (pressure mechanic).
- At 10 stacks your bleed disappears, unless you have the **Rupture** perk in the **Serration** skill, which causes a big phsyical damage tick equivelant to the sum of all ticks that ocurred.
- **Hemmorhage** Perk adds: Vulnerability: +2% **Physical damage taken** per stack (max +20%).
- **Improved Rupture** can increase max stacks to **20 (40% damage bonus, 20 second stack duration)** .

**Burning** (Fire death/expiration mark)
- Duration: **3s**.
- No periodic damage (not a DoT) baseline.
- If the target dies while Burning is active, trigger a small AoE **Fire** explosion (does not apply Burning).
- Fire Magic T5 modifies Burning so it is treated as a DoT with a single tick at expiration, enabling DoT scaling and DoT cashout; it also causes detonation on expiration.

**Slow**
- Lasts **2s**
- Does not stack; reapply refreshes.
- -25% move speed 
- -25% attack speed
- -25% projectile speed

**Hexed**
- +10% **Magical** damage taken for **6s**. Does not stack; reapply refreshes.

**Chilled**
- increases frost damage taken by 2% per stack
- Stacks up to 5 times
- lasts 3 seconds
- Can be refreshed
- if the target is also stunned, **Frost** increases damage by 50%

**Stunned**
- Cannot perform any action including movement and attacks
- Can receive healing, but cannot initiate healing while stunned
- Variable length duration, generally between 1 - 3 seconds
- Cannot stack

**Rooted**
- stuck in place, cannot move, but can continue to use abilities

**Disarm**
- An enemy, player, or summoned creature **cannot deal Contact damage or any weapon ability damage** while Disarmed. Generally lasts **1 - 3s**.

**Silence**
- Stops use of **non-Weapon** abilities for duration (boots, glove proc, helmet, chest passive). Generally silence is **1 - 3s**.

### Wards
- **Wards** absorb **ALL Magical damage** (non-Physical).
- Generated by the Warding skill and certain items.
- Wards are not affected by healing/regen blocks.
- Each stack of Ward can absorb 1 point of damage. Default limit is 20 stacks.
- Ward stacks do not disappear unless the character takes damage.

### Damage delivery (separate from damage type)
Damage type (Physical vs Magical subtypes) is separate from *how* damage is delivered.
- **Projectile:** bullets/arrows/bolts/knives
- **Beam:** sustained or tick-based ray
- **AoE:** ground zones, explosions, cones
- **Contact:** collision impacts at range **0**. Some weapons and effects have a baseline **Contact Damage** value and **Contact Damage Rate** (how often contact damage can be dealt while in contact).

---

## 6) Tags (the glue that makes skills work)
Every ability has a small tag set so skills can hook consistently.

### Required tag dimensions
- **Weapon tags:** `Bow`, `Crossbow`, `Dagger`, `Ritual Dagger`, `Spellbook`, `Staff`, `Spellblade`, `Sword`, `Axe`
- **Delivery tags:** `Projectile`, `Beam`, `AoE`, `Contact`
- **Mobility tags:** `Dash`, `Blink`, `Leap`, `Phase`
- **Summon:** `Summon`
- **Sigil/Trap:** `Sigil`, `Trap` (behavior tags, not delivery)
- **Behavior tags:** `DoT`, `Pierce`, `Homing`, `Channel`, `Burst`, `Slow`, `Stealth`, `Ward`, `Ambush`, `Focus`, `Mark`, `Dodge`, `Stagger`, `Interrupt`, `Conduit`, `Bleeding`, `Spread`, `Affliction`, `Burning`, `Poisoned`, `Chilled`, `Stunned`, `Hexed`, `Diseased`, `Disarm`, `Silence`, `Consume`
- **Damage tags:** `Physical`, `Poison`, `Frost`, `Fire`, `Shock`
- **Category tags:** `Physical`, `Magic` (incoming mitigation)

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


| Skill | Core tags it adds/uses | T1 Enablement | T2 Choice | T3 Numbers | T4 Choice | T5 Numbers |
|---|---|---|---|---|---|---|
| **Stealth** | `Stealth`, `Ambush`, `Defensive` | While **Stealthed**, take reduced **Projectile** damage (bullets). | **A)** *Ghost*: more Magic DR while stealthed **B)** *Assassin*: bigger ambush + execute vs low HP | **Ambush**: first hit out of Stealth deals big bonus damage. | **A)** *Shadow Refill*: ambush hit refunds Mana (ICD) **B)** *Chain Ambush*: ambush kill re-stealths (ICD) | Stealth lasts longer or has a short “grace” window before breaking. |
| **Poison** | `Poison`, `Poisoned` | Adds a **Poison** projectile to your generator ability that fires **every second** and has a **range of 5**. All direct **Poison** damage caused by the player can apply **Poisoned** with a base **5%** chance. | **A)** Poison dart firing rate increased to **2 poison projectiles / second** **B)** Your poison dart can now **Pierce** and has **range increased by 3**. | Increases all **Poison** damage by **10%**. Increases application rate of **Poisoned** by an additional **+15%**. All **Poison** damage increased by an additional **+15%**. **Also:** Every time you refresh **Poisoned** on an enemy, you instantly deal **an extra tick** of Poison damage. | **A)** TODO **B)** Chance apply poison to all enemies nearby target **C)** Chance to Summon Poison Elemental on enemy death (limit 1 poison elemental) | Increases application rate of **Poisoned** by an additional **+15%**. All **Poison** damage increased by an additional **+15%**.
| **Frost Magic** | `Frost`, `Chilled`, `Stunned` | Adds a **Frost** projectile to your generator ability that fires **every second** and has a **range of 5**. All direct **Frost** damage caused by the player can apply **Chilled** with a base **5%** chance. | **A)** Chilled application chance increased by an additional **+15%**. Chilled also applies **Slow**. **B)** Instead of applying Chilled, your attacks now generate a **+1 HP Ward**. All skills and abilities that increase application chance of **Ward** now also increase this Ward generation chance. | Increases all **Frost** damage by **10%**. Increases application rate of **Chilled** by an additional **+15%**. All **Frost** damage increased by an additional **+15%**. **Also:** At **5 Chilled stacks**, **Stun** the target for **2 seconds**. While at **5 stacks of Chilled** and **Stunned**, the target takes **+50% more Frost damage**. When the Stun ends, **remove Chilled**. | **A)** Your Weapon Special costs **5** extra Mana but now launches **3 icicles** in a **90° cone** with a **radius of 6**. Damage is **doubled** against **Rooted** or **Stunned** targets that are also **Chilled**. **B)** Your Boots Mobility ability now also **Roots** nearby enemies for **2 seconds** and applies **1 stack of Chilled**. **C)** Chance to Summon Frost Elemental on enemy death (limit 1 Frost elemental) | **Ice Block:** auto ice-block at low HP which makes you immune to all attacks for **5 seconds** but you cannot perform any action. |
| **Fire Magic** |  `Fire`, `Burning` | Adds a **Fire** projectile to your generator ability that fires **every second** and has a **range of 5**. All direct **Fire** damage caused by the player can apply **Burning** with a base **5%** chance. | **A)** *Bigger Boom*: Burning explosion damage/radius increased **B)** *Long Fuse*: Burning duration increased | Increases all **Fire** damage by **10%**. Increases application rate of **Burning** by an additional **+15%**. All **Fire** damage increased by an additional **+15%**. **Also:** Massively increases **detonation radius** of **Burning**. | **A)** **Shrapnel:** Burning detonations apply **Weakened** (**3s**) to enemies hit. **B)** TODO **C)** Chance to Summon Fire Elemental on enemy death (limit 1 fire elemental) | Increases application rate of **Burning** by an additional **+15%**. All **Fire** damage increased by an additional **+15%**.
| **Shock Magic** |  `Shock`, `Conduit`, `Static Charge` | Adds a **Shock** projectile to your generator ability that fires **every second** and has a **range of 6**. All direct **Shock** damage caused by the player can apply **Conduit** with a base **5%** chance. Direct Shock damage that hits a target afflicted by **Conduit** will **jump** to **1** nearby target for full Shock damage. Limit: **1** jump target. | **A)** Shock damage does more damage at short range: at **range 0** it does **+50%** more damage; at **range 4** it does normal damage. **B)** Each Conduit jump generates **+1 Mana**. Increases chance to apply Conduit by an additional **+15%**. | Increase Conduit application chance by an additional **+15%**. **Static Charges:** Whenever a **Conduit jump actually occurs**, gain **1 Static Charge**. Static Charge lasts **10s**, stacks up to **10**. Gaining a charge at **10 stacks** refreshes the duration of all stacks. No gain-rate cap. **Contact:** Static Charge will do Shock damage on **Contact** (range=0) against a **Conduit** enemy, dealing a Shock hit that consumes **1 Static Charge**. | **A)** **Discharge:** Using Weapon Special while you have at least **7 Static Charges** consumes **7** Static Charges and triggers a Discharge (range **4**) that **Stuns** Conduit enemies for **1s**. Affected targets **lose Conduit after the stun fades**. **B)** When dealing Static Charge damage on contact (from Tier 3), **Disarm** the target. **C)** Chance to Summon Shock Elemental on enemy death (limit 1 shock elemental) | **Proximity scaling:** Static Charge-based damage (T4A and T4B) deals up to **+100%** damage at point-blank, scaling linearly down to **+0%** bonus at distance **>= range 4**. |
| **Necromancy** | `Diseased`, `DoT`, `Summon`, `Undead`, `Corpse` | Adds a **Physical** projectile to your generator ability that fires **every second** and has a **range of 5**. All direct **Physical** damage caused by the player can apply **Diseased** with a base **5%** chance. | **A)** *Swarm*: Diseased spreads 1 stack to **up to 3** enemies on death and has increased spread radius (TBD). **B)** *Plague*: set base chance to apply Diseased to **50%** for all player Diseased interactions. | Increases all **Physical** damage by **10%**. Increases application rate of **Diseased** by an additional **+15%**. All **Physical** damage increased by an additional **+15%**. **Also:** Diseased application also applies **Hexed**. | **A)** *Corpse Raise*: Weapon Special consumes a nearby corpse (if present) to raise weak undead that last **30s**; limit 3; separate cap; auto-only. Add-on: Weapon Special still does its normal attack. At cap, replaces oldest. **B)** *Army of the Dead*: converts other summons to Undead, replacing their identity and special; all their attack damage becomes Physical; their on-hit signature chance stays the same but the applied status becomes Diseased. | Increases application rate of **Diseased** by an additional **+15%**. All **Physical** damage increased by an additional **+15%**.
| **Spirit Speak** | `Summon`, `AI` | Increase summoning chance by an additional **+5%**. Summoned creatures have their signature debuff proc chance increased by an additional **+5%**. Summons health and mana increased by **10%**. | **A)** Summon limit from items increased by **+1** **B)** Summons expire **10 seconds** sooner, but their damage is increased by **20%** | Glove proc has a **25%** chance to summon a **snake** that lasts **25 seconds** and spits venom. Limit **1**. Increase summoning chance by an additional **+5%**. Summoned creatures have their signature debuff proc chance increased by an additional **+5%**. Summons health and mana increased by **10%**. | **A)** Summoned monsters have an additional **+20%** chance to apply their signature debuff **B)** Your summons last **10 seconds** less, but they explode when killed or their timer runs out, dealing their damage type as **360° AoE projectiles** **C)** TODO | **Spirit Link:** **20%** of damage you take is split among your pets. |
| **Healing** | `Healing`, `Bandage`, `Defensive`, `Trigger` | Bandages heal more. | **A)** *Emergency Wrap*: auto-bandage trigger at low HP (long CD) **B)** *Field Medic*: bandage grants move speed + small barrier | Bandage use time reduced or adds small HoT. | **A)** *Overheal Shield*: overheal becomes temporary shield **B)** *Second Wind*: after bandage, cooldown recovery briefly faster | Bandages cleanse minor debuff or reduce incoming DoT briefly. |
| **Warding** | `Warding`, `Ward`, `Shield`, `Defensive` | If you avoid damage for **X seconds**, begin generating **Ward** stacks up to **20** every second. | **A)** *Bulwark*: higher max Ward stacks **B)** *Purity*: when a Ward stack breaks, cleanse 1 random negative status (ICD **8s**) | Numeric scaling (stack size/rate/cap). | **A)** *Mirror Ward*: absorbed magic fires a retaliatory bolt (ICD) **B)** *Ward Surge*: Ward break grants brief barrier + speed (ICD) | Numeric scaling |
| **Dodge** | `Dodge`, `Evasion`, `Defensive`, `Physical`, `Contact` | Chance to dodge **Physical direct damage** (not DoTs). | **A)** *Duelist*: higher dodge while close **B)** *Footwork*: higher dodge while moving | Improves dodge chance. | **A)** *Perfect Step*: on dodge, gain brief move speed + Mana (ICD) **B)** *Ghost Frame*: first eligible hit each X seconds is dodged | Further scaling. |
| **Serration** | `Bleeding`, `DoT`, `Physical` | Adds a **Physical** projectile to your generator ability that fires **every second** and has a **range of 5**. All direct **Physical** damage caused by the player can apply **Bleeding** with a base **5%** chance. | **A)** *Cleave*: heavy hits splash 1 Bleeding stack to nearby enemies (ICD) **B)** *Deep Cuts*: higher Bleeding stack cap / faster stacking | **Hemmorhage**: **Bleed** stacks make the target vulnerable, increasing **Physical** damage taken by **2%**. Increases application rate of **Bleeding** by an additional **+15%**.  When **10 Bleeding stacks** are reached the target **Ruptures**, dealing **sum of dot tick damage as Physical** instantly**. Consumes all **Bleed** stacks. | **A)** *Improved Rupture*: Bleed can now stack up to 20. **B)** *Crippling Bleed*: Bleed applies **Slow** and the **Physical** damage ticks deal 25% more damage. | Increases application rate of **Bleeding** by an additional **+15%**. All **Physical** damage increased by an additional **+15%**.
| **Arms Lore** | `Status`, `Utility`, `Amplify` | Improve **non-damaging** status effects you apply (NOT DoTs). | **A)** *Control*: increase duration/magnitude of debuffs **B)** *Conduit*: improve Conduit effectiveness | Numeric scaling. | **A)** *Extended Control*: further duration scaling **B)** *Deep Hexed*: increases Hexed magnitude further | Capstone: up to +150% duration/magnitude for non-damaging statuses; also increases Conduit chain depth up to 5 hops. |
| **Virulence** | `DoT`, `Spread`, `Affliction` | You have an additional **5%** chance to apply signature debuffs like `Poisoned`, `Bleeding`, `Diseased`, `Conduit`, `Burning` and `Chilled`. Your signature debuffs that deal damage now deal an additional **10%** damage. | **A)** Increase damage for `Poisoned`, `Bleeding`, Burning detonation damage and `Diseased` by an additional **25%**. **B)** Using a **Weapon Special** ability will increase the duration of `Stunned` by **0.5s**, `Rooted` by **1s**, and `Slow` by **1.5s**. | Your DoT duration/application improves. | **A)** *Outbreak*: when an enemy dies all your negative effects are transferred from that enemy to a nearby enemy. **B)** TODO | Higher DoT stack caps or faster stacking. (At various tiers, Virulence will also increase DoT apply chance; details TBD.) |
| **Inscription** | `Scroll`, `Buff`, `Support` | Unlocks **Scroll** consumables on the Toolbelt; scroll buffs are stronger. | **A)** *Chorus*: scrolls also affect nearby allies more strongly **B)** *Quick Ink*: scroll cooldowns reduced further | Scroll duration increased and/or cooldown reduced. | **A)** *Grand Script*: scrolls affect the whole party **B)** *Runic Guard*: using a scroll grants a brief barrier | Scroll effects scale higher (magnitude). |
| **Shamanism** | `Totem`, `Support`, `AoE` | Unlocks **Totem** consumables on the Toolbelt. | **A)** *Larger Radius*: Fill me in **B)** *More Boom Boom*: more damage | Totem duration increased and/or cooldown reduced. | **A)** TODO **B)** TODO | Totem radius increased. |

---

# 11) Equipment Tables (Abilities + Tiers)

## 11.1 Weapons (by type)
Weapon identity is fixed per weapon type. Bullet behaviors are primarily expressed via tier progression.

### Weapon identity and bullet behavior (design notes)
- Weapons have **common patterns** and **unique behaviors** per type.
- **Tier 2 and Tier 4** provide **bullet upgrade choices** (A/B). These are **selected per run**:
  - This creates “class-like” identity through weapon mastery without locking players into classes.
- **Weapon Specials have no cooldown.** Any tier text referencing Special cooldown should be replaced with **improved Mana efficiency**.

### Bow
| Tier | Auto Attack bullet upgrade | Special (Volley) upgrade |
|---|---|---|
| **T1** | **Quick Shot:** single, fast arrow projectile. Rate: **1.0s**. Range: **10**. Damage Type: **Physical**. Damage: **10**. Projectile Speed: **10** Contact Damage: **1**. Contact Damage Rate: **0.75s**. | **Volley:** cone of arrows (baseline). |
| **T2 (Choice)** | **A) Heavy Arrow Cadence:** every 4th shot becomes a **large** arrow (+size, +damage, +Pierce 1). **B) Twin Lane:** every 3rd shot fires **2 parallel arrows**. | Volley gains +1 arrow **or** tighter cone (matches your chosen style). |
| **T3** | Faster projectile + slight damage increase. | Better mana efficiency. |
| **T4 (Choice)** | **A) Piercing Line:** autos gain **Pierce +1** (or heavy arrows gain +2 Pierce). **B) Split-on-Hit:** arrows split into 2 weaker arrows after first hit. | **A)** Volley arrows pierce once **B)** Volley gains a small impact pop on marked/center hit. |
| **T5** | Modest fire-rate increase. | Modest damage scaling. |

### Crossbow (ballistics weapon: pierce/homing/patterns live here)
| Tier | Auto Attack bullet upgrade | Special (Barrage) upgrade |
|---|---|---|
| **T1** | **Bolt Shot:** slower heavy bolt projectile. Rate: **1.25s**. Range: **8**. Damage Type: **Frost**. Damage: **12**. Projectile Speed: **8** Contact Damage: **1**. Contact Damage Rate: **2.5s**. | **Barrage:** fire a short burst of bolts in lanes (baseline). |
| **T2 (Choice)** | **A) Piercing Bolts:** bolts gain **Pierce +1**. **B) Guided Bolts:** bolts gain mild **Homing** (prefers cursor-near targets). | **A)** Barrage gains an extra lane **B)** Barrage gains tighter lanes (more accurate). |
| **T3** | Increased bolt speed + reliability (less whiff). | Better mana efficiency or +1 bolt per barrage. |
| **T4 (Choice)** | **A) Explosive Tips:** bolts create a small impact detonation (low AoE). **B) Split Bolts:** bolts split into 2 weaker bolts after first hit. | **A)** Barrage bolts pierce once **B)** Barrage bolts gain homing (reduced strength). |
| **T5** | Modest fire-rate increase (“reload” improvement). | Modest scaling. |

### Dagger
| Tier | Auto Attack bullet upgrade | Special (Piercing Ambush) upgrade |
|---|---|---|
| **T1** | **Needle Toss:** short-range, very fast thrown knives (projectiles). Rate: **0.75s**. Range: **10**. Damage Type: **Poison**. Damage: **3**. Projectile Speed: **13** Contact Damage: **1**. Contact Damage Rate: **0.75s**. | **Piercing Ambush:** short-range piercing strike directly forward. Deals **bonus damage** if you are at **full Mana** and you have **not damaged an enemy in 5s**. |
| **T2 (Choice)** | **A) Return Blades:** every 5th knife boomerangs back (can hit again). **B) Triple Burst:** autos fire in 3-shot micro-bursts (spiky proc behavior). | **A)** Piercing Ambush width slightly increased **B)** Piercing Ambush pierces +1 target. |
| **T3** | Slight fire-rate increase + minor range bump. | Better mana efficiency. |
| **T4 (Choice)** | **A) Ambush Payload:** first hit after Stealth fires +2 extra knives (ambush shotgun). **B) Detonating Kunai:** knives create a tiny impact pop on hit (small AoE). | **A)** If Piercing Ambush kills, re-stealth briefly (ICD) **B)** If Piercing Ambush hits an elite, refund part of Mana (ICD). |
| **T5** | Better projectile speed + reliability. | Bonus damage scaling (still conditional). |

### Ritual Dagger (DoT cashout weapon)
| Tier | Auto Attack bullet upgrade | Special (Ritual Harvest) upgrade |
|---|---|---|
| **T1** | **Blight Dart:** fires fewer projectiles; each hit applies a **Diseased** stack on direct hit with a **15%** chance. Rate: **1.10s**. Range: **5**. Damage Type: **Physical**. Damage: **2**. Projectile Speed: **6** Contact Damage: **1**. Contact Damage Rate: **2.5s**. | **Ritual Harvest:** consume **all damage-over-time statuses** (Poisoned, Diseased, Bleeding, and **Burning if it is in DoT mode via Fire Magic T5**) on enemies in an area, instantly dealing **all remaining scheduled tick damage** as direct damage, then removing those statuses. *(Does not consume Conduit.)* |
| **T2 (Choice)** | **A) Piercing Darts:** darts gain Pierce +1. **B) Heavy Darts:** bigger hitbox (fewer misses). | **A)** Harvest radius increased **B)** Harvest deals bonus vs elites. |
| **T3** | Slightly faster darts or better reliability. | Better mana efficiency or slightly larger radius. |
| **T4 (Choice)** | **A) Affliction Darts:** applying a DoT has higher spread chance (small). **B) Splitting Darts:** darts split after first hit (weaker). | **A)** Harvest leaves a short lingering blight zone (tiny) **B)** Harvest refunds some Mana on elite hit (ICD). |
| **T5** | Modest scaling. | Modest scaling. |

### Spellbook
| Tier | Auto Attack bullet upgrade | Special (Sigil Cast) upgrade |
|---|---|---|
| **T1** | **Arc Bolt:** medium speed, long projectile. Rate: **1.0s**. Range: **7**. Damage Type: **Shock**. Damage: **6**. Projectile Speed: **9** Contact Damage: **1**. Contact Damage Rate: **0.75s**. | **Sigil Cast:** place rune that detonates / triggers (baseline). |
| **T2 (Choice)** | **A) Chain Script:** bolts chain 1 time. **B) Rune Mark:** bolts leave a rune on hit (sigils detonate runes for bonus). | Sigil gains +1 charge **or** triggers faster. |
| **T3** | Faster bolt travel + slightly larger hitbox. | Better mana efficiency or larger sigil radius. |
| **T4 (Choice)** | **A) Tri-Lane Bolts:** every 4th bolt fires 3 lanes. **B) Detonation Glyph:** bolts detonate on impact (small AoE pop). | **A)** Sigil inherits dominant tag stronger **B)** Double-sigil (two smaller circles). |
| **T5** | Slight fire-rate increase. | Better mana efficiency. |

### Staff
| Tier | Auto Attack bullet upgrade | Special (Channel Beam) upgrade |
|---|---|---|
| **T1** | **Pulse Shot:** fires a large, slow projectile at enemies that has a **5%** chance to apply the **Chilled** effect. Rate: **1.5s**. Range: **5**. Damage Type: **Frost**. Damage: **5**. Projectile Speed: **4** Contact Damage: **1**. Contact Damage Rate: **2.5s**. | **Channel Beam:** sustained beam (Mana/sec). |
| **T2 (Choice)** | **A) Piercing Pulses:** final pulse gains Pierce +1. **B) Wide Pulse:** pulses get bigger hitbox. | Beam gains ramp-up **or** allows slightly more move speed while channeling. |
| **T3** | More consistent pulses + modest scaling. | Slight DR while channeling **or** reduced mana drain. |
| **T4 (Choice)** | **A) Impact Nova Pulse:** final pulse creates tiny AoE on hit. **B) Twin Pulse:** bursts fire two parallel pulse lanes. | **A)** Release triggers Overload cone **B)** Beam applies status tags more reliably. |
| **T5** | Faster burst cycle (modest). | Better mana efficiency. |

### Spellblade (hybrid weapon)
| Tier | Auto Attack bullet upgrade | Special (Arc Slash) upgrade |
|---|---|---|
| **T1** | **Spellblade Shot:** emits a long, hard, and fast fireball that has a **5%** chance to apply **Burning** to anything it touches. Rate: **0.8s**. Range: **9**. Damage Type: **Fire**. Damage: **6**. Projectile Speed: **9** Contact Damage: **1**. Contact Damage Rate: **0.75s**. | **Arc Slash:** AoE arc in front of caster (“lightning-themed”; Shock-tagged). |
| **T2 (Choice)** | **A) Dual Lane:** every 3rd shot fires 2 lanes. **B) Channel Stance:** auto converts into a short **Beam** (still generates Mana on hit ticks). | Arc Slash gains slightly longer reach **or** stronger hit confirm. |
| **T3** | Faster shots + slightly larger hitbox. | Better mana efficiency or slightly wider arc. |
| **T4 (Choice)** | **A) Impact Pop:** shots create a tiny detonation on hit. **B) Piercing Runes:** shots gain Pierce +1. | **A)** Arc Slash applies a brief Mark-like debuff (readable damage window) **B)** Arc Slash has a chance to refund a bit of Mana on elite hit (ICD). |
| **T5** | Modest scaling. | Modest scaling. |

### Sword
| Tier | Auto Attack bullet upgrade | Special (Whirl Slash) upgrade |
|---|---|---|
| **T1** | **Slash Wave:** a wide, arc shape travels a short distance at a modest speed dealing physical damage. Rate: **1.0s**. Range: **4**. Damage Type: **Physical**. Projectile Speed: **8** Contact Damage: **1**. Contact Damage Rate: **0.75s**. | **Whirl Slash:** AoE spin/wave that **Interrupts** enemies hit (stops casting). |
| **T2 (Choice)** | **A) Double Wave:** every 3rd attack fires two waves. **B) Crescent Arc:** wave gets bigger hitbox (shorter range). | Whirl gains a small pull-in **or** brief Guard on use. |
| **T3** | Slightly wider wave + modest scaling. | Better mana efficiency or improved duration. |
| **T4 (Choice)** | **A) Piercing Crescent:** waves gain Pierce +1. **B) Returning Wave:** waves return after max range. | **A)** Interrupt lasts slightly longer **B)** Hitting 2+ enemies grants Guard vs next Magic hit. |
| **T5** | Modest fire-rate increase (still slower than dagger). | Better mana efficiency. |

### Axe
| Tier | Auto Attack bullet upgrade | Special (Ground Slam) upgrade |
|---|---|---|
| **T1** | **Spin Shockwave:** a short-range, 360° circle travels out from the player as they spin. Rate: **1.4s**. Range: **3**. Damage Type: **Physical**. Projectile Speed: **2** Contact Damage: **1**. Contact Damage Rate: **2.5s**. | **Ground Slam:** AoE knockback; **inner radius Staggers** (brief stun). |
| **T2 (Choice)** | **A) Boomerang Hatchet:** every 4th auto becomes returning shockwave/hatchet. **B) Split Cleaver:** autos occasionally fire two lanes. | Slam leaves fissure line **or** stronger knockback. |
| **T3** | Faster travel + better reliability. | Slightly larger inner radius or better mana efficiency. |
| **T4 (Choice)** | **A) Explosive Impact:** shockwaves create small impact detonation. **B) Crushing Wave:** shockwaves become thicker “bigger bullets”. | **A)** Aftershocks (2 pulses) **B)** Stagger affects slightly larger inner radius (careful tuning). |
| **T5** | Modest scaling (axe stays slower). | Better mana efficiency. |

## 11.2 Helmets (Utility actives)
Helmets provide the **Utility** button. Mana/CD numbers are placeholders.

| Helmet | Utility Ability (Mana / CD) | Tags | What it does |
|---|---|---|---|
| **Wizard Hat** | **Conjure Elemental** (E+CD) | `Summon`, `Utility` + element tag | Summon an elemental companion that attacks automatically and slightly prioritizes your cursor target. Necromancy can convert it into an **Undead Wight** (tag/behavior swap). |
| **Barrier Halo** | **Holy Barrier** (E+CD) | `Defensive`, `Magic` | Drop a small dome that reduces **Magic** damage and slows projectiles passing through. *(Not Ward stacks.)* **30s** cooldown. **55** mana. |
| **Ward Crown** | **Ward Crown** (E+CD) | `Ward`, `Defensive`, `Magic` | Instantly generate 10 ward stacks. **15s** cooldown. **25** mana.  |
| **Mirror Helm** | **Mirror Window** (E+CD) | `Reflect`, `Defensive`, `Magic` | Brief reflect window that reflects a limited number of incoming projectiles. |
| **Aegis Half-Dome Helm** | **Projectile Bulwark** (E+CD) | `Reflect`, `Defensive`, `Projectile` | Create a 180° half-bubble shield in front of you for a short duration. **Reflects all incoming projectiles** that hit the shield back toward enemies. |
| **Decoy Mask** | **Throw Decoy** (E+CD) | `Utility`, `Taunt` | Toss a decoy that pulls aggro and draws bullets for a few seconds. |
| **Hunter Visor** | **Mark Target** (E+CD) | `Mark`, `Utility` | Mark the nearest/cursor target: you deal more damage to it; projectiles bias toward it. |
| **Medic Hood** | **Field Patch** (E+CD) | `Healing`, `Utility` | Instant small heal plus short HoT (or boosts your next bandage). Supports bandage builds without replacing them. |
| **Smoke Cowl** | **Vanish** (E+CD) | `Stealth`, `Utility` | Enter stealth instantly and gain brief **Magic** damage reduction to slip through bullet pressure. |
| **Grave Helm** | **Corpse Spark** (E+CD) | `Necro`, `Utility` | Consume/detonate nearby corpses for damage or raise 1 temporary skeleton (uses corpses). |
| **Frost Diadem** | **Root** (E**+12s**) | `Frost`, `Utility`, `Rooted` | Roots enemies in place for **2 seconds**. **12 second cooldown.** |
| **Ember Circlet** | **Fireburst** (E+CD) | `Fire`, `AoE` | Small AoE detonation (at cursor or around you) that applies Burn. |

## 11.3 Boots (Mobility actives)
Boots provide the **Mobility** button. Mana/CD numbers are placeholders.

| Boots | Mobility Ability (Mana / CD) | Tags | What it does | Tier 3 perk | Tier 5 perk |
|---|---|---|---|---|---|
| **Phasewalker Boots** | **Blink** (E+CD) | `Blink`, `Utility` | Teleport a short distance in **movement direction**. Brief i-frames. | Leaves a brief afterimage that draws fire (mini-decoy). | Blink partially resets on elite hit (ICD). |
| **Shadowstep Greaves** | **Vanish Step** (E+CD) | `Dash`, `Stealth`, `Utility` | Short dash that grants instant **Stealth** briefly (breaks on attack). | Exiting stealth grants a small Ambush bonus. | Using Weapon Special while stealthed extends stealth briefly (ICD). |
| **Frosttrail Striders** | **Skate Dash** (E+CD) | `Dash`, `Frost`, `Chilled` | Dash leaves an ice trail that chills enemies crossing it. | Trail lasts longer and stacks Chill faster. | Trail causes a mini-freeze pulse when a target reaches max `Chilled` (ICD). |
| **Emberstride Boots** | **Cinder Leap** (E+CD) | `Leap`, `Fire`, `AoE` | Leap forward; landing creates a small fire burst that applies Burn. | Landing burst radius increased. | Landing on burning enemies triggers a small Ignition explosion (ICD). |
| **Wardrunner Boots** | **Phase Slip** (E+CD) | `Dash`, `Ward`, `Defensive`, `Magic` | Dash grants brief **Magic** damage reduction. | Grants **3 Ward stacks** after dash (if you have not taken damage recently). | Dash leaves a short Ward Field trail that slows projectiles. |
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
- **Overdrive Gloves:** big fire-rate increase for **20s**.
- **Frostweave Gloves:** your hits apply extra Chill and you gain brief damage reduction for **12s**.
- **Ritual Gloves:** your DoTs tick faster for **15s**.

### Sample Gloves (T1–T5)
Proc rules (applies to all gloves):
- Proc is **chance on Auto hit**.
- Proc has an **internal cooldown (ICD)**.
- Proc costs **Mana** to trigger.
- If you do not have enough Mana when it would trigger, the proc **fails** and still consumes the ICD.

#### Overdrive Gloves
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Overdrive:** 10% on-hit → **+60% Fire Rate** for **12s**. Cost 30 Mana. ICD **30s**. |
| **T2 (Choice)** | **A) Stable Overdrive:** lower bonus, **ICD -8s**. **B) Redline:** higher bonus (+95%), **Mana cost +15**. |
| **T3** | Stronger numbers (e.g., +75% Fire Rate, **14s**). |
| **T4 (Choice)** | **A) Kill Switch:** during Overdrive, Weapon Special costs less Mana (or refunds on first use) (ICD). **B) Sustained Heat:** extend duration on kill (cap). |
| **T5** | Adds projectile feel boost (e.g., +proj speed) and/or duration scaling. |

#### Frostweave Gloves
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Frostweave:** 10% on-hit → **12s** window: **+Chill application** and **reduced Projectile damage taken**. Cost 30 Mana. ICD **30s**. |
| **T2 (Choice)** | **A) Deep Chill:** extra Chill stacks. **B) Crystal Guard:** stronger projectile DR + brief slow immunity. |
| **T3** | Stronger projectile DR and chill application. |
| **T4 (Choice)** | **A) Shiver Nova:** on proc, emit small Chill nova. **B) Ice Thread:** while active, Weapon Specials have increased chance to Freeze (ICD). |
| **T5** | Slightly longer window and/or stronger DR; improve freeze reliability slightly. |

#### Venomcrafters
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Toxic Surge:** 10% on-hit → **15s** window: your hits apply **extra Poison stacks**. Cost 25 Mana. ICD **30s**. |
| **T2 (Choice)** | **A) Caustic Spread:** higher Poison spread chance. **B) Viscous Slow:** stronger slow while Toxic Surge is active. |
| **T3** | Stronger Poison output (stacks/tick rate) during window. |
| **T4 (Choice)** | **A) Toxic Bloom:** poisoned kills create poison cloud (ICD). **B) Antivenom Loop:** heal from Poison ticks (cap). |
| **T5** | Longer window and higher Poison damage. |

#### Gravepulse Grips
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Gravepulse:** 10% on-hit → **12s** window: apply **extra Diseased**; higher corpse drop chance. Cost 35 Mana. ICD **35s**. |
| **T2 (Choice)** | **A) Corpse Magnet:** corpses pull toward you (QoL) during window. **B) Blight Power:** Diseased ticks harder during window. |
| **T3** | Stronger Diseased stacks and longer duration. |
| **T4 (Choice)** | **A) Harvest Ready:** DoT cashout (e.g., Ritual Harvest) deals bonus damage during window. **B) Wight Frenzy:** summons gain big atk speed during window. |
| **T5** | Also boosts minions slightly (atk speed/HP) and/or Diseased spread chance. |

#### Duelist Wraps
| Tier | Proc / Upgrade |
|---|---|
| **T1** | **Adrenal Spike:** 10% on-hit → **12s** window: **+Physical damage** and **+move speed**. Cost 30 Mana. ICD **30s**. |
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
| **Aegis Robe** (Pure Mage) | **Arcane Ward:** after **3s** without taking damage, gain **Ward stacks (max 3)**. |
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
- Toolbelt **starts with 3 slots**.
- **All consumables, including Food, occupy Toolbelt slots.**
- There is a way (TBD) to increase Toolbelt slot count later.
- Loadout selections **cannot be changed later** (for the run / instance).
- Consumables are **cooldown-based** (not mana-based), simple and readable.
- **Consumables are tiered (T1–T5)**, but they are chosen in your loadout **before** the run.
- **Consumables do not upgrade during the run**.

### Food
- Food is a consumable that provides a **5-minute buff**.
- Food upgrades are TBD.
- Food buff variants include increased XP gain, HP regen, Mana regen, and Gold loot.

## 12.2 Initial items
| Item | Effect | Notes / Tags |
|---|---|---|
| **Health Potion** | Instant heal (or fast heal-over-time). | `Consumable`, `Healing` |
| **Mana Potion** | Restore Mana instantly (or regen burst). | `Consumable`, `Mana` |
| **Bandages** | Healing item that scales strongly with the **Healing** skill. | `Consumable`, `Bandage`, `Healing` |
| **Smoke Bomb** | Instant **Stealth** + brief reposition window (optional small slow cloud). | `Consumable`, `Stealth`, `Utility` |
| **Food** | Grants a **5-minute buff** (variant by food type; e.g., XP gain, HP regen, Mana regen, Gold loot). | `Consumable`, `Food`, `Buff` |
| **Scroll: Mana Regen** | Grants **Mana Regen** to the whole party for a short duration. | `Consumable`, `Scroll`, `Mana`, scales with **Inscription** |
| **Scroll: Health Regen** | Grants **HP Regen** to the whole party for a short duration. | `Consumable`, `Scroll`, `Healing`, scales with **Inscription** |
| **Scroll: Max Health** | Increases **Max HP** for the whole party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |
| **Scroll: Magical Damage** | Increases **Magical damage** (all non-Physical types) dealt by the party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |
| **Scroll: Physical Damage** | Increases **Physical damage** dealt by the party for a duration. | `Consumable`, `Scroll`, `Buff`, scales with **Inscription** |
| **Totem: Frost** | Pulses **Frost damage** and has a chance to apply **chilled**. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |
| **Totem: Healing** | Heals nearby allies every pulse. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |
| **Totem: Fire** | Pulses **Fire damage** projectiles. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |
| **Totem: Shock** | Pulses **Shock damage** projectiles. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |
| **Totem: Ward** | Drops a totem that applies **ward**. | `Consumable`, `Totem`, `Buff`, scales with **Shamanism** |

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
