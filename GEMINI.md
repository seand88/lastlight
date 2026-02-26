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

## Current State & History
- **Initial Setup:** The workspace was completely wiped and re-scaffolded due to a previous incomplete state.
- **Server Authoritative Everything:** 
    - **Movement:** Client sends `InputRequest`, Server simulates and broadcasts `AuthoritativePlayerUpdate`. Uses Client-Side Prediction and Reconciliation.
    - **Bullets:** Client predicts fire, Server validates `WeaponType` and shooter position, spawns bullets, and runs AABB collision detection.
    - **Enemies & Spawners:** Managed entirely on the server. Spawners replenish enemies up to a cap (5) and ensure they spawn on walkable tiles.
- **Dungeon System (Rooms & Portals):**
    - Transitioned from a single world to a **Multi-Room Architecture**.
    - **ServerRoom:** Each room (Nexus, Dungeons) is an isolated instance with its own `WorldManager`, `Enemies`, `Spawners`, `Bosses`, `Items`, and `Bullets`.
    - **Nexus (Room 0):** The main hub using the `Biomes` generation style.
    - **Dungeons:** Generated on-demand using a `Dungeon` generation style (maze-like).
    - **Portals:** Portals can spawn in the world (e.g., dropped by destroyed Spawners or defeated Bosses). Players can interact with them (Space key) to switch rooms.
    - **Networking:** The server tracks which room each player is in and only broadcasts room-specific updates to relevant players.
- **Leveling & EXP System:**
    - Players gain EXP for kills. Leveling up restores health and increases `MaxHealth`.
    - Visualized in the HUD via a yellow EXP bar and Gold dots.
- **Weapon System:**
    - Four weapon types: `Single`, `Double`, `Spread`, and `Rapid`.
    - Pickups drop from enemies to upgrade weapon tiers.
- **Visuals & UI:**
    - **High-Detail Atlas:** 256x256 procedurally generated texture with detailed character, environment, and portal sprites.
    - **Minimap:** Functional 100x100 minimap showing room-specific terrain and entities.
    - **Camera:** Follows local player and transforms screen coordinates to world space.

## Next Development Steps
1. **Persistent Inventory:** Save player stats and weapon progress between sessions (requires a simple database or file store).
2. **Sound Effects & Particles:** Add audio feedback for shooting/hits and visual effects for explosions and level-ups.
3. **Skills/Classes:** Add different character classes with unique passive abilities or active skills (e.g., Dash, Shield).
4. **Party System:** Allow players to form groups to enter dungeon instances together.
