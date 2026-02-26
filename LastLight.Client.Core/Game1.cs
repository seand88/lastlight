using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LastLight.Client.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ClientNetworking _networking;
    private Texture2D _pixel;
    private Texture2D _atlas;
    private Player _localPlayer;
    private Dictionary<int, Player> _otherPlayers = new();
    private BulletManager _bulletManager = new();
    private EnemyManager _enemyManager = new();
    private SpawnerManager _spawnerManager = new();
    private ItemManager _itemManager = new();
    private LastLight.Common.WorldManager _worldManager = new();
    private Camera _camera;
    private float _moveSpeed = 200f;
    private float _shootInterval = 0.1f;
    private float _shootTimer = 0f;
    private int _bulletCounter = 0;
    private int _score = 0;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _networking = new ClientNetworking();
        _localPlayer = new Player { IsLocal = true, Position = new Vector2(400, 300) };
        _networking.OnPlayerUpdate = HandlePlayerUpdate;
        _networking.OnJoinResponse = (response) => 
        {
            if (response.Success) 
            {
                _localPlayer.Id = response.PlayerId;
                _localPlayer.MaxHealth = response.MaxHealth;
                _localPlayer.CurrentHealth = response.MaxHealth;
            }
        };
        _networking.OnSpawnBullet = HandleSpawnBullet;
        _networking.OnBulletHit = HandleBulletHit;
        
        _networking.OnEnemySpawn = _enemyManager.HandleSpawn;
        _networking.OnEnemyUpdate = _enemyManager.HandleUpdate;
        _networking.OnEnemyDeath = _enemyManager.HandleDeath;

        _networking.OnSpawnerSpawn = _spawnerManager.HandleSpawn;
        _networking.OnSpawnerUpdate = _spawnerManager.HandleUpdate;
        _networking.OnSpawnerDeath = _spawnerManager.HandleDeath;

        _networking.OnItemSpawn = _itemManager.HandleSpawn;
        _networking.OnItemPickup = _itemManager.HandlePickup;

        _networking.OnWorldInit = (init) =>
        {
            _worldManager.GenerateWorld(init.Seed, init.Width, init.Height, init.TileSize);
        };
    }

    private void HandleBulletHit(LastLight.Common.BulletHit hit)
    {
        _bulletManager.Destroy(hit.BulletId);
    }

    private void HandleSpawnBullet(LastLight.Common.SpawnBullet spawn)
    {
        if (spawn.OwnerId == _localPlayer.Id) return;

        _bulletManager.Spawn(spawn.BulletId, spawn.OwnerId, new Vector2(spawn.Position.X, spawn.Position.Y), new Vector2(spawn.Velocity.X, spawn.Velocity.Y));
    }

    private void HandlePlayerUpdate(LastLight.Common.AuthoritativePlayerUpdate update)
    {
        if (update.PlayerId == _localPlayer.Id)
        {
            _localPlayer.Position = new Vector2(update.Position.X, update.Position.Y);
            _localPlayer.CurrentHealth = update.CurrentHealth;
            
            _localPlayer.PendingInputs.RemoveAll(i => i.InputSequenceNumber <= update.LastProcessedInputSequence);

            foreach (var input in _localPlayer.PendingInputs)
            {
                _localPlayer.ApplyInput(input, _moveSpeed, _worldManager);
            }
            return;
        }

        if (!_otherPlayers.TryGetValue(update.PlayerId, out var player))
        {
            player = new Player { Id = update.PlayerId, IsLocal = false, MaxHealth = 100 };
            _otherPlayers[update.PlayerId] = player;
        }

        player.Position = new Vector2(update.Position.X, update.Position.Y);
        player.Velocity = new Vector2(update.Velocity.X, update.Velocity.Y);
        player.CurrentHealth = update.CurrentHealth;
    }

    protected override void Initialize()
    {
        _networking.Connect("localhost", 5000);
        Exiting += (sender, args) => _networking.Disconnect();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        GenerateAtlas();

        _camera = new Camera(GraphicsDevice.Viewport);
    }

    private void GenerateAtlas()
    {
        int size = 256;
        _atlas = new Texture2D(GraphicsDevice, size, size);
        Color[] data = new Color[size * size];

        for (int i = 0; i < data.Length; i++) data[i] = Color.Transparent;

        void FillRect(int x, int y, int w, int h, Color color)
        {
            for (int ix = x; ix < x + w; ix++)
                for (int iy = y; iy < y + h; iy++)
                    if (ix >= 0 && ix < size && iy >= 0 && iy < size)
                        data[iy * size + ix] = color;
        }

        // --- Grass (96, 0, 32, 32) ---
        FillRect(96, 0, 32, 32, new Color(34, 139, 34)); // Forest Green
        // Add some grass blades
        data[(2 * size) + 100] = Color.LimeGreen; data[(5 * size) + 115] = Color.LimeGreen;
        data[(20 * size) + 105] = Color.LimeGreen; data[(25 * size) + 120] = Color.LimeGreen;

        // --- Water (96, 32, 32, 32) ---
        FillRect(96, 32, 32, 32, new Color(30, 144, 255)); // Dodger Blue
        // Add some waves
        FillRect(100, 40, 10, 2, Color.AliceBlue);
        FillRect(110, 55, 10, 2, Color.AliceBlue);

        // --- Wall (64, 0, 32, 32) ---
        FillRect(64, 0, 32, 32, Color.DimGray);
        FillRect(66, 2, 28, 28, Color.Gray);
        // Bricks
        FillRect(64, 14, 32, 2, Color.Black);
        FillRect(80, 0, 2, 14, Color.Black);
        FillRect(72, 16, 2, 16, Color.Black);

        // --- Player (0, 0, 32, 32) ---
        FillRect(4, 4, 24, 24, Color.LightGray); // Helmet/Body
        FillRect(8, 10, 4, 6, Color.Black); // Left Eye
        FillRect(20, 10, 4, 6, Color.Black); // Right Eye
        FillRect(2, 12, 4, 16, Color.DarkSlateGray); // Shield/Arm
        FillRect(26, 12, 4, 12, Color.Goldenrod); // Sword hilt

        // --- Enemy (32, 0, 32, 32) ---
        FillRect(36, 4, 24, 24, new Color(139, 0, 0)); // Dark Red Body
        FillRect(40, 10, 6, 4, Color.Yellow); // Mean Eyes
        FillRect(50, 10, 6, 4, Color.Yellow);
        FillRect(32, 20, 32, 4, Color.Black); // Mouth/Stitch

        // --- Spawner (0, 64, 64, 64) ---
        FillRect(0, 64, 64, 64, Color.Indigo);
        FillRect(4, 68, 56, 56, Color.Purple);
        FillRect(16, 80, 32, 32, Color.Black); // Dark portal
        // Glow effect
        for(int g=0; g<10; g++) data[(80+g)*size + 32] = Color.Magenta;

        // --- Health Potion (64, 32, 32, 32) ---
        FillRect(72, 40, 16, 20, Color.White); // Bottle
        FillRect(74, 44, 12, 14, Color.Red); // Liquid
        FillRect(76, 36, 8, 4, Color.SaddleBrown); // Cork

        _atlas.SetData(data);
    }

    private int _inputSequenceNumber = 0;

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        var input = HandleInput(dt);
        if (input != null)
        {
            _localPlayer.PendingInputs.Add(input);
            _localPlayer.ApplyInput(input, _moveSpeed, _worldManager);
            _networking.SendInputRequest(input);
        }

        foreach (var player in _otherPlayers.Values)
        {
            player.Update(gameTime, _worldManager);
        }
        _bulletManager.Update(gameTime);
        _networking.PollEvents();

        base.Update(gameTime);
    }

    private LastLight.Common.InputRequest? HandleInput(float dt)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        Vector2 move = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.W)) move.Y -= 1;
        if (keyboard.IsKeyDown(Keys.S)) move.Y += 1;
        if (keyboard.IsKeyDown(Keys.A)) move.X -= 1;
        if (keyboard.IsKeyDown(Keys.D)) move.X += 1;

        if (move != Vector2.Zero)
        {
            move.Normalize();
        }

        _shootTimer += dt;
        if (mouse.LeftButton == ButtonState.Pressed && _shootTimer >= _shootInterval)
        {
            _shootTimer = 0;
            var worldMousePos = _camera.ScreenToWorld(mouse.Position.ToVector2());
            Shoot(worldMousePos);
        }

        return new LastLight.Common.InputRequest
        {
            Movement = new LastLight.Common.Vector2(move.X, move.Y),
            DeltaTime = dt,
            InputSequenceNumber = _inputSequenceNumber++
        };
    }

    private void Shoot(Vector2 targetPos)
    {
        var dir = targetPos - _localPlayer.Position;
        if (dir == Vector2.Zero) dir = new Vector2(1, 0);
        dir.Normalize();

        var vel = dir * 500f;
        int bulletId = _bulletCounter++;
        
        _bulletManager.Spawn(bulletId, _localPlayer.Id, _localPlayer.Position, vel);
        
        _networking.SendFireRequest(new LastLight.Common.FireRequest
        {
            BulletId = bulletId,
            Direction = new LastLight.Common.Vector2(dir.X, dir.Y)
        });
    }

    private void DrawWorld()
    {
        if (_worldManager.Tiles == null) return;

        for (int x = 0; x < _worldManager.Width; x++)
        {
            for (int y = 0; y < _worldManager.Height; y++)
            {
                var tile = _worldManager.Tiles[x, y];
                Rectangle sourceRect = tile switch
                {
                    LastLight.Common.TileType.Grass => new Rectangle(96, 0, 32, 32),
                    LastLight.Common.TileType.Water => new Rectangle(96, 32, 32, 32),
                    LastLight.Common.TileType.Wall => new Rectangle(64, 0, 32, 32),
                    _ => new Rectangle(0, 0, 0, 0)
                };

                _spriteBatch.Draw(_atlas, new Rectangle(x * _worldManager.TileSize, y * _worldManager.TileSize, _worldManager.TileSize, _worldManager.TileSize), sourceRect, Color.White);
            }
        }
    }

    private void DrawHUD()
    {
        _spriteBatch.Begin();
        
        int barWidth = 200;
        float healthPerc = (float)_localPlayer.CurrentHealth / _localPlayer.MaxHealth;
        _spriteBatch.Draw(_pixel, new Rectangle(20, _graphics.PreferredBackBufferHeight - 40, barWidth, 20), Color.DarkRed);
        _spriteBatch.Draw(_pixel, new Rectangle(20, _graphics.PreferredBackBufferHeight - 40, (int)(barWidth * healthPerc), 20), Color.Red);
        
        _spriteBatch.Draw(_atlas, new Rectangle(20, 20, 32, 32), new Rectangle(0, 0, 32, 32), Color.White);
        
        _spriteBatch.End();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _camera.Position = _localPlayer.Position;

        _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix());
        
        DrawWorld();

        _itemManager.Draw(_spriteBatch, _atlas);
        _spawnerManager.Draw(_spriteBatch, _atlas, _pixel);
        _localPlayer.Draw(_spriteBatch, _atlas, _pixel);
        foreach (var player in _otherPlayers.Values)
        {
            player.Draw(_spriteBatch, _atlas, _pixel);
        }
        _enemyManager.Draw(_spriteBatch, _atlas, _pixel);
        _bulletManager.Draw(_spriteBatch, _pixel);
        _spriteBatch.End();

        DrawHUD();

        base.Draw(gameTime);
    }
}