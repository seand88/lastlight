# Gemini Project Context: LastLight

## Project Overview
LastLight is a real-time multiplayer co-op bullet hell game (inspired by Realm of the Mad God) built using C# and the MonoGame framework.

## Architecture
- **Framework:** MonoGame (targets .NET 9.0)
- **Networking:** LiteNetLib (UDP-based, chosen for high performance and low latency suitable for a bullet hell game)
- **Projects:**
    - `LastLight.Common`: Shared classes, struct definitions, network packets, and `WorldManager`.
    - `LastLight.Server`: Standalone authoritative C# console application managing state, world generation, and broadcasting updates at 20 ticks per second (now using high-precision `Stopwatch`).
    - `LastLight.Client.Core`: Shared game logic, rendering, networking handler, input processing, and `Camera` system.
    - `LastLight.Client.Desktop`: Desktop execution wrapper.
    - `LastLight.Client.Android`: Android execution wrapper.

## Current State & History
- **Initial Setup:** The workspace was completely wiped and re-scaffolded due to a previous incomplete state.
- **Networking Implemented:** `LiteNetLib` 2.0.2 is installed. The server handles connections, assigns IDs, tracks player state, and broadcasts updates.
- **Server Authoritative Movement:** Upgraded from client-authoritative to server-authoritative movement.
    - **Client:** Sends `InputRequest` (DeltaTime, Movement vector, Sequence Number) via Unreliable UDP whenever input changes. Uses Client-Side Prediction to move locally instantly, maintaining a list of `PendingInputs`.
    - **Server:** Receives inputs, simulates physics (calculates true position based on `MoveSpeed`), and stores the authoritative state and the `LastProcessedInputSequence`. Broadcasts `AuthoritativePlayerUpdate`.
    - **Reconciliation:** When the client receives the `AuthoritativePlayerUpdate`, it "snaps" to the server's position, discards acknowledged inputs, and re-simulates the remaining unacknowledged `PendingInputs`.
- **Server Authoritative Bullets & Collisions:**
    - **Client:** Uses prediction to spawn a bullet locally immediately, then sends a `FireRequest` (with bullet ID and direction) reliably to the server.
    - **Server:** Tracks bullets using `ServerBulletManager`. Upon receiving a `FireRequest`, it spawns the authoritative bullet at the shooter's *server-verified* position and broadcasts a `SpawnBullet` to all clients.
    - **Collisions:** The server runs AABB collision detection in its update loop. When an active bullet overlaps an active player (not the owner), an active enemy, or a wall, it destroys the bullet on the server, calculates damage, and broadcasts a `BulletHit` to all clients so they destroy it locally.
- **Co-op AI Enemies (Server Authoritative):**
    - Introduced `ServerEnemyManager` to handle AI logic on the server.
    - Enemies use a basic AI to constantly find the nearest player and move towards them.
    - **Enemy Attacks:** Enemies now fire bullet hell patterns (currently an 8-way radial burst every 2 seconds). The server uses negative IDs (e.g. -1, -2) for enemies to distinguish them from players. The server handles spawning these bullets and they are colored pink on the client.
- **Player Health & Damage:**
    - Players spawn with 100 health.
    - If hit by an enemy bullet (a bullet with `OwnerId < 0`), they lose 10 health.
    - If health reaches 0, the server automatically respawns the player at the starting coordinate `(400, 300)` and restores their health.
- **World Generation (Server Authoritative):**
    - Shared `WorldManager` in `Common` project.
    - Server generates a 50x50 tile-based world using a seed (`12345`).
    - **Tiles:** `Grass` (Walkable), `Water` (Non-walkable, Shootable), `Wall` (Non-walkable, Non-shootable).
    - **Synchronization:** Server sends a `WorldInit` packet to new clients. Clients generate the exact same world locally using the same seed.
    - **Collisions:** Server enforces wall collisions for player movement, enemy movement, and bullet travel. Client predicts wall collisions for local movement.
- **Enhanced Camera System:**
    - Implemented a `Camera` class in `Client.Core`.
    - The camera automatically follows the local player's position.
    - Added coordinate transformation logic so mouse-clicks (screen coordinates) are correctly translated to world coordinates for shooting.
    - The `Draw` loop now uses a transformation matrix, allowing for a world much larger than the screen dimensions.

## Next Development Steps
1. **Refined UI/Graphics:** Add real sprites, particle effects, and a proper UI HUD.
2. **More Enemy Types:** Introduce different AI behaviors, speeds, and bullet patterns.
3. **Inventory/Item System:** Add drops from enemies and spawners (e.g., weapon upgrades, health potions).
