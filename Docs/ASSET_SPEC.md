# ASSET_SPEC.md (v1.6)

This document defines the unified asset management workflow for LastLight. It describes how raw source files are transformed and staged for the MonoGame Content Pipeline.

## 1. Directory Structure

All raw assets are stored in `LastLight.Client.Core/Assets/`. The `pack-assets` tool uses the folder name to determine the processing logic.

| Root Folder | Sub-Folder Pattern | Logic Type | Output Destination |
| :--- | :--- | :--- | :--- |
| **`/Audio`** | `**/*.{wav,mp3}` | **Direct Copy** | `Content/Audio/` |
| **`/Graphics/Pack`** | `/[AtlasName]/*.png` | **Atlas Packing** | `Content/Graphics/[AtlasName]/` |
| **`/Graphics/Static`**| `**/*.{png,jpg}` | **Direct Copy** | `Content/Graphics/` |
| **`/Fonts`** | `/*.spritefont` | **Direct Copy** | `Content/Fonts/` |

---

## 2. Transformation Logic

### 2.1 Audio (Direct Copy)
Files are copied recursively. The tool maintains the source directory structure within `Content/Audio/`.

### 2.2 Graphics: Pack (Texture Atlases)
The tool treats every immediate sub-folder of `Assets/Graphics/Pack/` as a distinct, isolated atlas. 

*   **Logical Isolation:** Each folder name becomes the `Atlas` property in game data (e.g., `Items`).
*   **Casing Rule:** The output sub-folder name in `Content/Graphics/` matches the exact casing of the source folder.
*   **Naming Rule:** All atlases are named exactly **`atlas.png`** and all maps are **`atlas_map.json`**.
*   **Key Mapping:** JSON map keys match the source filename (lowercase, no extension) with **no prefixes**. (e.g., `Sword.png` -> `"sword"`).

### 2.3 Graphics: Static (Backgrounds/UI)
Files are copied recursively to `Content/Graphics/`, preserving the source subdirectory structure.

---

## 3. The `pack-assets` Tool Requirements

### 3.1 Conflict Policy (Fail-Fast)
The tool maintains a registry of all files written to `Content/`. If a file write is attempted for a path that has already been written to, the tool must throw a fatal error and terminate.

### 3.2 Total Regeneration Policy
The tool wipes the entire `Content/` directory (excluding `bin/` and `obj/`) on every run. The `Content.mgcb` file is regenerated from scratch using hardcoded Global Properties and the tracked file registry.

---

## 4. Project Deployment (MSBuild)

Since `.json` maps are not processed by the MonoGame Content Pipeline, they must be manually deployed to the execution directory. Both `Core` and `Desktop` project files must include the following target:

```xml
<Target Name="CopyJsonMaps" AfterTargets="Build">
  <ItemGroup>
    <JsonMaps Include="Content\**\*.json" />
  </ItemGroup>
  <Copy SourceFiles="@(JsonMaps)" DestinationFolder="$(OutputPath)\Content\%(RecursiveDir)" SkipUnchangedFiles="true" />
</Target>
```

---

## 5. Usage Flow

1.  Designer adds `Assets/Graphics/Pack/Items/Sword.png`.
2.  Developer runs: `dotnet run --project LastLight.Tools -- pack-assets`.
3.  The tool:
    *   Generates `atlas.png` and `atlas_map.json` in `Content/Graphics/Items/`.
    *   `atlas_map.json` contains the key `"sword"`.
    *   Updates `Content.mgcb` with an entry for `Graphics/Items/atlas.png`.
4.  Game Code loads using: `LoadAtlas("Items", ...)` and looks up `"sword"`.
5.  Developer builds the game; MSBuild copies the `.json` map to the `bin/` folder.
