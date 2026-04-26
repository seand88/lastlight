# Gemini Project Context: LastLight

## Project Overview
LastLight is a real-time multiplayer co-op bullet hell game (inspired by Realm of the Mad God) built using C# and the Godot 4.7 Engine. **Note: The original MonoGame client is deprecated.**

## Architecture
- **Framework:** Godot 4.7 (C# / .NET 9.0)
- **Networking:** LiteNetLib (UDP-based)
- **Projects:**
    - `LastLight.Common`: Shared classes, struct definitions, network packets, `WorldManager`, and core enums.
    - `LastLight.Server`: Standalone authoritative C# console application managing state, physics, AI, world generation, item drops, and broadcasting.
    - `godot-client`: Primary client implemented in Godot 4.7. Uses `Node2D` scenes for entities and `TileMapLayer` for the world.
    - `LastLight.Client.*`: (DEPRECATED) Old MonoGame-based client projects.

## CRITICAL INVARIANTS (Do Not Break)
### 1. Client-Side Prediction & Reconciliation
The Godot client (`Main.cs`) MUST maintain a `PendingInputs` list and increment `InputSequenceNumber`. Movement is applied locally immediately for zero-latency feel. When a server `AuthoritativePlayerUpdate` arrives, the client MUST snap to the server position and re-apply all pending inputs that the server hasn't acknowledged yet.

### 2. Server-Side Room State Sync
The `SwitchPlayerRoom` method on the server MUST send the complete state of the target room to the player *after* the `WorldInit` packet. This includes all active Portals, Enemies, Spawners, Bosses, and Items.

### 3. Sprite Assets
Entities (Player, Enemy, Boss, Spawner, Portal) MUST be instantiated from `.tscn` scenes. These scenes use physical `.png` assets (originally generated from the procedural atlas) located in `res://`. Do not switch back to procedural drawing in code.

### 4. ID Management
- **Players:** IDs >= 0 (Assigned by LiteNetLib).
- **AI Entities (Hostile):** IDs < 0 (Assigned by Server).
- **Collision Logic:** Players only take damage from bullets where `OwnerId < 0`. AI only take damage from bullets where `OwnerId >= 0`.

### 5. World Style Synchronization
`ServerRoom` MUST store its `GenerationStyle` as a property. The `SwitchPlayerRoom` method on the server MUST send the stored `room.Style` in the `WorldInit` packet. The client MUST use this style to generate its local `TileMapLayer` using the shared `WorldManager` logic.

### 6. Walkable Spawn Enforcement
`SwitchPlayerRoom` MUST execute a loop using `room.World.IsWalkable(testPos)` to find a valid grass/sand tile before updating the player's position. This ensures players never spawn inside walls or water.

### 7. LiteNetLib Packet Registration
`Networking.cs` MUST register every packet type used by the server in its `RegisterPackets` method. If a packet type is sent by the server but not registered on the client, LiteNetLib will throw an "Undefined packet in NetDataReader" exception.

## Current State
- **Godot Transition:** Core gameplay (movement, shooting, world generation, multi-room) has been successfully ported to Godot.
- **Nexus Social Hub:** A 30x30 non-combat room. Contains permanent portals to "Forest Realm" and "Dungeon Realm".
- **Combat:** Server-authoritative movement, shooting, and health. Multi-phase bosses implemented.
- **HUD:** Godot-based UI showing Level and HP.

## Next Development Steps
1. **Character Classes:** Implement unique stats and skills for different roles (Archer, Knight, Mage) using Godot scenes.
2. **Persistent Saves:** Save Level/EXP/Weapon to a simple file or DB.
3. **Audio:** Add sound effects using Godot's `AudioStreamPlayer`.
4. **UI Polishing:** Implement inventory management and leaderboard UI in Godot.
