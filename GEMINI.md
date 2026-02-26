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
    - Players gain 20 EXP per enemy kill, 100 EXP per spawner kill, and 1000 EXP per boss kill.
    - Leveling up restores health and increases `MaxHealth` by 20.
    - Visualized in the HUD via a yellow EXP bar and Gold dots for the Level counter.
- **Weapon System:**
    - Four weapon types: `Single`, `Double` (2 shots), `Spread` (3 shots fan), and `Rapid` (High fire rate).
    - Pickups drop from enemies (10% chance) to upgrade weapon tiers.
- **Multi-Stage Bosses:**
    - Large 128x128 authoritative bosses implemented on the server.
    - **Phase 1 (100-66% HP):** Slow movement, triple aimed shots.
    - **Phase 2 (66-33% HP):** Faster movement, 12-way radial bursts.
    - **Phase 3 (33-0% HP):** Erratic/Spiral movement, rapid-fire spiral attack patterns. Flashes red on client.
    - **Boss HUD:** Large purple health bar appears at the top of the screen when a boss is active.
- **World & Environment:**
    - 100x100 tile world with `Grass`, `Water`, and `Wall`.
    - Procedurally generated from a shared seed.
    - Wall collisions for all entities (Players, Enemies, Bosses, Bullets).
- **Visuals & UI:**
    - **High-Detail Atlas:** 256x256 procedurally generated texture with detailed character, environment, and boss sprites.
    - **HUD:** Shows Health bar, EXP bar, Level dots, current Weapon icon, and Boss HP bar.
    - **Minimap:** Functional 200x200 minimap showing all active entities and terrain.
    - **Camera:** Follows local player and transforms screen coordinates to world space for accurate mouse shooting.

## Next Development Steps
1. **Dungeon System:** Transition from a single flat world to rooms connected by portals.
2. **Persistent Inventory:** Save player stats and weapon progress between sessions (requires a simple database or file store).
3. **Sound Effects & Particles:** Add audio feedback for shooting/hits and visual effects for explosions and level-ups.
4. **Skills/Classes:** Add different character classes with unique passive abilities or active skills.