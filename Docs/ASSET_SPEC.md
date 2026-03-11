# ASSET SPECIFICATION

## 1. Overview (v1.6)
This document defines the unified asset management workflow for **LastLight**. It describes how raw source files (stored in `Assets/`) are transformed, packed, and staged for the MonoGame Content Pipeline. The system is designed to be fully automated via the `pack-assets` tool, ensuring consistency across all platforms and execution environments.

## 2. Design Goals & Functional Requirements
The primary goal is to maintain a "Total Regeneration" pipeline where raw source assets are the single source of truth, and the processed `Content/` folder is a transient build artifact.

### 2.1 Directory Structure
The `pack-assets` tool uses the folder name to determine the processing logic.

| Root Folder | Sub-Folder Pattern | Logic Type | Output Destination |
| :--- | :--- | :--- | :--- |
| **`/Audio`** | `**/*.{wav,mp3}` | **Direct Copy** | `Content/Audio/` |
| **`/Graphics/Pack`** | `/[AtlasName]/*.png` | **Atlas Packing** | `Content/Graphics/[AtlasName]/` |
| **`/Graphics/Static`**| `**/*.{png,jpg}` | **Direct Copy** | `Content/Graphics/` |
| **`/Fonts`** | `/*.spritefont` | **Direct Copy** | `Content/Fonts/` |

### 2.2 Transformation Rules
- **Audio (Direct Copy):** Files are copied recursively, maintaining the source directory structure within `Content/Audio/`.
- **Graphics: Pack (Texture Atlases):** Every immediate sub-folder of `Assets/Graphics/Pack/` is treated as a distinct, isolated atlas.
    - **Logical Isolation:** Each folder name becomes the `Atlas` property in game data (e.g., `Items`).
    - **Casing Rule:** Output sub-folder name in `Content/Graphics/` matches the exact casing of the source folder.
    - **Naming Rule:** All atlases are named exactly **`atlas.png`** and all maps are **`atlas_map.json`**.
- **Graphics: Static (Backgrounds/UI):** Files are copied recursively to `Content/Graphics/`, preserving the source subdirectory structure.

### 2.3 Usage Flow
1.  Designer adds `Assets/Graphics/Pack/Items/Sword.png`.
2.  Developer runs: `dotnet run --project LastLight.Tools -- pack-assets`.
3.  The tool:
    *   Generates `atlas.png` and `atlas_map.json` in `Content/Graphics/Items/`.
    *   `atlas_map.json` contains the key `"sword"`.
    *   Updates `Content.mgcb` with an entry for `Graphics/Items/atlas.png`.
4.  Game Code loads using: `LoadAtlas("Items", ...)` and looks up `"sword"`.
5.  Developer builds the game; MSBuild copies the `.json` map to the `bin/` folder.

## 3. Data Specification

### 3.1 Atlas Map Schema (atlas_map.json)
The JSON map translates sprite names to their coordinates within the packed atlas.
- **Key Mapping:** JSON map keys match the source filename (lowercase, no extension) with **no prefixes**. (e.g., `Sword.png` -> `"sword"`).
- **Structure:** `Dictionary<string, AtlasRegion>` where `AtlasRegion` defines `X, Y, W, H`.

## 4. Technical Implementation

### 4.1 The pack-assets Tool Requirements
The tool is a C# console application located in the `LastLight.Tools` project. It can be executed from the project root using the following command:

```bash
dotnet run --project LastLight.Tools -- pack-assets
```

- **Conflict Policy (Fail-Fast):** The tool maintains a registry of all files written to `Content/`. If a file write is attempted for a path that has already been written to, the tool must throw a fatal error and terminate.
- **Total Regeneration Policy:** The tool wipes the entire `Content/` directory (excluding `bin/` and `obj/`) on every run. The `Content.mgcb` file is regenerated from scratch using hardcoded Global Properties and the tracked file registry.

### 4.2 Project Deployment (MSBuild)
Since `.json` maps are not processed by the MonoGame Content Pipeline, they must be manually deployed to the execution directory. Both `Core` and `Desktop` project files must include the following target:

```xml
<Target Name="CopyJsonMaps" AfterTargets="Build">
  <ItemGroup>
    <JsonMaps Include="Content\**\*.json" />
  </ItemGroup>
  <Copy SourceFiles="@(JsonMaps)" DestinationFolder="$(OutputPath)\Content\%(RecursiveDir)" SkipUnchangedFiles="true" />
</Target>
```

### 4.3 Recommended approach for asset usage in client
Gameplay code should not interact with the filesystem or `ContentManager` directly for individual sprites. Instead, a centralized `AssetManager` abstraction should be used to handle atlas lookups and texture caching. This ensures that the complex mapping between JSON keys and texture regions is hidden from high-level logic.

#### Recommended C# Abstraction
```csharp
public static class AssetManager 
{
    // Caches: AtlasName -> Texture and AtlasName -> (IconKey -> SourceRect)
    private static Dictionary<string, Texture2D> _textures = new();
    private static Dictionary<string, Dictionary<string, Rectangle>> _regions = new();

    public static void LoadAtlas(ContentManager content, string atlasName) 
    {
        // Load the XNB compiled texture
        string path = $"Graphics/{atlasName}/atlas";
        _textures[atlasName] = content.Load<Texture2D>(path);
        
        // Load the companion JSON map (deployed via MSBuild target)
        string mapPath = Path.Combine(content.RootDirectory, $"Graphics/{atlasName}/atlas_map.json");
        var json = File.ReadAllText(mapPath);
        var map = JsonSerializer.Deserialize<Dictionary<string, AtlasRegion>>(json);
        
        _regions[atlasName] = map.ToDictionary(
            k => k.Key, 
            v => new Rectangle(v.Value.X, v.Value.Y, v.Value.W, v.Value.H)
        );
    }

    public static void DrawIcon(SpriteBatch sb, string atlas, string icon, Vector2 pos, Color color) 
    {
        if (_textures.TryGetValue(atlas, out var tex) && 
            _regions.TryGetValue(atlas, out var atlasRegions) && 
            atlasRegions.TryGetValue(icon, out var sourceRect)) 
        {
            sb.Draw(tex, pos, sourceRect, color);
        }
    }
}
```

**Benefits:**
- **Decoupling:** Gameplay code refers to logical names like `"Items"` and `"sword"` rather than hardcoded file paths.
- **Performance:** Utilizes O(1) dictionary lookups for sprite coordinates, crucial for high-intensity combat.
- **Consistency:** Aligns perfectly with the data-driven model used in `Items.json` and `Enemies.json`.

### 4.4 Client Usage Examples

#### Loading and Drawing an Icon
This example shows how to initialize an atlas and draw a specific sprite key defined in JSON.

```csharp
// 1. At startup (e.g., in LoadContent)
AssetManager.LoadAtlas(Content, "Items");

// 2. In Draw loop
// Position and Info (Atlas="Items", Icon="iron_bow") come from entity data
AssetManager.DrawIcon(spriteBatch, Info.Atlas, Info.Icon, Position, Color.White);
```

#### Loading and Playing a Sound Effect
Audio assets are copied directly to `Content/Audio/`. Use the standard MonoGame `SoundEffect` class.

```csharp
// 1. Variable storage
SoundEffect hitSound;

// 2. Load (e.g., in LoadContent)
// The path excludes "Content/" and the file extension
hitSound = Content.Load<SoundEffect>("Audio/Sound/hit_effect");

// 3. Play (e.g., when a collision occurs)
hitSound.Play(volume: 0.5f, pitch: 0.0f, pan: 0.0f);
```
