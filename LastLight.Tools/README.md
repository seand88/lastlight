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

### 1. `generate-sprites`
**Purpose:** Processes all graphics from the source directory and prepares them for the game.

- **Source:** `LastLight.Client.Core/Assets/Graphics/`
- **Output:** `LastLight.Client.Core/Assets/Output/` (All files are generated as siblings in this folder)

#### Sub-workflows:
- **Final Assets:** Sourced from `Assets/Graphics/Final/`. These are copied directly to `Output/`. Filenames are converted to lowercase and prepended with the lowercase folder name (e.g., `Final/Login/Background.png` -> `Output/login_background.png`).
- **Raw Assets:** Sourced from `Assets/Graphics/Raw/`. These are packed into texture atlases.
    - Generates `[category]_atlas.png` and `[category]_map.json`.
    - Detects dimensions automatically.
    - All filenames and map entries are forced to lowercase.

### 2. `generate-sounds`
**Purpose:** Recursively copies audio files from assets to the content directory.

- **Source:** `LastLight.Client.Core/Assets/Audio/`
- **Output:** `LastLight.Client.Core/Content/Audio/`
- **Behavior:** Mirrors the directory structure. Filenames are preserved exactly as they are in the source (no renaming or casing changes).

### 3. `resize`
**Purpose:** Resizes an individual image to a target width while automatically maintaining its original aspect ratio.

- **Arguments:** `<path_to_image> <target_width>`
- **Behavior:** Overwrites the original file with the resized version.

**Example:**
```bash
dotnet run --project LastLight.Tools -- resize "C:\Path\To\Image.png" 300
```

---

## Adding New Commands (Template)
To extend this tool, follow these steps:

1. **Create a Command Class:** Add a new static class in `LastLight.Tools/Commands/`.
2. **Implement Logic:** Create an `Execute(string[] args)` method.
3. **Register in Program:** Update `LastLight.Tools/Program.cs` to recognize the new command string.
4. **Update README:** Document the command in this file.

### Template:
```csharp
namespace LastLight.Tools.Commands;

public static class MyNewCommand
{
    public static void Execute(string[] args)
    {
        // Your logic here
    }
}
```
