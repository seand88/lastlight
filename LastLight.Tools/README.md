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
**Purpose:** Scans the raw graphics directory and packs individual PNG files into optimized texture atlases.

- **Source:** `LastLight.Client.Core/Assets/Graphics/`
- **Output:** `LastLight.Client.Core/Content/Graphics/Icons/`
- **Generates:**
    - `[category]_atlas.png`: A single combined texture.
    - `[category]_map.json`: A coordinate mapping (JSON) so the game knows where each sprite is located within the atlas.

**Example:**
```bash
dotnet run --project LastLight.Tools -- pack-assets
```

### 2. `resize`
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
