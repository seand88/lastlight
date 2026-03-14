# GAME LOOP SPECIFICATION

## 1. Overview
Last updated: 2026-03-06 (America/Los_Angeles). This document is the **single source of truth** for the current design of LastLight: a top-down 2D bullet-hell action RPG. It covers high-level concepts, loadout rules, system responsibilities, resources (XP, Mana, Gold, HP), status effects, and character customization systems (Equipment, Skills, Streaks, Toolbelt).

## 2. Design Goals & Functional Requirements

### 2.1 High-Concept & Pillars
- **Genre:** Top-down 2D **bullet-hell** action RPG.
- **Session loop:** **Instanced dungeons** lasting **5–25 minutes**.
- **Aiming:** **Mouse aim** (movement via WASD).
- **PvP:** **None**.
- **Loot:** Pick up gold/chests during the run, but **open rewards only at the end if you survive** (no in-run inventory).

**Design Pillar: Gear gives the buttons (verbs). Skills mutate the buttons (grammar).**
- All active abilities come from equipment slots.
- Skills never add new buttons; they only add modifiers, triggers, conversions, and scaling.
- **Proc City:** Abilities proc effects that are greatly improved by skills.

### 2.2 Game Loop
- Loadout lock: Selections cannot be changed during a run.
- Equipment and consumables are lost on death.
- Dungeon tiers: Easy, Medium, Hard, Dangerous, Impossible.
- Loot chests: Drop on ground; disappear quickly; require gold to unlock at end; accept risk/debuffs for higher tiers.
- In-run progression: Level up equipment using gold.

### 2.3 Character Customization Systems
1. **Equipment:** Ephemeral verbs. Upgraded during run with gold.
2. **Skills:** Permanent grammar. Purchased with XP. Cap on total points.
3. **Streaks:** Endgame perks earned after successful runs. Ephemeral; lost on death.
4. **Cosmetic Items:** Robes, capes, etc. (Source: TBD).
5. **Toolbelt:** Consumables (potions, bandages, totems). Effectiveness often tied to skills.

### 2.4 Abilities
Abilities are tied to equipment. All Equipment has upgrade perks at certain tiers. These are upgradeable during a dungeon run via gold. They reset after each run.

- **Weapons:** Own **Generator + Special**, baseline feel, and choice perks. *(2 active abilities)*
- **Helmets:** Own **Defensive** verbs (summon/ward/decoy/mark/heal). *(1 active ability)*
- **Boots:** Own **Mobility** verbs (dash/blink/phase/leap). *(1 active ability)*
- **Body Armor:** Owns passive build loops (stealth, wards, overcharge). *(1 passive ability)*
- **Gloves:** Proc-only slot supporting the build. *(1 proc ability)*

### 2.5 Resources

#### XP (Permanent)
| Tier | Point cost | 
|---|---|
| I | 1 |
| II | 1 | 
| III | 2 | 
| IV | 2 | 
| V | 3 |

**Skill Point XP Costs:**
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

#### Mana (Generator/Spender)
- Auto Attack generates Mana.
- Passive regen via buffs
- Consumed by Special, Utility, and Mobility abilities.

#### Gold (Run-Only)
- Earned during run (drops, chests).
- Spent on in-run equipment upgrades (T1-T5) and Mastery.
- Does not persist between runs.

### 2.6 Damage and Status Effects

#### Damage types (tags-only)
| Damage Type Tag | Signature Debuff Tags |
|---|---|
| `Physical` | `Bleed`, `Diseased` |
| `Poison` | `Poisoned` |
| `Frost` | `Chilled` |
| `Fire` | `Burning` |
| `Shock` | `Conduit` |

#### Negative Status Effects
- **Conduit:** 4s duration. Direct Shock jumps to 1 nearby enemy.
- **Poisoned (Poison, DoT):** 8s duration. Blocks HP healing/regen.
- **Diseased (Physical, DoT, Spread):** 12s duration. Stacks up to 5. Spreads on death.
- **Bleeding (Physical, DoT, Vulnerability):** 10s duration. Stacks to 10. Rupture perk at 10 stacks triggers total sum dmg.
- **Burning (Fire, Delayed, AoE):** 3s duration. Detonates AoE Fire dmg on death. Treated as DoT with Fire Magic T5.
- **Slow:** 2s duration. -25% move/attack/projectile speed.
- **Hexed:** 6s duration. +10% Magic damage taken.
- **Chilled:** 3s duration. +2% Frost damage taken per stack (max 5). 50% dmg bonus if target is Stunned.
- **Stunned:** 1-3s duration. Cannot perform any action.
- **Rooted:** Cannot move, but can use abilities.
- **Disarm:** 1-3s duration. Cannot deal Contact or Weapon ability damage.
- **Silence:** 1-3s duration. Cannot use Utility abilities (boots, helmet, armor).

#### Positive Status Effects
- **HoT:** Heal every 1s. Variable duration (2-8s). Does not stack.
- **Wards:** Absorb Magical damage (non-Physical). Stacks to 20.
- **Stealth:** 5-20s duration. 90% damage reduction. Breaks on action. Enables Ambush bonus.

### 2.7 Streaks (Endgame)
Must spend 3 points in each tier to advance.

| Tier | Cost | Option 1 | Option 2 | Option 3 |
|---|---|---|---|---|
| I | 1000/3,500/10,000 | Your healing received from alls ources is increased by 5/10/15% | Dodging or Warding a projectile has a 5/10/15% chance to reflect it back to the attacker | 5/10/15% chance every second to generate a ward that absorbs 10 damage; stacks witgh other Ward sources | 
| II | 25,000/40,000/50,000 | Reduces the cooldown of helmet ability by 10/20/30% | Reduces the cooldown of boot ability by 10/20/30% | Reduces the cooldown of glove proc by 10/20/30% |
| III | TODO| TODO | TODO | TODO |
| IV | TODO | TODO | TODO | TODO |
| V | TODO| TODO | TODO | TODO |


## 3. Data Specification

### 3.1 Tag Dimensions
The system relies on consistent tagging for skill hooks.
- **Weapon tags:** `Bow`, `Crossbow`, `Dagger`, `Ritual Dagger`, `Spellbook`, `Staff`, `Spellblade`, `Sword`, `Axe`
- **Delivery tags:** `Projectile`, `Beam`, `AoE`, `Contact`
- **Mobility tags:** `Dash`, `Blink`, `Leap`, `Phase`
- **Summon:** `Summon`
- **Sigil/Trap:** `Sigil`, `Trap` (behavior tags, not delivery)
- **Behavior tags:** `DoT`, `Pierce`, `Homing`, `Channel`, `Burst`, `Slow`, `Stealth`, `Ward`, `Ambush`, `Focus`, `Mark`, `Dodge`, `Stagger`, `Interrupt`, `Conduit`, `Bleeding`, `Spread`, `Affliction`, `Burning`, `Poisoned`, `Chilled`, `Stunned`, `Hexed`, `Diseased`, `Disarm`, `Silence`, `Consume`, `Weapon`, `Utility`, `Delayed`, `HoT`
- **Damage tags:** `Physical`, `Poison`, `Frost`, `Fire`, `Shock`
- **Category tags:** `Physical` (Physical damage), `Magic` (all Non-Physical damage), `Elemental` (Frost, Fire and Shock damage)

### 3.2 Ability Implementation Model
Each ability is assembled from:
- **Delivery** (projectile/beam/etc.)
- **Tags**
- **Knobs** (mana cost, cooldown, duration, radius, tick rate, pierce, homing, etc.)

## 4. Technical Implementation

### 4.1 Class Responsibilities
- **`ServerRoom`:** Manages the parallel execution of physics, AI, and collision for each instance.
- **`StatusManager`:** Tracks active duration and tick intervals for all status effects on entities.
- **`EffectProcessor`:** Authoritatively calculates and applies the result of impacts (damage, healing, status application).

### 4.2 Implementation Notes
- Keep tag lists small (2–6 core tags per ability).
- Skills should hook via tags.
- Summons must reach “primary damage” viability.
- Optional balance valve: top 2 elemental skills apply at full strength, others reduced (future consideration).

### 4.3 Crafting (TODO)
- buy T1 and T2 from vendors; T3+ player crafted.
- resource crates in dungeons instead of just loot chests.
- **Cooking:** food buffs/crafting.
- **Alchemy:** potion buffs/crafting.
- **Inscription:** scroll buffs/crafting.
- **Shamanism:** totem buffs/crafting.
