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

## Current State
- **Nexus Social Hub:** A 30x30 non-combat room. Contains permanent portals to "Forest Realm" ('F' icon) and "Dungeon Realm" ('D' icon).
- **Multi-Room System:** Full support for isolated room instances with automatic cleanup (30s inactivity timer).
- **Combat:** Server-authoritative movement, shooting, and health. Multi-phase bosses implemented.
- **HUD:** Health, EXP, Level counter, Weapon Icon, and 100x100 Minimap.

## Next Development Steps
1. **Character Classes:** Implement unique stats and skills for different roles (e.g. Archer, Knight, Mage).
2. **Persistent Saves:** Save Level/EXP/Weapon to a simple file or DB.
3. **Audio:** Add sound effects for shooting, hits, and room transitions.


## Dev Status

> Rules: AI Should never touch this. **HUMANS ONLY**!

### Critical Issues

These are things reported by the `/code-review` command which should be run after dev is done, but prior to committing code. You can put plans here as well if you have them. This section is subject to change frequently and should help the AI focus on a specific task.

- Client / Server Mismatch:
    - Client Boss.cs The reason Boss.cs still exists in the client is that we have unified the Server architecture, but we haven't yet unified the Client visuals. On the Server, we replaced the specific ServerBoss class with a generic ServerEnemy powered by a PhasedAiDriver. This successfully decoupled the logic (the "Brain"). On the Client, however, we still have two legacy "Visual Classes" (Enemy.cs and Boss.cs) and two separate managers. The Client is still waiting for a server packet like BossSpawn and manually routing it to the BossManager which uses the hardcoded 128x128 drawing logic in Boss.cs.  To align the Client with the new architecture and remove Boss.cs, we need to:
        1. Upgrade Enemy.cs (Client): Add a DataId and Phase property so it can represent any entity from Enemies.json (including bosses).
        2. Data-Driven Drawing: Update the Draw method in Enemy.cs to use the DataId to look up its size and icon key, rather than hardcoding (32, 0, 32, 32).
        3. Merge Managers: Update EnemyManager.cs to handle both EnemySpawn and BossSpawn network packets.
        4. Cleanup: Delete LastLight.Client.Core/Boss.cs and LastLight.Client.Core/BossManager.cs.    
    - The plan to address:
        1. Consolidate Packets (LastLight.Common/Models.cs)
            * Create a single EntitySpawn packet (replacing EnemySpawn and BossSpawn).
            * Create a single EntityUpdate packet (replacing EnemyUpdate and BossUpdate).
            * Create a single EntityDeath packet.
        2. Update Server Broadcasting (LastLight.Server/ServerRoom.cs)
            * Update the server to only broadcast the unified Entity packets.
            * The server still knows which is which internally, but it tells the client: "Here is entity -101 with DataId boss_overlord."
        3. Refactor Client (LastLight.Client.Core/)
            * Enemy.cs: Rename to ClientEntity.cs. It will draw itself based on its DataId.
            * EnemyManager.cs: Rename to EntityManager.cs. It will manage one dictionary of ClientEntity objects.
            * Game1.cs: Remove all references to BossManager. Route all entity packets to EntityManager.
        4. Cleanup
            * Delete all "Boss" specific files (Boss.cs, BossManager.cs).
- Deviance List:
    - LastLight.Client.Core/Boss.cs (Lines 30-34), Enemy.cs (Lines 28-32), Player.cs (Lines 43-47): The TakeDamage method is implemented with authoritative logic (CurrentHealth -= amount) on the Client. Client entities should not be subtracting their own health directly, violating the authoritative server networking mandate.
    - LastLight.Common/Abilities/EffectProcessor.cs (Line 47): target.TakeDamage((int)value, source); was added. If the client runs effect processing for visual prediction, it will invoke the local TakeDamage method and cause an authoritative state change on the Client.
    - LastLight.Server/ServerNetworking.cs (Line ~304): In Update logic, BossUpdate network packet hardcodes Phase = 1 (new BossUpdate { ... Phase = 1 }). Since ServerEnemy doesn't track phases anymore, this will silently break client-side boss phase visualizations or mechanics.
- Performance Flag:
    - LastLight.Server/ServerEnemyManager.cs (Line 76): var keys = _logicTimers.Keys.ToList(); inside ServerEnemy.Update creates a new List allocation every frame for every active enemy. This is a massive MonoGame red flag that will cause severe GC pressure and frame stuttering.
    - LastLight.Server/ServerEnemyManager.cs (Line 186): foreach (var enemy in _enemies.Values.ToList()) inside ServerEnemyManager.Update creates an additional unneeded list allocation on every single server frame.

## Feature Roadmap

List of features and their status (pending, in progress, completed)

| Feature | Status | Details |
|---|---|---|
| Item System | COMPLETE | Groundwork for items with static data (`ItemData`) and instance data (`ItemInfo`) |
| Window Focus | COMPLETE | Desktop client responds to inputs even when not in focus (e.g. mouse clicks fire bullets while browsing the web) |
| Test Music | COMPLETE | Play some login music POC. Note the music is my own creation.  |
| Asset Pipeline | COMPLETE | Create a tool that can pack assets. There should be one source of truth in Client Core. Asset packing needs to a) Copy static resources (don't need any special handling), sounds, music to Content Core. It needs to generate texture atlas and sprite sheet for things that need packing. These need to be auto-inserted into the MGCB file. |
| Mana System | COMPLETE | Player should have an ability to generate mana. Mana should be handled by server and broadcast to only one client. UI shuold have mana bar. |
| Ability System | COMPLETE | Basic data templating that describes abilities that can be used by players and monsters. These abilities can be attached to Equipment (players) or assigned to an Enemy Template |
| Enemy Templates | COMPLETE | Configuration driven monster stats and AI behaviors. Templates that correspond to code to allow **ALL** existing functionality. Expands on existing system. Incorporates ability system. |
| Weapons | COMPLETE | Create the `Weapon` type Item. Should contain a primary ability. Add to configuration and code. | 
| Standardize Spec Docs | COMPLETE | All 8 major specifications have been reformatted into a standardized 5-part anatomical structure for better AI/Human readability. |
| Gemini Configuration | In Progress | Add standardized behavioral instructions by leveraging the .gemini/GEMINI.md convention. Add skills and commands to facilitate code reviews. Move statuses to *this* file.  |
| Enemy / Boss Logic | In Progress | Unify and consolidate enemies vs. bosses. They are the same server entity. They are governed by AIDrivers. Add a flag to enemies that indicate boss vs. paragon vs. normal mob. We need a flag to tell us that the enmey is a boss so we can do special behaviors like display a boss health bar. Should update the ENEMIES_SPEC.md to include an "enemy_type" with values like "boss", "mini", "enemy" |
| Add Weapon Tiering | Pending | Create interface for player to upgrade weapons |
| Add Gold | Pending | Drop gold on ground. Should disappear after dungeon is exited. Used for unlocking equipment upgrades during a run. Used for opening chests at end of run. Should have weight to it so players have to choose between potential loot or opening the loot they already have. Also should be balanced against loadout |
| Draft Stats System Spec | Pending | More HP, Mana, Luck, Range, Move Speed, Carrying Capacity (No spec for this yet) |
| Animation Framework | Pending | Add animation system where assets integrated into asset pipeline. Need sprite sheets and atlases. Can have a simple 1 frame animation. Assign this to enmeies. |
| Gloves | Pending | Add to the Item System, data definition in json. Need at least one sample item. |
| Helmet | Pending | Add to the Item System, data definition in json. Need at least one sample item. |
| Body Armor | Pending | Add to the Item System, data definition in json. Need at least one sample item. |
| Boots | Pending | Add to the Item System, data definition in json. Need at least one sample item. |
| Equipment Slots and Ability Buttons | Pending | Need ability buttons on the UI. Need 4 of them for the 4 actives. Need to somehow display the passive / proc.  |
| Stash system | Pending | Inventory accessed only from the waiting room |
| Skill System | Pending | Implement Skills, xp system and integration with combat damage / abilities |

## Spec Status

The state of the specifications. All technical specifications now follow a standardized 5-part anatomical structure (Overview, Design Goals, Data Specification, Technical Implementation, and Examples).

1. ABILITY_SPEC.md
    * Implementation Status: **Architecture Implemented.** Core "Vehicle & Payload" engine and Projectile delivery are operational. 
    * Pending Work: Field delivery is missing; Beam, Instant, and Contact types are stubbed. `remove_status` logic is not yet in the EffectProcessor.
    * Spec Completeness: Complete & Thorough (v4.2). Defines exact JSON schemas, execution priorities, and visual prediction walkthroughs.
2. ASSET_SPEC.md
    * Implementation Status: **Fully Implemented.** The `pack-assets` tool and MSBuild deployment targets are operational.
    * Spec Completeness: Complete (v1.6). Defines transformation rules, usage flow, and the recommended `AssetManager` client abstraction.
3. EFFECT_TEMPLATE_SPEC.md
    * Implementation Status: **Fully Implemented.** Serves as the lookup metadata for client-side visuals. 
    * Note: This spec is closely coupled with ABILITY_SPEC and provides the visual mapping for the `template_id` field.
4. ENEMIES_SPEC.md
    * Implementation Status: **100% Implemented.** The Polymorphic AI Driver architecture (Standard/Phased) and Driver Factory are fully operational.
    * Spec Completeness: Complete (v2.0). Defines the Brain/Body/Memory decoupling and C# method signatures.
5. GAME_SPEC.md
    * Implementation Status: **Conceptual / In Progress.** Dungeons and Equipment loadouts are operational.
    * Pending Work: Streaks, Crafting, and advanced Skill point formulas are design goals (TODO).
    * Spec Completeness: High-Level Source of Truth. Contains placeholders for endgame systems and economy balance.
6. ITEM_SPEC.md
    * Implementation Status: **Architecture Implemented.** Blueprint (`ItemData`) and Instance (`ItemInfo`) split is operational.
    * Pending Work: Consumable cooldown/buff logic is stubbed. Persistence needs optimization (currently repeats blueprint data).
    * Spec Completeness: Partially Complete. Contains detailed data for 84+ equipment/consumable rows but has design gaps in T5 Mastery.
7. NETWORK_SPEC.md
    * Implementation Status: **Fully Implemented.** 20Hz sync, visibility tiers, and movement/combat prediction protocol is operational.
    * Spec Completeness: Complete (v1.0). Defines the transport layer (LiteNetLib) and security mandates.
8. SKILL_SPEC.md
    * Implementation Status: **Design WIP.**
    * Pending Work: Most skills are in the design spreadsheet phase. Technical hooks for tag-based mutation aggregation in the engine are pending.
    * Spec Completeness: Incomplete. Contains significant "TODO" design data for Tier 4/5 perks.