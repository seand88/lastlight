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
**Description:** Used for Bosses or Elite enemies. Transitions between different stats and abilities based on HP thresholds (so it's really a Phased*HP*Driver).

#### `AiConfig` Schema:

| Property | Type | Description |
| :--- | :--- | :--- |
| `phases` | array | A list of [Phase Objects](#phase-object-schema). |

#### Phase Object Schema:
| Property | Type | Description |
| :--- | :--- | :--- |
| `threshold` | float | HP % required to enter this phase (e.g., `0.5` = 50% HP). |
| `movement` | enum | Movement style (`chase`, `kite`, `stationary`, `spiral`). |
| `speed` | float | **Override.** Movement speed for this phase. |
| `base_damage` | int | **Override.** Base damage for this phase (e.g., for enrage). |
| `behaviors` | array | List of [Behavior Objects](#3-behavior-triggers) active in this phase. |

#### Behaviors Object Schema: 

Behaviors define *when* an entity executes an `action` (Ability ID) for `PhasedAiDriver`.

| Trigger | Parameters | Description |
| :--- | :--- | :--- |
| **`on_timer`** | `interval` (float) | Fires the action repeatedly every `X` seconds. |
| **`on_damaged`** | — | Fires the action whenever the entity takes damage. |
| **`on_hp_below`** | `hp` (float) | Fires once when HP % drops below the specified value. |
| **`on_event`** | `event` (string) | Fires on specific room/player events (e.g., `player_heal`). |

#### Example JSON:
```json
"AiConfig": {
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




## 3. Driver Methods

Drivers are C# classes that implement the `IAiDriver` interface. This formalizes the lifecycle events that the server engine broadcasts to the entity's "Brain."

> There is a 1:1 relationship between AiDriver and a `ServerEntity`.

To write your own driver, implement the following methods as needed.

---

3.1 Initialize

**Signature:** `void Initialize(JsonElement config, ServerEnemy entity);`


**Description:** This is the Setup Phase of the AI lifecycle. It is called exactly once by the ServerEnemyManager immediately after the entity is instantiated and before its first update tick.

Its primary responsibility is to "Bootstrap" the Driver by translating the raw JSON configuration into optimized C# data. It also allows the Driver to perform initial modifications to the entity's physical body (e.g., setting the starting movement speed or base damage based on the configuration).

**Parameters:**

* `JsonElement config`: The raw polymorphic JSON object from Enemies.json (specifically the AiConfig property). **Usage:** Each Driver interprets this differently. A standard driver looks for primary and special keys, while a phased driver parses a phases array. Because it's a JsonElement, the Driver can use high-performance deserialization to map the data to private class variables.
* `ServerEnemy entity`: The physical monster instance this driver has been assigned to control. **Usage:** The Driver uses this reference to apply initial state. For example, if the config specifies a custom speed for this monster type, the Driver writes that value to entity.Speed here. This ensures the "Body" is ready for the first physics tick.

Implementation Example:

```
    1 public void Initialize(JsonElement config, ServerEnemy entity)
    2 {
    3     // 1. Load Ability IDs
    4     this.primaryAbility = config.GetProperty("primary").GetString();
    5
    6     // 2. Load and Apply Movement Speed from Config
    7     if (config.TryGetProperty("speed", out var speedProp)) {
    8         entity.Speed = speedProp.GetSingle();
    9     }
   10 }
```

---

### 3.1 `OnUpdate`
**Signature:** `void OnUpdate(float dt, ServerEnemy entity, Dictionary<int, ServerPlayer> players);`

**Description:** This method represents the "always-on" consciousness of the AI. It is called during every **Server Tick**, which is the fundamental unit of time in the server's simulation. A tick is a single pass through the game's physics and logic loop (e.g., if the server runs at 60Hz, a tick happens every 16.6 milliseconds). 

While most of the "heavy lifting" like shooting happens on long-term timers, `OnUpdate` is used for logic that must react instantly to the changing world, such as steering the entity toward a player or adjusting its velocity to avoid walking into walls. Because this runs every single tick, it is the highest-frequency method in the Driver.

**Parameters:**
*   `float dt` (Delta Time): The literal duration of the current server tick in seconds. Essential for ensuring movement speed remains consistent regardless of server frame rate.
*   `ServerEnemy entity`: The unique physical entity in the game world that this driver is currently piloting.
*   `Dictionary<int, ServerPlayer> players`: The real-time snapshot of all players currently present in the monster's room. Used for targeting and environmental awareness.

Implementation Example:

```
    1 public void OnUpdate(float dt, ServerEnemy entity, Dictionary<int, ServerPlayer> players)
    2 {
    3     // 1. Simple Targeting Logic
    4     var nearest = players.Values.OrderBy(p => Vector2.Distance(p.Position, entity.Position)).FirstOrDefault();
    5
    6     if (nearest != null)
    7     {
    8         // 2. Continuous Steering: Update velocity toward target every tick
    9         var direction = Vector2.Normalize(nearest.Position - entity.Position);
   10         entity.Velocity = direction * entity.Speed;
   11     }
   12 }
```

---

### 3.2 `OnTimerTick`
**Signature:** `void OnTimerTick(string actionId, ServerEnemy entity, ServerAbilityManager abilityManager);`

**Description:** This method represents the "Action Execution" phase of the AI. It is called by the server framework whenever one of the entity's internal behavior timers reaches its configured interval. This method maps directly to one or more **`on_timer`** behaviors defined in the JSON. 

Because a single enemy can have multiple rhythmic behaviors, the framework passes the **`actionId`** (the Ability ID) to this method. This allows the Driver to use a single method to handle any number of timed abilities without needing separate functions for each. The server manages the specific math for every timer instance independently, ensuring that a 1-second timer and a 10-second timer pulse accurately without drifting.

**Parameters:**
*   `string actionId`: The unique identifier of the ability that is ready to be fired. The Driver uses this ID to decide exactly which logic to execute (typically via a `switch` statement).
*   `ServerEnemy entity`: The physical monster body currently performing the action. Provides coordinates and stats for the ability execution.
*   `ServerAbilityManager abilityManager`: The server-side service responsible for validating and manifesting gameplay effects. The Driver calls `HandleEnemyAbility` here to pull the trigger.

Implementation Example:

```
    1 public void OnTimerTick(string actionId, ServerEnemy entity, ServerAbilityManager abilityManager) 
    2 {
    3     // 1. The framework tells us EXACTLY which ability timer just finished.
    4     // We calculate the firing direction (using a target cached during OnUpdate).
    5     Vector2 direction = Vector2.Normalize(_cachedTargetPos - entity.Position);
    6
    7     // 2. Execute the specific action requested by the timer
    8     switch (actionId)
    9     {
   10         case "enemy_basic_shot":
   11             abilityManager.HandleEnemyAbility(entity, actionId, direction, entity.RoomBullets);   
   12             break;
   13
   14         case "enemy_special_nova":
   15             // Some abilities might not need a direction (like a point-blank nova)
   16             abilityManager.HandleEnemyAbility(entity, actionId, Vector2.Zero, entity.RoomBullets);
   17             break;
   18     }
   19 }
```
---

## 4. Complete Example Driver Json Config 

### 4.1 Complete Implementation Example: The "Angry Sentinel"

This is a `Phased` **AiConfig**.

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