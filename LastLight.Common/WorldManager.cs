using System;
using System.Collections.Generic;

namespace LastLight.Common;

public class WorldManager
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int TileSize { get; private set; }
    public TileType[,] Tiles { get; private set; }

    public enum GenerationStyle { Biomes, Dungeon, Nexus }
    private enum BiomeType { Forest, Desert, Swamp, Mountains }

    public void GenerateWorld(int seed, int width, int height, int tileSize, GenerationStyle style = GenerationStyle.Biomes)
    {
        Width = width;
        Height = height;
        TileSize = tileSize;
        Tiles = new TileType[width, height];
        
        var random = new Random(seed);

        if (style == GenerationStyle.Nexus)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Tiles[x, y] = (x == 0 || x == width - 1 || y == 0 || y == height - 1) ? TileType.Wall : TileType.Grass;
            return;
        }

        if (style == GenerationStyle.Biomes)
        {
            GenerateBiomes(random, width, height);
        }
        else
        {
            GenerateDungeon(random, width, height);
        }
        
        // Ensure starting area is clear
        int startX = (width / 2);
        int startY = (height / 2);
        for(int x = startX - 2; x <= startX + 2; x++)
            for(int y = startY - 2; y <= startY + 2; y++)
                if(x > 0 && x < width - 1 && y > 0 && y < height - 1)
                    Tiles[x, y] = TileType.Grass;
    }

    private void GenerateBiomes(Random random, int width, int height)
    {
        int numBiomes = 10;
        var biomeSeeds = new List<(int x, int y, BiomeType type)>();
        for (int i = 0; i < numBiomes; i++) biomeSeeds.Add((random.Next(width), random.Next(height), (BiomeType)random.Next(4)));

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                BiomeType currentBiome = BiomeType.Forest;
                float minDist = float.MaxValue;
                foreach (var s in biomeSeeds) {
                    float d = (float)(Math.Pow(x - s.x, 2) + Math.Pow(y - s.y, 2));
                    if (d < minDist) { minDist = d; currentBiome = s.type; }
                }

                int r = random.Next(100);
                switch (currentBiome) {
                    case BiomeType.Forest: Tiles[x, y] = r < 5 ? TileType.Wall : (r < 10 ? TileType.Water : TileType.Grass); break;
                    case BiomeType.Desert: Tiles[x, y] = r < 2 ? TileType.Wall : TileType.Sand; break;
                    case BiomeType.Swamp: Tiles[x, y] = r < 40 ? TileType.Water : (r < 45 ? TileType.Wall : TileType.Grass); break;
                    case BiomeType.Mountains: Tiles[x, y] = r < 25 ? TileType.Wall : (r < 35 ? TileType.Sand : TileType.Grass); break;
                }
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) Tiles[x, y] = TileType.Wall;
            }
        }
    }

    private void GenerateDungeon(Random random, int width, int height)
    {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) Tiles[x, y] = TileType.Wall;
                else {
                    Tiles[x, y] = random.Next(100) < 20 ? TileType.Wall : TileType.Grass;
                }
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
        return Tiles[tx, ty] != TileType.Wall;
    }
}