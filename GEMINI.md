# Gemini Project Context: LastLight

## Project Overview
LastLight is a real-time multiplayer co-op bullet hell game (inspired by Realm of the Mad God) built using C# and the MonoGame framework.

## Architecture
- **Framework:** MonoGame (targets .NET 9.0)
- **Networking:** LiteNetLib (UDP-based, chosen for high performance and low latency suitable for a bullet hell game)
- **Projects:**
    - `LastLight.Common`: Shared classes, struct definitions, network packets, `WorldManager`, and core enums.
    - `LastLight.Server`: Standalone authoritative C# console application managing state, physics, AI, world generation, item drops, and broadcasting.
    - `LastLight.Client.Core`: Shared game logic, rendering, networking handler, input processing, `Camera` system, and `Sprite Atlas`.
    - `LastLight.Client.Desktop`: Desktop execution wrapper.
    - `LastLight.Client.Android`: Android execution wrapper.

## CRITICAL INVARIANTS (Do Not Break)
### 1. Client-Side Manager Re-binding
When switching rooms (`OnWorldInit`), all local managers (`_enemyManager`, `_spawnerManager`, etc.) are recreated. **Delegates MUST be re-bound** immediately after creation, or incoming network packets will be processed by "dead" managers and entities will be invisible.
```csharp
_networking.OnWorldInit = (init) => {
    _enemyManager = new EnemyManager();
    // RE-BIND IMMEDIATELY
    _networking.OnEnemySpawn = _enemyManager.HandleSpawn;
};
```

### 2. Server-Side Room State Sync
The `SwitchPlayerRoom` method on the server MUST send the complete state of the target room to the player *after* the `WorldInit` packet. This includes all active Portals, Enemies, Spawners, Bosses, and Items. Failure to do this results in players entering empty/broken rooms.

### 3. Procedural Atlas Detail
The `GenerateAtlas` method in `Game1.cs` MUST contain high-detail pixel logic for:
- Player (Helmet, Shield, Sword)
- Enemy (Mean eyes, stitched mouth)
- Environment (Wall bricks, Water waves, Grass tufts)
- Boss (Horns, Big eyes, Mouth)
Never simplify this method into solid colors.

### 4. ID Management
- **Players:** IDs >= 0 (Assigned by LiteNetLib).
- **AI Entities (Hostile):** IDs < 0 (Assigned by Server).
- **Collision Logic:** Players only take damage from bullets where `OwnerId < 0`. AI only take damage from bullets where `OwnerId >= 0`.

### 5. World Style Synchronization
`ServerRoom` MUST store its `GenerationStyle` as a property. The `SwitchPlayerRoom` method on the server MUST NOT guess the style based on room names; it must send the stored `room.Style` in the `WorldInit` packet. The client MUST use `init.Style` to generate its local map. This prevents "Invisible Wall" desyncs.

### 6. Walkable Spawn Enforcement
`SwitchPlayerRoom` MUST execute a loop (e.g., 100 attempts) using `room.World.IsWalkable(testPos)` to find a valid grass/sand tile before updating the player's position. This ensures players never spawn inside walls or water when entering a dungeon.

## Current State
- **Nexus Social Hub:** A 30x30 non-combat room. Contains permanent portals to "Forest Realm" ('F' icon) and "Dungeon Realm" ('D' icon).
- **Multi-Room System:** Full support for isolated room instances with automatic cleanup (30s inactivity timer).
- **Combat:** Server-authoritative movement, shooting, and health. Multi-phase bosses implemented.
- **HUD:** Health, EXP, Level counter, Weapon Icon, and 100x100 Minimap.

## Next Development Steps
1. **Character Classes:** Implement unique stats and skills for different roles (e.g. Archer, Knight, Mage).
2. **Persistent Saves:** Save Level/EXP/Weapon to a simple file or DB.
3. **Audio:** Add sound effects for shooting, hits, and room transitions.