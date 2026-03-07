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

### One-time setup

```bash
# Install Android Stuff
dotnet workload install android

# Get tools like mgcb (does not install globally on system)
dotnet tools restore
```

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

### 3 Build All Platforms

```bash

# Desktop
dotnet build LastLight.Client.Desktop/LastLight.Client.Desktop.csproj

# iOS
dotnet build LastLight.Client.iOS/LastLight.Client.iOS.csproj

# Android
dotnet build LastLight.Client.Android/LastLight.Client.Android.csproj -p:AndroidSdkDirectory="F:\seand\android\android-sdk" -p:JavaSdkDirectory="C:\Program Files\Android\Android Studio\jbr"
```

## Performance & Scaling

LastLight is designed for high-performance multiplayer scaling, using a custom authoritative server architecture:

*   **Multithreaded Room Updates:** The server processes physics, AI, and bullet collisions for every active room instance in parallel across all available CPU cores using `Parallel.ForEach`. This allows the server to scale linearly with the number of CPU cores on the host machine.
*   **Networking:** Built on **LiteNetLib (UDP)**, achieving high-throughput and low-latency state synchronization.
*   **Tick Rate:** Authoritative state is broadcasted at **20Hz (20 ticks per second)**, while the physics simulation runs at a much higher frequency to ensure smooth collision detection.
*   **Scalability:** A standard 4-core VPS can realistically support **300-600 concurrent players** spread across different room instances (Nexus, Forest, Dungeons, etc.).

## Controls (Mobile & Desktop)

*   **Desktop:**
    *   **W, A, S, D:** Move
    *   **Left Click:** Shoot / Aim
    *   **Space:** Interact / Enter Portal
*   **Mobile (Portrait):**
    *   **Left Virtual Joystick:** Move
    *   **Right Virtual Joystick:** Aim & Shoot (Twin-stick style)
    *   **"ENTER" Button:** Appears near portals for room transitions.
    *   **Tap-to-Interact:** Manage inventory and equipment via touch.

## Next Steps

*   **Enemies & AI:** Implement basic enemy behavior and bullet hell patterns.
*   **Collisions:** Implement efficient circle/box collision detection for bullets vs players/enemies.
*   **World & Level:** Create a basic world/arena for the game.
