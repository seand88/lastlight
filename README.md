# LastLight

A real-time multiplayer bullet hell game built with MonoGame and LiteNetLib.

## Architecture

*   **LastLight.Common:** Shared models and networking packets (e.g., `Vector2`, `PlayerUpdate`, `SpawnBullet`).
*   **LastLight.Server:** Standalone C# .NET 9.0 console application using LiteNetLib for authoritative state broadcasting and UDP networking.
*   **LastLight.Client.Core:** The shared MonoGame logic, input handling, rendering, and client-side networking.
*   **LastLight.Client.Desktop:** The Windows/macOS/Linux desktop entry point.
*   **LastLight.Client.Android:** The Android entry point.

## Features Currently Implemented

*   **Networking:** Client-server communication with automatic packet serialization via LiteNetLib.
*   **Player Movement:** Local prediction and server-side state broadcasting (20 ticks/sec).
*   **Shooting & Bullets:** High-performance bullet pooling system (up to 2000 active bullets) with networked synchronization of spawns.

## How to Run

### 1. Start the Server

The server must be running before clients can connect.

```bash
dotnet run --project LastLight.Server/LastLight.Server.csproj
```

### 2. Start the Desktop Client

Open a new terminal window and run:

```bash
dotnet run --project LastLight.Client.Desktop/LastLight.Client.Desktop.csproj
```

*You can open multiple terminal windows and run the client command multiple times to simulate multiple players connecting to the same server.*

## Controls

*   **W, A, S, D:** Move
*   **Left Click:** Shoot

## Next Steps

*   **Enemies & AI:** Implement basic enemy behavior and bullet hell patterns.
*   **Collisions:** Implement efficient circle/box collision detection for bullets vs players/enemies.
*   **World & Level:** Create a basic world/arena for the game.
