# Item Tables

## 11 Equipment

> Global Equipment Rule: **The Max Tier System**
Every piece of equipment found in LastLight is dropped with an inherent Max Tier (ranging from 1 to 5). This value represents the item's ultimate growth limit; it can be upgraded using gold at a vendor up to, but never exceeding, its specific Max Tier. For example, an item found with a Max Tier of 3 can only be upgraded to Tier 3, regardless of the player's wealth or progress.

Key rules about equipment:
- each piece comes with a max tier rank (1 - 5)
- has to be leveled up with gold looted during dungeon run
- this process of leveling is repeated each dungeon run as weapon perks reset each run
- weapons provide two abilities (at tiers 2 and 4)
- all other equipment (helmet, body armor, gloves, and boots) provide a single ability that is either active (click button), passive (continuously on), or proc (chance to trigger)
- abilities for non-weapons are unlocked at T2, there is a perk upgrade at T4, the rest of the tiers are stat upgrades
- all non-weapons provide damage reduction (formula TBD)

### 11.1 Weapons (Generator + Special)

Weapons grant your two primary attack abilities.

Unique Scaling: There is no universal power curve. Each weapon type (Axe, Dagger, Bow) scales its Base Damage, Attack Speed, and Range differently across Tiers 1-4 to maintain a distinct "feel."

**Abilities**: Every weapon defines **two abilities**:
- The **Generator* ability which fires projectiles, has no cooldown, and can be channeled coninuously. It generates `Mana` every hit.
- The **Special** ability is the spender in this generator/spender cycle. 

**Base Damage**: Primary and special abilities scale from these modifiers which get improved with each Tier unlock:

1. Base Damage (P): The raw power multiplier for all ability effects.
2. Attack Speed Mod (%): Modifies the fire-rate/interval of the character.
3. Range Bonus (Tiles): A flat addition/subtraction to the distance projectiles or beams travel.

Those mods are used in these combat formulas:
```
# Damage Formula
Final Damage: WeaponBaseDamage * (1 + SumSkillBonuses%) * AbilityMultiplier

# Projectile Firing Rate
Firing Interval: AbilityBaseInterval / (1 + WeaponAttackSpeedMod + SkillSpeedBonus%)

# Range
Final Range: AbilityBaseRange + WeaponRangeBonus
```

**Perks**:
- Tier 1: Unlocks the weapon's Generator (Basic Attack).
- Tier 2: Stat growth + Choice of two Generator Perks (Behavior modifiers).
- Tier 3: Stat growth + Unlocks Weapon Special (Mana-spending ability).
- Tier 4: Stat growth + Choice of three Special Perks (Elemental/Utility upgrades).
- Tier 5: Mastery. Raw stat growth ends. Mastery grants three incremental +8% damage boosts (Total +24%) via gold rank-ups.

```bash
{
  DEFINE BOW HERE
}

```

#### 11.1.1 Iron Bow

| Tier | Cost | Stats | Perk |
|---|---|---|---|
| I | — | Dmg: **10**<br>Range Bonus: -<br />Rate Mod: **+10%** | Unlocks **Quick Shot:** Fires a single, fast arrow projectile.<br />\* Dmg Multiplier: **100%** base weapon (`Physical`)<br />\* Projectile Speed: **10**<br />\* Mana Generated: **+1** |
| II | 120g | Dmg: **17**<br>Range Bonus: **+1**<br />Rate Mod: **+10%** | **A) Heavy Arrow Cadence:** Every 4th shot becomes a large arrow, gaining +size, +damage, and Pierce 1.<br />**B) Twin Lane:** Every 3rd shot fires 2 parallel arrows. |
| III | 350g | Dmg: **25**<br>Range Bonus: -<br />Rate Mod: **+10%** | Unlocks **Volley:** Fires 5 arrows in a 90° cone.<br />\* Damage: **5 per arrow** (Physical)<br />\* Mana Cost: **20**<br />\* Projectile Speed: **12**<br />\* Mana Generated: **+1 per arrow hit** |
| IV | 900g | Dmg: **36**<br>Range Bonus: **+1**<br />Rate Mod: **+10%** | **A) Piercing Volley:** +1 extra arrow. Arrows pierce 1 target (Physical).<br />**B) Ember Volley:** +1 extra arrow. Deals 3 Fire splash damage (Range 1) on hit.<br />**C) Frost Volley:** Targets gain 1 stack of Chilled. 50% chance to Slow. (Range -1, Mana Cost +10, Frost). |
| V | 2,000g | Dmg: -<br>Range Bonus: -<br />Rate Mod: - | **Mastery 1:** +8% damage (500g).<br />**Mastery 2:** +8% damage (500g).<br />**Mastery 3:** +8% damage (1000g).<br />**Total Mastery Bonus:** +24% damage. |


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

## 11.5 Body Armor (passive)
Body armor is **passive-only** (no button). Identity comes from **always-on effects**.

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

# 12. Consumables (Toolbelt)
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


### Bandage
**Tags:** `Consumable`, `Bandage`, `Healing`, `Delayed`

- **Bandage is the consumable required for the Healing skill.**
- No Bandage = no Healing skill benefit.
- You do **not** need the Healing skill to use a Bandage.
- Bandage item tier defines the baseline heal and delay.
- Healing skill bonuses apply **additively** to the equipped Bandage item:
  - heal bonuses add to item heal
  - time reductions subtract from item delay

| Bandage Tier | Heal | Delay |
|---|---:|---:|
| **T1** | 15 | 8s |
| **T2** | 20 | 8s |
| **T3** | 25 | 7s |
| **T4** | 30 | 7s |
| **T5** | 40 | 6s |

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

## 13. Current Configuration Status (Audit)

| Property | Status | Implementation Requirement |
| :--- | :--- | :--- |
| **ItemData / ItemInfo** | **Implemented** | Split between static blueprints (`GameData.cs`) and instance refs (`Models.cs`). |
| **Persistence** | **Not Workign As Intended** | JSON serialization into `Players.Data` column in SQLite should probably ignore certain properties in `Iteminfo`, right now it is repeating all of `ItemData` which is wasteful. |
| **Inventory Slots** | **Implemented** | 8 Inventory slots, 3 Equipment slots (defined in `ServerPlayer.cs` and `Models.cs`). |
| **Toolbelt Slots** | **Pending** | Appears in DB exports but missing from `ServerPlayer` / `JoinResponse` / `Models.cs` classes. |
| **Max Tier System** | **Pending** | Growth limits (1-5) and Gold upgrade logic not yet implemented in `ServerRoom`. |
| **Weapon Abilities** | **In Progress** | `Generator` / `Special` slots exist, but tier-based unlocking is not enforced. |
| **Combat Scaling** | **Pending** | `Final Damage` and `Final Range` formulas from spec not yet in `ServerBulletManager`. |
| **Consumables** | **Pending** | Initial items exist in JSON, but cooldown/buff logic is not active on server. |
| **Stacking** | **Pending** | Some items should stack, like potions and bandages. This should happen in `ItemInfo`. |
| **Dungeon Inventory** | **Pending** | Inventory should actually only exist for loot in a dungeon. After dungoen all items you unlock go to your stash. There is not a traditional inventory. The Dungeon Inventory will hold loot as you progress. Loot comes in the form of a Loot Chest that can be of Tier 1 - 5. Tier 5 is the best. Loot Chests will spawn on the ground during a dungeon run. You can loot them an unlock them at the end. Note that picking up a loot chest will warn player about a debuff that comes with it. Higher tier chests have higer debuffs. You can't just leave them on the groud because they despawn quickly. So... We can repurpose the Player Inventory for this. |
| **Stash** | **Pending** | This is global storage, like a bank or stash in diablo. Can be accessed in the waiting room. All your good stuff goes here. |