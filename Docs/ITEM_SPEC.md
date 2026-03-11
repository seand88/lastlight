# ITEM SPECIFICATION

## 1. Overview
LastLight features a class-less itemization system where player archetypes emerge from equipment loadouts. Equipment provides the "Verbs" (active abilities), while Skills provide the "Grammar" (mutations). 

The core progression loop involves finding equipment with inherent growth limits (Max Tier), upgrading them during a dungeon run using gold dropped from monsters, and managing a limited "Toolbelt" of consumables. Equipment is ephemeral and can be lost on death, contrasting with permanent Skill progression.

## 2. Design Goals & Functional Requirements

### 2.1 The Max Tier System
Every piece of equipment is dropped with an inherent Max Tier (1 to 5). It can be upgraded using gold at a vendor up to, but never exceeding, its specific Max Tier. For example, an item found with a Max Tier of 3 can only be upgraded to Tier 3.

### 2.2 Equipment Rules
- Each piece has a Max Tier rank (1-5).
- Upgraded with gold looted during a run; perks reset each run.
- Weapons provide two abilities (Tiers 2 and 4).
- Non-weapons (Helmet, Body Armor, Gloves, Boots) provide a single ability (Active, Passive, or Proc).
- Non-weapon abilities are unlocked at T2, with a perk upgrade at T4.
- All non-weapons provide damage reduction (formula TBD).

### 2.3 Weapons (Generator + Special)
Weapons grant two primary attack abilities:
- **Generator:** Fires projectiles, no cooldown, channeled continuously, generates `Mana` on hit.
- **Special:** Mana-spender, typically no cooldown.

#### Iron Bow Table
| Tier | Cost | Stats | Perk |
|---|---|---|---|
| I | — | Dmg: **10**<br>Range Bonus: -<br />Rate Mod: **+10%** | Unlocks **Quick Shot:** Fires a single, fast arrow projectile.<br />\* Dmg Multiplier: **100%** base weapon (`Physical`)<br />\* Projectile Speed: **10**<br />\* Mana Generated: **+1** |
| II | 120g | Dmg: **17**<br>Range Bonus: **+1**<br />Rate Mod: **+10%** | **A) Heavy Arrow Cadence:** Every 4th shot becomes a large arrow, gaining +size, +damage, and Pierce 1.<br />**B) Twin Lane:** Every 3rd shot fires 2 parallel arrows. |
| III | 350g | Dmg: **25**<br>Range Bonus: -<br />Rate Mod: **+10%** | Unlocks **Volley:** Fires 5 arrows in a 90° cone.<br />\* Damage: **5 per arrow** (Physical)<br />\* Mana Cost: **20**<br />\* Projectile Speed: **12**<br />\* Mana Generated: **+1 per arrow hit** |
| IV | 900g | Dmg: **36**<br>Range Bonus: **+1**<br />Rate Mod: **+10%** | **A) Piercing Volley:** +1 extra arrow. Arrows pierce 1 target (Physical).<br />**B) Ember Volley:** +1 extra arrow. Deals 3 Fire splash damage (Range 1) on hit.<br />**C) Frost Volley:** Targets gain 1 stack of Chilled. 50% chance to Slow. (Range -1, Mana Cost +10, Frost). |
| V | 2,000g | Dmg: -<br>Range Bonus: -<br />Rate Mod: - | **Mastery 1:** +8% damage (500g).<br />**Mastery 2:** +8% damage (500g).<br />**Mastery 3:** +8% damage (1000g).<br />**Total Mastery Bonus:** +24% damage. |

```bash
{
  DEFINE BOW HERE
}
```

### 2.4 Helmets (Utility Actives)
Helmets provide the **Utility** button.

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

### 2.5 Boots (Mobility Actives)
Boots provide the **Mobility** button.

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

### 2.6 Gloves (Proc-Only Slot)
Gloves have **no active button**.
- **Proc model:** chance on Auto Attack hit → applies powerful temporary buff.
- **Proc gating:** internal cooldown (ICD) + Mana cost. If Mana is insufficient, proc fails and consumes ICD.

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

### 2.7 Body Armor (Passive)
Body armor is **passive-only** (no button).

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

### 2.8 Consumables (Toolbelt)
- **Toolbelt:** Starts with 3 slots.
- **Loadout Lock:** Selections cannot be changed during a run.
- **Cooldown-based:** Consumables use cooldowns, not mana.
- **Tiered (T1-T5):** Chosen before the run; do not upgrade during the run.

#### Bandage Table
| Bandage Tier | Heal | Delay |
|---|---:|---:|
| **T1** | 15 | 8s |
| **T2** | 20 | 8s |
| **T3** | 25 | 7s |
| **T4** | 30 | 7s |
| **T5** | 40 | 6s |

#### Initial Items Table
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

## 3. Data Specification

### 3.1 Items.json Schema
Items store scaling data, unlocked abilities, and tier-based progression. Weapon scaling is decoupled from ability patterns.

**Example Weapon Definition:**
```json
{
  "id": "weapon_iron_bow",
  "name": "Iron Bow",
  "category": "Weapon",
  "atlas": "Items",
  "icon": "iron_bow",
  "tiers": [
    {
      "tier": 1,
      "base_damage": 10,
      "attack_speed_mod": 0.10,
      "range_bonus": 0,
      "unlocked_abilities": ["iron_bow_quick_shot"],
      "icon": "fill_me_in"
    },
    {
      "tier": 2,
      "base_damage": 17,
      "attack_speed_mod": 0.10,
      "range_bonus": 1,
      "perk_options": [
        { "id": "bow_heavy", "name": "Heavy Arrow Cadence", "desc": "Every 4th shot becomes a large arrow, gaining +size, +damage, and Pierce 1." },
        { "id": "bow_twin", "name": "Twin Lane", "desc": "Every 3rd shot fires 2 parallel arrows." }
      ],
      "icon": "fill_me_in"
    }
  ]
}
```

### 3.2 Combat Scaling Formulas
```
# Damage Formula
Final Damage: WeaponBaseDamage * (1 + SumSkillBonuses%) * AbilityMultiplier

# Projectile Firing Rate
Firing Interval: AbilityBaseInterval / (1 + WeaponAttackSpeedMod + SkillSpeedBonus%)

# Range
Final Range: AbilityBaseRange + WeaponRangeBonus
```

## 4. Technical Implementation

### 4.1 Data Structures
- **`ItemData`**: Static blueprint defining the base item stats and tier rules (loaded from `Items.json`).
- **`ItemInfo`**: Instance data representing a specific item in a player's inventory, including its current `Tier` and selected `Perks`.

### 4.2 Class Responsibilities
- **`ServerItemManager`**: Handles item spawning, pickup validation, and tier upgrades.
- **`ServerPlayer`**: Manages the equipment slots (Weapon, Helmet, Body, Gloves, Boots) and inventory array.
- **Persistence**: Items are serialized into the `Players.Data` column in SQLite. Note: Persistence currently repeats `ItemData` unnecessarily and should be optimized to store only `ItemInfo` refs.

### 4.3 Inventory Sync
- Sent via **`InventoryUpdate` (Private)** packet.
- Includes `SlotIndex` and the `ItemInfo` object.
- Ground items are managed via **`ItemSpawn`** and **`ItemPickup`** broadcast packets.
