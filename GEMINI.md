# Gemini Project Context: LastLight

## Project Overview
LastLight is a real-time multiplayer co-op bullet hell game (inspired by Realm of the Mad God) built using C# and the MonoGame framework.

## Architecture
- **Framework:** MonoGame (targets .NET 9.0)
- **Networking:** LiteNetLib (UDP-based, chosen for high performance and low latency suitable for a bullet hell game)
- **Projects:**
    - `LastLight.Common`: Shared classes, struct definitions, network packets, and `WorldManager`.
    - `LastLight.Server`: Standalone authoritative C# console application managing state, world generation, and broadcasting updates at 20 ticks per second (now using high-precision `Stopwatch`).
    - `LastLight.Client.Core`: Shared game logic, rendering, networking handler, input processing, `Camera` system, and `Sprite Atlas`.
    - `LastLight.Client.Desktop`: Desktop execution wrapper.
    - `LastLight.Client.Android`: Android execution wrapper.

## Current State & History
- **Initial Setup:** The workspace was completely wiped and re-scaffolded due to a previous incomplete state.
- **Networking Implemented:** `LiteNetLib` 2.0.2 is installed. The server handles connections, assigns IDs, tracks player state, and broadcasts updates.
- **Server Authoritative Movement:** Upgraded from client-authoritative to server-authoritative movement with Client-Side Prediction and Reconciliation.
- **Server Authoritative Bullets & Collisions:**
    - **Client:** Uses prediction to spawn a bullet locally immediately, then sends a `FireRequest` (with bullet ID and direction) reliably to the server.
    - **Server:** Tracks bullets using `ServerBulletManager`. Upon receiving a `FireRequest`, it spawns the authoritative bullet at the shooter's *server-verified* position and broadcasts a `SpawnBullet` to all clients.
    - **Collisions:** The server runs AABB collision detection. When an active bullet overlaps an active player (not the owner), an active enemy, or a wall, it destroys the bullet on the server, calculates damage, and broadcasts a `BulletHit`.
- **Co-op AI Enemies & Spawners:**
    - Introduced `ServerEnemyManager` and `ServerSpawnerManager`.
    - **Spawners:** Purple 64x64 blocks that spawn enemies up to a cap (5). Replenish enemies as they die. Guaranteed to spawn on walkable tiles.
    - **Enemies:** Green 32x32 squares. Use basic AI to chase nearest player.
    - **Enemy Attacks:** Enemies fire 8-way radial bursts every 2 seconds. Bullets are Pink on client.
- **Player Health & Damage:** Players have 100 health, take 10 damage from enemy bullets, and respawn at origin upon death.
- **World Generation:** Shared `WorldManager`. Generates a 50x50 tile-based world from a seed. Includes `Grass`, `Water`, and `Wall` tiles.
- **Enhanced Camera System:** Camera follows the local player. Mouse coordinates are transformed to world space for accurate shooting.
- **Procedural Sprite Atlas:**
    - Instead of just colored squares, the game now uses a **128x128 Sprite Atlas** generated at runtime.
    - Entities have visual details (e.g., eyes for players/enemies, stone textures for walls).
    - Tile rendering uses source rectangles from this atlas.
    - This system is ready to be swapped with a real PNG asset pack (like Kenney Tiny Dungeon).

## Next Development Steps
1. **Real Assets:** Replace the `GenerateAtlas()` procedural code with a call to `Content.Load<Texture2D>("tilesheet")` once a real asset is added to the MGCB.
2. **Refined UI/HUD:** Display health, score, and connection status in a screen-space overlay.
3. **Inventory/Item System:** Add drops from enemies and spawners (e.g., weapon upgrades, health potions).
4. **Levels/Progression:** Add multiple spawners and a goal (e.g., kill all spawners to win).