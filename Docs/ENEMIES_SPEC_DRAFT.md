# ENEMIES_SPEC.md (v2.0 - DRAFT)

This document defines the data-driven schema for enemies in **LastLight**. Logic is decoupled from the entity and managed by **Polymorphic AI Drivers**.

```bash
┌─────────────┬──────┬────────────┬────────────────────────────────────────────────────┐
│ Component   │ Type │ Analog     │ Definition                                         │
├─────────────┼──────┼────────────┼────────────────────────────────────────────────────┤
│ ServerEnemy │ C#   │ The Body   │ Stats (HP, BaseDamage) and World State (Position). │
│ AI Driver   │ C#   │ The Brain  │ The hardcoded logic rules (Strategy Pattern).      │
│ AI Config   │ JSON │ The Memory │ The specific settings/IDs for this unique monster. │
└─────────────┴──────┴────────────┴────────────────────────────────────────────────────┘
```

## 1. Core Entity Properties (Global Defaults)
These properties define the entity's base identity. If a Phase or Behavior does not provide an override, the engine falls back to these values.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | string | Unique internal identifier (e.g., `enemy_goblin`). |
| `Name` | string | User-facing display name. |
| `MaxHealth` | int | Starting health for the entity. |
| `BaseDamage` | int | **Default** power used for scaling abilities. |
| `AttackSpeedBonus`| float | **Default** percentage mod for fire rate (e.g., `0.1` = +10% speed). |
| `RangeBonus` | float | **Default** tile addition to projectile/beam travel distance. |
| `Atlas` | string | The texture atlas to use for rendering (e.g., `Items`). |
| `Icon` | string | The specific sprite key within the atlas (e.g., `usable_bomb`). |
| `AiDriver` | enum | Determines which **AI Driver** class to instantiate (`standard`, `phased`). |
| `AiConfig` | object | The polymorphic data payload for the selected driver. |

---

## 2. AI Drivers (The c# Brain)

The AI Driver is a specialized C# class that implements the IAiDriver interface. It is the implementation of how an entity moves and when it pulls the trigger.

* Responsibility: It contains the actual math and update loops for movement (chase, kite, stationary) and ability execution (on a timer, on damage, etc.).
* Decoupling: The Driver doesn't own the entity's health or position; instead, it "pilots" the entity. It receives a reference to the ServerEnemy and ServerPlayer list each frame.
* Example Drivers:
    * StandardAiDriver: Logic for basic "move and shoot" enemies.
    * PhasedAiDriver: Logic for bosses that monitors HP and swaps behavior lists.


### 2.1 Driver: `standard`
**C# Class:** `StandardAiDriver`  
**Description:** Default behavior for common mobs. Supports basic movement patterns and two ability slots.

**`AiConfig` Schema:**
| Property | Type | Description |
| :--- | :--- | :--- |
| `movement` | enum | `chase`, `kite`, `stationary`. |
| `speed` | float | Movement speed in pixels per second. |
| `primary` | string | Ability ID fired continuously on its `fire_rate`. |
| `special` | string | Ability ID fired as soon as its `cooldown` expires. |

**Example JSON:**
```json
{
  "movement": "chase",
  "speed": 80,
  "primary": "enemy_basic_shot",
  "special": "enemy_radial_burst"
}
```

---

### 2.2 Driver: `phased`
**C# Class:** `PhasedAiDriver`  
**Description:** Used for Bosses or Elite enemies. Transitions between different stats and abilities based on HP thresholds.

**`AiConfig` Schema:**
| Property | Type | Description |
| :--- | :--- | :--- |
| `phases` | array | A list of [Phase Objects](#phase-object-schema). |

**Phase Object Schema:**
| Property | Type | Description |
| :--- | :--- | :--- |
| `threshold` | float | HP % required to enter this phase (e.g., `0.5` = 50% HP). |
| `movement` | enum | Movement style (`chase`, `kite`, `stationary`, `spiral`). |
| `speed` | float | **Override.** Movement speed for this phase. |
| `base_damage` | int | **Override.** Base damage for this phase (e.g., for enrage). |
| `behaviors` | array | List of [Behavior Objects](#3-behavior-triggers) active in this phase. |

**Example JSON:**
```json
{
  "phases": [
    {
      "threshold": 1.0,
      "speed": 50,
      "movement": "stationary",
      "behaviors": [ { "trigger": "on_timer", "interval": 1.0, "action": "boss_shot" } ]
    },
    {
      "threshold": 0.5,
      "speed": 100,
      "movement": "chase",
      "base_damage": 40,
      "behaviors": [ 
        { "trigger": "on_timer", "interval": 0.5, "action": "boss_rapid" },
        { "trigger": "on_damaged", "action": "boss_counter" }
      ]
    }
  ]
}
```

---

## 3. Behavior Triggers
Behaviors define *when* an entity executes an `action` (Ability ID).

| Trigger | Parameters | Description |
| :--- | :--- | :--- |
| **`on_timer`** | `interval` (float) | Fires the action repeatedly every `X` seconds. |
| **`on_damaged`** | — | Fires the action whenever the entity takes damage. |
| **`on_hp_below`** | `hp` (float) | Fires once when HP % drops below the specified value. |
| **`on_event`** | `event` (string) | Fires on specific room/player events (e.g., `player_heal`). |

### 3.1 `on_timer` Example
```json
{
  "trigger": "on_timer",
  "interval": 1.5,
  "action": "enemy_basic_shot"
}
```

### 3.2 `on_damaged` Example
```json
{
  "trigger": "on_damaged",
  "action": "enemy_counter_nova"
}
```

### 3.3 `on_hp_below` Example
```json
{
  "trigger": "on_hp_below",
  "hp": 0.30,
  "action": "enemy_emergency_heal"
}
```

---

## 4. Complete Implementation Example: The "Angry Sentinel"
```json
{
  "Id": "enemy_sentinel",
  "Name": "Angry Sentinel",
  "MaxHealth": 500,
  "BaseDamage": 15,
  "AiDriver": "phased",
  "AiConfig": {
    "phases": [
      {
        "threshold": 1.0,
        "movement": "stationary",
        "behaviors": [
          { "trigger": "on_timer", "interval": 2.0, "action": "sentinel_snipe" },
          { "trigger": "on_damaged", "action": "sentinel_shield_pulse" }
        ]
      },
      {
        "threshold": 0.4,
        "speed": 120,
        "movement": "chase",
        "base_damage": 30,
        "behaviors": [
          { "trigger": "on_timer", "interval": 0.5, "action": "sentinel_rapid_fire" },
          { "trigger": "on_hp_below", "hp": 0.4, "action": "sentinel_enrage_scream" }
        ]
      }
    ]
  }
}
```

---

## 5. Technical Implementation Status (Audit)

| Property | Status | Requirement |
| :--- | :--- | :--- |
| **Driver Architecture** | **Pending** | Create `IAiDriver` and update `ServerEnemy` to hold a driver instance. |
| **Phased Overrides** | **Pending** | `PhasedAiDriver` must check for `base_damage` or `speed` overrides. |
| **JSON Polymorphism**| **Pending** | Update `EnemyData` to use `JsonElement` for `AiConfig`. |




## 6. Driver Hooks

Drivers are C# classes that implement the `IAiDriver` interface. This formalizes the lifecycle events that the server engine broadcasts to the entity's "Brain."

---

### 6.1 `OnUpdate`
**Signature:** `void OnUpdate(float dt, ServerEnemy entity, Dictionary<int, ServerPlayer> players);`

**Description:** This method represents the "always-on" consciousness of the AI. It is called during every **Server Tick**, which is the fundamental unit of time in the server's simulation. A tick is a single pass through the game's physics and logic loop (e.g., if the server runs at 60Hz, a tick happens every 16.6 milliseconds). 

While most of the "heavy lifting" like shooting happens on long-term timers, `OnUpdate` is used for logic that must react instantly to the changing world, such as steering the entity toward a player or adjusting its velocity to avoid walking into walls. Because this runs every single tick, it is the highest-frequency method in the Driver.

**Parameters:**
*   **`float dt` (Delta Time):** The literal duration of the current server tick in seconds. Essential for ensuring movement speed remains consistent regardless of server frame rate.
*   **`ServerEnemy entity`:** The unique physical entity in the game world that this driver is currently piloting.
*   **`Dictionary<int, ServerPlayer> players`:** The real-time snapshot of all players currently present in the monster's room. Used for targeting and environmental awareness.

> Because OnUpdate is the baseline for every entity, we don't need an "on_update" trigger in the JSON. Instead, the movement (or ai_type) field in your AiConfig is what tells the OnUpdate method what to do.
>  * If movement: "chase", the OnUpdate code runs the chasing math.
>  * If movement: "stationary", the OnUpdate code does nothing (or just rotates).
>
> Future-Proofing (Planned):
> While there's no plan for an "on_update" trigger that fires an ability ID, there is a plan for Conditions inside OnUpdate. **Example:** You might add a JSON field like "update_logic": "avoid_water". The OnUpdate method would read that and add "water-avoidance" math to its movement calculation.

---

### 6.2 `OnTick`

Lets fill this in.