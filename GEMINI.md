# Gemini Project Context: LastLight

## Project Overview
LastLight is a real-time multiplayer bullet hell game (inspired by Realm of the Mad God) built using C# and the MonoGame framework.

## Architecture
- **Framework:** MonoGame (targets .NET 9.0)
- **Networking:** LiteNetLib (UDP-based, chosen for high performance and low latency suitable for a bullet hell game)
- **Projects:**
    - `LastLight.Common`: Shared classes, struct definitions, and network packets (`JoinRequest`, `PlayerUpdate`, `SpawnBullet`, etc.).
    - `LastLight.Server`: Standalone authoritative C# console application managing state and broadcasting updates at 20 ticks per second.
    - `LastLight.Client.Core`: Shared game logic, rendering, networking handler, and input processing.
    - `LastLight.Client.Desktop`: Desktop execution wrapper.
    - `LastLight.Client.Android`: Android execution wrapper.

## Current State & History
- **Initial Setup:** The workspace was completely wiped and re-scaffolded due to a previous incomplete state.
- **Networking Implemented:** `LiteNetLib` 2.0.2 is installed. The server handles connections, assigns IDs, tracks player state, and broadcasts updates. The client connects on port 5000, sends its position/velocity updates (unreliable), and processes updates from other clients.
- **Game Loop:** A basic local player entity exists with WASD movement. Other connected players are represented as red squares, while the local player is white.
- **Shooting Mechanics:** Implemented a `BulletManager` with a robust object pool (up to 2000 active bullets) to avoid garbage collection hitches. The local client predicts its bullets immediately and sends a reliable `SpawnBullet` packet to the server to broadcast to all other clients.
- **Compilation:** The project currently builds successfully across `Common`, `Server`, `Client.Core`, and `Client.Desktop`. (Note: Android build currently requires Android SDK setup on the host machine).

## Next Development Steps
1. **Collision Detection:** Need to implement efficient circle-based or AABB collision detection for bullets hitting players and enemies.
2. **Server Authority:** Currently, clients dictate their position and spawn their own bullets. The server needs to transition to full authority (validating movement, hit detection, and health management).
3. **Enemies & AI:** Introduce enemy entities and bullet hell attack patterns (e.g., radial bursts, spirals).
4. **World/Level System:** Create an arena or tile-based world instead of a blank blue background.
