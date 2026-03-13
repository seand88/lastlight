using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using LastLight.Common;
using LiteNetLib;
using LastLight.Common.Abilities;

namespace LastLight.Client.Core;

public class AtlasRegion {
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ClientNetworking _networking;
    private Texture2D _pixel;
    public static Texture2D MainAtlas; // Exposed for AssetManager/Entity use
    private Texture2D _loginBackground;
    private Texture2D _loginAtlas;
    
    public static IAssetManager AssetManager; 
    private WorldRenderer _worldRenderer;

    private Player _localPlayer;
    private Dictionary<int, Player> _otherPlayers = new();
    private BulletManager _bulletManager = new();
    private EntityManager _entityManager = new();
    private SpawnerManager _spawnerManager = new();
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
    private int _predictedBulletCounter = 1;
    private Dictionary<string, float> _localCooldowns = new();
    
    private VirtualJoystick _leftJoystick;
    private VirtualJoystick _rightJoystick;

    private enum GameState { MainMenu, Playing, Disconnected }
    private GameState _gameState = GameState.MainMenu;
    private string _disconnectReason = "";
    private string _username = "Guest";
    private SpriteFont? _font;
    private int _selectedSlotIndex = -1;
    private MouseState _lastMouseState;
    private bool _wasHoveringLocal = false;
    private bool _wasHoveringRemote = false;
    
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
        
        // Initialize fields to avoid CS8618/Warnings
        _leftJoystick = new VirtualJoystick(Microsoft.Xna.Framework.Vector2.Zero, 0);
        _rightJoystick = new VirtualJoystick(Microsoft.Xna.Framework.Vector2.Zero, 0);
        _worldRenderer = null!; 
        AssetManager = null!;
        MainAtlas = null!;
        _spriteBatch = null!;
        _pixel = null!;
        _loginBackground = null!;
        _loginAtlas = null!;
        _camera = null!;

        _networking.OnJoinResponse = (res) => { 
            if (res.Success) { 
                _localPlayer.Id = res.PlayerId; 
                _localPlayer.MaxHealth = res.MaxHealth; 
                _localPlayer.CurrentHealth = res.MaxHealth; 
                _gameState = GameState.Playing;
                _worldRenderer?.PlayAnimation(_localPlayer, "Player2", "idle", true);
            } 
        };
        _networking.OnWorldInit = (init) => {
            _localPlayer.RoomId = init.RoomId;
            _worldManager.GenerateWorld(init.Seed, init.Width, init.Height, init.TileSize, init.Style);
            _roomCleanupTimer = init.CleanupTimer;

            _entityManager = new EntityManager();
            _spawnerManager = new SpawnerManager();
            _itemManager = new ItemManager();
            _bulletManager = new BulletManager();
            _particleManager = new ParticleManager();
            _portals.Clear();

            _worldRenderer = new WorldRenderer(AssetManager);
            _worldRenderer.PlayAnimation(_localPlayer, "Player2", "idle", true);

            _networking.OnEntitySpawn = (e) => { 
                _entityManager.HandleSpawn(e); 
                var clientEntity = _entityManager.GetAllEntities().FirstOrDefault(x => x.Id == e.EntityId);
                if (clientEntity != null && !string.IsNullOrEmpty(clientEntity.Animation)) {
                    _worldRenderer?.PlayAnimation(clientEntity, clientEntity.Animation, "idle", true);
                }
            };
            _networking.OnEntityUpdate = _entityManager.HandleUpdate;
            _networking.OnEntityDeath = (d) => _entityManager.HandleDeath(d, _particleManager);

            _networking.OnSpawnerSpawn = _spawnerManager.HandleSpawn;
            _networking.OnSpawnerUpdate = _spawnerManager.HandleUpdate;
            _networking.OnSpawnerDeath = (d) => _spawnerManager.HandleDeath(d, _particleManager);

            _networking.OnItemSpawn = _itemManager.HandleSpawn;
            _networking.OnItemPickup = _itemManager.HandlePickup;

            _networking.OnPortalSpawn = (p) => { _portals[p.PortalId] = new PortalSpawn { PortalId = p.PortalId, Position = p.Position, TargetRoomId = p.TargetRoomId, Name = p.Name }; };
            _networking.OnPortalDeath = (p) => { _portals.Remove(p.PortalId); };
            
            _networking.OnLeaderboardUpdate = (u) => { _leaderboard = u.Entries; };
            _networking.OnRoomStateUpdate = (u) => { _roomCleanupTimer = u.CleanupTimer; };
            
            // Re-bind other players when world re-inits (clears ghosts)
            _otherPlayers.Clear();
        };
        _networking.OnPlayerSpawn = HandlePlayerSpawn;
        _networking.OnPlayerLeave = HandlePlayerLeave;
        _networking.OnPlayerUpdate = HandlePlayerUpdate;
        _networking.OnSelfStateUpdate = (u) => {
            _localPlayer.CurrentMana = u.CurrentMana;
            _localPlayer.MaxMana = u.MaxMana;
            _localPlayer.Experience = u.Experience;
            _localPlayer.Attack = u.Attack;
            _localPlayer.Defense = u.Defense;
            _localPlayer.Speed = u.Speed;
            _localPlayer.Dexterity = u.Dexterity;
            _localPlayer.Vitality = u.Vitality;
            _localPlayer.Wisdom = u.Wisdom;
            _localPlayer.Inventory = u.Inventory;
            _localPlayer.Equipment = u.Equipment;
        };
        _networking.OnSpawnBullet = (s) => { 
            if(s.OwnerId != _localPlayer.Id) {
                // Use authoritative LifeTime from server
                _bulletManager.Spawn(s.BulletId, s.OwnerId, new Microsoft.Xna.Framework.Vector2(s.Position.X, s.Position.Y), new Microsoft.Xna.Framework.Vector2(s.Velocity.X, s.Velocity.Y), s.LifeTime, s.AbilityId); 
                if (_localPlayer.RoomId != 0 && s.OwnerId >= 0) AudioManager.PlayShoot();
            }
        };
        _networking.OnEffectEvent = (e) => {
            if (e.EffectName == "damage") {
                AudioManager.PlayHit();
                _particleManager.SpawnBurst(new Microsoft.Xna.Framework.Vector2(e.Position.X, e.Position.Y), 5, Color.Yellow, 80f, 0.3f, 3f);
            }
            
            // Reconcile local ghost bullet
            if (e.SourceId == _localPlayer?.Id && e.SourceProjectileId != 0) {
                _bulletManager.Destroy(e.SourceProjectileId, _particleManager);
            }
        };
        _networking.OnBulletHit = (h) => { if (_bulletManager.Destroy(h.BulletId, _particleManager) >= 0) AudioManager.PlayHit(); };
        _networking.OnPortalSpawn = (p) => { var clone = new PortalSpawn { PortalId = p.PortalId, Position = p.Position, TargetRoomId = p.TargetRoomId, Name = p.Name }; _portals[clone.PortalId] = clone; };
        _networking.OnPortalDeath = (p) => _portals.Remove(p.PortalId);
        _networking.OnLeaderboardUpdate = (u) => _leaderboard = u.Entries;
        _networking.OnDisconnected = (reason) => { _gameState = GameState.Disconnected; _disconnectReason = reason; };
    }

    private void HandlePlayerSpawn(PlayerSpawn s)
    {
        if (s.PlayerId == _localPlayer.Id) {
             _localPlayer.Name = s.Username;
             _localPlayer.MaxHealth = s.MaxHealth;
             _localPlayer.Level = s.Level;
             return;
        }
        
        if (!_otherPlayers.ContainsKey(s.PlayerId)) {
            var p = new Player { 
                Id = s.PlayerId, 
                Name = s.Username, 
                IsLocal = false, 
                MaxHealth = s.MaxHealth,
                CurrentHealth = s.MaxHealth,
                Level = s.Level,
                Position = new Microsoft.Xna.Framework.Vector2(s.Position.X, s.Position.Y)
            };
            _otherPlayers[s.PlayerId] = p;
            _worldRenderer?.PlayAnimation(p, "Player2", "idle", true);
        }
    }

    private void HandlePlayerLeave(PlayerLeave l)
    {
        if (_otherPlayers.TryGetValue(l.PlayerId, out var p)) {
            _worldRenderer?.RemoveEntity(p);
            _otherPlayers.Remove(l.PlayerId);
        }
    }

    private void HandlePlayerUpdate(PlayerUpdate u)
    {
        if (u.PlayerId == _localPlayer.Id) {
            _localPlayer.Position = new Microsoft.Xna.Framework.Vector2(u.Position.X, u.Position.Y);
            _localPlayer.CurrentHealth = u.CurrentHealth;
            _localPlayer.PendingInputs.RemoveAll(i => i.InputSequenceNumber <= u.LastProcessedInputSequence);
            foreach (var input in _localPlayer.PendingInputs) _localPlayer.ApplyInput(input, _moveSpeed, _worldManager);
            return;
        }
        
        if (_otherPlayers.TryGetValue(u.PlayerId, out var p)) { 
            p.Position = new Microsoft.Xna.Framework.Vector2(u.Position.X, u.Position.Y); 
            p.Velocity = new Microsoft.Xna.Framework.Vector2(u.Velocity.X, u.Velocity.Y); 
            p.CurrentHealth = u.CurrentHealth;
        }
    }

    protected override void Initialize() { 
        GameDataManager.Load("Data");
        Exiting += (s, a) => _networking.Disconnect(); 
        AudioManager.Initialize(this);
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
        
        AssetManager = new AssetManager(Content);
        AssetManager.LoadAll();
        _worldRenderer = new WorldRenderer(AssetManager);

        _camera = new Camera(GraphicsDevice.Viewport);
        
        try { _font = AssetManager.GetFont("font"); } catch { }
        try { _loginBackground = AssetManager.GetStaticImage("login_background"); } catch { }
        try { _loginAtlas = AssetManager.GetAtlasTexture("Login"); } catch { }
        
        AudioManager.LoadContent(AssetManager);
        
        try { 
            var song = AssetManager.GetMusic("login");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.20f;
            MediaPlayer.Play(song);
        } catch { }
        
        // Setup a legacy fallback for MainAtlas
        try { MainAtlas = AssetManager.GetAtlasTexture("Items"); } catch { } 
    }

    protected override void Update(GameTime gameTime)
    {
        TotalTime = gameTime.TotalGameTime.TotalSeconds;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _networking.PollEvents(); 

        if (IsActive && Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

        int vw = _graphics.PreferredBackBufferWidth;
        int vh = _graphics.PreferredBackBufferHeight;
        
        var rawMs = Mouse.GetState();
        bool isMouseInWindow = GraphicsDevice.Viewport.Bounds.Contains(rawMs.Position);
        bool hasFocus = IsActive;

        var kb = hasFocus ? Keyboard.GetState() : new KeyboardState();
        var ms = hasFocus ? rawMs : new MouseState(-1, -1, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        var touches = hasFocus ? TouchPanel.GetState() : new TouchCollection();

        if (!isMouseInWindow && _gameState == GameState.Playing) {
            ms = new MouseState(ms.X, ms.Y, ms.ScrollWheelValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        }

        if (_gameState == GameState.Disconnected)
        {
            bool pressed = ms.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;
            foreach(var touch in touches) if(touch.State == TouchLocationState.Pressed) pressed = true;
            
            if (pressed) _gameState = GameState.MainMenu;
            _lastMouseState = ms;
            base.Update(gameTime);
            return;
        }

        if (_gameState == GameState.MainMenu)
        {
            if (IsActive && MediaPlayer.State != MediaState.Playing) {
                MediaPlayer.Resume();
            } else if (!IsActive && MediaPlayer.State == MediaState.Playing) {
                MediaPlayer.Pause();
            }

            bool pressed = ms.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;
            Microsoft.Xna.Framework.Vector2 pressPos = ms.Position.ToVector2();

            foreach(var touch in touches) {
                if(touch.State == TouchLocationState.Pressed) {
                    pressed = true; pressPos = touch.Position;
                }
            }

            if (pressed)
            {
                int buttonW = 300; int buttonH = 123;
                int bottomMargin = (int)(vh * 0.10f) - 40; int gap = 20;
                Rectangle remoteRect = new Rectangle(vw / 2 - buttonW / 2, vh - bottomMargin - buttonH, buttonW, buttonH);
                Rectangle localRect = new Rectangle(vw / 2 - buttonW / 2, remoteRect.Y - gap - buttonH, buttonW, buttonH);

                if (localRect.Contains(pressPos)) {
                    MediaPlayer.Stop();
                    _networking.Connect("localhost", 5000, _username);
                    _gameState = GameState.Playing;
                } else if (remoteRect.Contains(pressPos)) {
                    MediaPlayer.Stop();
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

        var input = HandleInput(dt, kb, ms, touches);
        if (input != null) { 
            _localPlayer.PendingInputs.Add(input); 
            _localPlayer.ApplyInput(input, _moveSpeed, _worldManager); 
            _networking.SendInputRequest(input); 

            if (input.Movement.X != 0 || input.Movement.Y != 0) AudioManager.StartFootsteps();
            else AudioManager.StopFootsteps();
        } else AudioManager.StopFootsteps();

        foreach (var p in _otherPlayers.Values) if(p.RoomId == _localPlayer.RoomId) p.Update(gameTime, _worldManager);
        _bulletManager.Update(gameTime, _worldManager, _entityManager, _spawnerManager, _particleManager);
        _particleManager.Update(dt);
        _worldRenderer.Update(gameTime);

        // UI Interactions
        bool interactPressed = false;
        Microsoft.Xna.Framework.Vector2 interactPos = Microsoft.Xna.Framework.Vector2.Zero;
        bool isRightClick = false;

        if (ms.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released) {
            interactPressed = true; interactPos = ms.Position.ToVector2();
        } else if (ms.RightButton == ButtonState.Pressed && _lastMouseState.RightButton == ButtonState.Released) {
            interactPressed = true; interactPos = ms.Position.ToVector2(); isRightClick = true;
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

            bool canInteract = false; PortalSpawn? nearest = null;
            foreach(var p in _portals.Values) {
                if (Microsoft.Xna.Framework.Vector2.Distance(_localPlayer.Position, new Microsoft.Xna.Framework.Vector2(p.Position.X, p.Position.Y)) < 60) {
                    canInteract = true; nearest = p; break;
                }
            }
            if (canInteract && nearest != null && new Rectangle(vw - 160, vh - 280, 80, 80).Contains(interactPos)) {
                 _networking.SendPacket(new PortalUseRequest { PortalId = nearest.PortalId }, DeliveryMethod.ReliableOrdered);
            }
        }

        _lastMouseState = ms;
        base.Update(gameTime);
    }

    private InputRequest? HandleInput(float dt, KeyboardState kb, MouseState ms, TouchCollection touches)
    {
        if (!IsActive) return null;

        Microsoft.Xna.Framework.Vector2 mv = Microsoft.Xna.Framework.Vector2.Zero;
        if (kb.IsKeyDown(Keys.W)) mv.Y -= 1; if (kb.IsKeyDown(Keys.S)) mv.Y += 1; if (kb.IsKeyDown(Keys.A)) mv.X -= 1; if (kb.IsKeyDown(Keys.D)) mv.X += 1;
        
        if (_leftJoystick.IsActive) mv += _leftJoystick.Value;
        if (mv != Microsoft.Xna.Framework.Vector2.Zero) mv.Normalize();
        
        if (kb.IsKeyDown(Keys.Space)) {
            foreach(var p in _portals.Values) {
                if (Microsoft.Xna.Framework.Vector2.Distance(_localPlayer.Position, new Microsoft.Xna.Framework.Vector2(p.Position.X, p.Position.Y)) < 60) {
                    _networking.SendPacket(new PortalUseRequest { PortalId = p.PortalId }, DeliveryMethod.ReliableOrdered);
                    break;
                }
            }
        }

        float interval = _localPlayer.Equipment != null && _localPlayer.Equipment.Length > 0 && _localPlayer.Equipment[0].WeaponType == WeaponType.Rapid ? 0.05f : _shootInterval;
        _shootTimer += dt;
        
        bool isMouseInWindow = GraphicsDevice.Viewport.Bounds.Contains(ms.Position);
        if (ms.LeftButton == ButtonState.Pressed && _shootTimer >= interval && !_rightJoystick.IsActive && isMouseInWindow) { 
            _shootTimer = 0; Shoot("basic_attack", _camera.ScreenToWorld(ms.Position.ToVector2())); 
        }

        if (_rightJoystick.IsActive && _shootTimer >= interval) {
            _shootTimer = 0;
            Shoot("basic_attack", _localPlayer.Position + _rightJoystick.Value * 100f);
        }

        return new InputRequest { Movement = new LastLight.Common.Vector2(mv.X, mv.Y), DeltaTime = dt, InputSequenceNumber = _bulletCounter++ };
    }

    private void Shoot(string abilityId, Microsoft.Xna.Framework.Vector2 targetPos)
    {
        if (!IsActive) return;
        if (_localPlayer.RoomId == 0) return;
        if (!GameDataManager.Abilities.TryGetValue(abilityId, out var spec)) return;

        float effectiveCooldown = spec.Cooldown;
        if (spec.Delivery is LastLight.Common.Abilities.ProjectileDelivery projSpec)
        {
            float fireRateInterval = projSpec.FireRate > 0 ? 1.0f / projSpec.FireRate : 0f;
            effectiveCooldown = Math.Max(effectiveCooldown, fireRateInterval);
        }

        _localCooldowns.TryGetValue(abilityId, out float lastUsed);
        if (TotalTime < lastUsed + effectiveCooldown) return;
        _localCooldowns[abilityId] = (float)TotalTime;

        if (spec.Delivery is LastLight.Common.Abilities.ProjectileDelivery proj)
        {
            var baseDir = targetPos - _localPlayer.Position; 
            if (baseDir == Microsoft.Xna.Framework.Vector2.Zero) baseDir = new Microsoft.Xna.Framework.Vector2(1, 0); 
            baseDir.Normalize();

            var vel = baseDir * proj.Speed;
            float lifeTime = (proj.RangeTiles * 32.0f) / proj.Speed;
            int bid = _predictedBulletCounter++;

            _bulletManager.Spawn(bid, _localPlayer.Id, _localPlayer.Position, vel, lifeTime, abilityId);
            _networking.SendAbilityUseRequest(new AbilityUseRequest { AbilityId = abilityId, Direction = new LastLight.Common.Vector2(baseDir.X, baseDir.Y), TargetPosition = new LastLight.Common.Vector2(targetPos.X, targetPos.Y), ClientInstanceId = bid });
            AudioManager.PlayShoot();
        }
    }

    public static Rectangle GetIconRegion(string atlas, string icon)
    {
        try {
            return AssetManager.GetIconSourceRect(atlas, icon);
        } catch {
            return Rectangle.Empty;
        }
    }

    private void DrawWorld()
    {
        if (_worldManager.Tiles == null) return;
        var tex = AssetManager.GetAtlasTexture("GroundTiles");
        
        for (int x = 0; x < _worldManager.Width; x++) {
            for (int y = 0; y < _worldManager.Height; y++) {
                string key = _worldManager.Tiles[x, y] switch { 
                    TileType.Grass => "grass", 
                    TileType.Water => "water", 
                    TileType.Wall => "wall", 
                    TileType.Sand => "sand", 
                    _ => "" 
                };
                if (string.IsNullOrEmpty(key)) continue;
                
                var source = AssetManager.GetIconSourceRect("GroundTiles", key);
                _spriteBatch.Draw(tex, new Rectangle(x * 32, y * 32, 32, 32), source, Color.White);
            }
        }
    }

    private void DrawHUD()
    {
        _spriteBatch.Begin();
        int vw = _graphics.PreferredBackBufferWidth; 
        int vh = _graphics.PreferredBackBufferHeight;
        
        int tx = 10; int ty = 10;
        _spriteBatch.Draw(_pixel, new Rectangle(5, 5, 260, 100), Color.Black * 0.5f);

        if (_font != null) _spriteBatch.DrawString(_font, $"{_username} (Lv. {_localPlayer.Level})", new Microsoft.Xna.Framework.Vector2(tx, ty), Color.White);
        
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
        float manaP = _localPlayer.MaxMana > 0 ? (float)_localPlayer.CurrentMana / _localPlayer.MaxMana : 0f;
        _spriteBatch.Draw(_pixel, new Rectangle(tx, ty, 210, 20), Color.DarkBlue);
        _spriteBatch.Draw(_pixel, new Rectangle(tx, ty, (int)(210 * manaP), 20), Color.DodgerBlue);
        if (_font != null) {
            var manaTxt = $"{_localPlayer.CurrentMana} / {_localPlayer.MaxMana}";
            var ts = _font.MeasureString(manaTxt);
            _spriteBatch.DrawString(_font, manaTxt, new Microsoft.Xna.Framework.Vector2(tx + 105 - ts.X/2, ty + 2), Color.White);
        }

        ty += 25;
        float exP = Math.Min(1f, (float)_localPlayer.Experience / (_localPlayer.Level * 100));
        _spriteBatch.Draw(_pixel, new Rectangle(tx, ty, 210, 10), Color.DarkSlateGray); 
        _spriteBatch.Draw(_pixel, new Rectangle(tx, ty, (int)(210 * exP), 10), Color.Yellow);

        ty += 15;
        if (_font != null) _spriteBatch.DrawString(_font, $"A:{_localPlayer.Attack} D:{_localPlayer.Defense} S:{_localPlayer.Speed} X:{_localPlayer.Dexterity} V:{_localPlayer.Vitality} W:{_localPlayer.Wisdom}", new Microsoft.Xna.Framework.Vector2(tx, ty), Color.LightGray, 0f, Microsoft.Xna.Framework.Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

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
            foreach(var e in _entityManager.GetAllEntities().Where(en => en.Active)) Dot(new Microsoft.Xna.Framework.Vector2(e.Position.X, e.Position.Y), Color.Red, 2);
        }

        int invW = 4 * 50; int eqW = 3 * 60; int bY = vh - 220; 
        int eqX = (vw - eqW) / 2; int eqY = bY;
        for (int i = 0; i < 3; i++) {
            Rectangle rect = new Rectangle(eqX + (i * 60), eqY, 40, 40);
            _spriteBatch.Draw(_pixel, rect, i == _selectedSlotIndex ? Color.White * 0.4f : Color.Black * 0.7f);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.Gray);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.Gray);
            if (_localPlayer.Equipment != null && i < _localPlayer.Equipment.Length && _localPlayer.Equipment[i].ItemId != 0) {
                var item = _localPlayer.Equipment[i];
                Rectangle src = GetIconRegion(item.Atlas, item.Icon);
                _spriteBatch.Draw(MainAtlas, new Rectangle(rect.X + 4, rect.Y + 4, 32, 32), src, Color.White);
            }
        }

        int invX = (vw - invW) / 2; int invY = eqY + 50;
        for (int r = 0; r < 2; r++) {
            for (int c = 0; c < 4; c++) {
                int idx = 3 + (r * 4 + c);
                Rectangle rect = new Rectangle(invX + (c * 50), invY + (r * 50), 40, 40);
                _spriteBatch.Draw(_pixel, rect, idx == _selectedSlotIndex ? Color.White * 0.4f : Color.Black * 0.7f);
                _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.Gray);
                _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.Gray);
                if (_localPlayer.Inventory != null && (idx-3) < _localPlayer.Inventory.Length && _localPlayer.Inventory[idx-3].ItemId != 0) {
                    var item = _localPlayer.Inventory[idx-3];
                    Rectangle src = GetIconRegion(item.Atlas, item.Icon);
                    _spriteBatch.Draw(MainAtlas, new Rectangle(rect.X + 4, rect.Y + 4, 32, 32), src, Color.White);
                }
            }
        }

        if (_leaderboard != null && _leaderboard.Length > 0) {
            int lbY = 120; int bgWidth = _font != null ? 150 : 50;
            _spriteBatch.Draw(_pixel, new Rectangle(5, lbY - 5, bgWidth, 10 + (_leaderboard.Length * 15)), Color.Black * 0.5f);
            float maxScore = Math.Max(1, _leaderboard.Max(e => e.Score));
            for (int i = 0; i < Math.Min(5, _leaderboard.Length); i++) {
                var entry = _leaderboard[i];
                Color rankColor = i == 0 ? Color.Gold : (i == 1 ? Color.Silver : (i == 2 ? Color.DarkOrange : Color.White));
                if (entry.PlayerId == _localPlayer.Id) rankColor = Color.LimeGreen;
                int barWidth = (int)((entry.Score / maxScore) * 30);
                if (_font != null) {
                    string n = entry.Username ?? "Guest"; if (n.Length > 8) n = n.Substring(0, 8);
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

        var boss = _entityManager.GetAllEntities().FirstOrDefault(e => e.Active && e.EnemyType == "boss");
        if (boss != null) { float bP = (float)boss.CurrentHealth / boss.MaxHealth; _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 150, 20, 300, 20), Color.Black * 0.5f); _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 150, 20, (int)(300 * bP), 20), Color.Purple); }

        bool canInteract = false; PortalSpawn? nearest = null;
        foreach(var p in _portals.Values) if (Microsoft.Xna.Framework.Vector2.Distance(_localPlayer.Position, new Microsoft.Xna.Framework.Vector2(p.Position.X, p.Position.Y)) < 60) { canInteract = true; nearest = p; break; }
        if (canInteract && nearest != null) {
            Rectangle interactBtn = new Rectangle(vw - 160, vh - 280, 80, 80);
            _spriteBatch.Draw(_pixel, interactBtn, Color.Yellow * 0.7f);
            if (_font != null) _spriteBatch.DrawString(_font, "ENTER", new Microsoft.Xna.Framework.Vector2(interactBtn.X + 10, interactBtn.Y + 30), Color.Black);
        }

        _leftJoystick.Draw(_spriteBatch, _pixel); _rightJoystick.Draw(_spriteBatch, _pixel);
        _spriteBatch.End();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue); 
        int vw = _graphics.PreferredBackBufferWidth; int vh = _graphics.PreferredBackBufferHeight;
        
        if (_gameState == GameState.Disconnected) {
            _spriteBatch.Begin();
            if (_font != null) {
                string text = "DISCONNECTED"; var size = _font.MeasureString(text);
                _spriteBatch.DrawString(_font, text, new Microsoft.Xna.Framework.Vector2(vw / 2 - size.X / 2, vh / 2 - 50), Color.Red);
                string rText = $"Reason: {_disconnectReason}"; var rSize = _font.MeasureString(rText);
                _spriteBatch.DrawString(_font, rText, new Microsoft.Xna.Framework.Vector2(vw / 2 - rSize.X / 2, vh / 2 + 10), Color.White);
                string cText = "Tap anywhere to continue";
                if (TotalTime % 1 < 0.5) { var cSize = _font.MeasureString(cText); _spriteBatch.DrawString(_font, cText, new Microsoft.Xna.Framework.Vector2(vw / 2 - cSize.X / 2, vh / 2 + 100), Color.Gray); }
            }
            _spriteBatch.End(); base.Draw(gameTime); return;
        }

        if (_gameState == GameState.MainMenu) {
            _spriteBatch.Begin();
            if (_loginBackground != null) _spriteBatch.Draw(_loginBackground, new Rectangle(0, 0, vw, vh), Color.White);
            if (_font != null) {
                string text = $"Enter Name: {_username}"; if (TotalTime % 1 < 0.5) text += "_";
                var size = _font.MeasureString(text); _spriteBatch.DrawString(_font, text, new Microsoft.Xna.Framework.Vector2(vw / 2 - size.X / 2, vh / 4), Color.White);
            }
            int buttonW = 300; int buttonH = 123; int bottomMargin = (int)(vh * 0.10f) - 40; int gap = 20;
            Rectangle remoteRect = new Rectangle(vw / 2 - buttonW / 2, vh - bottomMargin - buttonH, buttonW, buttonH);
            Rectangle localRect = new Rectangle(vw / 2 - buttonW / 2, remoteRect.Y - gap - buttonH, buttonW, buttonH);
            var ms = Mouse.GetState();
            bool isHoveringLocal = localRect.Contains(ms.Position); bool isClickingLocal = isHoveringLocal && ms.LeftButton == ButtonState.Pressed;
            if (isHoveringLocal && !_wasHoveringLocal) AudioManager.PlayDrop();
            _wasHoveringLocal = isHoveringLocal;
            if (_loginAtlas != null) {
                string buttonKey = isClickingLocal ? "button_pressed" : (isHoveringLocal ? "button_hover" : "button");
                Rectangle srcRect = GetIconRegion("Login", buttonKey);
                _spriteBatch.Draw(_loginAtlas, localRect, srcRect, Color.White);
            } else _spriteBatch.Draw(_pixel, localRect, Color.LimeGreen);
            bool isHoveringRemote = remoteRect.Contains(ms.Position); bool isClickingRemote = isHoveringRemote && ms.LeftButton == ButtonState.Pressed;
            if (isHoveringRemote && !_wasHoveringRemote) AudioManager.PlayDrop();
            _wasHoveringRemote = isHoveringRemote;
            if (_loginAtlas != null) {
                string buttonKey = isClickingRemote ? "button_pressed" : (isHoveringRemote ? "button_hover" : "button");
                Rectangle srcRect = GetIconRegion("Login", buttonKey);
                _spriteBatch.Draw(_loginAtlas, remoteRect, srcRect, Color.White);
            } else _spriteBatch.Draw(_pixel, remoteRect, Color.Blue);
            _spriteBatch.End(); base.Draw(gameTime); return;
        }

        _camera.ViewportWidth = vw; _camera.ViewportHeight = vh; _camera.Position = _localPlayer.Position;
        _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix());
        DrawWorld();
        var envTex = AssetManager.GetAtlasTexture("Environment");
        foreach(var p in _portals.Values) {
            Color pc = (p.Name ?? "").Contains("Forest") ? Color.LimeGreen : ((p.Name ?? "").Contains("Nexus") ? Color.Cyan : Color.MediumPurple);
            var portalSrc = AssetManager.GetIconSourceRect("Environment", "portal");
            _spriteBatch.Draw(envTex, new Rectangle((int)p.Position.X - 16, (int)p.Position.Y - 16, 32, 32), portalSrc, pc);
            
            string n = p.Name ?? "";
            string letterKey = n.Contains("Forest") ? "letter_f" : (n.Contains("Nexus") ? "letter_n" : "letter_d");
            var letterSrc = AssetManager.GetIconSourceRect("Environment", letterKey);
            _spriteBatch.Draw(envTex, new Rectangle((int)p.Position.X - 8, (int)p.Position.Y - 40, 16, 16), letterSrc, Color.White);
        }
        _itemManager.Draw(_spriteBatch, MainAtlas); _spawnerManager.Draw(_spriteBatch, MainAtlas, _pixel);
        _localPlayer.Draw(_spriteBatch, MainAtlas, _pixel);
        foreach (var p in _otherPlayers.Values) if(p.RoomId == _localPlayer.RoomId) p.Draw(_spriteBatch, MainAtlas, _pixel);
        
        // --- FIXED RENDERING ORDER ---
        _worldRenderer.Draw(_spriteBatch); // 1. Draw animated sprites
        _entityManager.Draw(_spriteBatch, _pixel); // 2. Draw health bars on top
        
        _particleManager.Draw(_spriteBatch, _pixel);
        _bulletManager.Draw(_spriteBatch, _pixel);
        _spriteBatch.End(); DrawHUD(); base.Draw(gameTime);
    }
}
