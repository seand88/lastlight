# ENEMIES SPECIFICATION

## 1. Overview (v2.0 - DRAFT)
This document defines the data-driven schema for enemies in **LastLight**. Logic is decoupled from the entity and managed by **Polymorphic AI Drivers**. This architecture allows for a wide variety of monster behaviors—from simple "move and shoot" mobs to complex multi-phase bosses—without requiring unique C# classes for every enemy type.

## 2. Design Goals & Functional Requirements
The system is designed around the separation of the physical entity (The Body) from its logic controller (The Brain).

```bash
┌─────────────┬──────┬────────────┬────────────────────────────────────────────────────┐
│ Component   │ Type │ Analog     │ Definition                                         │
├─────────────┼──────┼────────────┼────────────────────────────────────────────────────┤
│ ServerEnemy │ C#   │ The Body   │ Stats (HP, BaseDamage) and World State (Position). │
│ AI Driver   │ C#   │ The Brain  │ The hardcoded logic rules (Strategy Pattern).      │
│ AI Config   │ JSON │ The Memory │ The specific settings/IDs for this unique monster. │
└─────────────┴──────┴────────────┴────────────────────────────────────────────────────┘
```

### 2.1 AI Driver Concept
- **Responsibility:** Contains actual math and update loops for movement (chase, kite, stationary) and ability execution.
- **Decoupling:** Drivers "pilot" the entity but do not own its health or position.
- **Polymorphism:** The `AiDriver` enum in JSON determines which C# Driver class is instantiated.

## 3. Data Specification
Enemies are defined in `Enemies.json`.

### 3.1 Core Entity Properties (Global Defaults)
These properties define the entity's base identity. If a Phase or Behavior does not provide an override, the engine falls back to these values.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | string | Unique internal identifier (e.g., `enemy_goblin`). |
| `Name` | string | User-facing display name. |
| `EnemyType` | enum | **Optional.** `enemy`, `boss`. Defaults to `enemy`. Used by client to decide on UI (e.g., Boss health bar). |
| `Width` | int | **Optional.** Sprite width in pixels. Defaults to `32`. |
| `Height` | int | **Optional.** Sprite height in pixels. Defaults to `32`. |
| `MaxHealth` | int | Starting health for the entity. |
| `BaseDamage` | int | **Default** power used for scaling abilities. |
| `AttackSpeedBonus`| float | **Default** percentage mod for fire rate (e.g., `0.1` = +10% speed). |
| `RangeBonus` | float | **Default** tile addition to projectile/beam travel distance. |
| `Animation` | string | The animation sheet key to use for rendering via `WorldRenderer` (e.g., `BossOverlord` or `EnemyOrc`). Currently only plays the `idle` clip on a loop. |
| `AiDriver` | enum | Determines which **AI Driver** class to instantiate (`standard`, `phased`). |
| `AiConfig` | object | The polymorphic data payload for the selected driver. |

### 3.2 Driver Schema: `standard`
- **C# Class:** `StandardAiDriver` | Default behavior for common mobs.
- **Targetting:** This driver calculates the normalized direction vector toward the nearest player every update frame and store it as the firing direction for all abilities. 

| Property | Type | Description |
| :--- | :--- | :--- |
| `movement` | enum | `chase`, `kite`, `stationary`. |
| `speed` | float | Movement speed in pixels per second. |
| `primary` | string | Ability ID fired continuously on its `fire_rate`. |
| `special` | string | Ability ID fired as soon as its `cooldown` expires. |

### 3.3 Driver Schema: `phased`
- **C# Class:** `PhasedAiDriver` | Used for Bosses or Elite enemies.
- **Targetting:** This driver calculates the normalized direction vector toward the nearest player every update frame and store it as the firing direction for all abilities. 

| Property | Type | Description |
| :--- | :--- | :--- |
| `phases` | array | A list of [Phase Objects](#34-phase-object-schema). |

#### 3.3.1 Phase Object Schema
| Property | Type | Description |
| :--- | :--- | :--- |
| `threshold` | float | HP % required to enter this phase (e.g., `0.5` = 50% HP). |
| `movement` | enum | Movement style (`chase`, `kite`, `stationary`, `spiral`). |
| `speed` | float | **Override.** Movement speed for this phase. |
| `base_damage` | int | **Override.** Base damage for this phase (e.g., for enrage). |
| `behaviors` | array | List of [Behavior Objects](#35-behaviors-object-schema) active in this phase. |

#### 3.3.2 Behaviors Object Schema
| Trigger | Parameters | Description |
| :--- | :--- | :--- |
| **`on_timer`** | `interval` (float) | Fires the action repeatedly every `X` seconds. |
| **`on_damaged`** | — | Fires the action whenever the entity takes damage. |
| **`on_hp_below`** | `hp` (float) | Fires once when HP % drops below the specified value. |
| **`on_event`** | `event` (string) | Fires on specific room/player events (e.g., `player_heal`). |

### 3.6 Data Example: The "Angry Sentinel"
```json
{
  "Id": "enemy_sentinel",
  "Name": "Angry Sentinel",
  "EnemyType": "boss",
  "Width": 128,
  "Height": 128,
  "MaxHealth": 500,
  "BaseDamage": 15,
  "Animation": "BossOverlord",
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

## 4. Technical Implementation

### 4.1 Driver Interface
Drivers are C# classes that implement the `IAiDriver` interface. This formalizes the lifecycle events that the server engine broadcasts to the entity's "Brain." There is a 1:1 relationship between `AiDriver` and a `ServerEnemy`.

#### `Initialize`
**Signature:** `void Initialize(JsonElement config, ServerEnemy entity, ITimerRegistry registry);`
Called once by `ServerEnemyManager` after instantiation. Translates JSON into C# data and registers timers via the **"Register & Pulse" Pattern**.

#### `OnUpdate`
**Signature:** `void OnUpdate(float dt, ServerEnemy entity, Dictionary<int, ServerPlayer> players, ServerAbilityManager abilityManager);`
Called every Server Tick. Used for high-frequency logic like steering toward players or adjusting velocity.

#### `OnTimerTick`
**Signature:** `void OnTimerTick(string actionId, ServerEnemy entity, ServerAbilityManager abilityManager);`
Called when an internal behavior timer reaches its interval. Maps to `on_timer` behaviors.

#### `OnDamaged`
**Signature:** `void OnDamaged(ServerEnemy entity, int damage, IEntity? source, ServerAbilityManager abilityManager);`
Called immediately after `CurrentHealth` is reduced. Used for retaliatory strikes or enrage triggers.

### 4.2 Client Implementation (Visuals)
On the client, all non-player entities are represented by a single generic **`ClientEntity`** class. The client does not simulate AI or behavior; it purely renders state based on the `DataId` and `Phase` received from the server.

- **Data-Driven Rendering:** The `Draw` loop no longer directly interacts with static atlases. Instead, the `DataId` is used to look up the `Animation` string property from `Enemies.json`. This key is passed to `WorldRenderer.PlayAnimation(...)` targeting the `"idle"` clip, which manages all frame timing and drawing.
- **Size Scaling:** Rendering dimensions are determined by the `Width` and `Height` properties in the JSON metadata.
- **Phase Feedback:** Visual overlays or color flashes (e.g., boss enrage) are triggered by the `Phase` byte sent in the `EntityUpdate` packet.
- **Unified Manager:** A single **`EntityManager`** manages the active dictionary of `ClientEntity` objects, replacing the legacy Boss and Enemy managers.
