using System;
using System.Collections.Generic;

namespace LastLight.Common;

public class WorldManager
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int TileSize { get; private set; }
    public TileType[,] Tiles { get; private set; }

    private enum BiomeType { Forest, Desert, Swamp, Mountains }

    public void GenerateWorld(int seed, int width, int height, int tileSize)
    {
        Width = width;
        Height = height;
        TileSize = tileSize;
        Tiles = new TileType[width, height];
        
        var random = new Random(seed);

        // 1. Create Biome Seeds
        int numBiomes = 10;
        var biomeSeeds = new List<(int x, int y, BiomeType type)>();
        for (int i = 0; i < numBiomes; i++)
        {
            biomeSeeds.Add((random.Next(width), random.Next(height), (BiomeType)random.Next(4)));
        }

        // 2. Assign tiles to nearest biome
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Find nearest seed
                BiomeType currentBiome = BiomeType.Forest;
                float minDist = float.MaxValue;
                foreach (var s in biomeSeeds)
                {
                    float d = (float)(Math.Pow(x - s.x, 2) + Math.Pow(y - s.y, 2));
                    if (d < minDist)
                    {
                        minDist = d;
                        currentBiome = s.type;
                    }
                }

                // 3. Generate content based on biome
                int r = random.Next(100);
                switch (currentBiome)
                {
                    case BiomeType.Forest:
                        if (r < 5) Tiles[x, y] = TileType.Wall;
                        else if (r < 10) Tiles[x, y] = TileType.Water;
                        else Tiles[x, y] = TileType.Grass;
                        break;
                    case BiomeType.Desert:
                        if (r < 2) Tiles[x, y] = TileType.Wall;
                        else Tiles[x, y] = TileType.Sand;
                        break;
                    case BiomeType.Swamp:
                        if (r < 40) Tiles[x, y] = TileType.Water;
                        else if (r < 45) Tiles[x, y] = TileType.Wall;
                        else Tiles[x, y] = TileType.Grass;
                        break;
                    case BiomeType.Mountains:
                        if (r < 25) Tiles[x, y] = TileType.Wall;
                        else if (r < 35) Tiles[x, y] = TileType.Sand;
                        else Tiles[x, y] = TileType.Grass;
                        break;
                }

                // Borders are always walls
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    Tiles[x, y] = TileType.Wall;
                }
            }
        }
        
        // 4. Ensure starting area is clear (spawn is usually around 400, 300)
        int startX = (400 / tileSize);
        int startY = (300 / tileSize);
        for(int x = startX - 3; x <= startX + 3; x++)
        {
            for(int y = startY - 3; y <= startY + 3; y++)
            {
                if(x > 0 && x < width - 1 && y > 0 && y < height - 1)
                    Tiles[x, y] = TileType.Grass;
            }
        }
    }

    public bool IsWalkable(Vector2 position)
    {
        int tx = (int)(position.X / TileSize);
        int ty = (int)(position.Y / TileSize);

        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return false;
        
        var tile = Tiles[tx, ty];
        return tile == TileType.Grass || tile == TileType.Sand;
    }
    
    public bool IsShootable(Vector2 position)
    {
        int tx = (int)(position.X / TileSize);
        int ty = (int)(position.Y / TileSize);

        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return false;
        
        var tile = Tiles[tx, ty];
        return tile != TileType.Wall;
    }
}