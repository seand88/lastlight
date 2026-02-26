# Gemini Project Context: LastLight

## Project Overview
LastLight is a real-time multiplayer bullet hell game (inspired by Realm of the Mad God) built using C# and the MonoGame framework.

## Architecture
- **Framework:** MonoGame (targets .NET 9.0)
- **Networking:** LiteNetLib (UDP-based, chosen for high performance and low latency suitable for a bullet hell game)
- **Projects:**
    - `LastLight.Common`: Shared classes, struct definitions, and network packets (`JoinRequest`, `InputRequest`, `FireRequest`, `AuthoritativePlayerUpdate`, `SpawnBullet`, `BulletHit`, etc.).
    - `LastLight.Server`: Standalone authoritative C# console application managing state and broadcasting updates at 20 ticks per second.
    - `LastLight.Client.Core`: Shared game logic, rendering, networking handler, and input processing.
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
    - **Collisions:** The server runs collision detection in its update loop. When an active bullet overlaps an active player (not the owner), it destroys the bullet on the server and broadcasts a `BulletHit` to all clients so they destroy it locally.
- **Game Loop:** A basic local player entity exists with WASD movement. Other connected players are represented as red squares, while the local player is white.
- **Compilation:** The project currently builds successfully across `Common`, `Server`, `Client.Core`, and `Client.Desktop`.

## Next Development Steps
1. **Health and Damage:** Players currently get hit by bullets but don't take damage. We need to add health variables to players and deduct health on the server upon `BulletHit`. 
2. **Enemies & AI:** Introduce enemy entities and bullet hell attack patterns (e.g., radial bursts, spirals).
3. **World/Level System:** Create an arena or tile-based world instead of a blank blue background.
