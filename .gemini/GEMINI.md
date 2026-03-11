# LastLight: Project Configuration & Skill Manifest

## Project Identity
- **Engine:** MonoGame (.NET 8.0).
- **Genre:** 2D Top-Down MMORPG / Bullet Hell.
- **Architecture:** Authoritative Server, Client Visual Prediction, Shared "Common" logic.

## Architecture
- **Framework:** MonoGame (targets .NET 9.0)
- **Networking:** LiteNetLib (UDP-based, chosen for high performance and low latency suitable for a bullet hell game)
- **Projects:**
    - `LastLight.Common`: Shared classes, struct definitions, network packets, `WorldManager`, and core enums.
    - `LastLight.Server`: Standalone authoritative C# console application managing state, physics, AI, world generation, item drops, and broadcasting.
    - `LastLight.Client.Core`: Shared game logic, rendering, networking handler, input processing, `Camera` system, and `Sprite Atlas`.
    - `LastLight.Client.Desktop`: Desktop execution wrapper.
    - `LastLight.Client.Android`: Android execution wrapper.
- **Specs:**
    - `../Docs/ASSET_SPEC.md`: How to work with and incorporate assets like images, textures, sprite sheets, audio, animations
    - `../Docs/ABILITY_SPEC.md`: How abilities are defined and implemented. Includes weapon attacks, utlity, passives and procs. Also covers damage types and several combat topics.
    - `../Docs/ENEMIES_SPEC.md`: How to define new enemies in the game. Includes their attacks, AI behavior, hitpoints, etc.
    - `../Docs/GAME_SPEC.md`: General gameplay loop and systems overview. Goes into depth for topics like dungeons, instances, tier upgrades, the loot system, risk vs. reward.
    - `../Docs/ITEM_SPEC.md`: Item types, functionality and how to implement in data.
    - `../Docs/NETWORK_SPEC.md`: Packets between server and client. How states are represented over network. Frequency. Etc.
    - `../Docs/SKILL_SPEC.md`: Skills design.

## Command & Skill Binding
- **Skill Root:** `.gemini/skills/`
- **Active Skills:**
    - `code-reviewer`: Professional Architect persona for code-level diff analysis.
    - `spec-auditor`: Architect persona for Doc-to-Code consistency.
- **Commands:** Map `/code-review` and `/spec-audit` to their respective skill subdirectories.

## Global Development Policies
- **Authoritative Rule:** All damage/heals must be processed in `Common/Abilities/EffectProcessor.cs`.
- **Input Rule:** Direct `Mouse.GetState()` is forbidden in game logic; use the centralized "Safe" objects.
- **Scaling:** 1 Tile = 32 pixels. All `range_tiles` logic must reflect this.
- **Never Touch:** `../GEMINI.md` in the root folder. **DO** use it as a reference for project-specifics and current status. It is maintained by **HUMANS**.
- **Content Integrity:** - **Zero-Loss Rule on Specs in `./Docs`:** Strictly prohibited from removing sections, code blocks, or data tables during reformatting.
    - **Summarization:** Never collapse detailed technical lists into summaries unless explicitly asked.
    - **Confirmation:** AI must confirm that 100% of technical data was preserved after any edit.
    - **Literal Pass-Through Mandate:** "All Markdown tables and code blocks must be treated as Atomic Objects. They must be copied character-for-character into the new format. No rows may be omitted, combined, or reworded."
    - **The "Row-Count Check" Requirement**: "Before writing a reformatted spec, the AI must explicitly count the number of rows in every table in the source and verify that the count matches exactly in the destination."
    - **Template Expansion Rule:** "The 5-part template is a container, not a limit. If a section (like Implementation Examples) requires 500 lines of literal tables to maintain Zero-Loss, the AI must provide all 500 lines."
    - **Bold Rule**: "Do not use bold syntax within a header tag"

## Specification Standards
All files in `/Docs` must follow this 5-part anatomical structure. When reformatting, the **Content Integrity** rules (Zero-Loss, row-counting) are in full effect.

**H1 Header (Spec Name):** The title of the system.

**## 1. Overview:** A 1-3 paragraph summary of what the system covers and its role in the game.

**## 2. Design Goals & Functional Requirements:**
- The "Human" section.
- Descriptive text and tables defining the desired end-state, gameplay goals, and player-facing rules.
- Detailed breakdown of mechanics.

**## 3. Data Specification:**
- Definition of in-game JSON files.
- Markdown tables for every property: `Property | Type | Description`.
- Must include at least one concrete JSON example block.

**## 4. Technical Implementation:**
- The "Code" section.
- C# function signatures, class responsibilities, and parameter descriptions.
- Must include code examples or sequence diagrams showing implementation flow.
