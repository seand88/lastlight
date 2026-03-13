# Asset Manager and World Renderer Specification

## 1. Overview

This specification defines the V1 asset-loading and rendering-support architecture for a top-down 2D MMORPG built with MonoGame and .NET. It covers how audio, songs, static images, texture atlases, animation sheets, and fonts are organized on disk, described in data files, loaded into runtime memory, and consumed by gameplay and rendering code.

The design is intentionally simple for V1. `AssetManager` is responsible for loading and returning MonoGame-native assets and metadata. `WorldRenderer` is responsible for per-entity animation playback state, animation timing, and drawing entities using data provided by `AssetManager`. `Game1` remains the coordinator that constructs both systems and calls them from the MonoGame update and draw loop.

This specification explicitly avoids grouped loading/unloading, public custom asset wrapper types, and pushing live animation state into the asset loader. Sounds and songs are returned as MonoGame handles and are managed externally by the caller. Animation metadata is returned piecemeal through asset queries and is managed at runtime by `WorldRenderer`.

## 2. Design Goals & Functional Requirements

### 2.1 Design Goals

| Goal | Description |
|---|---|
| Keep `Game1` thin | `Game1` should bootstrap MonoGame, construct `AssetManager` and `WorldRenderer`, call `LoadAll()`, and route `Update()` / `Draw()` calls. |
| Keep the public API MonoGame-native | Public return types should be `SoundEffect`, `Song`, `Texture2D`, `SpriteFont`, `Rectangle`, primitives, or `void`. |
| Separate loading from live playback state | `AssetManager` loads and returns assets and metadata. `WorldRenderer` owns animation playback state for entities. |
| Keep V1 simple | All assets are loaded up front with `LoadAll()`. No asset groups or unload logic are required in V1. |
| Support future growth | The design should allow future additions such as scoped loading, convenience wrappers, or a more formal animation subsystem without changing core call sites too aggressively. |
| Preserve semantic lookups | Gameplay and UI code should request assets by stable logical keys, not by raw content file paths. |

### 2.2 Runtime Ownership Rules

| Concern | Owner in V1 | Notes |
|---|---|---|
| Asset loading and caching | `AssetManager` | Loads all content and metadata during startup. |
| Sound effect playback | External caller | Caller gets a `SoundEffect` and calls `Play()` or `CreateInstance()`. |
| Song playback | External caller | Caller gets a `Song` and uses `MediaPlayer`. |
| Static image drawing | External caller / renderer | Caller gets a `Texture2D` and draws it. |
| Atlas icon lookup | `AssetManager` | Returns `Texture2D` and `Rectangle` data for the caller to draw. |
| Animation frame metadata | `AssetManager` | Returns frame count, source rectangles, durations, and loop flags. |
| Animation playback state | `WorldRenderer` | Tracks current clip, frame index, elapsed time, and play/stop behavior per entity. |
| Entity drawing | `WorldRenderer` | Uses `AssetManager` queries plus runtime entity state to draw the current frame. |

### 2.3 Asset Pipeline Requirements

Expected directory structure and asset locations by type:

| Root Folder | Sub-Folder Pattern | Logic Type | Output Destination | Sample File | Canonical V1 Call(s) | Notes | Runtime Type |
|---|---|---|---|---|---|---|---|
| `/Assets/Audio/SoundEffects` | `**/*.{wav,mp3}` | Direct Copy | `Content/Audio/SoundEffects` | `fire_arrow.wav` | `GetSound("fire_arrow")` | Repeatable short sounds. No grouped unloading in V1. Multiple instances may play at once. | `SoundEffect` |
| `/Assets/Audio/Songs` | `**/*.{wav,mp3}` | Direct Copy | `Content/Audio/Music` | `login_song.wav` | `GetMusic("login_song")` | Generally one active song at a time. Playback is controlled outside `AssetManager`. | `Song` |
| `/Assets/Graphics/Pack/TextureAtlases` | `/[AtlasName]/*.png` | Atlas Packing | `Content/Graphics/TextureAtlases/[AtlasName]/` | `my_image1.png`, `my_image2.png` | `GetAtlasTexture("AtlasName")`, `GetIconSourceRect("AtlasName", "my_image1")` | Non-animated images packed into a texture atlas and described by `atlas_map.json`. Commonly used for icons and UI sprites. | `Texture2D`, `Rectangle` |
| `/Assets/Graphics/Pack/Animations` | `/[Entity]/*.png` | Animation Packing | `Content/Graphics/Animations/[Entity]/` | `run_01.png`, `run_02.png` | `GetAnimationTexture("Entity")`, etc. | Tool packs individual frames into `animation.png` and generates `animation_map.json` based on `<clip>_<frame>.png` naming. | `Texture2D`, `Rectangle`, `int`, `float`, `bool` |
| `/Assets/Graphics/Static/Images` | `**/*.png` | Direct Copy | `Content/Graphics/Static/Images/` | `login_background.png` | `GetStaticImage("login_background")` | Single raw image files not packed into an atlas. | `Texture2D` |
| `/Assets/Graphics/Static/Animations` | `/[Entity]/*.{png,jpg}` | Direct Copy | `Content/Graphics/Animations/[Entity]/` | `animation.png` | `GetAnimationTexture("Entity")`, `GetAnimationFrameCount("Entity", "run")`, `GetAnimationFrameSourceRect("Entity", "run", 1)`, `GetAnimationFrameDurationMs("Entity", "run", 2)`, `IsAnimationLooping("Entity", "run")` | Entity folders must include animation metadata that defines clips and frame timing. `WorldRenderer` consumes this data. | `Texture2D`, `Rectangle`, `int`, `float`, `bool` |
| `/Assets/Fonts` | `/*.spritefont` | Direct Copy | `Content/Fonts/` | `my_font.spritefont` | `GetFont("my_font")` | Fonts are loaded and returned directly. | `SpriteFont` |

### 2.3.1 Standardized Animation Registry

To ensure consistent behavior across the `WorldRenderer` and AI systems, all animation clips must use names from this registry. 

| Clip Key | Mandatory? | Description | Example Entities |
|---|---|---|---|
| `idle` | YES | The default state when not moving or acting. | Player, Enemy, Torch, Portal |

*Extensions to this registry must be formally added to this specification.*



### 2.4 The pack-assets Tool Requirements

The tool is a C# console application located in the `LastLight.Tools` project. It can be executed from the project root using the following command:

```bash
dotnet run --project LastLight.Tools -- pack-assets
```

The tool wipes the entire `Content/` directory (excluding `bin/` and `obj/`) on every run. The `Content.mgcb` file is regenerated from scratch..

### 2.4.1 Animation Packing Logic

The tool automatically processes folders in `Assets/Graphics/Pack/Animations/`. For each entity sub-folder:
1.  **Frame Identification:** Identifies all `.png` files following the `<clip>_<frame>.png` naming convention (e.g., `run_00.png`, `idle_00.png`).
    -   **Indexing:** Must be **0-indexed** (starting at `00`).
    -   **Padding:** Must be **2-digit padded** (e.g., `00, 01, ..., 99`) for deterministic lexicographical sorting.
2.  **Grouping & Sorting:** Groups frames by `<clip>` and sorts them numerically by `<frame>`.
3.  **Contiguity & Gap Detection (CRASH ON ERROR):** If a sequence for a clip is not contiguous (e.g., `idle_00.png` and `idle_02.png` exist, but `idle_01.png` is missing), the tool **must terminate with a fatal error**.
4.  **Sprite Sheet Generation:** Stitches grouped frames into a single `animation.png` sprite sheet for the entity.
5.  **Metadata Generation:** Generates an `animation_map.json` file.
    -   `loop`: Defaults to `true` for all clips.
    -   `durationMs`: Defaults to `150` for all frames.
    -   `x, y, w, h`: Automatically calculated based on the frame's position in the generated sheet.

### 2.4.2 Ambiguity Resolution & Collision Policy

To ensure a strict single source of truth and prevent "ghost asset" bugs, the `pack-assets` tool must implement a fail-fast collision policy for the following categories:

| Asset Category | Automated Source (Pack) | Manual Source (Static) | Failure Condition |
|---|---|---|---|
| **Animations** | `Assets/Graphics/Pack/Animations/[Entity]/` | `Assets/Graphics/Static/Animations/[Entity]/` | If an `[Entity]` folder exists in both paths. |
| **TextureAtlases** | `Assets/Graphics/Pack/TextureAtlases/[AtlasName]/` | `Assets/Graphics/Static/TextureAtlases/[AtlasName]/` | If an `[AtlasName]` folder exists in both paths. |

**Required Tool Behavior:** If a collision is detected, the tool **must print a fatal error message** listing the offending paths and **terminate immediately** with a non-zero exit code.

> **NOTE:** Since `.json` maps are not processed by the MonoGame Content Pipeline, they must be manually deployed to the execution directory. Both `Core` and `Desktop` project files must include the following target:

```xml
<Target Name="CopyJsonMaps" AfterTargets="Build">
  <ItemGroup>
    <JsonMaps Include="Content\**\*.json" />
  </ItemGroup>
  <Copy SourceFiles="@(JsonMaps)" DestinationFolder="$(OutputPath)\Content\%(RecursiveDir)" SkipUn
```

### 2.4 Functional Requirements

| Requirement ID | Requirement |
|---|---|
| FR-01 | The system shall expose a single startup call, `LoadAll()`, that loads all assets and metadata required by V1. |
| FR-02 | Gameplay code shall not hardcode content pipeline file paths. All lookups shall use logical keys. |
| FR-03 | `AssetManager` shall return MonoGame-native types or primitive metadata only. |
| FR-04 | `AssetManager` shall not own live per-entity animation playback state. |
| FR-05 | `WorldRenderer` shall own per-entity animation playback state and advance that state during `Update()`. |
| FR-06 | `WorldRenderer` shall draw animated entities by querying `AssetManager` for texture, source rectangles, frame durations, and loop behavior. |
| FR-07 | Sound and song playback shall be managed externally by the caller after retrieving a MonoGame handle from `AssetManager`. |
| FR-08 | Atlas lookup shall be split into atlas texture retrieval and icon source-rectangle retrieval. |
| FR-09 | Fonts shall be retrievable through `AssetManager` using semantic keys. |
| FR-10 | V1 shall not implement asset groups, staged unloading, or runtime hot reload. |

### 2.5 High-Level Usage Rules

| Use Case | Rule |
|---|---|
| Play a one-shot sound | Call `GetSound(key)` and then `sound.Play()`. |
| Play a looped sound | Call `GetSound(key)`, create a `SoundEffectInstance`, set `IsLooped = true`, then `Play()`. |
| Play a song | Call `GetMusic(key)` and pass the returned `Song` to `MediaPlayer.Play(song)`. |
| Draw a static image | Call `GetStaticImage(key)` and pass the returned `Texture2D` to `SpriteBatch.Draw(...)`. |
| Draw an icon from an atlas | Call `GetAtlasTexture(atlasKey)` and `GetIconSourceRect(atlasKey, iconKey)`, then draw with `SpriteBatch.Draw(...)`. |
| Play an entity animation | Call `WorldRenderer.PlayAnimation(entity, sheetKey, clipKey, restart)`. |
| Advance animations | Call `WorldRenderer.Update(gameTime)` from `Game1.Update(...)`. |
| Draw an animated entity | Call `WorldRenderer.Draw(spriteBatch)` from `Game1.Draw(...)`. |

## 3. Data Specification

### 3.1 `asset_manifest.json`

`asset_manifest.json` is the central lookup file that maps semantic keys to runtime content paths and metadata files.

#### 3.1.1 Root Properties

| Property | Type | Description |
|---|---|---|
| `sounds` | `object<string, string>` | Maps sound effect keys to content paths under `Content/Audio/SoundEffects`. |
| `music` | `object<string, string>` | Maps song keys to content paths under `Content/Audio/Music`. |
| `staticImages` | `object<string, string>` | Maps static image keys to content paths under `Content/Graphics/Static/Images`. |
| `fonts` | `object<string, string>` | Maps font keys to content paths under `Content/Fonts`. |
| `atlases` | `object<string, AtlasManifestEntry>` | Maps atlas keys to atlas texture and atlas metadata paths. |
| `animations` | `object<string, AnimationManifestEntry>` | Maps animation sheet keys to texture and metadata paths. |

#### 3.1.2 `AtlasManifestEntry`

| Property | Type | Description |
|---|---|---|
| `texture` | `string` | Content path for the packed atlas texture. |
| `map` | `string` | File path to `atlas_map.json` for this atlas. |

#### 3.1.3 `AnimationManifestEntry`

| Property | Type | Description |
|---|---|---|
| `texture` | `string` | Content path for the animation sheet texture. |
| `map` | `string` | File path to `animation_map.json` for this sheet. |

#### 3.1.4 Example `asset_manifest.json`

```json
{
  "sounds": {
    "fire_arrow": "Audio/SoundEffects/fire_arrow",
    "ui_click": "Audio/SoundEffects/ui_click"
  },
  "music": {
    "login_song": "Audio/Music/login_song",
    "forest_theme": "Audio/Music/forest_theme"
  },
  "staticImages": {
    "login_background": "Graphics/Static/Images/login_background",
    "forest_background": "Graphics/Static/Images/forest_background"
  },
  "fonts": {
    "my_font": "Fonts/my_font"
  },
  "atlases": {
    "Items": {
      "texture": "Graphics/TextureAtlases/Items/atlas",
      "map": "Content/Graphics/TextureAtlases/Items/atlas_map.json"
    },
    "UI": {
      "texture": "Graphics/TextureAtlases/UI/atlas",
      "map": "Content/Graphics/TextureAtlases/UI/atlas_map.json"
    }
  },
  "animations": {
    "Hero": {
      "texture": "Graphics/Animations/Hero/animation",
      "map": "Content/Graphics/Animations/Hero/animation_map.json"
    },
    "Goblin": {
      "texture": "Graphics/Animations/Goblin/animation",
      "map": "Content/Graphics/Animations/Goblin/animation_map.json"
    }
  }
}
```

### 3.2 `atlas_map.json`

`atlas_map.json` maps individual image keys inside a packed atlas to source rectangles.

#### 3.2.1 Root Properties

| Property | Type | Description |
|---|---|---|
| `sprites` | `object<string, AtlasSpriteEntry>` | Maps a sprite/icon key to its source rectangle in the atlas texture. |

#### 3.2.2 `AtlasSpriteEntry`

| Property | Type | Description |
|---|---|---|
| `x` | `int` | Left pixel coordinate of the sprite in the atlas texture. |
| `y` | `int` | Top pixel coordinate of the sprite in the atlas texture. |
| `w` | `int` | Width of the sprite in pixels. |
| `h` | `int` | Height of the sprite in pixels. |

#### 3.2.3 Example `atlas_map.json`

```json
{
  "sprites": {
    "my_image1": { "x": 0, "y": 0, "w": 32, "h": 32 },
    "my_image2": { "x": 32, "y": 0, "w": 32, "h": 32 },
    "sword": { "x": 64, "y": 0, "w": 32, "h": 32 }
  }
}
```

### 3.3 `animation_map.json`

`animation_map.json` defines clips, frame rectangles, per-frame duration, and whether each clip loops.

#### 3.3.1 Root Properties

| Property | Type | Description |
|---|---|---|
| `clips` | `object<string, AnimationClipEntry>` | Maps clip keys such as `idle`, `run`, or `attack` to clip definitions. |

#### 3.3.2 `AnimationClipEntry`

| Property | Type | Description |
|---|---|---|
| `loop` | `bool` | Whether the clip loops when it reaches its final frame. |
| `frames` | `AnimationFrameEntry[]` | Ordered list of frames for the clip. |

#### 3.3.3 `AnimationFrameEntry`

| Property | Type | Description |
|---|---|---|
| `x` | `int` | Left pixel coordinate of the frame inside the animation sheet texture. |
| `y` | `int` | Top pixel coordinate of the frame inside the animation sheet texture. |
| `w` | `int` | Width of the frame in pixels. |
| `h` | `int` | Height of the frame in pixels. |
| `durationMs` | `float` | Duration of this frame in milliseconds before advancing to the next frame. |

#### 3.3.4 Example `animation_map.json`

```json
{
  "clips": {
    "idle": {
      "loop": true,
      "frames": [
        { "x": 0, "y": 0, "w": 32, "h": 32, "durationMs": 150 },
        { "x": 32, "y": 0, "w": 32, "h": 32, "durationMs": 150 }
      ]
    },
    "run": {
      "loop": true,
      "frames": [
        { "x": 0, "y": 32, "w": 32, "h": 32, "durationMs": 100 },
        { "x": 32, "y": 32, "w": 32, "h": 32, "durationMs": 100 },
        { "x": 64, "y": 32, "w": 32, "h": 32, "durationMs": 100 }
      ]
    },
    "attack": {
      "loop": false,
      "frames": [
        { "x": 0, "y": 64, "w": 32, "h": 32, "durationMs": 80 },
        { "x": 32, "y": 64, "w": 32, "h": 32, "durationMs": 80 },
        { "x": 64, "y": 64, "w": 32, "h": 32, "durationMs": 120 }
      ]
    }
  }
}
```

### 3.4 Data Rules

| Rule ID | Rule |
|---|---|
| DR-01 | All semantic keys must be stable and unique within their category. |
| DR-02 | `asset_manifest.json` is the only public source of truth for runtime semantic lookups. |
| DR-03 | Atlas metadata must define source rectangles in pixels. |
| DR-04 | Animation metadata must define clips, ordered frames, frame rectangles, and frame durations. |
| DR-05 | Animation sheet keys in `asset_manifest.json` must match the `sheetKey` used by `WorldRenderer.PlayAnimation(...)`. |
| DR-06 | V1 does not require grouped asset ownership or unload metadata. |
| DR-07 | **Ambiguity Resolution Rule:** If an entity key exists in both a `Pack/` and `Static/` directory for the same category (Animations or TextureAtlases), the `pack-assets` tool must fail with a fatal error. |
| DR-08 | **Frame Indexing Rule:** Frames must be 0-indexed (`_00`) and two-digit padded. Sequences must be contiguous; gaps result in tool failure. |
| DR-09 | **Clip Naming Rule:** All clip names must be lower-case and chosen from the **Standardized Animation Registry (Section 2.3.1)**. Non-standard keys are prohibited. |

## 4. Technical Implementation

### 4.1 Architecture Summary

The V1 runtime architecture is split into three responsibilities:

| Class / System | Responsibility |
|---|---|
| `Game1` | MonoGame bootstrap, construction of services, and invocation of `LoadContent()`, `Update()`, and `Draw()`. |
| `AssetManager` | Loads all content and metadata up front. Returns MonoGame assets and primitive metadata through semantic lookup methods. |
| `WorldRenderer` | Owns per-entity animation playback state, advances animation timing, and draws animated world entities using data retrieved from `AssetManager`. |

### 4.2 Public Interfaces and Classes

#### 4.2.1 `IAssetManager`

```csharp
public interface IAssetManager
{
    void LoadAll();

    SoundEffect GetSound(string key);
    Song GetMusic(string key);
    Texture2D GetStaticImage(string key);
    SpriteFont GetFont(string key);

    Texture2D GetAtlasTexture(string atlasKey);
    Rectangle GetIconSourceRect(string atlasKey, string iconKey);

    Texture2D GetAnimationTexture(string sheetKey);
    int GetAnimationFrameCount(string sheetKey, string clipKey);
    Rectangle GetAnimationFrameSourceRect(string sheetKey, string clipKey, int frameIndex);
    float GetAnimationFrameDurationMs(string sheetKey, string clipKey, int frameIndex);
    bool IsAnimationLooping(string sheetKey, string clipKey);
}
```

#### 4.2.2 `AssetManager`

```csharp
public sealed class AssetManager : IAssetManager
{
    public void LoadAll();

    public SoundEffect GetSound(string key);
    public Song GetMusic(string key);
    public Texture2D GetStaticImage(string key);
    public SpriteFont GetFont(string key);

    public Texture2D GetAtlasTexture(string atlasKey);
    public Rectangle GetIconSourceRect(string atlasKey, string iconKey);

    public Texture2D GetAnimationTexture(string sheetKey);
    public int GetAnimationFrameCount(string sheetKey, string clipKey);
    public Rectangle GetAnimationFrameSourceRect(string sheetKey, string clipKey, int frameIndex);
    public float GetAnimationFrameDurationMs(string sheetKey, string clipKey, int frameIndex);
    public bool IsAnimationLooping(string sheetKey, string clipKey);
}
```

#### 4.2.3 `WorldRenderer`

```csharp
public sealed class WorldRenderer
{
    public void Update(GameTime gameTime);
    public void Draw(SpriteBatch spriteBatch);

    public void PlayAnimation(Entity entity, string sheetKey, string clipKey, bool restart = false);
    public void StopAnimation(Entity entity);
}
```

### 4.3 Class Responsibility Details

#### 4.3.1 `AssetManager` Responsibilities

| Responsibility | Description |
|---|---|
| Load manifest | Read `asset_manifest.json` and validate required sections. |
| Load assets | Load all configured sounds, music, static images, atlas textures, animation sheet textures, and fonts. |
| Load metadata | Parse `atlas_map.json` and `animation_map.json` files into internal lookup structures. |
| Cache runtime objects | Store assets and metadata in internal dictionaries keyed by semantic identifiers. |
| Serve runtime lookups | Return assets and metadata through the public API. |
| Stay stateless regarding entity animation playback | Do not track per-entity clip, frame index, or elapsed frame time. |

#### 4.3.2 `WorldRenderer` Responsibilities

| Responsibility | Description |
|---|---|
| Track animation playback state | Maintain current animation clip, current frame, elapsed time, and play/stop state per entity. |
| Advance playback | Update frame timing during `Update(GameTime)` using animation durations from `AssetManager`. |
| Handle looping behavior | Restart looping clips and stop or hold the last frame for non-looping clips according to implementation rules. |
| **Purge inactive state** | **Automatically remove tracking for entities where `Active == false` or health is zero to prevent corpse persistence.** |
| Draw entities | During `Draw(SpriteBatch)`, query `AssetManager` for animation sheet textures and source rectangles, then draw the current frame. |
| Expose control methods | Support `PlayAnimation(...)` and `StopAnimation(...)` for gameplay systems. |

### 4.4 Method Signatures and Parameter Descriptions

#### 4.4.1 `IAssetManager` / `AssetManager` Methods

| Method | Return Type | Parameters | Description |
|---|---|---|---|
| `LoadAll()` | `void` | None | Loads all configured assets and metadata into memory during startup. |
| `GetSound(string key)` | `SoundEffect` | `key`: semantic sound key | Returns a loaded sound effect for external playback control. |
| `GetMusic(string key)` | `Song` | `key`: semantic song key | Returns a loaded song for external playback via `MediaPlayer`. |
| `GetStaticImage(string key)` | `Texture2D` | `key`: semantic static image key | Returns a raw static image texture. |
| `GetFont(string key)` | `SpriteFont` | `key`: semantic font key | Returns a loaded font. |
| `GetAtlasTexture(string atlasKey)` | `Texture2D` | `atlasKey`: semantic atlas key | Returns the packed atlas texture associated with the atlas key. |
| `GetIconSourceRect(string atlasKey, string iconKey)` | `Rectangle` | `atlasKey`: semantic atlas key, `iconKey`: sprite/icon key inside the atlas | Returns the source rectangle for a single icon or sprite within an atlas texture. |
| `GetAnimationTexture(string sheetKey)` | `Texture2D` | `sheetKey`: animation sheet key | Returns the animation sheet texture for an entity or sheet identifier. |
| `GetAnimationFrameCount(string sheetKey, string clipKey)` | `int` | `sheetKey`: animation sheet key, `clipKey`: clip name | Returns the number of frames in the requested clip. |
| `GetAnimationFrameSourceRect(string sheetKey, string clipKey, int frameIndex)` | `Rectangle` | `sheetKey`: animation sheet key, `clipKey`: clip name, `frameIndex`: zero-based frame index | Returns the source rectangle for the requested animation frame. |
| `GetAnimationFrameDurationMs(string sheetKey, string clipKey, int frameIndex)` | `float` | `sheetKey`: animation sheet key, `clipKey`: clip name, `frameIndex`: zero-based frame index | Returns the display duration for the requested frame in milliseconds. |
| `IsAnimationLooping(string sheetKey, string clipKey)` | `bool` | `sheetKey`: animation sheet key, `clipKey`: clip name | Returns whether the requested clip loops. |

#### 4.4.2 `WorldRenderer` Methods

| Method | Return Type | Parameters | Description |
|---|---|---|---|
| `Update(GameTime gameTime)` | `void` | `gameTime`: MonoGame frame timing data | Advances animation state for all tracked entities. |
| `Draw(SpriteBatch spriteBatch)` | `void` | `spriteBatch`: the active `SpriteBatch` used for drawing | Draws world entities using current animation state and asset lookups. |
| `PlayAnimation(Entity entity, string sheetKey, string clipKey, bool restart = false)` | `void` | `entity`: target renderable entity, `sheetKey`: animation sheet key, `clipKey`: clip name, `restart`: whether to reset the animation if already active | Starts or switches the active animation for an entity. |
| `StopAnimation(Entity entity)` | `void` | `entity`: target renderable entity | Stops advancing the active animation for an entity. The implementation may hold the current frame or switch to a default clip according to project rules. |

### 4.5 Expected Internal Caches and State

The following internal structures are acceptable implementation guidance. They are not public contract types.

```csharp
Dictionary<string, SoundEffect> _sounds;
Dictionary<string, Song> _music;
Dictionary<string, Texture2D> _staticImages;
Dictionary<string, SpriteFont> _fonts;
Dictionary<string, Texture2D> _atlasTextures;
Dictionary<(string AtlasKey, string IconKey), Rectangle> _atlasSourceRects;
Dictionary<string, Texture2D> _animationTextures;
Dictionary<(string SheetKey, string ClipKey), Rectangle[]> _animationSourceRects;
Dictionary<(string SheetKey, string ClipKey), float[]> _animationDurationsMs;
Dictionary<(string SheetKey, string ClipKey), bool> _animationLoops;
```

`WorldRenderer` may use any private structure needed to store per-entity animation state. Example guidance:

```csharp
private struct AnimationState
{
    public string SheetKey;
    public string ClipKey;
    public int FrameIndex;
    public float ElapsedMs;
    public bool Playing;
}
```

The exact internal representation may vary, but the ownership boundary must remain the same: `AssetManager` owns loaded assets and metadata, while `WorldRenderer` owns live playback state.

### 4.6 MonoGame Integration Example

```csharp
private IAssetManager _assetManager;
private WorldRenderer _worldRenderer;
private SpriteBatch _spriteBatch;

protected override void LoadContent()
{
    _spriteBatch = new SpriteBatch(GraphicsDevice);

    _assetManager = new AssetManager(/* dependencies */);
    _assetManager.LoadAll();

    _worldRenderer = new WorldRenderer(/* dependencies */);
}

protected override void Update(GameTime gameTime)
{
    _worldRenderer.Update(gameTime);
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);

    _spriteBatch.Begin();
    
    // Draw sprites first, then UI/Manager overlays (health bars)
    _worldRenderer.Draw(_spriteBatch);
    _entityManager.Draw(_spriteBatch); 
    
    _spriteBatch.End();

    base.Draw(gameTime);
}
```

### 4.7 Implementation Flow Examples

#### 4.7.1 Startup Loading Flow

```text
Game1.LoadContent()
    -> create SpriteBatch
    -> create AssetManager
    -> AssetManager.LoadAll()
        -> read asset_manifest.json
        -> load sounds
        -> load music
        -> load static images
        -> load fonts
        -> load atlas textures + atlas_map.json
        -> load animation textures + animation_map.json
        -> build internal lookup dictionaries
    -> create WorldRenderer
```

#### 4.7.2 Play and Draw an Animation

```text
Gameplay code requests animation change
    -> WorldRenderer.PlayAnimation(entity, "Hero", "run", restart: false)
    -> WorldRenderer stores or updates AnimationState for that entity

Game1.Update(gameTime)
    -> WorldRenderer.Update(gameTime)
        -> for each tracked entity animation state
            -> query AssetManager.GetAnimationFrameDurationMs(sheetKey, clipKey, frameIndex)
            -> advance elapsed time
            -> move to next frame if needed
            -> if on final frame and clip loops
                -> wrap to frame 0
            -> if on final frame and clip does not loop
                -> hold last frame or stop according to implementation

Game1.Draw(gameTime)
    -> WorldRenderer.Draw(spriteBatch)
        -> for each visible animated entity
            -> query AssetManager.GetAnimationTexture(sheetKey)
            -> query AssetManager.GetAnimationFrameSourceRect(sheetKey, clipKey, frameIndex)
            -> spriteBatch.Draw(texture, entity.Position, sourceRect, Color.White)
```

#### 4.7.3 Play a One-Shot Sound

```text
Gameplay event occurs
    -> sound = AssetManager.GetSound("fire_arrow")
    -> sound.Play()
```

#### 4.7.4 Draw an Atlas Icon

```text
UI code needs icon
    -> texture = AssetManager.GetAtlasTexture("Items")
    -> sourceRect = AssetManager.GetIconSourceRect("Items", "sword")
    -> spriteBatch.Draw(texture, iconPosition, sourceRect, Color.White)
```

### 4.8 Error Handling Guidance

| Case | Required Behavior |
|---|---|
| Missing semantic key | Throw a descriptive exception in development builds. |
| Missing atlas icon entry | Throw a descriptive exception in development builds. |
| Missing animation clip | Throw a descriptive exception in development builds. |
| Invalid frame index | Throw a descriptive exception in development builds. |
| Malformed JSON metadata | Fail fast during `LoadAll()` with a clear message identifying the file and invalid field. |

### 4.9 V1 Constraints

| Constraint | Description |
|---|---|
| No grouped loading | All content is loaded by `LoadAll()` during startup. |
| No unload support | V1 does not unload assets during runtime. |
| No public wrapper types | Public APIs return MonoGame-native types or primitive metadata only. |
| No animation state in `AssetManager` | Animation playback belongs to `WorldRenderer`. |
| No hardcoded content paths in gameplay code | Only semantic keys are allowed outside the asset-loading internals. |

### 4.10 V1 Deliverables

The implementation described by this specification must produce the following concrete code artifacts:

| Deliverable | Description |
|---|---|
| `AssetManager` | Concrete loader/cache class for assets and metadata. |
| `IAssetManager` | Interface for runtime asset queries. |
| `WorldRenderer` | Concrete rendering-support class that owns per-entity animation playback state. |
| `asset_manifest.json` | Semantic lookup manifest for all asset categories. |
| `atlas_map.json` files | Per-atlas rectangle metadata files. |
| `animation_map.json` files | Per-sheet animation clip and frame timing metadata files. |

This document is the required V1 source of truth for the asset-loading and world-rendering boundary.
