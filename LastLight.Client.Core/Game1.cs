using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using LastLight.Common;
using LiteNetLib;
using LastLight.Common.Abilities;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace LastLight.Client.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ClientNetworking _networking;
    private Texture2D _pixel;
    public static Texture2D MainAtlas = null!; 
    private Texture2D _loginBackground;
    private Texture2D _loginAtlas;
    
    public static IAssetManager AssetManager = null!; 
    private WorldRenderer _worldRenderer = null!;
    private TileMapRenderer _tileMapRenderer = null!;
    private bool _pendingWorldBake = false;

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
    private InventoryCollection _selectedCollection = InventoryCollection.Equipment;
    private int _selectedSlotIndex = -1;
    private bool _showCharacterSheet = false;
    private bool _showDungeonLoot = false;
    private bool _showStash = false;
    private MouseState _lastMouseState;
    private KeyboardState _lastKeyboardState;
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
        _tileMapRenderer = null!;

        _networking.OnJoinResponse = (res) => { 
            if (res.Success) { 
                _localPlayer.Id = res.PlayerId; 
                _localPlayer.MaxHealth = res.MaxHealth; 
                _localPlayer.CurrentHealth = res.MaxHealth; 
                _localPlayer.Level = res.Level;
                _localPlayer.Experience = res.Experience;
                _localPlayer.Attack = res.Attack;
                _localPlayer.Defense = res.Defense;
                _localPlayer.Speed = res.Speed;
                _localPlayer.Dexterity = res.Dexterity;
                _localPlayer.Vitality = res.Vitality;
                _localPlayer.Wisdom = res.Wisdom;
                _localPlayer.Equipment = res.Equipment;
                _localPlayer.Toolbelt = res.Toolbelt;
                _localPlayer.Stash = res.Stash;
                _localPlayer.DungeonLoot = res.DungeonLoot;
                _localPlayer.RunGold = res.RunGold;
                
                if (res.Equipment != null && res.Equipment.Length > 0) {
                    Console.WriteLine($"[Client] Eq[0] - ItemId: {res.Equipment[0].ItemId}, DataId: {res.Equipment[0].DataId}");
                }

                _gameState = GameState.Playing;
                _worldRenderer?.PlayAnimation(_localPlayer, "Player2", "idle", true);
            } 
        };
        _networking.OnWorldInit = (init) => {
            _localPlayer.RoomId = init.RoomId;
            _worldManager.GenerateWorld(init.Seed, init.Width, init.Height, init.TileSize, init.Style);
            _roomCleanupTimer = init.CleanupTimer;
            
            if (_tileMapRenderer != null) _tileMapRenderer.BakeWorld(GraphicsDevice, _worldManager);
            else _pendingWorldBake = true;

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
            
            _otherPlayers.Clear();
        };

        _networking.OnPlayerSpawn = HandlePlayerSpawn;
        _networking.OnPlayerLeave = HandlePlayerLeave;
        _networking.OnPlayerUpdate = HandlePlayerUpdate;
        _networking.OnSelfStateUpdate = (u) => {
            _localPlayer.CurrentMana = u.CurrentMana;
            _localPlayer.MaxMana = u.MaxMana;
            _localPlayer.Experience = u.Experience;
            _localPlayer.RunGold = u.RunGold;
            _localPlayer.Attack = u.Attack;
            _localPlayer.Defense = u.Defense;
            _localPlayer.Speed = u.Speed;
            _localPlayer.Dexterity = u.Dexterity;
            _localPlayer.Vitality = u.Vitality;
            _localPlayer.Wisdom = u.Wisdom;
        };
        _networking.OnInventoryUpdate = (u) => {
            if (u.Collection == InventoryCollection.Equipment) _localPlayer.Equipment[u.SlotIndex] = u.Item;
            else if (u.Collection == InventoryCollection.Toolbelt) _localPlayer.Toolbelt[u.SlotIndex] = u.Item;
            else if (u.Collection == InventoryCollection.Stash) _localPlayer.Stash[u.SlotIndex] = u.Item;
            else if (u.Collection == InventoryCollection.DungeonLoot) _localPlayer.DungeonLoot[u.SlotIndex] = u.Item;
        };
        _networking.OnSpawnBullet = (s) => { 
            if(s.OwnerId != _localPlayer.Id) {
                _bulletManager.Spawn(s.BulletId, s.OwnerId, new Microsoft.Xna.Framework.Vector2(s.Position.X, s.Position.Y), new Microsoft.Xna.Framework.Vector2(s.Velocity.X, s.Velocity.Y), s.LifeTime, s.AbilityId); 
                if (_localPlayer.RoomId != 0 && s.OwnerId >= 0) AudioManager.PlayShoot();
            }
        };
        _networking.OnEffectEvent = (e) => {
            if (e.EffectName == "damage") {
                AudioManager.PlayHit();
                _particleManager.SpawnBurst(new Microsoft.Xna.Framework.Vector2(e.Position.X, e.Position.Y), 5, Color.Yellow, 80f, 0.3f, 3f);
            }
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

    private PortalSpawn? GetNearestPortal()
    {
        foreach (var p in _portals.Values)
        {
            if (Vector2.Distance(_localPlayer.Position, new Vector2(p.Position.X, p.Position.Y)) < 60) return p;
        }
        return null;
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
        Exiting += (s, a) => {
            _networking.Disconnect();
            _tileMapRenderer?.Dispose();
        };
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
        _tileMapRenderer = new TileMapRenderer(AssetManager);
        if (_pendingWorldBake) {
            _tileMapRenderer.BakeWorld(GraphicsDevice, _worldManager);
            _pendingWorldBake = false;
        }

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
        bool hasFocus = IsActive;
        var ms = hasFocus ? rawMs : new MouseState(-1, -1, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        var kb = hasFocus ? Keyboard.GetState() : new KeyboardState();
        var touches = hasFocus ? TouchPanel.GetState() : new TouchCollection();

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
            if (IsActive && MediaPlayer.State != MediaState.Playing) MediaPlayer.Resume();
            else if (!IsActive && MediaPlayer.State == MediaState.Playing) MediaPlayer.Pause();

            bool pressed = ms.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;
            Vector2 pressPos = ms.Position.ToVector2();
            foreach(var touch in touches) if(touch.State == TouchLocationState.Pressed) { pressed = true; pressPos = touch.Position; }

            if (pressed)
            {
                int buttonW = 300; int buttonH = 123;
                int bottomMargin = (int)(vh * 0.10f) - 40; int gap = 20;
                Rectangle remoteRect = new Rectangle(vw / 2 - buttonW / 2, vh - bottomMargin - buttonH, buttonW, buttonH);
                Rectangle localRect = new Rectangle(vw / 2 - buttonW / 2, remoteRect.Y - gap - buttonH, buttonW, buttonH);

                if (localRect.Contains(pressPos)) { MediaPlayer.Stop(); _networking.Connect("localhost", 5000, _username); _gameState = GameState.Playing; }
                else if (remoteRect.Contains(pressPos)) { MediaPlayer.Stop(); _networking.Connect("169.155.55.157", 5000, _username); _gameState = GameState.Playing; }
            }
            _lastMouseState = ms;
            base.Update(gameTime);
            return;
        }

        _leftJoystick.Position = new Vector2(120, vh - 150);
        _rightJoystick.Position = new Vector2(vw - 120, vh - 150);
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

        foreach (var p in _otherPlayers.Values) p.Update(gameTime, _worldManager);
        _bulletManager.Update(gameTime, _worldManager, _entityManager, _spawnerManager, _particleManager);
        _particleManager.Update(dt);
        _worldRenderer.Update(gameTime);

        UpdateUIInteractions(ms, touches, vw, vh);

        _lastMouseState = ms;
        base.Update(gameTime);
    }

    private void UpdateUIInteractions(MouseState ms, TouchCollection touches, int vw, int vh)
    {
        bool interactPressed = false;
        Vector2 interactPos = Vector2.Zero;
        bool isRightClick = false;

        if (ms.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released) { interactPressed = true; interactPos = ms.Position.ToVector2(); }
        else if (ms.RightButton == ButtonState.Pressed && _lastMouseState.RightButton == ButtonState.Released) { interactPressed = true; interactPos = ms.Position.ToVector2(); isRightClick = true; }

        foreach (var touch in touches) if (touch.State == TouchLocationState.Pressed) {
            if (Vector2.Distance(touch.Position, _leftJoystick.Position) > _leftJoystick.Radius * 1.5f &&
                Vector2.Distance(touch.Position, _rightJoystick.Position) > _rightJoystick.Radius * 1.5f) { interactPressed = true; interactPos = touch.Position; }
        }

        if (interactPressed) {
            InventoryCollection clickedCol = (InventoryCollection)255;
            int clickedIdx = -1;

            if (_showCharacterSheet) {
                int eqX = 20; int eqY = 150;
                for (int i = 0; i < 5; i++) if (new Rectangle(eqX, eqY + (i * 50), 40, 40).Contains(interactPos)) { clickedCol = InventoryCollection.Equipment; clickedIdx = i; }
                int tbX = (vw - 400) / 2; int tbY = vh - 100;
                for (int i = 0; i < 8; i++) if (new Rectangle(tbX + (i * 50), tbY, 40, 40).Contains(interactPos)) { clickedCol = InventoryCollection.Toolbelt; clickedIdx = i; }
            }
            if (_showDungeonLoot) {
                int dlX = (vw - 250) / 2; int dlY = 150;
                for (int r = 0; r < 10; r++) for (int c = 0; c < 5; c++) if (new Rectangle(dlX + (c * 50), dlY + (r * 50), 40, 40).Contains(interactPos)) { clickedCol = InventoryCollection.DungeonLoot; clickedIdx = r * 5 + c; }
            }
            if (_showStash) {
                int stX = (vw - 250) / 2; int stY = 150;
                for (int r = 0; r < 10; r++) for (int c = 0; c < 5; c++) if (new Rectangle(stX + (c * 50), stY + (r * 50), 40, 40).Contains(interactPos)) { clickedCol = InventoryCollection.Stash; clickedIdx = r * 5 + c; }
            }

            if (clickedIdx != -1) {
                if (isRightClick) _networking.SendUseItemRequest(clickedCol, clickedIdx);
                else {
                    if (_selectedSlotIndex == -1) { _selectedCollection = clickedCol; _selectedSlotIndex = clickedIdx; }
                    else { if (_selectedCollection != clickedCol || _selectedSlotIndex != clickedIdx) _networking.SendSwapItemRequest(_selectedCollection, _selectedSlotIndex, clickedCol, clickedIdx); _selectedSlotIndex = -1; }
                }
            } else _selectedSlotIndex = -1;

            var nearest = GetNearestPortal();
            if (nearest != null && new Rectangle(vw - 160, vh - 280, 80, 80).Contains(interactPos)) _networking.SendPacket(new PortalUseRequest { PortalId = nearest.PortalId }, DeliveryMethod.ReliableOrdered);
        }
    }

    private InputRequest? HandleInput(float dt, KeyboardState kb, MouseState ms, TouchCollection touches)
    {
        if (!IsActive) return null;
        Vector2 mv = Vector2.Zero;
        if (kb.IsKeyDown(Keys.W)) mv.Y -= 1; if (kb.IsKeyDown(Keys.S)) mv.Y += 1; if (kb.IsKeyDown(Keys.A)) mv.X -= 1; if (kb.IsKeyDown(Keys.D)) mv.X += 1;
        if (_leftJoystick.IsActive) mv += _leftJoystick.Value;
        if (mv != Vector2.Zero) mv.Normalize();
        
        if (kb.IsKeyDown(Keys.Space)) {
            var p = GetNearestPortal();
            if (p != null) _networking.SendPacket(new PortalUseRequest { PortalId = p.PortalId }, DeliveryMethod.ReliableOrdered);
        }

        if (kb.IsKeyDown(Keys.C) && !_lastKeyboardState.IsKeyDown(Keys.C)) _showCharacterSheet = !_showCharacterSheet;
        if (kb.IsKeyDown(Keys.I) && !_lastKeyboardState.IsKeyDown(Keys.I)) _showDungeonLoot = !_showDungeonLoot;
        if (kb.IsKeyDown(Keys.B) && !_lastKeyboardState.IsKeyDown(Keys.B)) {
            if (_localPlayer.RoomId == 0) _showStash = !_showStash;
        }
        _lastKeyboardState = kb;

        float interval = _localPlayer.Equipment != null && _localPlayer.Equipment.Length > 0 && !string.IsNullOrEmpty(_localPlayer.Equipment[0].DataId) && GameDataManager.Items.TryGetValue(_localPlayer.Equipment[0].DataId, out var d) && d.GetString(_localPlayer.Equipment[0].CurrentTier, "weapon_type") == "rapid" ? 0.05f : _shootInterval;
        _shootTimer += dt;
        if (ms.LeftButton == ButtonState.Pressed && _shootTimer >= interval && !_rightJoystick.IsActive && GraphicsDevice.Viewport.Bounds.Contains(ms.Position)) { 
            _shootTimer = 0; Shoot("basic_attack", _camera.ScreenToWorld(ms.Position.ToVector2())); 
        }
        if (_rightJoystick.IsActive && _shootTimer >= interval) { _shootTimer = 0; Shoot("basic_attack", _localPlayer.Position + _rightJoystick.Value * 100f); }
        return new InputRequest { Movement = new LastLight.Common.Vector2(mv.X, mv.Y), DeltaTime = dt, InputSequenceNumber = _bulletCounter++ };
    }

    private void Shoot(string abilityId, Vector2 targetPos)
    {
        if (!IsActive || _localPlayer.RoomId == 0 || !GameDataManager.Abilities.TryGetValue(abilityId, out var spec)) return;
        float effectiveCooldown = spec.Cooldown;
        if (spec.Delivery is ProjectileDelivery projSpec) effectiveCooldown = Math.Max(effectiveCooldown, projSpec.FireRate > 0 ? 1.0f / projSpec.FireRate : 0f);
        _localCooldowns.TryGetValue(abilityId, out float lastUsed);
        if (TotalTime < lastUsed + effectiveCooldown) return;
        _localCooldowns[abilityId] = (float)TotalTime;

        if (spec.Delivery is ProjectileDelivery proj) {
            Vector2 baseDir = targetPos - _localPlayer.Position; if (baseDir == Vector2.Zero) baseDir = new Vector2(1, 0); baseDir.Normalize();
            Vector2 vel = baseDir * proj.Speed;
            float lifeTime = (proj.RangeTiles * 32.0f) / proj.Speed;
            int bid = _predictedBulletCounter++;
            _bulletManager.Spawn(bid, _localPlayer.Id, _localPlayer.Position, vel, lifeTime, abilityId);
            _networking.SendAbilityUseRequest(new AbilityUseRequest { AbilityId = abilityId, Direction = new LastLight.Common.Vector2(baseDir.X, baseDir.Y), TargetPosition = new LastLight.Common.Vector2(targetPos.X, targetPos.Y), ClientInstanceId = bid });
            AudioManager.PlayShoot();
        }
    }

    public static Rectangle GetIconRegion(string atlas, string icon) { try { return AssetManager.GetIconSourceRect(atlas, icon); } catch { return Rectangle.Empty; } }

    private void DrawHUD()
    {
        _spriteBatch.Begin();
        int vw = _graphics.PreferredBackBufferWidth; int vh = _graphics.PreferredBackBufferHeight;
        _spriteBatch.Draw(_pixel, new Rectangle(5, 5, 260, 100), Color.Black * 0.5f);
        if (_font != null) _spriteBatch.DrawString(_font, $"{_username} (Lv. {_localPlayer.Level})", new Vector2(10, 10), Color.White);
        
        int ty = 35;
        void Bar(int x, int y, float p, Color c1, Color c2, string txt) {
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, 210, 20), c1);
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, (int)(210 * p), 20), c2);
            if (_font != null) { Vector2 sz = _font.MeasureString(txt); _spriteBatch.DrawString(_font, txt, new Vector2(x + 105 - sz.X/2, y + 2), Color.White); }
        }
        Bar(10, ty, (float)_localPlayer.CurrentHealth / _localPlayer.MaxHealth, Color.DarkRed, Color.Red, $"{_localPlayer.CurrentHealth} / {_localPlayer.MaxHealth}"); ty += 25;
        Bar(10, ty, _localPlayer.MaxMana > 0 ? (float)_localPlayer.CurrentMana / _localPlayer.MaxMana : 0f, Color.DarkBlue, Color.DodgerBlue, $"{_localPlayer.CurrentMana} / {_localPlayer.MaxMana}"); ty += 25;
        float exP = Math.Min(1f, (float)_localPlayer.Experience / (_localPlayer.Level * 100));
        _spriteBatch.Draw(_pixel, new Rectangle(10, ty, 210, 10), Color.DarkSlateGray); _spriteBatch.Draw(_pixel, new Rectangle(10, ty, (int)(210 * exP), 10), Color.Yellow); ty += 15;
        if (_font != null) _spriteBatch.DrawString(_font, $"A:{_localPlayer.Attack} D:{_localPlayer.Defense} S:{_localPlayer.Speed} X:{_localPlayer.Dexterity} V:{_localPlayer.Vitality} W:{_localPlayer.Wisdom}", new Vector2(10, ty), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        if (_showCharacterSheet) {
            int eqX = 20; int eqY = 150;
            string[] eqNames = { "Wpn", "Hlm", "Arm", "Glv", "Bts" };
            for (int i = 0; i < 5; i++) {
                Rectangle rect = new Rectangle(eqX, eqY + (i * 50), 40, 40);
                bool sel = _selectedCollection == InventoryCollection.Equipment && i == _selectedSlotIndex;
                _spriteBatch.Draw(_pixel, rect, sel ? Color.White * 0.4f : Color.Black * 0.7f);
                if (_font != null) _spriteBatch.DrawString(_font, eqNames[i], new Vector2(eqX + 45, eqY + (i * 50) + 10), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                if (_localPlayer.Equipment != null && i < _localPlayer.Equipment.Length && _localPlayer.Equipment[i].ItemId != 0) {
                    var item = _localPlayer.Equipment[i];
                    _spriteBatch.Draw(MainAtlas, new Rectangle(rect.X + 4, rect.Y + 4, 32, 32), GetIconRegion(item.Atlas, item.Icon), Color.White);
                }
            }
            int tbX = (vw - 400) / 2; int tbY = vh - 100;
            for (int i = 0; i < 8; i++) {
                Rectangle rect = new Rectangle(tbX + (i * 50), tbY, 40, 40);
                bool sel = _selectedCollection == InventoryCollection.Toolbelt && i == _selectedSlotIndex;
                _spriteBatch.Draw(_pixel, rect, i >= _localPlayer.ToolbeltSize ? Color.Gray * 0.3f : (sel ? Color.White * 0.4f : Color.Black * 0.7f));
                if (_localPlayer.Toolbelt != null && i < _localPlayer.Toolbelt.Length && _localPlayer.Toolbelt[i].ItemId != 0) {
                    var item = _localPlayer.Toolbelt[i];
                    _spriteBatch.Draw(MainAtlas, new Rectangle(rect.X + 4, rect.Y + 4, 32, 32), GetIconRegion(item.Atlas, item.Icon), Color.White);
                }
            }
        }

        void DrawGrid(InventoryCollection col, ItemInfo[] items, string title) {
            int gX = (vw - 250) / 2; int gY = 150;
            _spriteBatch.Draw(_pixel, new Rectangle(gX - 10, gY - 40, 270, 550), Color.Black * 0.8f);
            if (_font != null) _spriteBatch.DrawString(_font, title, new Vector2(gX, gY - 35), Color.Yellow);
            for (int r = 0; r < 10; r++) for (int c = 0; c < 5; c++) {
                int idx = r * 5 + c; Rectangle rect = new Rectangle(gX + (c * 50), gY + (r * 50), 40, 40);
                bool sel = _selectedCollection == col && idx == _selectedSlotIndex;
                _spriteBatch.Draw(_pixel, rect, sel ? Color.White * 0.4f : Color.Black * 0.5f);
                if (items != null && idx < items.Length && items[idx].ItemId != 0) {
                    var item = items[idx];
                    _spriteBatch.Draw(MainAtlas, new Rectangle(rect.X + 4, rect.Y + 4, 32, 32), GetIconRegion(item.Atlas, item.Icon), Color.White);
                }
            }
        }
        if (_showDungeonLoot) DrawGrid(InventoryCollection.DungeonLoot, _localPlayer.DungeonLoot, "DUNGEON LOOT");
        if (_showStash) DrawGrid(InventoryCollection.Stash, _localPlayer.Stash, "STASH / BANK");

        int ms = 150; int mx = vw - ms - 20; int my = 20; 
        _spriteBatch.Draw(_pixel, new Rectangle(mx - 2, my - 2, ms + 4, ms + 4), Color.Black * 0.5f);
        if (_worldManager.Tiles != null) {
            for (int x = 0; x < _worldManager.Width; x++) for (int y = 0; y < _worldManager.Height; y++) {
                Color c = _worldManager.Tiles[x, y] switch { TileType.Wall => Color.Gray, TileType.Water => Color.Blue, TileType.Sand => Color.SandyBrown, _ => Color.Transparent };
                if (c != Color.Transparent) _spriteBatch.Draw(_pixel, new Rectangle(mx + (int)(x*ms/(float)_worldManager.Width), my + (int)(y*ms/(float)_worldManager.Height), 1, 1), c * 0.5f);
            }
            void Dot(Vector2 p, Color c, int s = 3) { _spriteBatch.Draw(_pixel, new Rectangle(mx + (int)(p.X/32*ms/(float)_worldManager.Width) - s/2, my + (int)(p.Y/32*ms/(float)_worldManager.Height) - s/2, s, s), c); }
            Dot(_localPlayer.Position, Color.White, 5);
            foreach(var p in _otherPlayers.Values) Dot(p.Position, Color.LimeGreen, 3);
            foreach(var s in _spawnerManager.GetAllSpawners()) if (s.Active) Dot(new Vector2(s.Position.X, s.Position.Y), Color.Orange, 4);
            foreach(var e in _entityManager.GetAllEntities()) {
                if (!e.Active) continue;
                if (e.EnemyType == "boss") Dot(new Vector2(e.Position.X, e.Position.Y), Color.Purple, 6);
                else Dot(new Vector2(e.Position.X, e.Position.Y), Color.Red, 2);
            }
        }

        if (_roomCleanupTimer > 0 && _font != null) {
            string t = $"Room collapsing in: {Math.Ceiling(_roomCleanupTimer)}s"; Vector2 sz = _font.MeasureString(t);
            _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - (int)sz.X/2 - 10, 50, (int)sz.X + 20, (int)sz.Y + 10), Color.Black * 0.7f);
            _spriteBatch.DrawString(_font, t, new Vector2(vw/2 - sz.X/2, 55), Color.Red);
        }

        ClientEntity? boss = null;
        foreach (var e in _entityManager.GetAllEntities()) if (e.Active && e.EnemyType == "boss") { boss = e; break; }
        if (boss != null) { float bP = (float)boss.CurrentHealth / boss.MaxHealth; _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 150, 20, 300, 20), Color.Black * 0.5f); _spriteBatch.Draw(_pixel, new Rectangle(vw/2 - 150, 20, (int)(300 * bP), 20), Color.Purple); }

        var near = GetNearestPortal();
        if (near != null) {
            Rectangle btn = new Rectangle(vw - 160, vh - 280, 80, 80); _spriteBatch.Draw(_pixel, btn, Color.Yellow * 0.7f);
            if (_font != null) _spriteBatch.DrawString(_font, "ENTER", new Vector2(btn.X + 10, btn.Y + 30), Color.Black);
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
                string t = "DISCONNECTED"; Vector2 sz = _font.MeasureString(t); _spriteBatch.DrawString(_font, t, new Vector2(vw/2 - sz.X/2, vh/2 - 50), Color.Red);
                string rt = $"Reason: {_disconnectReason}"; Vector2 rsz = _font.MeasureString(rt); _spriteBatch.DrawString(_font, rt, new Vector2(vw/2 - rsz.X/2, vh/2 + 10), Color.White);
            }
            _spriteBatch.End(); base.Draw(gameTime); return;
        }

        if (_gameState == GameState.MainMenu) {
            _spriteBatch.Begin();
            if (_loginBackground != null) _spriteBatch.Draw(_loginBackground, new Rectangle(0, 0, vw, vh), Color.White);
            if (_font != null) {
                string t = $"Enter Name: {_username}"; if (TotalTime % 1 < 0.5) t += "_";
                Vector2 sz = _font.MeasureString(t); _spriteBatch.DrawString(_font, t, new Vector2(vw/2 - sz.X/2, vh/4), Color.White);
            }
            Rectangle rr = new Rectangle(vw/2 - 150, vh - 150, 300, 123); Rectangle lr = new Rectangle(vw/2 - 150, rr.Y - 143, 300, 123);
            MouseState m = Mouse.GetState(); bool hl = lr.Contains(m.Position); bool hr = rr.Contains(m.Position);
            if (hl && !_wasHoveringLocal) AudioManager.PlayDrop(); _wasHoveringLocal = hl;
            if (hr && !_wasHoveringRemote) AudioManager.PlayDrop(); _wasHoveringRemote = hr;
            if (_loginAtlas != null) {
                _spriteBatch.Draw(_loginAtlas, lr, GetIconRegion("Login", m.LeftButton == ButtonState.Pressed && hl ? "button_pressed" : (hl ? "button_hover" : "button")), Color.White);
                _spriteBatch.Draw(_loginAtlas, rr, GetIconRegion("Login", m.LeftButton == ButtonState.Pressed && hr ? "button_pressed" : (hr ? "button_hover" : "button")), Color.White);
            } else { _spriteBatch.Draw(_pixel, lr, Color.LimeGreen); _spriteBatch.Draw(_pixel, rr, Color.Blue); }
            _spriteBatch.End(); base.Draw(gameTime); return;
        }

        _camera.ViewportWidth = vw; _camera.ViewportHeight = vh; _camera.Position = _localPlayer.Position;
        _spriteBatch.Begin(transformMatrix: _camera.GetTransformationMatrix());
        _tileMapRenderer.Draw(_spriteBatch, _camera);
        var envTex = AssetManager.GetAtlasTexture("Environment");
        foreach(var p in _portals.Values) {
            string n = p.Name ?? "";
            Color pc = n.Contains("Forest") ? Color.LimeGreen : (n.Contains("Nexus") ? Color.Cyan : Color.MediumPurple);
            _spriteBatch.Draw(envTex, new Rectangle((int)p.Position.X - 16, (int)p.Position.Y - 16, 32, 32), AssetManager.GetIconSourceRect("Environment", "portal"), pc);
        }
        _itemManager.Draw(_spriteBatch, MainAtlas); _spawnerManager.Draw(_spriteBatch, MainAtlas, _pixel);
        _localPlayer.Draw(_spriteBatch, MainAtlas, _pixel);
        foreach (var p in _otherPlayers.Values) p.Draw(_spriteBatch, MainAtlas, _pixel);
        _worldRenderer.Draw(_spriteBatch); _entityManager.Draw(_spriteBatch, _pixel);
        _particleManager.Draw(_spriteBatch, _pixel); _bulletManager.Draw(_spriteBatch, _pixel);
        _spriteBatch.End(); DrawHUD(); base.Draw(gameTime);
    }
}
