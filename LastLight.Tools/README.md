# LastLight.Tools

A cross-platform CLI utility for managing the LastLight development workflow. This project contains automated tools for asset processing, data validation, and other development tasks.

## Why this exists
LastLight targets multiple platforms (Desktop, Android, iOS). To keep the game consistent and performant, we use this tool to transform "Developer-friendly" raw assets into "Game-ready" formats.

## Usage
Run the tool from the repository root using the .NET CLI:

```bash
dotnet run --project LastLight.Tools -- <command> [args]
```

---

## Available Commands

### 1. `pack-assets`
**Purpose:** The master command to prepare all assets for the MonoGame Content Pipeline.

- **Source:** `LastLight.Client.Core/Assets/`
- **Behavior:** 
    1. **Cleans:** Wipes the existing `Content/` folder (Total Regeneration).
    2. **Packs:** Every sub-folder in `Assets/Graphics/Pack/` is packed into an `atlas.png` and `atlas_map.json`.
    3. **Copies:** Recursive copy of `Audio/`, `Graphics/Static/`, and `Fonts/`.
    4. **MGCB:** Automatically generates a fresh `Content.mgcb` file with all necessary build entries.

### 2. `resize`
**Purpose:** Resizes an individual image to a target width while automatically maintaining its original aspect ratio.

- **Arguments:** `<path_to_image> <target_width>`
- **Behavior:** Overwrites the original file with the resized version.

---

## Technical Details
For detailed asset organization rules, see [ASSET_SPEC.md](../../Docs/ASSET_SPEC.md).
