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
- **Leveling & EXP System:**
    - Players gain 20 EXP per enemy kill and 100 EXP per spawner kill.
    - Leveling up restores health and increases `MaxHealth` by 20.
    - Visualized in the HUD via a yellow EXP bar and Gold dots for the Level counter.
- **Weapon System:**
    - Four weapon types: `Single`, `Double` (2 shots), `Spread` (3 shots fan), and `Rapid` (High fire rate).
    - Enemies have a 10% chance to drop a `WeaponUpgrade` item.
- **World & Environment:**
    - 100x100 tile world with `Grass`, `Water`, and `Wall`.
    - Procedurally generated from a shared seed.
    - Features 15 active portals (Spawners) scattered across the map.
- **Visuals & UI:**
    - **High-Detail Atlas:** 256x256 procedurally generated texture with detailed character and environment sprites.
    - **HUD:** Shows Health bar, EXP bar, Level dots, and current Weapon icon.
    - **Minimap:** Functional 200x200 minimap in the top right showing world layout and all active entities.
    - **Camera:** Follows local player and transforms screen coordinates to world space for accurate mouse shooting.

## Next Development Steps
1. **Multi-Stage Bosses:** Create larger, more complex enemies with health phases and changing bullet patterns.
2. **Persistent Inventory:** Save player stats and weapon progress between sessions (requires a simple database or file store).
3. **Sound Effects & Particles:** Add audio feedback for shooting/hits and visual effects for explosions and level-ups.
4. **Enhanced World Gen:** Add different biomes or rooms instead of a uniform random grid.
