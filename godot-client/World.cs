using Godot;
using System;
using LastLight.Common;

public partial class World : TileMapLayer
{
    private TileSet _tileSet = null!;

    public override void _Ready()
    {
        _tileSet = new TileSet();
        _tileSet.TileSize = new Vector2I(32, 32);
        TileSet = _tileSet;

        var source = new TileSetAtlasSource();
        source.Texture = TextureManager.Atlas;
        source.TextureRegionSize = new Vector2I(32, 32);

        // Define tiles based on atlas
        // TileType.Grass => (96, 0, 32, 32) => Column 3, Row 0 in 32x32 grid
        source.CreateTile(new Vector2I(3, 0)); // Grass
        source.CreateTile(new Vector2I(3, 1)); // Water
        source.CreateTile(new Vector2I(2, 0)); // Wall
        source.CreateTile(new Vector2I(2, 1)); // Sand

        _tileSet.AddSource(source);
    }

    public void Generate(int seed, int width, int height, WorldManager.GenerationStyle style)
    {
        Clear();
        var worldManager = new WorldManager();
        worldManager.GenerateWorld(seed, width, height, 32, style);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tileType = worldManager.Tiles[x, y];
                Vector2I atlasCoords = tileType switch
                {
                    TileType.Grass => new Vector2I(3, 0),
                    TileType.Water => new Vector2I(3, 1),
                    TileType.Wall => new Vector2I(2, 0),
                    TileType.Sand => new Vector2I(2, 1),
                    _ => new Vector2I(-1, -1)
                };

                if (atlasCoords.X != -1)
                {
                    SetCell(new Vector2I(x, y), 0, atlasCoords);
                }
            }
        }
    }
}
