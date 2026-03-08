# Equipment Tables (Abilities + Tiers)

## 11.1 Weapons (by type)
Weapon identity is fixed per weapon type. Bullet behaviors are primarily expressed via tier progression.

### Weapon identity and bullet behavior (design notes)
- Weapons have **common patterns** and **unique behaviors** per type.
- Tier 1 unlocks generator ability which is projectile form and contains range, rate, damage type, bullet speed, contact damage (collision with monsters), contact damage rate. Generators generate mana.
- Tier 2 unlocks an enhancement to the generator. 2 choices.
- Tier 3 adds damage to generator and unlocks Weapon Special ability. Weapon specials cost mana, have no cooldown, and add thematic fun.
- Tier 4 is a choice between 3 perks for an upgrade to the weapon special. Typically this can involve changing the damage type.
- Tier 5 is mastery. This is something that can be upgraded 3 times with static +x% bonuses. The idea is not to make T5 (rare) weapons mandatory. All the good stuff is already unlocked if you're using a T5 weapon. This jsut gives you some extra swagger, but meaningful swagger. More on this later.

### NEW USE THIS:

| Weapon | T1 Generator | T2 Generator Choice | T3 Unlock / Flat Damage | T4 Special Choice | T5 Mastery |
|---|---|---|---|---|---|
| **Bow** | **Quick Shot** — Single, fast arrow projectile. **Rate:** 1.0s. **Range:** 12. **Damage Type:** Physical. **Damage:** 10. **Speed:** 10. **Contact Damage:** 1. **Contact Damage Rate:** 2.0s. **Mana generated per hit:** +1. | **A) Heavy Arrow Cadence** — Every 4th shot becomes a large arrow, gaining +size, +damage, and Pierce 1. **Gold Cost:** 120. **B) Twin Lane** — Every 3rd shot fires 2 parallel arrows. **Gold Cost:** 120. | Generator damage **+7**. **Quick Shot damage becomes 17.** Unlocks **Volley**. **Volley** — Fires 5 arrows in a 90° cone. **Range:** 8. **Damage Type:** Physical. **Damage:** 5 per arrow. **Mana Cost:** 20. **Projectile Speed:** 12. Multiple arrows can hit the same target. No cooldown. **Mana generated per hit:** +1 per arrow hit. | **A) Piercing Volley** — Damage Type: Physical. Fires +1 extra arrow. Arrows pierce 1 target. **Gold Cost:** 180. **B) Ember Volley** — Damage Type: Fire. Fires +1 extra arrow. On hit, deals 3 Fire splash damage to enemies within range 1 of the target hit. **Gold Cost:** 180. **C) Frost Volley** — Damage Type: Frost. Range -1. Targets hit gain 1 stack of Chilled. 50% chance to also apply Slow. Mana Cost +10. **Gold Cost:** 180. | **Mastery 1:** +8% damage. **Gold Cost:** 200. **Mastery 2:** +8% damage. **Gold Cost:** 300. **Mastery 3:** +8% damage. **Gold Cost:** 400. **Total Mastery Bonus:** +24% damage. |

### WORK AWAY FROM THIS:

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
