using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace LastLight.Client.Core;

public sealed class TileMapRenderer : IDisposable
{
    private const int ChunkSize = 32; // 32x32 tiles
    private const int TilePixelSize = 32;
    private const int ChunkPixelSize = ChunkSize * TilePixelSize; // 1024x1024

    private readonly IAssetManager _assetManager;
    private RenderTarget2D[,]? _chunks;
    private int _worldWidth;
    private int _worldHeight;
    private bool _isDisposed;

    public TileMapRenderer(IAssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public void BakeWorld(GraphicsDevice graphicsDevice, WorldManager worldManager)
    {
        DisposeChunks();

        _worldWidth = worldManager.Width;
        _worldHeight = worldManager.Height;

        int numChunksX = (int)Math.Ceiling((float)_worldWidth / ChunkSize);
        int numChunksY = (int)Math.Ceiling((float)_worldHeight / ChunkSize);

        _chunks = new RenderTarget2D[numChunksX, numChunksY];

        using var spriteBatch = new SpriteBatch(graphicsDevice);
        var atlas = _assetManager.GetAtlasTexture("GroundTiles");

        for (int cy = 0; cy < numChunksY; cy++)
        {
            for (int cx = 0; cx < numChunksX; cx++)
            {
                var target = new RenderTarget2D(
                    graphicsDevice, 
                    ChunkPixelSize, 
                    ChunkPixelSize, 
                    false, 
                    SurfaceFormat.Color, 
                    DepthFormat.None
                );

                graphicsDevice.SetRenderTarget(target);
                graphicsDevice.Clear(Color.Transparent);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

                for (int ty = 0; ty < ChunkSize; ty++)
                {
                    for (int tx = 0; tx < ChunkSize; tx++)
                    {
                        int worldX = cx * ChunkSize + tx;
                        int worldY = cy * ChunkSize + ty;

                        if (worldX >= _worldWidth || worldY >= _worldHeight) continue;

                        string key = worldManager.Tiles[worldX, worldY] switch
                        {
                            TileType.Grass => "grass",
                            TileType.Water => "water",
                            TileType.Wall => "wall",
                            TileType.Sand => "sand",
                            _ => string.Empty
                        };

                        if (string.IsNullOrEmpty(key)) continue;

                        var sourceRect = _assetManager.GetIconSourceRect("GroundTiles", key);
                        var destRect = new Rectangle(tx * TilePixelSize, ty * TilePixelSize, TilePixelSize, TilePixelSize);
                        spriteBatch.Draw(atlas, destRect, sourceRect, Color.White);
                    }
                }

                spriteBatch.End();
                _chunks[cx, cy] = target;
            }
        }

        graphicsDevice.SetRenderTarget(null);
    }

    public void Draw(SpriteBatch spriteBatch, Camera camera)
    {
        if (_chunks == null) return;

        var viewport = camera.GetVisibleWorldBounds();
        
        int startChunkX = Math.Max(0, viewport.Left / ChunkPixelSize);
        int startChunkY = Math.Max(0, viewport.Top / ChunkPixelSize);
        int endChunkX = Math.Min(_chunks.GetLength(0) - 1, viewport.Right / ChunkPixelSize);
        int endChunkY = Math.Min(_chunks.GetLength(1) - 1, viewport.Bottom / ChunkPixelSize);

        for (int cy = startChunkY; cy <= endChunkY; cy++)
        {
            for (int cx = startChunkX; cx <= endChunkX; cx++)
            {
                var chunk = _chunks[cx, cy];
                if (chunk == null) continue;

                var position = new Microsoft.Xna.Framework.Vector2(cx * ChunkPixelSize, cy * ChunkPixelSize);
                spriteBatch.Draw(chunk, position, Color.White);
            }
        }
    }

    private void DisposeChunks()
    {
        if (_chunks == null) return;

        for (int y = 0; y < _chunks.GetLength(1); y++)
        {
            for (int x = 0; x < _chunks.GetLength(0); x++)
            {
                _chunks[x, y]?.Dispose();
            }
        }
        _chunks = null;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        DisposeChunks();
        _isDisposed = true;
    }
}
