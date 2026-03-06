using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
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
    private ParticleManager _particleManager = new();
    private Dictionary<int, PortalSpawn> _portals = new();
    private LastLight.Common.WorldManager _worldManager = new();
    private LeaderboardEntry[] _leaderboard = Array.Empty<LeaderboardEntry>();
    private float _roomCleanupTimer = -1f;
    private Camera _camera;
    private float _moveSpeed = 200f;
    private float _shootInterval = 0.1f;
    private float _shootTimer = 0f;
    private int _bulletCounter = 0;
    
    private VirtualJoystick _leftJoystick;
    private VirtualJoystick _rightJoystick;

    private enum GameState { MainMenu, Playing, Disconnected }
    private GameState _gameState = GameState.MainMenu;
    private string _disconnectReason = "";
    private string _username = "Guest";
    private SpriteFont? _font;
    private int _selectedSlotIndex = -1;
    private MouseState _lastMouseState;
    
    public static double TotalTime;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 540;
        _graphics.PreferredBackBufferHeight = 960;
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _networking = new ClientNetworking();
        _localPlayer = new Player { IsLocal = true, Position = new Microsoft.Xna.Framework.Vector2(400, 300) };
        
        _networking.OnJoinResponse = (res) => { if (res.Success) { _localPlayer.Id = res.PlayerId; _localPlayer.MaxHealth = res.MaxHealth; _localPlayer.CurrentHealth = res.MaxHealth; } };
        _networking.OnWorldInit = (init) => {
            _worldManager.GenerateWorld(init.Seed, init.Width, init.Height, init.TileSize, init.Style);
            _roomCleanupTimer = init.CleanupTimer;

            _enemyManager = new EnemyManager();
            _spawnerManager = new SpawnerManager();
            _bossManager = new BossManager();
            _itemManager = new ItemManager();
            _bulletManager = new BulletManager();
            _particleManager = new ParticleManager();
            _portals.Clear();

            _networking.OnEnemySpawn = _enemyManager.HandleSpawn;
            _networking.OnEnemyUpdate = _enemyManager.HandleUpdate;
            _networking.OnEnemyDeath = (d) => _enemyManager.HandleDeath(d, _particleManager);

            _networking.OnSpawnerSpawn = _spawnerManager.HandleSpawn;
            _networking.OnSpawnerUpdate = _spawnerManager.HandleUpdate;
            _networking.OnSpawnerDeath = (d) => _spawnerManager.HandleDeath(d, _particleManager);

            _networking.OnBossSpawn = _bossManager.HandleSpawn;
            _networking.OnBossUpdate = _bossManager.HandleUpdate;
            _networking.OnBossDeath = (d) => _bossManager.HandleDeath(d, _particleManager);

            _networking.OnItemSpawn = _itemManager.HandleSpawn;
            _networking.OnItemPickup = _itemManager.HandlePickup;

            _networking.OnPortalSpawn = (p) => { _portals[p.PortalId] = new PortalSpawn { PortalId = p.PortalId, Position = p.Position, TargetRoomId = p.TargetRoomId, Name = p.Name }; };
            _networking.OnPortalDeath = (p) => { _portals.Remove(p.PortalId); };
            
            _networking.OnLeaderboardUpdate = (u) => { _leaderboard = u.Entries; };
            _networking.OnRoomStateUpdate = (u) => { _roomCleanupTimer = u.CleanupTimer; };
        };
        _networking.OnPlayerUpdate = HandlePlayerUpdate;
        _networking.OnSpawnBullet = (s) => { 
            if(s.OwnerId != _localPlayer.Id) {
                _bulletManager.Spawn(s.BulletId, s.OwnerId, new Microsoft.Xna.Framework.Vector2(s.Position.X, s.Position.Y), new Microsoft.Xna.Framework.Vector2(s.Velocity.X, s.Velocity.Y)); 
                if (_localPlayer.RoomId != 0 && s.OwnerId >= 0) AudioManager.PlayShoot();
            }
        };
        _networking.OnBulletHit = (h) => { if (_bulletManager.Destroy(h.BulletId, _particleManager) >= 0) AudioManager.PlayHit(); };
        _networking.OnPortalSpawn = (p) => { var clone = new PortalSpawn { PortalId = p.PortalId, Position = p.Position, TargetRoomId = p.TargetRoomId, Name = p.Name }; _portals[clone.PortalId] = clone; };
        _networking.OnPortalDeath = (p) => _portals.Remove(p.PortalId);
        _networking.OnLeaderboardUpdate = (u) => _leaderboard = u.Entries;
        _networking.OnDisconnected = (reason) => { _gameState = GameState.Disconnected; _disconnectReason = reason; };
        
        // Initial binding
        _networking.OnEnemySpawn = _enemyManager.HandleSpawn;
        _networking.OnEnemyUpdate = _enemyManager.HandleUpdate;
        _networking.OnEnemyDeath = (d) => _enemyManager.HandleDeath(d, _particleManager);
        _networking.OnSpawnerSpawn = _spawnerManager.HandleSpawn;
        _networking.OnSpawnerUpdate = _spawnerManager.HandleUpdate;
        _networking.OnSpawnerDeath = (d) => _spawnerManager.HandleDeath(d, _particleManager);
        _networking.OnBossSpawn = _bossManager.HandleSpawn;
        _networking.OnBossUpdate = _bossManager.HandleUpdate;
        _networking.OnBossDeath = (d) => _bossManager.HandleDeath(d, _particleManager);
        _networking.OnItemSpawn = _itemManager.HandleSpawn;
        _networking.OnItemPickup = _itemManager.HandlePickup;
    }

    private void HandlePlayerUpdate(AuthoritativePlayerUpdate u)
    {
        if (u.PlayerId == _localPlayer.Id) {
            if (u.Level > _localPlayer.Level) {
                AudioManager.PlayLevelUp();
                _particleManager.SpawnBurst(_localPlayer.Position, 50, Color.Gold, 200f, 1.0f, 8f);
            }
            _localPlayer.Position = new Microsoft.Xna.Framework.Vector2(u.Position.X, u.Position.Y);
            _localPlayer.CurrentHealth = u.CurrentHealth; _localPlayer.MaxHealth = u.MaxHealth;
            _localPlayer.Level = u.Level; _localPlayer.Experience = u.Experience; 
            _localPlayer.Attack = u.Attack; _localPlayer.Defense = u.Defense; _localPlayer.Speed = u.Speed;
            _localPlayer.Dexterity = u.Dexterity; _localPlayer.Vitality = u.Vitality; _localPlayer.Wisdom = u.Wisdom;
            _localPlayer.Equipment = u.Equipment; _localPlayer.Inventory = u.Inventory;
            _localPlayer.RoomId = u.RoomId;
            _localPlayer.PendingInputs.RemoveAll(i => i.InputSequenceNumber <= u.LastProcessedInputSequence);
            foreach (var input in _localPlayer.PendingInputs) _localPlayer.ApplyInput(input, _moveSpeed, _worldManager);
            return;
        }
        if (!_otherPlayers.TryGetValue(u.PlayerId, out var p)) { p = new Player { Id = u.PlayerId, IsLocal = false, MaxHealth = 100 }; _otherPlayers[u.PlayerId] = p; }
        p.Position = new Microsoft.Xna.Framework.Vector2(u.Position.X, u.Position.Y); p.Velocity = new Microsoft.Xna.Framework.Vector2(u.Velocity.X, u.Velocity.Y); p.CurrentHealth = u.CurrentHealth; p.RoomId = u.RoomId;
    }

    protected override void Initialize() { 
        GameDataManager.Load("Data");
        Exiting += (s, a) => _networking.Disconnect(); 
        AudioManager.Initialize();
        Window.TextInput += (s, a) => {
            if (_gameState == GameState.MainMenu) {
                if (a.Key == Keys.Back && _username.Length > 0) _username = _username.Substring(0, _username.Length - 1);
                else if (char.IsLetterOrDigit(a.Character) && _username.Length < 15) _username += a.Character;
            }
        };

        int vw = _graphics.PreferredBackBufferWidth;
        int vh = _graphics.PreferredBackBufferHeight;
        _leftJoystick = new VirtualJoystick(new Microsoft.Xna.Framework.Vector2(120, vh - 150), 80);
        _rightJoystick = new VirtualJoystick(new Microsoft.Xna.Framework.Vector2(vw - 120, vh - 150), 80);

        base.Initialize(); 
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1); _pixel.SetData(new[] { Color.White });
        GenerateAtlas();
        _camera = new Camera(GraphicsDevice.Viewport);
        try { _font = Content.Load<SpriteFont>("font"); } catch { }
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

        // QUADRANT 1 (Top-Left): Small Entity Assets
        FillRect(4, 4, 24, 24, Color.LightGray); FillRect(8, 10, 4, 6, Color.Black); FillRect(20, 10, 4, 6, Color.Black); FillRect(2, 12, 4, 16, Color.DarkSlateGray); FillRect(26, 12, 4, 12, Color.Goldenrod); // Player
        FillRect(36, 4, 24, 24, new Color(139, 0, 0)); FillRect(40, 10, 6, 4, Color.Yellow); FillRect(50, 10, 6, 4, Color.Yellow); FillRect(32, 20, 32, 4, Color.Black); // Enemy
        FillRect(64, 0, 32, 32, Color.DimGray); FillRect(66, 2, 28, 28, Color.Gray); FillRect(64, 14, 32, 2, Color.Black); FillRect(80, 0, 2, 14, Color.Black); FillRect(72, 16, 2, 16, Color.Black); // Wall
        FillRect(96, 0, 32, 32, new Color(34, 139, 34)); data[(2 * size) + 100] = Color.LimeGreen; data[(25 * size) + 120] = Color.LimeGreen; // Grass
        FillRect(8, 40, 16, 20, Color.White); FillRect(10, 44, 12, 14, Color.Red); FillRect(12, 36, 8, 4, Color.SaddleBrown); // Potion
        FillRect(40, 40, 16, 16, Color.Gold); FillRect(44, 36, 8, 24, Color.LightYellow); // Weapon Upgrade
        FillRect(64, 32, 32, 32, Color.SandyBrown); data[(35 * size) + 70] = Color.SaddleBrown; data[(40 * size) + 85] = Color.SaddleBrown; // Sand
        FillRect(96, 32, 32, 32, new Color(30, 144, 255)); FillRect(100, 40, 10, 2, Color.AliceBlue); FillRect(110, 55, 10, 2, Color.AliceBlue); // Water

        // QUADRANT 3 (Bottom-Left): Large Object Assets
        FillRect(0, 128, 64, 64, Color.Indigo); FillRect(4, 132, 56, 56, Color.Purple); FillRect(16, 144, 32, 32, Color.Black); for(int g=0; g<10; g++) data[(144+g)*size + 32] = Color.Magenta; // Spawner
        FillRect(64, 128, 32, 32, Color.White); FillRect(70, 134, 20, 20, Color.LightCyan); // Portal (Now White for better tinting)

        // QUADRANT 4 (Bottom-Right): Letters
        FillRect(128, 128, 12, 2, Color.White); FillRect(128, 128, 2, 12, Color.White); FillRect(128, 134, 8, 2, Color.White); // 'F'
        FillRect(144, 128, 2, 12, Color.White); FillRect(144, 128, 8, 2, Color.White); FillRect(144, 138, 8, 2, Color.White); FillRect(152, 130, 2, 8, Color.White); // 'D'
        FillRect(160, 128, 2, 12, Color.White); FillRect(172, 128, 2, 12, Color.White); for(int i=0; i<12; i++) data[(128+i)*size + 160+i] = Color.White; // 'N'

        // QUADRANT 2 (Top-Right): THE BOSS
        FillRect(128, 0, 128, 128, Color.DarkSlateBlue);
        FillRect(140, 20, 30, 30, Color.Yellow); // Eye L
        FillRect(214, 20, 30, 30, Color.Yellow); // Eye R
        FillRect(128, 80, 128, 20, Color.Black); // Mouth
        FillRect(128, 0, 20, 40, Color.Gray); // Horn L
        FillRect(236, 0, 20, 40, Color.Gray); // Horn R

        _atlas.SetData(data);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();
        TotalTime = gameTime.TotalGameTime.TotalSeconds;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        int vw = _graphics.PreferredBackBufferWidth;
        int vh = _graphics.PreferredBackBufferHeight;
        var touches = TouchPanel.GetState();

        if (_gameState == GameState.Disconnected)
        {
            var ms = Mouse.GetState();
            bool pressed = ms.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;
            foreach(var touch in touches) {
                if(touch.State == TouchLocationState.Pressed) pressed = true;
            }
            if (pressed) {
                _gameState = GameState.MainMenu;
            }
            _lastMouseState = ms;
            base.Update(gameTime);
            return;
        }

        if (_gameState == GameState.MainMenu)
        {
            var ms = Mouse.GetState();
            bool pressed = ms.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;
            Microsoft.Xna.Framework.Vector2 pressPos = ms.Position.ToVector2();

            foreach(var touch in touches) {
                if(touch.State == TouchLocationState.Pressed) {
                    pressed = true; pressPos = touch.Position;
                }
            }

            if (pressed)
            {
                Rectangle localRect = new Rectangle(vw / 2 - 100, vh / 2 - 110, 200, 100);
                Rectangle remoteRect = new Rectangle(vw / 2 - 100, vh / 2 + 10, 200, 100);

                if (localRect.Contains(pressPos))
                {
                    _networking.Connect("localhost", 5000, _username);
                    _gameState = GameState.Playing;
                }
                else if (remoteRect.Contains(pressPos))
                {
                    _networking.Connect("169.155.55.157", 5000, _username);
                    _gameState = GameState.Playing;
                }
            }
            _lastMouseState = ms;
            base.Update(gameTime);
            return;
        }

        _leftJoystick.Position = new Microsoft.Xna.Framework.Vector2(120, vh - 150);
        _rightJoystick.Position = new Microsoft.Xna.Framework.Vector2(vw - 120, vh - 150);
        _leftJoystick.Update(touches);
        _rightJoystick.Update(touches);

        var input = HandleInput(dt);
        if (input != null) { _localPlayer.PendingInputs.Add(input); _localPlayer.ApplyInput(input, _moveSpeed, _worldManager); _networking.SendInputRequest(input); }
        foreach (var p in _otherPlayers.Values) if(p.RoomId == _localPlayer.RoomId) p.Update(gameTime, _worldManager);
        _bulletManager.Update(gameTime, _worldManager, _enemyManager, _bossManager, _spawnerManager, _particleManager);
        _particleManager.Update(dt);
        _networking.PollEvents();

        // UI Interactions
        bool interactPressed = false;
        Microsoft.Xna.Framework.Vector2 interactPos = Microsoft.Xna.Framework.Vector2.Zero;
        bool isRightClick = false;

        var curMouse = Mouse.GetState();
        if (curMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released) {
            interactPressed = true; interactPos = curMouse.Position.ToVector2();
        } else if (curMouse.RightButton == ButtonState.Pressed && _lastMouseState.RightButton == ButtonState.Released) {
            interactPressed = true; interactPos = curMouse.Position.ToVector2(); isRightClick = true;
        }

        foreach (var touch in touches) {
            if (touch.State == TouchLocationState.Pressed) {
                if (Microsoft.Xna.Framework.Vector2.Distance(touch.Position, _leftJoystick.Position) > _leftJoystick.Radius * 1.5f &&
                    Microsoft.Xna.Framework.Vector2.Distance(touch.Position, _rightJoystick.Position) > _rightJoystick.Radius * 1.5f) {
                    interactPressed = true; interactPos = touch.Position; 
                }
            }
        }

        if (interactPressed) {
            int eqW = 3 * 60;
            int eqX = (vw - eqW) / 2;
            int eqY = vh - 220;
            int invW = 4 * 50;
            int invX = (vw - invW) / 2;
            int invY = eqY + 50;

            int clickedIdx = -1;
            for (int i = 0; i < 3; i++) {
                if (new Rectangle(eqX + (i * 60), eqY, 40, 40).Contains(interactPos)) clickedIdx = i;
            }
            for (int r = 0; r < 2; r++) {
                for (int c = 0; c < 4; c++) {
                    if (new Rectangle(invX + (c * 50), invY + (r * 50), 40, 40).Contains(interactPos)) clickedIdx = 3 + (r * 4 + c);
                }
            }

            if (isRightClick && clickedIdx != -1) {
                _networking.SendUseItemRequest(clickedIdx);
            }
            else if (clickedIdx != -1) {
                if (_selectedSlotIndex == -1) _selectedSlotIndex = clickedIdx;
                else {
                    if (_selectedSlotIndex != clickedIdx) _networking.SendSwapItemRequest(_selectedSlotIndex, clickedIdx);
                    _selectedSlotIndex = -1;
                }
            } else {
                _selectedSlotIndex = -1;
            }

            // Check portal interact button
            bool canInteract = false; PortalSpawn nearest = null;
            foreach(var p in _portals.Values) {
                if (Microsoft.Xna.Framework.Vector2.Distance(_localPlayer.Position, new Microsoft.Xna.Framework.Vector2(p.Position.X, p.Position.Y)) < 60) {
                    canInteract = true; nearest = p; break;
                }
            }
            if (canInteract && new Rectangle(vw - 160, vh - 280, 80, 80).Contains(interactPos)) {
                 _networking.SendPacket(new PortalUseRequest { PortalId = nearest.PortalId }, DeliveryMethod.ReliableOrdered);
            }
        }

        _lastMouseState = curMouse;

        base.Update(gameTime);
    }

    private InputRequest? HandleInput(float dt)
    {
        var kb = Keyboard.GetState(); var ms = Mouse.GetState(); Microsoft.Xna.Framework.Vector2 mv = Microsoft.Xna.Framework.Vector2.Zero;
        if (kb.IsKeyDown(Keys.W)) mv.Y -= 1; if (kb.IsKeyDown(Keys.S)) mv.Y += 1; if (kb.IsKeyDown(Keys.A)) mv.X -= 1; if (kb.IsKeyDown(Keys.D)) mv.X += 1;
        
        if (_leftJoystick.IsActive) {
            mv += _leftJoystick.Value;
        }

        if (mv != Microsoft.Xna.Framework.Vector2.Zero) mv.Normalize();
        
        if (kb.IsKeyDown(Keys.Space)) {
            foreach(var p in _portals.Values) {
                if (Microsoft.Xna.Framework.Vector2.Distance(_localPlayer.Position, new Microsoft.Xna.Framework.Vector2(p.Position.X, p.Position.Y)) < 60) {
                    _networking.SendPacket(new PortalUseRequest { PortalId = p.PortalId }, DeliveryMethod.ReliableOrdered);
                    break;
                }
            }
        }

        float interval = _localPlayer.Equipment[0].WeaponType == WeaponType.Rapid ? 0.05f : _shootInterval;
        _shootTimer += dt;
        
        if (ms.LeftButton == ButtonState.Pressed && _shootTimer >= interval && !_rightJoystick.IsActive) { 
            _shootTimer = 0; Shoot(_camera.ScreenToWorld(ms.Position.ToVector2())); 
        }

        if (_rightJoystick.IsActive && _shootTimer >= interval) {
            _shootTimer = 0;
            Shoot(_localPlayer.Position + _rightJoystick.Value * 100f);
        }

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
            AudioManager.PlayShoot();
        }
        switch (_localPlayer.Equipment[0].WeaponType) {
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
        int vw = _graphics.PreferredBackBufferWidth; 
        int vh = _graphics.PreferredBackBufferHeight;
        
        int tx = 10; int ty = 10;
        _spriteBatch.Draw(_pixel, new Rectangle(5, 5, 260, 100), Color.Black * 0.5f);

        if (_font != null) {
            _spriteBatch.DrawString(_font, $"{_username} (Lv. {_localPlayer.Level})", new Microsoft.Xna.Framework.Vector2(tx, ty), Color.White);
        }
        
        ty += 25;
        float hpP = Math.Max(0, (float)_localPlayer.CurrentHealth / _localPlayer.MaxHealth);
        _spriteBatch.Draw(_pixel, new Rectangle(tx, ty, 210, 20), Color.DarkRed); 
        _spriteBatch.Draw(_pixel, new Rectangle(tx, ty, (int)(210 * hpP), 20), Color.Red);
        if (_font != null) {
            var hpTxt = $"{_localPlayer.CurrentHealth} / {_localPlayer.MaxHealth}";
            var ts = _font.MeasureString(hpTxt);
            _spriteBatch.DrawString(_font, hpTxt, new Microsoft.Xna.Framework.Vector2(tx + 105 - ts.X/2, ty + 2), Color.White);
        }

        ty += 25;
        float exP = Math.Min(1f, (float)_localPlayer.Experience / (_localPlayer.Level * 100));
        _spriteBatch.Draw(_pixel, new Rectangle(tx, ty, 210, 10), Color.DarkSlateGray); 
        _spriteBatch.Draw(_pixel, new Rectangle(tx, ty, (int)(210 * exP), 10), Color.Yellow);

        ty += 15;
        if (_font != null) {
            _spriteBatch.DrawString(_font, $"A:{_localPlayer.Attack} D:{_localPlayer.Defense} S:{_localPlayer.Speed} X:{_localPlayer.Dexterity} V:{_localPlayer.Vitality} W:{_localPlayer.Wisdom}", new Microsoft.Xna.Framework.Vector2(tx, ty), Color.LightGray, 0f, Microsoft.Xna.Framework.Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        int ms = 150; int mx = vw - ms - 20; int my = 20; 
        _spriteBatch.Draw(_pixel, new Rectangle(mx - 2, my - 2, ms + 4, ms + 4), Color.Black * 0.5f);
        if (_worldManager.Tiles != null) {
            for (int x = 0; x < _worldManager.Width; x++) for (int y = 0; y < _worldManager.Height; y++) {
                var c = _worldManager.Tiles[x, y] switch { TileType.Wall => Color.Gray, TileType.Water => Color.Blue, TileType.Sand => Color.SandyBrown, _ => Color.Transparent };
                if (c != Color.Transparent) _spriteBatch.Draw(_pixel, new Rectangle(mx + (int)(x*ms/(float)_worldManager.Width), my + (int)(y*ms/(float)_worldManager.Height), 1, 1), c * 0.5f);
            }
            void Dot(Microsoft.Xna.Framework.Vector2 p, Color c, int s = 3) { _spriteBatch.Draw(_pixel, new Rectangle(mx + (int)(p.X/32*ms/(float)_worldManager.Width) - s/2, my + (int)(p.Y/32*ms/(float)_worldManager.Height) - s/2, s, s), c); }
            Dot(_localPlayer.Position, Color.White, 5);
            foreach(var p in _otherPlayers.Values) if(p.RoomId == _localPlayer.RoomId) Dot(p.Position, Color.Red);
            foreach(var s in _spawnerManager.GetAllSpawners().Where(sp => sp.Active)) Dot(new Microsoft.Xna.Framework.Vector2(s.Position.X, s.Position.Y), Color.Orange, 5);
            foreach(var e in _enemyManager.GetAllEnemies().Where(en => en.Active)) Dot(new Microsoft.Xna.Framework.Vector2(e.Position.X, e.Position.Y), Color.Red, 2);
        }

        int invW = 4 * 50;
        int eqW = 3 * 60;
        int bY = vh - 220; 
        
        int eqX = (vw - eqW) / 2;
        int eqY = bY;
        for (int i = 0; i < 3; i++) {
            Rectangle rect = new Rectangle(eqX + (i * 60), eqY, 40, 40);
            _spriteBatch.Draw(_pixel, rect, i == _selectedSlotIndex ? Color.White * 0.4f : Color.Black * 0.7f);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.Gray);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.Gray);
            
            if (_localPlayer.Equipment != null && i < _localPlayer.Equipment.Length && _localPlayer.Equipment[i].ItemId != 0) {
                Rectangle src = _localPlayer.Equipment[i].Category == ItemCategory.Weapon ? new Rectangle(40, 40, 16, 16) : (_localPlayer.Equipment[i].Category == ItemCategory.Armor ? new Rectangle(64, 0, 32, 32) : new Rectangle(40, 40, 16, 16));
                _spriteBatch.Draw(_atlas, new Rectangle(rect.X + 4, rect.Y + 4, 32, 32), src, Color.White);
            }
        }

        int invX = (vw - invW) / 2;
        int invY = eqY + 50;
        for (int r = 0; r < 2; r++) {
            for (int c = 0; c < 4; c++) {
                int idx = 3 + (r * 4 + c);
                Rectangle rect = new Rectangle(invX + (c * 50), invY + (r * 50), 40, 40);
                _spriteBatch.Draw(_pixel, rect, idx == _selectedSlotIndex ? Color.White * 0.4f : Color.Black * 0.7f);
                _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.Gray);
                _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.Gray);
                
                if (_localPlayer.Inventory != null && (idx-3) < _localPlayer.Inventory.Length && _localPlayer.Inventory[idx-3].ItemId != 0) {
                    Rectangle src = _localPlayer.Inventory[idx-3].Category == ItemCategory.Weapon ? new Rectangle(40, 40, 16, 16) : new Rectangle(8, 40, 16, 20);
                    _spriteBatch.Draw(_atlas, new Rectangle(rect.X + 4, rect.Y + 4, 32, 32), src, Color.White);
                }
            }
        }

        if (_leaderboard != null && _leaderboard.Length > 0) {
            int lbY = 120;
            int bgWidth = _font != null ? 150 : 50;
            _spriteBatch.Draw(_pixel, new Rectangle(5, lbY - 5, bgWidth, 10 + (_leaderboard.Length * 15)), Color.Black * 0.5f);
            float maxScore = Math.Max(1, _leaderboard.Max(e => e.Score));
            for (int i = 0; i < Math.Min(5, _leaderboard.Length); i++) {
                var entry = _leaderboard[i];
                Color rankColor = i == 0 ? Color.Gold : (i == 1 ? Color.Silver : (i == 2 ? Color.DarkOrange : Color.White));
                if (entry.PlayerId == _localPlayer.Id) rankColor = Color.LimeGreen;
                
                int barWidth = (int)((entry.Score / maxScore) * 30);
                if (_font != null) {
                    string n = entry.Username ?? "Guest";
                    if (n.Length > 8) n = n.Substring(0, 8);
                    _spriteBatch.DrawString(_font, $"{n} - {entry.Score}", new Microsoft.Xna.Framework.Vector2(50, lbY + (i * 15) - 5), rankColor, 0, Microsoft.Xna.Framework.Vector2.Zero, 0.8f, SpriteEffects.None, 0);
                }
                _spriteBatch.Draw(_pixel, new Rectangle(15, lbY + (i * 15), 5, 5), rankColor);
                _spriteBatch.Draw(_pixel, new Rectangle(25, lbY + (i * 15), barWidth, 5), rankColor * 0.8f);
            }
        }

        if (_roomCleanupTimer > 0 && _font != null) {
            string timerText = $"Room collapsing in: {Math.Ceiling(_roomCleanupTimer)}s";
            var size = _font.MeasureString(timerText);
            _spriteBatch.Draw(_pixel, new Rectangle(vw / 2 - (int)size.X / 2 - 10, 50, (int)size.X + 20, (int)size.Y + 10), Color.Black * 0.7f);
            _spriteBatch.DrawString(_font, timerText, new Microsoft.Xna.Framework.Vector2(vw / 2 - size.X / 2, 55), Color.Red);
        }

        var boss = _bossManager.GetActiveBosses().FirstOrDefault();
        if (boss != null) { float bP = (float)boss.CurrentHealth / boss.MaxHealth; _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 150, 20, 300, 20), Color.Black * 0.5f); _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 150, 20, (int)(300 * bP), 20), Color.Purple); }

        bool canInteract = false;
        foreach(var p in _portals.Values) {
            if (Microsoft.Xna.Framework.Vector2.Distance(_localPlayer.Position, new Microsoft.Xna.Framework.Vector2(p.Position.X, p.Position.Y)) < 60) {
                canInteract = true; break;
            }
        }
        
        if (canInteract) {
            Rectangle interactBtn = new Rectangle(vw - 160, vh - 280, 80, 80);
            _spriteBatch.Draw(_pixel, interactBtn, Color.Yellow * 0.7f);
            if (_font != null) _spriteBatch.DrawString(_font, "ENTER", new Microsoft.Xna.Framework.Vector2(interactBtn.X + 10, interactBtn.Y + 30), Color.Black);
        }

        _leftJoystick.Draw(_spriteBatch, _pixel);
        _rightJoystick.Draw(_spriteBatch, _pixel);
        
        _spriteBatch.End();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue); 
        int vw = _graphics.PreferredBackBufferWidth;
        int vh = _graphics.PreferredBackBufferHeight;
        
        if (_gameState == GameState.Disconnected)
        {
            _spriteBatch.Begin();
            if (_font != null) {
                string text = "DISCONNECTED";
                var size = _font.MeasureString(text);
                _spriteBatch.DrawString(_font, text, new Microsoft.Xna.Framework.Vector2(vw / 2 - size.X / 2, vh / 2 - 50), Color.Red);
                
                string rText = $"Reason: {_disconnectReason}";
                var rSize = _font.MeasureString(rText);
                _spriteBatch.DrawString(_font, rText, new Microsoft.Xna.Framework.Vector2(vw / 2 - rSize.X / 2, vh / 2 + 10), Color.White);

                string cText = "Tap anywhere to continue";
                if (TotalTime % 1 < 0.5) {
                    var cSize = _font.MeasureString(cText);
                    _spriteBatch.DrawString(_font, cText, new Microsoft.Xna.Framework.Vector2(vw / 2 - cSize.X / 2, vh / 2 + 100), Color.Gray);
                }
            }
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        if (_gameState == GameState.MainMenu)
        {
            _spriteBatch.Begin();
            
            if (_font != null) {
                string text = $"Enter Name: {_username}";
                if (TotalTime % 1 < 0.5) text += "_";
                var size = _font.MeasureString(text);
                _spriteBatch.DrawString(_font, text, new Microsoft.Xna.Framework.Vector2(vw / 2 - size.X / 2, vh / 4), Color.White);
            }

            Rectangle localRect = new Rectangle(vw / 2 - 100, vh / 2 - 110, 200, 100);
            _spriteBatch.Draw(_pixel, localRect, Color.LimeGreen);
            _spriteBatch.Draw(_pixel, new Rectangle(vw / 2 - 20, vh / 2 - 85, 10, 50), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(vw / 2 - 20, vh / 2 - 45, 40, 10), Color.White);

            Rectangle remoteRect = new Rectangle(vw / 2 - 100, vh / 2 + 10, 200, 100);
            _spriteBatch.Draw(_pixel, remoteRect, Color.Blue);
            _spriteBatch.Draw(_pixel, new Rectangle(vw / 2 - 20, vh / 2 + 35, 10, 50), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(vw / 2 - 20, vh / 2 + 35, 30, 10), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(vw / 2 + 10, vh / 2 + 45, 10, 10), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(vw / 2 - 20, vh / 2 + 55, 30, 10), Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(vw / 2 + 10, vh / 2 + 65, 10, 20), Color.White);

            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        _camera.ViewportWidth = vw;
        _camera.ViewportHeight = vh;
        _camera.Position = _localPlayer.Position;
        _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix());
        DrawWorld();
        foreach(var p in _portals.Values) {
            Color pc = (p.Name ?? "").Contains("Forest") ? Color.LimeGreen : ((p.Name ?? "").Contains("Nexus") ? Color.Cyan : Color.MediumPurple);
            _spriteBatch.Draw(_atlas, new Rectangle((int)p.Position.X - 16, (int)p.Position.Y - 16, 32, 32), new Rectangle(64, 128, 32, 32), pc);
            string n = p.Name ?? "";
            Rectangle letterSrc = n.Contains("Forest") ? new Rectangle(128, 128, 16, 16) : (n.Contains("Nexus") ? new Rectangle(160, 128, 16, 16) : new Rectangle(144, 128, 16, 16));
            _spriteBatch.Draw(_atlas, new Rectangle((int)p.Position.X - 8, (int)p.Position.Y - 40, 16, 16), letterSrc, Color.White);
        }
        _itemManager.Draw(_spriteBatch, _atlas); _spawnerManager.Draw(_spriteBatch, _atlas, _pixel); _bossManager.Draw(_spriteBatch, _atlas, _pixel);
        _localPlayer.Draw(_spriteBatch, _atlas, _pixel);
        foreach (var p in _otherPlayers.Values) if(p.RoomId == _localPlayer.RoomId) p.Draw(_spriteBatch, _atlas, _pixel);
        _enemyManager.Draw(_spriteBatch, _atlas, _pixel); 
        _particleManager.Draw(_spriteBatch, _pixel);
        _bulletManager.Draw(_spriteBatch, _pixel);
        _spriteBatch.End(); DrawHUD(); base.Draw(gameTime);
    }
}
