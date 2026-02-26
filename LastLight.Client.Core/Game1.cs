using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using LastLight.Common;
using LiteNetLib;

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
    private Dictionary<int, PortalSpawn> _portals = new();
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
        _localPlayer = new Player { IsLocal = true, Position = new Microsoft.Xna.Framework.Vector2(400, 300) };
        
        _networking.OnJoinResponse = (res) => { if (res.Success) { _localPlayer.Id = res.PlayerId; _localPlayer.MaxHealth = res.MaxHealth; _localPlayer.CurrentHealth = res.MaxHealth; } };
        _networking.OnWorldInit = (init) => { 
            _worldManager.GenerateWorld(init.Seed, init.Width, init.Height, init.TileSize, init.Style);
            
            _enemyManager = new EnemyManager();
            _spawnerManager = new SpawnerManager();
            _bossManager = new BossManager();
            _itemManager = new ItemManager();
            _portals.Clear();
            _bulletManager = new BulletManager();
            
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
            _networking.OnPortalSpawn = (p) => { var clone = new PortalSpawn { PortalId = p.PortalId, Position = p.Position, TargetRoomId = p.TargetRoomId, Name = p.Name }; _portals[clone.PortalId] = clone; };
        };
        _networking.OnPlayerUpdate = HandlePlayerUpdate;
        _networking.OnSpawnBullet = (s) => { if(s.OwnerId != _localPlayer.Id) _bulletManager.Spawn(s.BulletId, s.OwnerId, new Microsoft.Xna.Framework.Vector2(s.Position.X, s.Position.Y), new Microsoft.Xna.Framework.Vector2(s.Velocity.X, s.Velocity.Y)); };
        _networking.OnBulletHit = (h) => _bulletManager.Destroy(h.BulletId);
        _networking.OnPortalSpawn = (p) => { var clone = new PortalSpawn { PortalId = p.PortalId, Position = p.Position, TargetRoomId = p.TargetRoomId, Name = p.Name }; _portals[clone.PortalId] = clone; };
        _networking.OnPortalDeath = (p) => _portals.Remove(p.PortalId);
        
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
    }

    private void HandlePlayerUpdate(AuthoritativePlayerUpdate u)
    {
        if (u.PlayerId == _localPlayer.Id) {
            _localPlayer.Position = new Microsoft.Xna.Framework.Vector2(u.Position.X, u.Position.Y);
            _localPlayer.CurrentHealth = u.CurrentHealth; _localPlayer.MaxHealth = u.MaxHealth;
            _localPlayer.Level = u.Level; _localPlayer.Experience = u.Experience; _localPlayer.CurrentWeapon = u.CurrentWeapon;
            _localPlayer.RoomId = u.RoomId;
            _localPlayer.PendingInputs.RemoveAll(i => i.InputSequenceNumber <= u.LastProcessedInputSequence);
            foreach (var input in _localPlayer.PendingInputs) _localPlayer.ApplyInput(input, _moveSpeed, _worldManager);
            return;
        }
        if (!_otherPlayers.TryGetValue(u.PlayerId, out var p)) { p = new Player { Id = u.PlayerId, IsLocal = false, MaxHealth = 100 }; _otherPlayers[u.PlayerId] = p; }
        p.Position = new Microsoft.Xna.Framework.Vector2(u.Position.X, u.Position.Y); p.Velocity = new Microsoft.Xna.Framework.Vector2(u.Velocity.X, u.Velocity.Y); p.CurrentHealth = u.CurrentHealth; p.RoomId = u.RoomId;
    }

    protected override void Initialize() { _networking.Connect("localhost", 5000); Exiting += (s, a) => _networking.Disconnect(); base.Initialize(); }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1); _pixel.SetData(new[] { Color.White });
        GenerateAtlas();
        _camera = new Camera(GraphicsDevice.Viewport);
    }

    private void GenerateAtlas()
    {
        int size = 256;
        _atlas = new Texture2D(GraphicsDevice, size, size);
        Color[] data = new Color[size * size];
        for (int i = 0; i < data.Length; i++) data[i] = Color.Transparent;

        void FillRect(int x, int y, int w, int h, Color color) {
            for (int ix = x; ix < x + w; ix++)
                for (int iy = y; iy < y + h; iy++)
                    if (ix >= 0 && ix < size && iy >= 0 && iy < size)
                        data[iy * size + ix] = color;
        }

        // --- ROW 0: ENTITIES & TERRAIN ---
        FillRect(4, 4, 24, 24, Color.LightGray); FillRect(8, 10, 4, 6, Color.Black); FillRect(20, 10, 4, 6, Color.Black); FillRect(2, 12, 4, 16, Color.DarkSlateGray); FillRect(26, 12, 4, 12, Color.Goldenrod); // Player
        FillRect(36, 4, 24, 24, new Color(139, 0, 0)); FillRect(40, 10, 6, 4, Color.Yellow); FillRect(50, 10, 6, 4, Color.Yellow); FillRect(32, 20, 32, 4, Color.Black); // Enemy
        FillRect(64, 0, 32, 32, Color.DimGray); FillRect(66, 2, 28, 28, Color.Gray); FillRect(64, 14, 32, 2, Color.Black); FillRect(80, 0, 2, 14, Color.Black); FillRect(72, 16, 2, 16, Color.Black); // Wall
        FillRect(96, 0, 32, 32, new Color(34, 139, 34)); data[(2 * size) + 100] = Color.LimeGreen; data[(25 * size) + 120] = Color.LimeGreen; // Grass

        // --- ROW 1: ITEMS & TILES ---
        FillRect(8, 40, 16, 20, Color.White); FillRect(10, 44, 12, 14, Color.Red); FillRect(12, 36, 8, 4, Color.SaddleBrown); // Potion
        FillRect(40, 40, 16, 16, Color.Gold); FillRect(44, 36, 8, 24, Color.LightYellow); // Weapon Upgrade
        FillRect(64, 32, 32, 32, Color.SandyBrown); data[(35 * size) + 70] = Color.SaddleBrown; data[(40 * size) + 85] = Color.SaddleBrown; // Sand
        FillRect(96, 32, 32, 32, new Color(30, 144, 255)); FillRect(100, 40, 10, 2, Color.AliceBlue); FillRect(110, 55, 10, 2, Color.AliceBlue); // Water

        // --- ROW 2: SPECIALS ---
        FillRect(0, 64, 64, 64, Color.Indigo); FillRect(4, 68, 56, 56, Color.Purple); FillRect(16, 80, 32, 32, Color.Black); for(int g=0; g<10; g++) data[(80+g)*size + 32] = Color.Magenta; // Spawner
        FillRect(64, 64, 32, 32, Color.White); FillRect(70, 70, 20, 20, Color.LightCyan); // PORTAL (NOW WHITE FOR TINTING)
        FillRect(96, 64, 12, 2, Color.White); FillRect(96, 64, 2, 12, Color.White); FillRect(96, 70, 8, 2, Color.White); // 'F'
        FillRect(112, 64, 2, 12, Color.White); FillRect(112, 64, 8, 2, Color.White); FillRect(112, 74, 8, 2, Color.White); FillRect(120, 66, 2, 8, Color.White); // 'D'
        FillRect(128, 64, 2, 12, Color.White); FillRect(140, 64, 2, 12, Color.White); for(int i=0; i<12; i++) if(128+i < 256 && 64+i < 256) data[(64+i)*size + 128+i] = Color.White; // 'N'

        // --- ROW 3: BOSS ---
        FillRect(0, 128, 128, 128, Color.DarkSlateBlue); FillRect(12, 148, 30, 30, Color.Yellow); FillRect(86, 148, 30, 30, Color.Yellow); FillRect(0, 208, 128, 20, Color.Black); FillRect(0, 128, 20, 40, Color.Gray); FillRect(108, 128, 20, 40, Color.Gray);

        _atlas.SetData(data);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();
        TotalTime = gameTime.TotalGameTime.TotalSeconds;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var input = HandleInput(dt);
        if (input != null) { _localPlayer.PendingInputs.Add(input); _localPlayer.ApplyInput(input, _moveSpeed, _worldManager); _networking.SendInputRequest(input); }
        foreach (var p in _otherPlayers.Values) if(p.RoomId == _localPlayer.RoomId) p.Update(gameTime, _worldManager);
        _bulletManager.Update(gameTime, _worldManager);
        _networking.PollEvents();
        base.Update(gameTime);
    }

    private InputRequest? HandleInput(float dt)
    {
        var kb = Keyboard.GetState(); var ms = Mouse.GetState(); Microsoft.Xna.Framework.Vector2 mv = Microsoft.Xna.Framework.Vector2.Zero;
        if (kb.IsKeyDown(Keys.W)) mv.Y -= 1; if (kb.IsKeyDown(Keys.S)) mv.Y += 1; if (kb.IsKeyDown(Keys.A)) mv.X -= 1; if (kb.IsKeyDown(Keys.D)) mv.X += 1;
        if (mv != Microsoft.Xna.Framework.Vector2.Zero) mv.Normalize();
        
        if (kb.IsKeyDown(Keys.Space)) {
            foreach(var p in _portals.Values) {
                if (Microsoft.Xna.Framework.Vector2.Distance(_localPlayer.Position, new Microsoft.Xna.Framework.Vector2(p.Position.X, p.Position.Y)) < 60) {
                    _networking.SendPacket(new PortalUseRequest { PortalId = p.PortalId }, DeliveryMethod.ReliableOrdered);
                    break;
                }
            }
        }

        float interval = _localPlayer.CurrentWeapon == WeaponType.Rapid ? 0.05f : _shootInterval;
        _shootTimer += dt;
        if (ms.LeftButton == ButtonState.Pressed && _shootTimer >= interval) { _shootTimer = 0; Shoot(_camera.ScreenToWorld(ms.Position.ToVector2())); }
        return new InputRequest { Movement = new LastLight.Common.Vector2(mv.X, mv.Y), DeltaTime = dt, InputSequenceNumber = _bulletCounter++ };
    }

    private void Shoot(Microsoft.Xna.Framework.Vector2 targetPos)
    {
        if (_localPlayer.RoomId == 0) return;
        var baseDir = targetPos - _localPlayer.Position; if (baseDir == Microsoft.Xna.Framework.Vector2.Zero) baseDir = new Microsoft.Xna.Framework.Vector2(1, 0); baseDir.Normalize();
        float baseAngle = (float)Math.Atan2(baseDir.Y, baseDir.X);
        void Fire(float a) {
            var d = new Microsoft.Xna.Framework.Vector2((float)Math.Cos(a), (float)Math.Sin(a)); var v = d * 500f; int bid = _bulletCounter++;
            _bulletManager.Spawn(bid, _localPlayer.Id, _localPlayer.Position, v);
            _networking.SendFireRequest(new FireRequest { BulletId = bid, Direction = new LastLight.Common.Vector2(d.X, d.Y) });
        }
        switch (_localPlayer.CurrentWeapon) {
            case WeaponType.Single: Fire(baseAngle); break;
            case WeaponType.Double: Fire(baseAngle - 0.05f); Fire(baseAngle + 0.05f); break;
            case WeaponType.Spread: Fire(baseAngle - 0.2f); Fire(baseAngle); Fire(baseAngle + 0.2f); break;
            case WeaponType.Rapid: Fire(baseAngle); break;
        }
    }

    private void DrawWorld()
    {
        if (_worldManager.Tiles == null) return;
        for (int x = 0; x < _worldManager.Width; x++) for (int y = 0; y < _worldManager.Height; y++) {
            Rectangle s = _worldManager.Tiles[x, y] switch { TileType.Grass => new Rectangle(96, 0, 32, 32), TileType.Water => new Rectangle(96, 32, 32, 32), TileType.Wall => new Rectangle(64, 0, 32, 32), TileType.Sand => new Rectangle(64, 32, 32, 32), _ => Rectangle.Empty };
            if (s != Rectangle.Empty) _spriteBatch.Draw(_atlas, new Rectangle(x * 32, y * 32, 32, 32), s, Color.White);
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
        
        Rectangle weaponSource = _localPlayer.CurrentWeapon switch {
            WeaponType.Double => new Rectangle(32+8, 32+8, 16, 16),
            WeaponType.Spread => new Rectangle(32+8, 32+8, 16, 16),
            WeaponType.Rapid => new Rectangle(32+8, 32+8, 16, 16),
            _ => new Rectangle(0, 0, 32, 32)
        };
        _spriteBatch.Draw(_atlas, new Rectangle(20, 75, 32, 32), weaponSource, Color.White);

        var boss = _bossManager.GetActiveBosses().FirstOrDefault();
        if (boss != null) { float bP = (float)boss.CurrentHealth / boss.MaxHealth; _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 200, 20, 400, 20), Color.Black * 0.5f); _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 200, 20, (int)(400 * bP), 20), Color.Purple); }
        int ms = 100; int mx = vw - ms - 20; int my = 20; _spriteBatch.Draw(_pixel, new Rectangle(mx - 2, my - 2, ms + 4, ms + 4), Color.Black * 0.5f);
        if (_worldManager.Tiles != null) for (int x = 0; x < _worldManager.Width; x++) for (int y = 0; y < _worldManager.Height; y++) {
            var c = _worldManager.Tiles[x, y] switch { TileType.Wall => Color.Gray, TileType.Water => Color.Blue, TileType.Sand => Color.SandyBrown, _ => Color.Transparent };
            if (c != Color.Transparent) _spriteBatch.Draw(_pixel, new Rectangle(mx + (int)(x*ms/(float)_worldManager.Width), my + (int)(y*ms/(float)_worldManager.Height), 1, 1), c * 0.5f);
        }
        void Dot(Microsoft.Xna.Framework.Vector2 p, Color c, int s = 3) { _spriteBatch.Draw(_pixel, new Rectangle(mx + (int)(p.X/32*ms/(float)_worldManager.Width) - s/2, my + (int)(p.Y/32*ms/(float)_worldManager.Height) - s/2, s, s), c); }
        Dot(_localPlayer.Position, Color.White, 6);
        foreach(var p in _otherPlayers.Values) if(p.RoomId == _localPlayer.RoomId) Dot(p.Position, Color.Red);
        _spriteBatch.End();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue); _camera.Position = _localPlayer.Position;
        _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix());
        DrawWorld();
        foreach(var p in _portals.Values) {
            Color pc = (p.Name ?? "").Contains("Forest") ? Color.LimeGreen : ((p.Name ?? "").Contains("Nexus") ? Color.Cyan : Color.MediumPurple);
            _spriteBatch.Draw(_atlas, new Rectangle((int)p.Position.X - 16, (int)p.Position.Y - 16, 32, 32), new Rectangle(64, 64, 32, 32), pc);
            string name = p.Name ?? "";
            Rectangle letterSrc = name.Contains("Forest") ? new Rectangle(96, 64, 16, 16) : (name.Contains("Nexus") ? new Rectangle(128, 64, 16, 16) : new Rectangle(112, 64, 16, 16));
            _spriteBatch.Draw(_atlas, new Rectangle((int)p.Position.X - 8, (int)p.Position.Y - 40, 16, 16), letterSrc, Color.White);
        }
        _itemManager.Draw(_spriteBatch, _atlas); _spawnerManager.Draw(_spriteBatch, _atlas, _pixel); _bossManager.Draw(_spriteBatch, _atlas, _pixel);
        _localPlayer.Draw(_spriteBatch, _atlas, _pixel);
        foreach (var p in _otherPlayers.Values) if(p.RoomId == _localPlayer.RoomId) p.Draw(_spriteBatch, _atlas, _pixel);
        _enemyManager.Draw(_spriteBatch, _atlas, _pixel); _bulletManager.Draw(_spriteBatch, _pixel);
        _spriteBatch.End(); DrawHUD(); base.Draw(gameTime);
    }
}
