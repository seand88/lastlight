# ENEMIES_SPEC.md (v1.0)

This document defines the data-driven schema for enemies in **LastLight**. It bridges the gap between raw entity stats and the **Ability System**.

## 1. Core Enemy Properties

Enemies are defined in `Enemies.json`. Each entry represents a template for a specific enemy type.

| Property | Type | Description |
| :--- | :--- | :--- |
| `id` | string | Unique internal identifier (e.g., `enemy_goblin`). |
| `name` | string | User-facing display name. |
| `max_health` | int | Starting health for the enemy. |
| `base_damage` | int | The raw power of the enemy used for scaling abilities. |
| `attack_speed_bonus` | float | Percentage mod for fire rate (e.g., `0.1` = +10% speed). |
| `range_bonus` | float | Flat tile addition to projectile/beam travel distance. |
| `speed` | float | Movement speed in pixels per second. |
| `primary_ability_id` | string | The ID of the default attack (links to `abilities.json`). |
| `special_ability_id` | string | The ID of the elite/signature attack (links to `abilities.json`). |
| `ai_type` | enum | Behavior pattern (see **Section 2.3**). |
| `atlas` | string | The texture atlas to use for rendering. |
| `icon` | string | The specific sprite key within the atlas. |

---

## 2. Technical Logic Rules

### 2.0 Combat Formulas
Enemies use their base stats to scale the abilities they fire.

```
# Final Damage Formula
Final Damage: EntityBaseDamage * AbilityMultiplier

# Final Fire Rate Formula
# Note: Fire Rate is shots per second.
Final Fire Rate: AbilityBaseFireRate * (1 + EntityAttackSpeedBonus)

# Range
Final Range: AbilityBaseRange + EntityRangeBonus
```

### 2.1 Relative Targeting
The `target_type` in `abilities.json` is processed **relative to the source**:
*   If a **Monster** uses an ability with `target_type: "enemies"`, it will hit **Players**.
*   If a **Monster** uses an ability with `target_type: "caster"`, it will hit **Itself**.

### 2.2 The "No-Mana" Policy
Monsters do **not** have a mana pool. 
*   **Rule:** Any ability assigned to a monster will interpret its mana requirement as `mana_cost: 0` at runtime.
*   **Elite Balancing:** Frequency of special attacks is controlled purely by the `cooldown` property in `abilities.json`.

### 2.3 AI Attack Priority & Behavior
1.  **Special:** The AI attempts to use the `special_ability_id` as soon as its cooldown expires.
2.  **Primary:** If the special is on cooldown, the AI uses the `primary_ability_id` at its calculated `Final Fire Rate`.

**AI Types:**
| Type | Behavior Description |
| :--- | :--- |
| `chase` | Moves directly toward the nearest player until in range of its abilities. |
| `ranged_kite` | Attempts to maintain a distance of 5-8 tiles from the player while shooting. |
| `stationary` | Does not move. Rotates to face and shoot the nearest player. |

---

## 3. Sample Enemy JSON Definition

This example defines a standard Goblin that uses a basic projectile attack.

```json
{
  "id": "enemy_goblin",
  "name": "Goblin Grunt",
  "max_health": 100,
  "base_damage": 10,
  "attack_speed_bonus": 0.0,
  "range_bonus": 0.0,
  "speed": 80.0,
  "primary_ability_id": "enemy_basic_shot",
  "special_ability_id": "enemy_radial_burst",
  "ai_type": "chase",
  "atlas": "Items",
  "icon": "usable_bomb"
}
```

---

## 4. Current Configuration Status (Audit)

| Property | Status | Implementation Requirement |
| :--- | :--- | :--- |
| **MaxHealth** | **Implemented** | Already synced via `EnemySpawn` / `EnemyUpdate`. |
| **Speed** | **Implemented** | Used in `ServerEnemy.Update` for movement. |
| **BaseDamage** | **Pending** | Must add to `EnemyData`, `IEntity`, and `EffectProcessor`. |
| **AttackSpeedBonus**| **Pending** | Must update `ServerEnemy.Update` fire-rate logic. |
| **RangeBonus** | **Pending** | Must update `ServerBulletManager` or bullet spawn logic. |
| **PrimaryAbility** | **In Progress** | IDs exist, but logic doesn't use `BaseDamage` scaling. |
| **SpecialAbility**| **In Progress** | IDs exist, but logic doesn't use `BaseDamage` scaling. |
| **AI Logic** | **In Progress** | `chase` is implemented; others are hardcoded. |
