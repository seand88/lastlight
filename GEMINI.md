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

## CRITICAL INVARIANTS (Do Not Break)
### 1. Client-Side Manager Re-binding
When switching rooms (`OnWorldInit`), all local managers (`_enemyManager`, `_spawnerManager`, etc.) are recreated. **Delegates MUST be re-bound** immediately after creation, or incoming network packets will be processed by "dead" managers and entities will be invisible.
```csharp
_networking.OnWorldInit = (init) => {
    _enemyManager = new EnemyManager();
    // RE-BIND IMMEDIATELY
    _networking.OnEnemySpawn = _enemyManager.HandleSpawn;
};
```

### 2. Server-Side Room State Sync
The `SwitchPlayerRoom` method on the server MUST send the complete state of the target room to the player *after* the `WorldInit` packet. This includes all active Portals, Enemies, Spawners, Bosses, and Items. Failure to do this results in players entering empty/broken rooms.

### 3. Procedural Atlas Detail
The `GenerateAtlas` method in `Game1.cs` MUST contain high-detail pixel logic for:
- Player (Helmet, Shield, Sword)
- Enemy (Mean eyes, stitched mouth)
- Environment (Wall bricks, Water waves, Grass tufts)
- Boss (Horns, Big eyes, Mouth)
Never simplify this method into solid colors.

### 4. ID Management
- **Players:** IDs >= 0 (Assigned by LiteNetLib).
- **AI Entities (Hostile):** IDs < 0 (Assigned by Server).
- **Collision Logic:** Players only take damage from bullets where `OwnerId < 0`. AI only take damage from bullets where `OwnerId >= 0`.

### 5. World Style Synchronization
`ServerRoom` MUST store its `GenerationStyle` as a property. The `SwitchPlayerRoom` method on the server MUST NOT guess the style based on room names; it must send the stored `room.Style` in the `WorldInit` packet. The client MUST use `init.Style` to generate its local map. This prevents "Invisible Wall" desyncs.

### 6. Walkable Spawn Enforcement
`SwitchPlayerRoom` MUST execute a loop (e.g., 100 attempts) using `room.World.IsWalkable(testPos)` to find a valid grass/sand tile before updating the player's position. This ensures players never spawn inside walls or water when entering a dungeon.

### 7. Manual Packet Cloning (Client)
LiteNetLib's `SubscribeReusable` overwrites the same object for incoming packets. When storing entities in dictionaries (Portals, Enemies, etc.), the client MUST manually clone the packet data into a new instance inside the lambda. Failure to do so will cause entities to disappear or overwrite each other.
```csharp
_packetProcessor.SubscribeReusable<PortalSpawn>((r) => {
    _portals[r.PortalId] = new PortalSpawn { ... clone fields ... };
});
```

## Feature Roadmap

List of features and their status (pending, in progress, completed)

| Feature | Status | Details |
|---|---|---|
| **Nexus Social Hub** | COMPLETE | A 30x30 non-combat room. Contains permanent portals to "Forest Realm" ('F' icon) and "Dungeon Realm" ('D' icon). |
| **Multi-Room System** | COMPLETE | Full support for isolated room instances with automatic cleanup (30s inactivity timer). |
| **Combat** | COMPLETE | Server-authoritative movement, shooting, and health. Multi-phase bosses implemented. |
| **HUD** | COMPLETE | Health, EXP, Level counter, Weapon Icon, and 100x100 Minimap. |
| **Persistent Saves** | COMPLETE | Save Level/EXP/Weapon to a simple file or DB. |
| **Audio** | COMPLETE | Add sound effects for shooting, hits, and room transitions. |
| Character Classes | Pending | Implement unique stats and skills for different roles (e.g. Archer, Knight, Mage). |
| **Item System** | COMPLETE | Groundwork for items with static data (`ItemData`) and instance data (`ItemInfo`) |
| **Window Focus** | COMPLETE | Desktop client responds to inputs even when not in focus (e.g. mouse clicks fire bullets while browsing the web) |
| **Test Music** | COMPLETE | Play some login music POC. Note the music is my own creation.  |
| **Asset Pipeline** | COMPLETE | Create a tool that can pack assets. There should be one source of truth in Client Core. Asset packing needs to a) Copy static resources (don't need any special handling), sounds, music to Content Core. It needs to generate texture atlas and sprite sheet for things that need packing. These need to be auto-inserted into the MGCB file. |
| **Mana System** | COMPLETE | Player should have an ability to generate mana. Mana should be handled by server and broadcast to only one client. UI shuold have mana bar. |
| **Ability System** | COMPLETE | Basic data templating that describes abilities that can be used by players and monsters. These abilities can be attached to Equipment (players) or assigned to an Enemy Template |
| **Enemy Templates** | COMPLETE | Configuration driven monster stats and AI behaviors. Templates that correspond to code to allow **ALL** existing functionality. Expands on existing system. Incorporates ability system. |
| **Weapons** | COMPLETE | Create the `Weapon` type Item. Should contain a primary ability. Add to configuration and code. | 
| **Standardize Spec Docs** | COMPLETE | All 8 major specifications have been reformatted into a standardized 5-part anatomical structure for better AI/Human readability. |
| **Gemini Configuration** | COMPLETE | Add standardized behavioral instructions by leveraging the .gemini/GEMINI.md convention. Add skills and commands to facilitate code reviews. Gemini should always explain each edit it makes before the edit. Gemini should always check specs before editing. No spec. No edit.  |
| **Enemy / Boss Logic** | COMPLETE | Expand, unify, and consolidate enemies vs. bosses. They are the same server entity. They are governed by AIDrivers. Add a flag to enemies that indicate boss vs. paragon vs. normal mob. We need a flag to tell us that the enmey is a boss so we can do special behaviors like display a boss health bar. Should update the ENEMIES_SPEC.md to include an "enemy_type" with values like "boss", "mini", "enemy" |
| **Animation Framework** | COMPLETE | Add animation system where assets integrated into asset pipeline. Need sprite sheets and atlases. Can have a simple 1 frame animation. Assign this to enmeies. |
| **Asset Management System** | COMPLETE | Uniform system with an easy to use API for loading SoundEffects, Songs, Texture2Ds, and animations. These methods should be responsible for loading and caching - NOT managing lifecycle of looped effects or drawing effects. Also need to update our asset structure so we can standardize asset names and formats (e.g. animation.png + animation_map.json) for each animation in it's own Entity subdir |
| **World Renderer** | COMPLETE | Move rendering logic out of Game1.cs into it's own class. This will heavily use of the Asset Management System |
| **Animation Tool Pipeline** | COMPLETE | Need a system for building animation sprite sheets and frame map data files. If we use a standard naming convention we can have individual frame files that can joined into animations and packed into an atlas. Cons: That is a LOT of little fiels. Pros: Easier to add more elements to an existing sprite sheet. Don't have to manage the sprite sheets yourself. |
| Add Weapon Tiering | Pending | Create user interface, server logic, and data structures for player to upgrade weapons during dungeon-runs using gold |
| Add Gold | Pending | Drop gold on ground. Should disappear after dungeon is exited. Used for unlocking equipment upgrades during a run. Used for opening chests at end of run. Should have weight to it so players have to choose between potential loot or opening the loot they already have. Also should be balanced against loadout |
| Gloves | Pending | Add to the Item System, data definition in json. Need at least one sample item. |
| Helmet | Pending | Add to the Item System, data definition in json. Need at least one sample item. |
| Body Armor | Pending | Add to the Item System, data definition in json. Need at least one sample item. |
| Boots | Pending | Add to the Item System, data definition in json. Need at least one sample item. |
| Equipment Slots and Ability Buttons | Pending | Need ability buttons on the UI. Need 4 of them for the 4 actives. Need to somehow display the passive / proc.  |
| Stash / Loot / Unlock system | Pending | Expand on existing inventory such that: 1) Player has an instanced "loot" inventory where he stores locked chests collected during dungeon run. 2) At end of run player can unlock chests with gold earned from run. 3) Unlocked items go to permanent stash. 4) Toolbelt for consumables needs its own collection. 5) Check equipment, think we need 5 slots here. |
| Skill System | Pending | Implement Skills, xp system and integration with combat damage / abilities |
| Draft Stats System Spec | Pending | More HP, Mana, Luck, Range, Move Speed, Carrying Capacity (No spec for this yet) |