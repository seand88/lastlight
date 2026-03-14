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

## Tech Specs

Refer to these:

| Doc | Description |
|---|---|
| [ASSET_SPEC.md](./Docs/ASSET_SPEC.md)| How to work with and incorporate assets like images, textures, sprite sheets, audio, animations |
| [ABILITY_SPEC.md](./Docs/ABILITY_SPEC.md) | How abilities are defined and implemented. Includes weapon attacks, utlity, passives and procs. Also covers damage types and several combat topics. |
| [ENEMIES_SPEC.md](./Docs/ENEMIES_SPEC.md) | How to define new enemies in the game. Includes their attacks, AI behavior, hitpoints, etc. |
| [GAME_SPEC.md](./Docs/GAME_SPEC.md) | General gameplay loop and systems overview. Goes into depth for topics like dungeons, instances, tier upgrades, the loot system, risk vs. reward. |
| [ITEM_SPEC.md](./Docs/ITEM_SPEC.md) | Item types, functionality and how to implement in data. |
| [NETWORK_SPEC.md](./Docs/NETWORK_SPEC.md) | Packets between server and client. How states are represented over network. Frequency. Etc. |
| [SKILL_SPEC.md](./Docs/SKILL_SPEC.md) | Skills design. |

## Gemini AI Assistant Integration

This project is configured to work natively with the **Gemini CLI**, providing AI-assisted development, architectural auditing, and code review directly in the terminal.

The integration relies on a combination of context files to guide the AI, and a suite of custom commands and skills to execute project-specific tasks.

### 1. Context Files (Guiding the AI)
The AI assistant reads specific markdown files to understand the rules and state of the codebase before it takes action:

*   **`/GEMINI.md` (Root Level):** The primary instruction manual for the AI. It contains critical system invariants (e.g., "how client-side managers must be re-bound", "how IDs are assigned"), the current state of the project, and strict rules the AI must never break when writing or modifying code.
*   **`/.gemini/GEMINI.md` (Settings Level):** Defines the "Identity" of the project, maps available AI skills to their locations, and establishes global development policies (e.g., "All damage must be processed in EffectProcessor.cs", "1 Tile = 32 pixels").

### 2. Skills and Commands Structure
The `.gemini` folder houses reusable, project-specific AI tools:
*   **Skills (`/.gemini/skills/`):** Specialized "personas" or expert instruction sets for the AI. For example, a skill might tell the AI how to act as a Senior Game Developer reviewing bullet-hell performance.
*   **Commands (`/.gemini/commands/`):** TOML files that map standard CLI slash commands (e.g., `/code-review`) to specific AI prompts and Skill activations, making them easy for human developers to trigger.

### 3. Using Custom Commands
If you have the Gemini CLI installed, you can run the following project-specific commands in your terminal:


#### Code Review
Activates the `code-reviewer` skill to analyze your uncommitted local changes against the remote repository. It focuses on memory safety, network desyncs, and MonoGame performance red flags (like list allocations in `Update` loops). **This generates a nice commit message**. 
> NOTE: Always run this before committing!
*   **Usage:** `/code-review`
*   **With context:** `/code-review Focus specifically on the changes I made to the Boss targeting logic.`

#### Specification Audit
Activates the `spec-auditor` skill to cross-reference a specific implementation file against the official design specifications in the `/Docs/` folder. It ensures the code perfectly matches the documented design.
*   **Usage:** `/spec-audit <path-to-file>`
*   **Example:** `/spec-audit LastLight.Server/ServerEnemyManager.cs`

#### Custom Personas

Use `/clear` often to keep Gemini fresh. Long contexts result in poor overall performance and degraded accuracy. Adopt the pattern of resetting context and assigning a persona to solve focused tasks. Here are some personas:

```sh
# Reset Gemini Context
/clear

# An expert Monogame Engine Architect
/xp-mg 

# A LiteNetLib Expert
/xp-nc

# A Systems Architect (Multi-Threaded)
/xp-sa

# MMORPG Combat Designer
/xp-sd
```

### Direct Skill Activation
Under the hood, these commands activate specific agent skills. You can also invoke these skills naturally in standard conversation with the Gemini CLI:
*   *"Can you activate the **`code-reviewer`** skill and look at my latest commit?"*
*   *"Please use the **`spec-auditor`** skill to check if `EffectProcessor.cs` still matches the `ABILITY_SPEC.md`."*

### .gemini/settings.json Configuration Breakdown

The `.gemini/settings.json` file is the configuration manifest for the Gemini CLI. It defines how the assistant interacts with your project and which model drives the reasoning for your code audits and spec checks.

```json
{
  "general": {
    "previewFeatures": true
  },
  "model": {
    "name": "gemini-3.1-pro-preview"
  }
}

```

#### Parameters

##### 1. `general.previewFeatures` (`true`)
Enables experimental functionality not yet in the stable release. For a modular project like **LastLight**, this is vital for:
* **Advanced Workspace Indexing:** Allows the AI to map relationships across your different project folders and the `/Docs/` directory.
* **Modular Skill Loading:** Necessary for the CLI to correctly parse the nested `.gemini/skills/` directory structure and custom `.toml` command macros.

##### 2. `model.name` (`"gemini-3.1-pro-preview"`)
Specifies the exact AI model version used for all interactions.
* **gemini-3.1-pro-preview:** The Pro-tier model is designed for high-reasoning tasks, making it capable of enforcing your "Vehicle and Payload" patterns without losing context.
* **gemini-3.1-pro-flash:** Lightweight, fast. Good for quick fixes.
* **gemini-2.5-pro:** Don't use this if you can help it. It's slow, old and outlcassed by even 3.1-flash.

