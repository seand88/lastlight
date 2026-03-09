# ABILITY_SPEC.md (v4.0)

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

The `effects` array contains objects that define what happens to the target(s) upon delivery. The server processes these in order. If a `chance` check fails, that specific effect (and only that one) is skipped.

| Effect Name | Description | Required / Optional Parameters |
| :--- | :--- | :--- |
| **`damage`** | Instant reduction of the target's `CurrentHealth`. | `value` (amount), `damage_type` (e.g. fire). |
| **`heal`** | Instant increase of the target's `CurrentHealth`. | `value` (amount). |
| **`mana_gain`** | Instant increase of the target's `CurrentMana`. | `value` (amount). |
| **`dot`** | **Damage Over Time.** Periodically reduces health. | `value` (per tick), `duration` (total), `tick_rate`. |
| **`hot`** | **Heal Over Time.** Periodically restores health. | `value` (per tick), `duration` (total), `tick_rate`. |
| **`buff`** | Temporary increase to a specific stat. | `value` (flat bonus), `duration`, `stat_type` (see below). |
| **`debuff`** | Temporary decrease to a specific stat. | `value` (flat penalty), `duration`, `stat_type` (see below). |

### 3.1 Parameter Reference Table

| Property | Type | Description |
| :--- | :--- | :--- |
| `effect_name` | string | The logic key (e.g., `damage`). |
| `target_type` | enum | `caster`, `enemies`, `allies`, `all`. |
| `template_id` | string | **Optional.** Used by client to look up visual/sound templates. |
| `value` | float | Magnitude of the effect (damage amount, stat bonus, etc.). |
| `damage_type`| enum | **Optional.** `physical`, `fire`, `frost`, `shock`, `poison`. |
| `chance` | float | **Optional.** Probability (0.0 to 1.0) to trigger. Defaults to `1.0`. |
| `duration` | float | **Required for DOT/HOT/Buff/Debuff.** Total time in seconds. |
| `tick_rate` | float | **Required for DOT/HOT.** Seconds between ticks (e.g. `1.0` is once/sec). |
| `stat_type` | string | **Required for Buff/Debuff.** `attack`, `defense`, `speed`, `dexterity`. |

---


## 4. .NET Architecture & Networking

### 4.1 LastLight.Common (Shared Logic)

#### **`Abilities/AbilitySpec.cs`**
Contains the POCO classes for deserializing `abilities.json`.
- `AbilitySpec`: Root class.
- `DeliverySpec`: Base class for polymorphic delivery data.
- `EffectSpec`: Defines what happens on impact.

#### **`Abilities/EffectProcessor.cs`**
The core execution engine used by both Client and Server.
- `ApplyEffect(IEntity target, IEntity source, EffectSpec spec)`: Handles health/mana changes and applying status effects.

#### **`Abilities/StatusRegistry.cs`**
Manages active timed effects on entities.
- `StatusInstance`: Tracks duration and tick timers for a specific effect on an entity.
- `StatusManager`: Updated every frame to tick down durations and trigger periodic effects.

#### **Network Packets (`Models.cs`)**
- `AbilityUseRequest`: Sent by client when an ability is triggered.
  - `string AbilityId`
  - `Vector2 Direction` or `Vector2 TargetPosition`
- `EffectEvent`: Broadcast by server to inform clients of effect results.
  - `string EffectName`
  - `int TargetId`
  - `int SourceId`
  - `float Value`
  - `Vector2 Position` (For spawning hit particles/text)

### 4.2 LastLight.Server (Authoritative)
- `ServerAbilityManager`: Validates cooldowns and mana. Triggers `Delivery` logic.
- `ServerDeliveryProcessor`: Handles the physical manifestation (spawning projectiles, checking AoE).
- When a `Delivery` hits a target, it calls `EffectProcessor.ApplyEffect` and broadcasts `EffectEvent`.

### 4.3 LastLight.Client.Core (Visuals & Prediction)
- `ClientAbilityManager`: Spawns local "ghost" delivery objects for instant feedback.
- `ClientEffectHandler`: Listens for `EffectEvent` to:
  - Play sounds.
  - Spawn particles.
  - Show floating combat text.
  - Update local HP/Mana (reconciled by server updates).

---

## 5. Implementation Examples

### Basic Attack

This is an example of the **Generator** type weapon ability. It does `Physical` damage and generates mana.

```json
{
  "id": "basic_attack",
  "name": "Quick Shot",
  "icon": "icon_attack_01",
  "mana_cost": 0,
  "cooldown": 0,
  "delivery": {
    "type": "projectile",
    "fire_rate": 5.0,
    "speed": 1200,
    "pattern": "straight",
    "count": 1,
    "range_tiles": 12,
    "shape": "circle",
    "width": 8,
    "height": 8,
    "color": "255,255,255"
  },
  "effects": [
    { 
      "effect_name": "damage",
      "target_type": "enemies",
      "value": 15, 
      "damage_type": "physical" 
    },
    { 
      "effect_name": "mana_gain", 
      "target_type": "caster",
      "value": 2
    }
  ]
}

```

### Disease Sniper

This is an example of a **Special** ability from a weapon.

```json
{
  "id": "disease_sniper_ability",
  "name": "Plague Bringer",
  "icon": "icon_sniper_01",
  "mana_cost": 5,
  "cooldown": 0.5,
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
```

This section walks through the lifecycle of two different abilities to show how prediction, networking, and the payload (effects) interact.

### 6.1 Special Ability: Disease Sniper (Plague Bringer)

This ability features a 5-mana cost, a 0.5s cooldown, and a 50% chance to apply a Damage-Over-Time (DOT) effect.

#### **High-Level Flow Diagram**

```mermaid
sequenceDiagram
    participant C as Client (Player)
    participant S as Server
    participant O as Other Clients

    C->>C: Input: Right Click (Mouse Position)
    C->>C: Prediction: Spawn "Ghost" Projectile (Green)
    C->>S: Packet: AbilityUseRequest ("plague_bringer")
    
    S->>S: Validate Mana (5) & Cooldown (0.5s)
    S->>S: Success: Deduct Mana, Start Cooldown
    S->>O: Broadcast: SpawnBullet (Authoritative ID: -5001)
    
    Note over C,S: Projectile travels (Speed: 1800)...
    
    S->>S: Physics Collision with Enemy (-101)
    S->>S: Effect Logic: Apply 50 Damage
    S->>S: Effect Logic: Roll 50% Chance for DOT (Success)
    
    S->>C: Packet: EffectEvent ("damage", Value: 50)
    S->>C: Packet: EffectEvent ("dot", Duration: 3s)
    S->>O: Broadcast: EffectEvent (x2)
    
    C->>C: Visual: Destroy "Ghost", Spawn Red "-50" Text
    C->>C: Visual: Spawn Green Bubbles (Template: "disease")
```

#### **Timeline Detail**
| Step | Location | Event | Visuals / Network |
| :--- | :--- | :--- | :--- |
| **0ms** | Client | **Input Trigger** | Player clicks. `ClientAbilityManager` spawns a green bullet immediately. |
| **5ms** | Network | **Request Sent** | `AbilityUseRequest` sent with target coordinates. |
| **40ms**| Server | **Validation** | Server checks `mana_cost` and `cooldown`. Passes. |
| **45ms**| Network | **Sync Spawn** | `SpawnBullet` broadcast. All other players now see the green bullet. |
| **200ms**| Server | **Collision** | Server detects hit on Enemy `-101`. Bullet is destroyed. |
| **205ms**| Server | **Processing** | `EffectProcessor` reduces HP. `StatusManager` adds 3s DOT. |
| **210ms**| Network | **Impact Sync** | Two `EffectEvent` packets sent (Damage + DOT). |
| **250ms**| Client | **Resolution** | Client sees "-50" text and green "disease" particles. |

---

### 6.2 Primary Generator: Quick Shot (Basic Attack)

This is a high-speed, zero-mana "left-click" ability that restores mana to the caster on hit.

#### **High-Level Flow Diagram**

```mermaid
sequenceDiagram
    participant C as Client (Player)
    participant S as Server

    C->>C: Input: Left Click (Direction)
    C->>C: Prediction: Spawn "Ghost" Bullet (White)
    C->>S: Packet: AbilityUseRequest ("basic_attack")
    
    S->>S: Validate Cooldown (0s)
    S->>O: Broadcast: SpawnBullet
    
    Note over C,S: Bullet travels (Speed: 1200)...
    
    S->>S: Collision with Enemy
    S->>S: Effect 1 (Target: Enemy): Apply 15 Damage
    S->>S: Effect 2 (Target: Caster): Add 2 Mana
    
    S->>C: Packet: EffectEvent ("damage", Target: Enemy)
    S->>C: Packet: EffectEvent ("mana_gain", Target: Self)
    
    C->>C: Visual: Mana bar pulses blue (+2)
    C->>C: Visual: Enemy hit spark
```

#### **Timeline Detail**
| Step | Location | Event | Visuals / Network |
| :--- | :--- | :--- | :--- |
| **0ms** | Client | **Input Trigger** | Left click. High fire-rate (5/sec) means rapid ghost bullets. |
| **40ms**| Server | **Validation** | 0 Mana cost = Always passes. |
| **150ms**| Server | **Impact** | Bullet hits enemy. |
| **155ms**| Server | **Payload** | Caster's mana is increased by 2. Enemy takes 15 damage. |
| **200ms**| Client | **Feedback** | `EffectEvent` ("mana_gain") triggers a blue flash on the UI mana bar. |