# NETWORK SPECIFICATION

## 1. Overview (v1.0)
This document defines the communication protocol between the LastLight Client and Server. It utilizes a **Pure UDP** transport (via LiteNetLib) with an **Authoritative Server / Predictive Client** architecture. The system is optimized for high-intensity bullet-hell combat where low latency and state reconciliation are critical.

## 2. Design Goals & Functional Requirements

### 2.1 Data Visibility & Transmission Rules
To optimize bandwidth and prevent cheating (info-glance), data is categorized into three visibility tiers:

| Tier | Recipient | Logic | Example Data |
| :--- | :--- | :--- | :--- |
| **Broadcast** | All in Room | Sent periodically or on event to everyone. | Position, Health, Projectiles. |
| **Private** | Specific Peer | Sent only to the owner of the data. | Mana, Experience, Inventory. |
| **Request** | Server | Input or intent sent from client to server. | Movement, Ability Use. |

### 2.2 Periodic State Synchronization
The server broadcasts the world state **20 times per second (20Hz / every 50ms)** to ensure all clients have a consistent view of the game world.

### 2.3 Prediction & Reconciliation

#### Movement (Local Prediction)
1.  Client moves instantly and sends `InputRequest` with a `SequenceNumber`.
2.  Server validates the move and sends `AuthoritativePlayerUpdate` with the `LastProcessedSequence`.
3.  If the Server position differs significantly, the Client **snaps** to the server's position and re-plays any pending inputs.

#### Combat (Visual Prediction)
1.  On click, Client spawns a "Ghost" projectile with a local ID.
2.  Client sends `AbilityUseRequest` to Server.
3.  The Ghost travels until the Client receives an `EffectEvent` containing the matching `SourceProjectileId`.
4.  Client destroys the Ghost and spawns the authoritative hit visual at the `Position` provided by the server.

## 3. Data Specification (Packets)

### 3.1 Periodic Packets

#### `AuthoritativePlayerUpdate` (Broadcast)
*Lean packet for public presence.*
- `PlayerId`: int
- `Position`: Vector2
- `Velocity`: Vector2 (for dead-reckoning)
- `CurrentHealth`: int
- `MaxHealth`: int
- `Level`: int

#### `SelfStateUpdate` (Private)
*Sent only to the local player to sync their personal UI.*
- `CurrentMana`: int
- `MaxMana`: int
- `Experience`: int
- `Stats`: (Attack, Defense, Speed, Dexterity, Vitality, Wisdom)

### 3.2 Event-Driven Packets (On-Change)
Non-continuous data is sent only when a state transition occurs.

#### Inventory & Items
*   **`InventoryUpdate` (Private):** Sent to a player when their bag changes (e.g., equipment swap, chest reward). Contains `SlotIndex` and `ItemInfo`.
*   **`ItemSpawn` (Broadcast):** Notifies everyone that a loot item has dropped on the ground.
*   **`ItemPickup` (Broadcast):** Notifies everyone to remove the ground item from their world.

#### Combat & Abilities
*   **`AbilityUseRequest` (Request):** Client's intent to fire. Includes `ClientInstanceId` for prediction cleanup.
*   **`SpawnBullet` (Broadcast):** Server's confirmation that a projectile exists. Includes `AbilityId` for visual lookup.
*   **`EffectEvent` (Broadcast/Filtered):** The result of an impact. Includes `SourceProjectileId` to tell the shooter to destroy their local "ghost."

## 4. Technical Implementation

### 4.1 Security Mandates
1.  **No Stats in Broadcast:** A player's raw stats (e.g., Wisdom, Dexterity) are **never** broadcast to other players.
2.  **Server-Side Cooldowns:** The server ignores `AbilityUseRequest` if the time since the last valid use is less than `1/fire_rate`.
3.  **Range Enforcement:** The server destroys projectiles that exceed their `range_tiles` setting, regardless of client state.

### 4.2 Transport Layer
The communication is handled by **LiteNetLib**.
- **Unreliable:** Used for `InputRequest` and `AuthoritativePlayerUpdate` where the latest state is more important than missing packets.
- **Reliable Ordered:** Used for state transitions like `AbilityUseRequest`, `ItemPickup`, and `InventoryUpdate` where execution order and delivery are mandatory.
