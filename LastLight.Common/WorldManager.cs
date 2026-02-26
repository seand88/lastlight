using System;

namespace LastLight.Common;

public class WorldManager
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int TileSize { get; private set; }
    public TileType[,] Tiles { get; private set; }

    public void GenerateWorld(int seed, int width, int height, int tileSize)
    {
        Width = width;
        Height = height;
        TileSize = tileSize;
        Tiles = new TileType[width, height];
        
        var random = new Random(seed);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Borders are walls
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    Tiles[x, y] = TileType.Wall;
                }
                else
                {
                    // Generate random clusters or noise
                    int r = random.Next(100);
                    if (r < 5) 
                        Tiles[x, y] = TileType.Wall; // 5% chance of random rock/wall
                    else if (r < 15) 
                        Tiles[x, y] = TileType.Water; // 10% chance of puddle
                    else 
                        Tiles[x, y] = TileType.Grass;
                }
            }
        }
        
        // Ensure starting area is clear
        int startX = (400 / tileSize);
        int startY = (300 / tileSize);
        for(int x = startX - 2; x <= startX + 2; x++)
        {
            for(int y = startY - 2; y <= startY + 2; y++)
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
        
        return Tiles[tx, ty] == TileType.Grass;
    }
    
    public bool IsShootable(Vector2 position)
    {
        int tx = (int)(position.X / TileSize);
        int ty = (int)(position.Y / TileSize);

        if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return false;
        
        var tile = Tiles[tx, ty];
        return tile == TileType.Grass || tile == TileType.Water;
    }
}
