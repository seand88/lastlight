# ABILITY_SPEC.md (v4.1)

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

* **Logic Routing (effect_name):** This is the most important field. It tells the server which C# function to run (e.g., "If this says dot, use the StatusManager; if it says damage, just subtract HP").
* **Targeting (target_type):** This defines the "Filter." Even if a bullet hits an enemy, an effect with target_type: "caster" will apply to the person who shot it (like Life Steal or Mana Gain).
* **Visual Linking (template_id):** This is a "hook" for the client. The server doesn't care what a "poison bubble" looks like, but by putting that ID here, you're telling the client: "When this hits, look up the poison_bubble settings in the graphics folder and play those particles."
* **Timing & Magnitude (value, duration, tick_rate):** These are the raw numbers. By changing these in the JSON, you can turn a weak "Poison" (5 damage over 10 seconds) into a deadly "Plague" (50 damage over 2 seconds) without touching a single line of code.

### 3.1 JSON Schema: Effect Parameters (Design-Time)

This table defines the properties available in `abilities.json` for game designers to configure effects.

| Property | Type | Description |
| :--- | :--- | :--- |
| `effect_name` | string | The logic key (e.g., `damage`, `dot`, `buff`). |
| `target_type` | enum | `caster`, `enemies`, `allies`, `all`. |
| `template_id` | string | **Optional.** Visual ID (e.g., `"poison_bubble"`) to look up in `effect_templates.json`. |
| `value` | float | Base magnitude of the effect (e.g., 50 damage). |
| `damage_type`| enum | **Optional.** `physical`, `fire`, `frost`, `shock`, `poison`. |
| `chance` | float | **Optional.** Probability (0.0 to 1.0) to trigger. |
| `duration` | float | **Required for timed effects.** How long the status lasts in seconds. |
| `tick_rate` | float | **Required for DOT/HOT.** Seconds between periodic ticks. |
| `stat_type` | string | **Required for Buff/Debuff.** `attack`, `defense`, `speed`, `dexterity`. |

### 3.2 List of Effects

| Effect Name | Description | Required / Optional Parameters |
| :--- | :--- | :--- |
| **`damage`** | Instant reduction of the target's `CurrentHealth`. | `value` (amount), `damage_type` (e.g. fire). |
| **`heal`** | Instant increase of the target's `CurrentHealth`. | `value` (amount). |
| **`mana_gain`** | Instant increase of the target's `CurrentMana`. | `value` (amount). |
| **`dot`** | **Damage Over Time.** Periodically reduces health. | `value` (per tick), `duration` (total), `tick_rate`. |
| **`hot`** | **Heal Over Time.** Periodically restores health. | `value` (per tick), `duration` (total), `tick_rate`. |
| **`buff`** | Temporary increase to a specific stat. | `value` (flat bonus), `duration`, `stat_type` (see below). |
| **`debuff`** | Temporary decrease to a specific stat. | `value` (flat penalty), `duration`, `stat_type` (see below). |
| **`remove_status`** | Removes an active status effect (Buff/Debuff/DOT). | `template_id` (the specific status to remove). |

### 3.3 Status Effect Lifecycle

To ensure perfect synchronization between the server's math and the client's visuals, status effects (DOT, HOT, Buff, Debuff) follow these lifecycle rules:

1.  **Application:** The server sends an `EffectEvent` with the `Duration` and `Value`. The client uses this to display the debuff icon and start particle systems.
2.  **Authoritative Ticks:** For DOT/HOT, the server sends a new `EffectEvent` (Type: `damage` or `heal`) **every time a tick occurs**. The client should not "guess" tick damage; it must wait for the server packet to show floating combat text.
3.  **Removal/Expiry:** When an effect expires naturally or is cleansed, the server sends an `EffectEvent` with **`EffectName: "remove_status"`** and the matching `TemplateId`. The client then stops the associated visual effects.

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

#### **Network Packets (Runtime)**

These packets facilitate communication between the Client and the Server.

**`AbilityUseRequest` (Client -> Server)**
Sent when a player triggers an ability.

| Field | Type | Description |
| :--- | :--- | :--- |
| `AbilityId` | string | Unique ID of the ability to use. |
| `Direction` | Vector2 | Normalized vector of the shot direction. |
| `TargetPosition`| Vector2 | World coordinates of the mouse/target. |
| `ClientInstanceId`| int | A local ID generated by the client to track its predicted projectile. |

**`EffectEvent` (Server -> Broadcast)**
Informs clients of a calculated event.

| Field | Type | Description |
| :--- | :--- | :--- |
| `EffectName` | string | What happened (e.g., `"damage"`, `"heal"`, `"remove_status"`). |
| `TargetId` | int | The ID of the entity receiving the effect. |
| `SourceId` | int | The ID of the entity who caused the effect. |
| `SourceProjectileId`| int | The `ClientInstanceId` of the bullet that caused this effect (used to destroy predicted ghosts). |
| `Value` | float | **Calculated.** The final numeric result (after defense/stat logic). |
| `Duration` | float | **Calculated.** The remaining time for a status effect. |
| `Position` | Vector2 | **Calculated.** The world coordinates where the event occurred. |
| `TemplateId` | string | The Visual/Audio ID from the original JSON configuration. |

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

### 4.4 Technical Runtime Mandates

To ensure the system works as intended, the following runtime rules apply:

*   **Network Rule:** Every single point of health or mana changed by a DOT, HOT, or Buff must be backed by an `EffectEvent` packet from the server.
*   **Engine Update Rule:** Both the Client and Server game loops **MUST** call `StatusManager.Update(dt)` every frame to process active status durations and tick timers.
*   **Reasoning:** This prevents "Desync Deaths" where a client thinks a player is alive with 5 HP but the server-side tick already killed them. It also prevents memory leaks by ensuring expired statuses are purged from the registry.

---

## 5. Implementation Examples

### 5.1 Basic Attack

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

### 5.2 Disease Sniper

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

## 6. Network Walkthrough

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

---

### 6.3 Desync Case: Correcting a "False Hit" (Bad Prediction)

In this scenario, the Client thinks they hit a fast-moving enemy, but the Server (the source of truth) rules it a miss.

#### **Sequence Timeline**

| Step | Location | Event | Result |
| :--- | :--- | :--- | :--- |
| **0ms** | Client | **Input Trigger** | Player fires. Ghost bullet `ID: 99` spawned locally. |
| **100ms**| Client | **False Hit** | On the player's screen, the ghost bullet overlaps an enemy. |
| **105ms**| Client | **Visual Prediction**| **Client hides Ghost 99** and spawns a "Spark" particle. (Client assumes success). |
| **150ms**| Server | **Authoritative Miss**| The bullet passes through the enemy hitbox on the server due to latency. |
| **155ms**| Server | **Expiry** | The bullet reaches max range and is destroyed silently. |
| **200ms**| Client | **Resolution (Sync)** | The client **NEVER** receives an `EffectEvent` for Ghost 99. |
| **205ms**| Client | **Correction** | The client realizes the ghost was hidden but no server confirmation arrived. The "Spark" was just a visual lie; the Enemy HP bar does not move. |

**Note on Correction:** Because the Client only updates HP bars and spawns Floating Combat Text when an `EffectEvent` arrives, "False Hits" are automatically corrected. The player sees a spark, but no damage number appears, indicating the miss.
