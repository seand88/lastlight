# ABILITY_SPEC.md (v3.6)

This document defines the data-driven schema for **LastLight**. The system uses a "Vehicle and Payload" architecture to separate the *Trigger* from the *Action*.

## 1. Core Architecture & Relationships

The system is composed of three hierarchical layers. A game designer "builds" an ability by nesting these components:

1.  **Ability Root (The Trigger):** The entry point. Handles "Business Logic" like naming, Mana consumption, and Cooldown timers.
2.  **Delivery (The Vehicle):** The physical manifestation in the game world. The `type` property inside the delivery object determines if it is a `projectile`, `channeled`, `contact`, or `instant`.
3.  **Effects (The Payload):** The results of an impact. A single Delivery carries an array of effects (e.g., Damage + Life Steal). Each effect can be filtered by `target_type`.

---

## 2. Component & Delivery Type Specifications

### 2.1 Ability Root
| Property | Type | Description |
| :--- | :--- | :--- |
| `id` | string | Unique internal identifier (e.g., `basic_attack`). |
| `name` | string | User-facing display name. |
| `icon` | string | Sprite ID for the skill bar / UI. |
| `mana_cost` | int | Mana consumed upon activation. |
| `cooldown` | float | Seconds before the ability can be reused. |
| `delivery` | object | The container for the Delivery Spec (Must contain a `type` string). |
| `effects` | array | List of [Effect Objects](#3-effects-the-payload). |

### 2.2 Delivery Types (The Vehicle)
The `delivery` object **must** contain a `type` property to determine logic.

#### **type: "projectile"**
*Entities moving through space with collision logic.*
* **`fire_rate`**: Number of shots per second.
* **`speed`**: Movement speed in units per second.
* **`pattern`**: Layout of shots (`straight`, `cone`, `circle`, `parallel`).
* **`count`**: Number of projectiles spawned per shot.
* **`range_tiles`**: Max distance (1 tile = 32px) before destruction.
* **`pierce`**: If `true`, continues through targets until max range.
* **`spread`**: Total arc degrees (for `cone` or `circle`).
* **`shape`**: `circle` or `rectangle` for collision math.
* **`width` / `height`**: Dimensions for the collider and sprite.
* **`color`**: RGB string (e.g., `255,255,255`) for procedural tinting.

#### **type: "channeled"**
*Persistent states active while the input is held.*
* **`tick_rate`**: Frequency of effect application (e.g., `0.5` is every 0.5s).
* **`mana_per_second`**: Continuous mana cost while active.
* **`length_tiles`**: Length of the collision beam/area.
* **`width_pixels`**: Width of the collision beam/area.

#### **type: "contact"**
*Triggered via physical collision between player and enemy hitboxes.*
* **`knockback_force`**: Magnitude of displacement on hit.
* **`cooldown_per_target`**: Delay to prevent rapid-fire hits on one frame.

#### **type: "instant"**
*Immediate application of effects in a designated area.*
* **`anchor`**: Where the effect originates (`caster` or `mouse_cursor`).
* **`radius`**: Distance around the anchor to apply effects. Set to `0` for self-only.

---

## 3. Effects (The Payload)


| Property | Type | Description |
| :--- | :--- | :--- |
| `effect_name` | string | Logic key: `damage`, `mana_gain`, `heal`, `dot`, `slow`. |
| `target_type` | enum | `caster`, `enemies`, `allies`, `all`. |
| `template_id` | string | Links to `effect_templates.json` for UI/visuals. |
| `value` | float | Magnitude of the effect. |
| `damage_type`| enum | **Optional.** `physical`, `fire`, `frost`, `shock`, `poison`. |
| `chance` | float | **Optional.** Probability (0.0 to 1.0) to trigger. |
| `duration` | float | **Optional.** Total time for a DOT or status effect. |
| `tick_rate` | float | **Optional.** Frequency of ticks for DOT/HOT. |

---

## 4. Project & .NET Architecture Details

### 4.1 LastLight.Common (Shared Logic)
* **`Abilities/AbilitySpec.cs`**: Contains the POCO classes for deserializing `abilities.json`. Uses `[JsonExtensionData]` to handle polymorphic delivery types. [cite: 2026-03-08]
* **`Abilities/EffectProcessor.cs`**: The core execution engine. Contains a static `Apply(Entity target, Entity source, EffectSpec spec)` method. It uses a `switch(spec.EffectName)` to route logic to health reduction, mana addition, or status application. [cite: 2026-03-08]
* **`Abilities/StatusRegistry.cs`**: Handles the management of active `DotInstances` or `Buffs` on entities, ensuring timers tick down correctly. [cite: 2026-03-08]

### 4.2 LastLight.Server (Authoritative)
* **`ServerAbilityManager.cs`**: Manages a `Dictionary<string, float> lastUsedTime` for every player. It validates every "TryUse" request against current Mana and Cooldowns from the JSON. If valid, it tells the `ServerBulletManager` or `ServerChannelManager` to begin execution. [cite: 2026-03-08]
* **`ServerBulletManager.cs`**: Uses the `DeliverySpec` to create server-side hitboxes. It is responsible for detecting collisions and calling `EffectProcessor.Apply` upon impact. [cite: 2026-03-08]

### 4.3 LastLight.Client.Core (Visuals)
* **`ClientAbilityManager.cs`**: Listens for local input. It performs **Visual Prediction** by spawning local projectiles immediately while sending the network packet to the server. [cite: 2026-03-08]
* **`BulletManager.cs`**: Interprets the `color`, `shape`, `width`, and `height` from the JSON to render the appropriate sprite or procedural shape for the local player and other network entities. [cite: 2026-03-08]

---

## 5. JSON Sample: Disease Sniper (Generator)

This is an example *Generator* ability (left-click mana-generator) that fires a single, `Physical` damage projectile at a rate of 2 every second. It has a 50% chance to apply disease to the target which deals 5 `Physical` damage every second for **3** seconds.

```json
{
  "id": "disease_sniper_ability",
  "name": "Plague Bringer",
  "icon": "icon_sniper_01",
  "mana_cost": 0,
  "cooldown": 0,
  "delivery": {
    "type": "projectile",
    "fire_rate": 2.0,
    "speed": 1800,
    "pattern": "straight",
    "count": 1,
    "range_tiles": 20,
    "shape": "rectangle",
    "width": 16,
    "height": 4,
    "color": "75,150,75"
  },
  "effects": [
    { 
      "effect_name": "damage",
      "target_type": "enemies",
      "value": 50, 
      "damage_type": "physical" 
    },
    { 
      "effect_name": "dot", 
      "target_type": "enemies",
      "template_id": "disease",
      "value": 5, 
      "damage_type": "physical",
      "duration": 3.0,
      "tick_rate": 1.0,
      "chance": 0.5 
    }
  ]
}