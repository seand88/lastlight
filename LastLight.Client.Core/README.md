# LastLight Client Core - Asset & Content Pipeline

This project manages the shared game logic and the centralized asset pipeline for all platforms (Desktop, Android, iOS).

## Directory Overview

### `/Assets`
Contains **raw, uncompiled assets**. This is the "Source of Truth" for developers. 
- `/Graphics/Icons`: Source PNGs for item icons.

### `/Content`
Contains the **MonoGame Content Pipeline** (MGCB) source files and tooling.
- `Content.mgcb`: The main configuration file for compiling assets.
- Assets here should always be kept up-to-date with their raw counterparts.

## Workflow: Adding Assets

### 1. Atlases (Items, Icons)
If adding new item icons:
1. Drop the raw PNG into `Assets/Graphics/Icons/`.
2. Run the packer tool from the repository root:
   ```bash
   dotnet run --project LastLight.Tools -- pack-assets
   ```
   This generates `icons_atlas.png` and `icons_map.json` directly into the `Content/` folder.

### 2. Individual Sprites
If you have pre-ready sprites (e.g., Player, Bullets):
1. Add them directly to the appropriate subfolder in `Content/`.
2. Register them in `Content.mgcb`.

### 3. Compiling Content
Content compilation to `.xnb` happens **automatically** during every `dotnet build` or `dotnet run` via a custom target in the project file. 

If you ever need to trigger a manual rebuild of only the content, you can use the tool manifest from the root directory:
```bash
dotnet mgcb /@:Content/Content.mgcb /platform:DesktopGL /workingDir:Content
```

## Build & Deployment
At build time, the **compiled assets** (all `.xnb` files) located in `bin/Content` are automatically copied to the platform-specific project's output directory. This ensures that all clients share the exact same asset data.
