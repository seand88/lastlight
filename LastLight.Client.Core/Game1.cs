using System;
using System.Collections.Generic;
using System.Linq;
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
    private BossManager _bossManager = new();
    private ItemManager _itemManager = new();
    private LastLight.Common.WorldManager _worldManager = new();
    private Camera _camera;
    private float _moveSpeed = 200f;
    private float _shootInterval = 0.1f;
    private float _shootTimer = 0f;
    private int _bulletCounter = 0;
    
    public static double TotalTime;

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
                _localPlayer.Level = response.Level;
                _localPlayer.Experience = response.Experience;
                _localPlayer.CurrentWeapon = response.CurrentWeapon;
            }
        };
        _networking.OnSpawnBullet = (s) => { if(s.OwnerId != _localPlayer.Id) _bulletManager.Spawn(s.BulletId, s.OwnerId, new Vector2(s.Position.X, s.Position.Y), new Vector2(s.Velocity.X, s.Velocity.Y)); };
        _networking.OnBulletHit = (h) => _bulletManager.Destroy(h.BulletId);
        _networking.OnEnemySpawn = _enemyManager.HandleSpawn;
        _networking.OnEnemyUpdate = _enemyManager.HandleUpdate;
        _networking.OnEnemyDeath = _enemyManager.HandleDeath;
        _networking.OnSpawnerSpawn = _spawnerManager.HandleSpawn;
        _networking.OnSpawnerUpdate = _spawnerManager.HandleUpdate;
        _networking.OnSpawnerDeath = _spawnerManager.HandleDeath;
        _networking.OnBossSpawn = _bossManager.HandleSpawn;
        _networking.OnBossUpdate = _bossManager.HandleUpdate;
        _networking.OnBossDeath = _bossManager.HandleDeath;
        _networking.OnItemSpawn = _itemManager.HandleSpawn;
        _networking.OnItemPickup = _itemManager.HandlePickup;
        _networking.OnWorldInit = (init) => _worldManager.GenerateWorld(init.Seed, init.Width, init.Height, init.TileSize);
    }

    private void HandlePlayerUpdate(LastLight.Common.AuthoritativePlayerUpdate update)
    {
        if (update.PlayerId == _localPlayer.Id)
        {
            _localPlayer.Position = new Vector2(update.Position.X, update.Position.Y);
            _localPlayer.CurrentHealth = update.CurrentHealth;
            _localPlayer.MaxHealth = update.MaxHealth;
            _localPlayer.Level = update.Level;
            _localPlayer.Experience = update.Experience;
            _localPlayer.CurrentWeapon = update.CurrentWeapon;
            _localPlayer.PendingInputs.RemoveAll(i => i.InputSequenceNumber <= update.LastProcessedInputSequence);
            foreach (var input in _localPlayer.PendingInputs) _localPlayer.ApplyInput(input, _moveSpeed, _worldManager);
            return;
        }
        if (!_otherPlayers.TryGetValue(update.PlayerId, out var player)) { player = new Player { Id = update.PlayerId, IsLocal = false, MaxHealth = 100 }; _otherPlayers[update.PlayerId] = player; }
        player.Position = new Vector2(update.Position.X, update.Position.Y);
        player.Velocity = new Vector2(update.Velocity.X, update.Velocity.Y);
        player.CurrentHealth = update.CurrentHealth;
    }

    protected override void Initialize() { _networking.Connect("localhost", 5000); Exiting += (sender, args) => _networking.Disconnect(); base.Initialize(); }

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
        void FillRect(int x, int y, int w, int h, Color color) { for (int ix = x; ix < x + w; ix++) for (int iy = y; iy < y + h; iy++) if (ix >= 0 && ix < size && iy >= 0 && iy < size) data[iy * size + ix] = color; }
        
        // Row 0 (Y=0)
        // Player (0, 0, 32, 32)
        FillRect(4, 4, 24, 24, Color.LightGray); FillRect(8, 10, 4, 6, Color.Black); FillRect(20, 10, 4, 6, Color.Black); FillRect(2, 12, 4, 16, Color.DarkSlateGray); FillRect(26, 12, 4, 12, Color.Goldenrod);
        // Enemy (32, 0, 32, 32)
        FillRect(32+4, 4, 24, 24, new Color(139, 0, 0)); FillRect(32+8, 10, 6, 4, Color.Yellow); FillRect(32+18, 10, 6, 4, Color.Yellow); FillRect(32, 20, 32, 4, Color.Black);
        // Wall (64, 0, 32, 32)
        FillRect(64, 0, 32, 32, Color.DimGray); FillRect(66, 2, 28, 28, Color.Gray); FillRect(64, 14, 32, 2, Color.Black); FillRect(80, 0, 2, 14, Color.Black);
        // Grass (96, 0, 32, 32)
        FillRect(96, 0, 32, 32, new Color(34, 139, 34)); data[(2 * size) + 100] = Color.LimeGreen; data[(25 * size) + 120] = Color.LimeGreen;

        // Row 1 (Y=32)
        // Potion (0, 32, 32, 32)
        FillRect(8, 40, 16, 20, Color.White); FillRect(10, 44, 12, 14, Color.Red); FillRect(12, 36, 8, 4, Color.SaddleBrown);
        // WeaponUpgrade (32, 32, 32, 32)
        FillRect(32+8, 32+8, 16, 16, Color.Gold); FillRect(32+12, 32+4, 8, 24, Color.LightYellow);
        // Sand (64, 32, 32, 32)
        FillRect(64, 32, 32, 32, Color.SandyBrown); data[(35 * size) + 70] = Color.SaddleBrown; data[(40 * size) + 85] = Color.SaddleBrown;
        // Water (96, 32, 32, 32)
        FillRect(96, 32, 32, 32, new Color(30, 144, 255)); FillRect(100, 40, 10, 2, Color.AliceBlue);

        // Row 2 (Y=64)
        // Spawner (0, 64, 64, 64)
        FillRect(0, 64, 64, 64, Color.Indigo); FillRect(4, 68, 56, 56, Color.Purple); FillRect(16, 80, 32, 32, Color.Black); for(int g=0; g<10; g++) data[(80+g)*size + 32] = Color.Magenta;

        // Boss (128, 0, 128, 128)
        FillRect(128, 0, 128, 128, Color.DarkSlateBlue); FillRect(140, 20, 30, 30, Color.Yellow); FillRect(190, 20, 30, 30, Color.Yellow); FillRect(128, 80, 128, 20, Color.Black); FillRect(128, 0, 20, 40, Color.Gray); FillRect(236, 0, 20, 40, Color.Gray);

        _atlas.SetData(data);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();
        TotalTime = gameTime.TotalGameTime.TotalSeconds;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var input = HandleInput(dt);
        if (input != null) { _localPlayer.PendingInputs.Add(input); _localPlayer.ApplyInput(input, _moveSpeed, _worldManager); _networking.SendInputRequest(input); }
        foreach (var player in _otherPlayers.Values) player.Update(gameTime, _worldManager);
        _bulletManager.Update(gameTime);
        _networking.PollEvents();
        base.Update(gameTime);
    }

    private LastLight.Common.InputRequest? HandleInput(float dt)
    {
        var kb = Keyboard.GetState(); var ms = Mouse.GetState(); Vector2 mv = Vector2.Zero;
        if (kb.IsKeyDown(Keys.W)) mv.Y -= 1; if (kb.IsKeyDown(Keys.S)) mv.Y += 1; if (kb.IsKeyDown(Keys.A)) mv.X -= 1; if (kb.IsKeyDown(Keys.D)) mv.X += 1;
        if (mv != Vector2.Zero) mv.Normalize();
        float interval = _localPlayer.CurrentWeapon == LastLight.Common.WeaponType.Rapid ? 0.05f : _shootInterval;
        _shootTimer += dt;
        if (ms.LeftButton == ButtonState.Pressed && _shootTimer >= interval) { _shootTimer = 0; Shoot(_camera.ScreenToWorld(ms.Position.ToVector2())); }
        return new LastLight.Common.InputRequest { Movement = new LastLight.Common.Vector2(mv.X, mv.Y), DeltaTime = dt, InputSequenceNumber = _bulletCounter++ };
    }

    private void Shoot(Vector2 targetPos)
    {
        var baseDir = targetPos - _localPlayer.Position; if (baseDir == Vector2.Zero) baseDir = new Vector2(1, 0); baseDir.Normalize();
        float baseAngle = (float)Math.Atan2(baseDir.Y, baseDir.X);
        void Fire(float a) {
            var d = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)); var v = d * 500f; int bid = _bulletCounter++;
            _bulletManager.Spawn(bid, _localPlayer.Id, _localPlayer.Position, v);
            _networking.SendFireRequest(new LastLight.Common.FireRequest { BulletId = bid, Direction = new LastLight.Common.Vector2(d.X, d.Y) });
        }
        switch (_localPlayer.CurrentWeapon) {
            case LastLight.Common.WeaponType.Single: Fire(baseAngle); break;
            case LastLight.Common.WeaponType.Double: Fire(baseAngle - 0.05f); Fire(baseAngle + 0.05f); break;
            case LastLight.Common.WeaponType.Spread: Fire(baseAngle - 0.2f); Fire(baseAngle); Fire(baseAngle + 0.2f); break;
            case LastLight.Common.WeaponType.Rapid: Fire(baseAngle); break;
        }
    }

    private void DrawWorld()
    {
        if (_worldManager.Tiles == null) return;
        for (int x = 0; x < 100; x++) for (int y = 0; y < 100; y++) {
            Rectangle s = _worldManager.Tiles[x, y] switch { 
                LastLight.Common.TileType.Grass => new Rectangle(96, 0, 32, 32), 
                LastLight.Common.TileType.Water => new Rectangle(96, 32, 32, 32), 
                LastLight.Common.TileType.Wall => new Rectangle(64, 0, 32, 32), 
                LastLight.Common.TileType.Sand => new Rectangle(64, 32, 32, 32),
                _ => Rectangle.Empty 
            };
            _spriteBatch.Draw(_atlas, new Rectangle(x * 32, y * 32, 32, 32), s, Color.White);
        }
    }

    private void DrawHUD()
    {
        _spriteBatch.Begin();
        int vw = _graphics.PreferredBackBufferWidth; int vh = _graphics.PreferredBackBufferHeight;
        float hpP = (float)_localPlayer.CurrentHealth / _localPlayer.MaxHealth;
        _spriteBatch.Draw(_pixel, new Rectangle(20, vh - 40, 200, 20), Color.DarkRed); _spriteBatch.Draw(_pixel, new Rectangle(20, vh - 40, (int)(200 * hpP), 20), Color.Red);
        float exP = (float)_localPlayer.Experience / (_localPlayer.Level * 100);
        _spriteBatch.Draw(_pixel, new Rectangle(20, vh - 65, 200, 10), Color.DarkSlateGray); _spriteBatch.Draw(_pixel, new Rectangle(20, vh - 65, (int)(200 * exP), 10), Color.Yellow);
        _spriteBatch.Draw(_atlas, new Rectangle(20, 20, 48, 48), new Rectangle(0, 0, 32, 32), Color.White);
        for(int i=0; i<_localPlayer.Level; i++) _spriteBatch.Draw(_pixel, new Rectangle(75 + (i*12), 35, 8, 8), Color.Gold);
        // Boss Health Bar
        var boss = _bossManager.GetActiveBosses().FirstOrDefault();
        if (boss != null) {
            float bP = (float)boss.CurrentHealth / boss.MaxHealth;
            _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 200, 20, 400, 20), Color.Black * 0.5f);
            _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 200, 20, (int)(400 * bP), 20), Color.Purple);
        }
        // Minimap
        int ms = 200; int mx = vw - ms - 20; int my = 20; _spriteBatch.Draw(_pixel, new Rectangle(mx - 2, my - 2, ms + 4, ms + 4), Color.Black * 0.5f);
        if (_worldManager.Tiles != null) for (int x = 0; x < 100; x++) for (int y = 0; y < 100; y++) {
            var c = _worldManager.Tiles[x, y] switch { 
                LastLight.Common.TileType.Wall => Color.Gray, 
                LastLight.Common.TileType.Water => Color.Blue, 
                LastLight.Common.TileType.Sand => Color.SandyBrown,
                _ => Color.Transparent 
            };
            if (c != Color.Transparent) _spriteBatch.Draw(_pixel, new Rectangle(mx + x*2, my + y*2, 2, 2), c * 0.5f);
        }
        void Dot(Vector2 p, Color c, int s = 4) { _spriteBatch.Draw(_pixel, new Rectangle(mx + (int)(p.X/32)*2 - s/2, my + (int)(p.Y/32)*2 - s/2, s, s), c); }
        Dot(_localPlayer.Position, Color.White, 6);
        foreach(var p in _otherPlayers.Values) Dot(p.Position, Color.Red);
        foreach(var e in _enemyManager.GetAllEnemies()) if(e.Active) Dot(e.Position, Color.LimeGreen, 2);
        foreach(var s in _spawnerManager.GetAllSpawners()) if(s.Active) Dot(s.Position, Color.Purple, 4);
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
        _bossManager.Draw(_spriteBatch, _atlas, _pixel);
        _localPlayer.Draw(_spriteBatch, _atlas, _pixel);
        foreach (var p in _otherPlayers.Values) p.Draw(_spriteBatch, _atlas, _pixel);
        _enemyManager.Draw(_spriteBatch, _atlas, _pixel);
        _bulletManager.Draw(_spriteBatch, _pixel);
        _spriteBatch.End();
        DrawHUD();
        base.Draw(gameTime);
    }
}
