using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;

namespace LastLight.Server;

public partial class LocalServer : Node
{
    private readonly Dictionary<int, AuthoritativePlayerUpdate> _playerStates = new();
    private readonly Dictionary<int, string> _usernames = new();
    private readonly Dictionary<int, ServerRoom> _rooms = new();
    private readonly Dictionary<int, float> _playerFireCooldowns = new();
    
    private float _broadcastTimer = 0f;
    private float _broadcastInterval = 0.05f;
    private int _serverBulletCounter = -1;
    private int _localPlayerId = 0;

    // Mimic Networking.cs signals
    [Signal] public delegate void JoinResponseReceivedEventHandler(bool success, int playerId, string message);
    [Signal] public delegate void WorldInitReceivedEventHandler(int seed, int width, int height, int tileSize, int style, float cleanupTimer);
    
    public event Action<AuthoritativePlayerUpdate>? PlayerUpdateReceived;
    public event Action<LeaderboardUpdate>? LeaderboardUpdated;

    [Signal] public delegate void BulletSpawnedEventHandler(int ownerId, int bulletId, Godot.Vector2 position, Godot.Vector2 velocity);
    [Signal] public delegate void BulletHitEventHandler(int bulletId, int targetId, int targetType);
    [Signal] public delegate void EnemySpawnedEventHandler(int enemyId, Godot.Vector2 position, int maxHealth, string dataId);
    [Signal] public delegate void EnemyUpdatedEventHandler(int enemyId, Godot.Vector2 position, int currentHealth);
    [Signal] public delegate void EnemyDiedEventHandler(int enemyId);
    [Signal] public delegate void SpawnerSpawnedEventHandler(int spawnerId, Godot.Vector2 position, int maxHealth);
    [Signal] public delegate void SpawnerUpdatedEventHandler(int spawnerId, int currentHealth);
    [Signal] public delegate void SpawnerDiedEventHandler(int spawnerId);
    [Signal] public delegate void PortalSpawnedEventHandler(int portalId, Godot.Vector2 position, int targetRoomId, string name);
    [Signal] public delegate void PortalDiedEventHandler(int portalId);
    [Signal] public delegate void BossSpawnedEventHandler(int bossId, Godot.Vector2 position, int maxHealth, string dataId);
    [Signal] public delegate void BossUpdatedEventHandler(int bossId, Godot.Vector2 position, int currentHealth, int phase);
    [Signal] public delegate void BossDiedEventHandler(int bossId);
    [Signal] public delegate void ItemSpawnedEventHandler(int itemId, Godot.Vector2 position, string itemName);
    [Signal] public delegate void ItemPickedUpEventHandler(int itemId, int playerId);
    [Signal] public delegate void RoomStateUpdatedEventHandler(float cleanupTimer);

    public void Initialize()
    {
        var nexusData = GameDataManager.Rooms.TryGetValue("room_nexus", out var nd) ? nd : new RoomData { Id = "room_nexus", Name = "Nexus Social Hub", Width = 30, Height = 30, Style = WorldManager.GenerationStyle.Nexus };
        var nexus = new ServerRoom(0, nexusData, 12345, this, _playerStates);
        _rooms[0] = nexus;

        nexus.SpawnPortal(new LastLight.Common.Vector2(350, 480), -1, "Forest Realm", -3000);
        nexus.SpawnPortal(new LastLight.Common.Vector2(610, 480), -2, "Dungeon Realm", -3001);
    }

    public void JoinGame(string username)
    {
        _localPlayerId = 0;
        _usernames[_localPlayerId] = string.IsNullOrWhiteSpace(username) ? "Player" : username;

        var dbPlayer = DatabaseManager.LoadPlayer(_usernames[_localPlayerId]);
        if (dbPlayer == null) {
            dbPlayer = new PlayerSaveData();
            var starterWeapon = new ItemInfo { ItemId = 1, DataId = "weapon_basic_staff" };
            dbPlayer.Equipment[0] = starterWeapon;
        }

        var state = new AuthoritativePlayerUpdate { 
            PlayerId = _localPlayerId, 
            Position = new LastLight.Common.Vector2(480, 480), 
            CurrentHealth = dbPlayer.MaxHealth, 
            MaxHealth = dbPlayer.MaxHealth, 
            Level = dbPlayer.Level, 
            Experience = dbPlayer.Experience, 
            RoomId = 0, 
            Attack = dbPlayer.Attack, 
            Defense = dbPlayer.Defense, 
            Speed = dbPlayer.Speed, 
            Dexterity = dbPlayer.Dexterity, 
            Vitality = dbPlayer.Vitality, 
            Wisdom = dbPlayer.Wisdom, 
            Equipment = dbPlayer.Equipment, 
            Inventory = dbPlayer.Inventory 
        };
        _playerStates[_localPlayerId] = state;

        EmitSignal(SignalName.JoinResponseReceived, true, _localPlayerId, "Welcome to Singleplayer!");
        SwitchPlayerRoom(_localPlayerId, 0);
    }

    public void HandleInput(InputRequest req)
    {
        if (_playerStates.TryGetValue(_localPlayerId, out var state) && _rooms.TryGetValue(state.RoomId, out var room)) {
            float dt = Math.Min(req.DeltaTime, 0.1f);
            float speed = 100f + (state.Speed * 5f);
            state.Velocity = new LastLight.Common.Vector2(req.Movement.X * speed, req.Movement.Y * speed);
            var np = state.Position;
            np.X += state.Velocity.X * dt; if (!room.World.IsWalkable(np)) np.X = state.Position.X;
            np.Y += state.Velocity.Y * dt; if (!room.World.IsWalkable(np)) np.Y = state.Position.Y;
            state.Position = np; state.LastProcessedInputSequence = req.InputSequenceNumber;
        }
    }

    public void HandleFire(FireRequest req)
    {
        if (_playerStates.TryGetValue(_localPlayerId, out var state) && _rooms.TryGetValue(state.RoomId, out var room)) {
            if (state.RoomId == 0) return;

            float now = (float)Time.GetTicksMsec() / 1000f;
            _playerFireCooldowns.TryGetValue(_localPlayerId, out float lastFire);
            
            float baseInterval = state.Equipment[0].WeaponType == WeaponType.Rapid ? 0.1f : 0.2f;
            float interval = baseInterval / (1.0f + state.Dexterity * 0.1f);
            
            if (now - lastFire < interval * 0.9f) return; 
            _playerFireCooldowns[_localPlayerId] = now;

            float baseAngle = (float)Math.Atan2(req.Direction.Y, req.Direction.X);
            void ServerFire(float angle) {
                var d = new LastLight.Common.Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                var v = new LastLight.Common.Vector2(d.X * 500f, d.Y * 500f);
                int bid = _serverBulletCounter--;
                room.Bullets.Spawn(bid, _localPlayerId, state.Position, v);
                BroadcastPacket(new SpawnBullet { OwnerId = _localPlayerId, BulletId = bid, Position = state.Position, Velocity = v }, state.RoomId);
            }

            switch (state.Equipment[0].WeaponType) {
                case WeaponType.Single: ServerFire(baseAngle); break;
                case WeaponType.Double: ServerFire(baseAngle - 0.05f); ServerFire(baseAngle + 0.05f); break;
                case WeaponType.Spread: ServerFire(baseAngle - 0.2f); ServerFire(baseAngle); ServerFire(baseAngle + 0.2f); break;
                case WeaponType.Rapid: ServerFire(baseAngle); break;
            }
        }
    }

    public void HandlePortalUse()
    {
        if (_playerStates.TryGetValue(_localPlayerId, out var state) && _rooms.TryGetValue(state.RoomId, out var room)) {
            foreach(var portal in room.Portals.Values) {
                if (Math.Abs(state.Position.X - portal.Position.X) < 60 && Math.Abs(state.Position.Y - portal.Position.Y) < 60) {
                    int tid = portal.TargetRoomId;
                    if (tid < 0 || !_rooms.ContainsKey(tid)) {
                        if (portal.Name.Contains("Forest")) tid = CreateNewRoom("room_forest");
                        else tid = CreateNewRoom("room_dungeon");
                        portal.TargetRoomId = tid;
                    }
                    if (state.RoomId != 0) SavePlayer(_localPlayerId);
                    SwitchPlayerRoom(_localPlayerId, tid);
                    break;
                }
            }
        }
    }

    public void HandleSwapItem(SwapItemRequest req)
    {
        if (_playerStates.TryGetValue(_localPlayerId, out var p)) {
            ItemInfo[] fromArray = req.FromIndex < 3 ? p.Equipment : p.Inventory;
            int fromIdx = req.FromIndex < 3 ? req.FromIndex : req.FromIndex - 3;
            ItemInfo[] toArray = req.ToIndex < 3 ? p.Equipment : p.Inventory;
            int toIdx = req.ToIndex < 3 ? req.ToIndex : req.ToIndex - 3;

            if (fromIdx >= 0 && fromIdx < fromArray.Length && toIdx >= 0 && toIdx < toArray.Length) {
                ItemInfo item = fromArray[fromIdx];
                if (req.ToIndex < 3 && item.ItemId != 0) {
                    if (req.ToIndex == 0 && item.Category != ItemCategory.Weapon) return;
                    if (req.ToIndex == 1 && item.Category != ItemCategory.Armor) return;
                    if (req.ToIndex == 2 && item.Category != ItemCategory.Ring) return;
                }
                fromArray[fromIdx] = toArray[toIdx];
                toArray[toIdx] = item;
            }
        }
    }

    public void HandleUseItem(UseItemRequest req)
    {
        if (_playerStates.TryGetValue(_localPlayerId, out var p)) {
            ItemInfo[] slots = req.SlotIndex < 3 ? p.Equipment : p.Inventory;
            int idx = req.SlotIndex < 3 ? req.SlotIndex : req.SlotIndex - 3;
            if (idx >= 0 && idx < slots.Length) {
                ItemInfo item = slots[idx];
                if (item.ItemId != 0 && item.Category == ItemCategory.Consumable) {
                    if (item.Name.Contains("Health Potion")) {
                        p.CurrentHealth = Math.Min(p.MaxHealth, p.CurrentHealth + item.StatBonus);
                        slots[idx] = new ItemInfo();
                    }
                }
            }
        }
    }

    public void SavePlayer(int playerId) {
        if (_usernames.TryGetValue(playerId, out var username) && _playerStates.TryGetValue(playerId, out var state)) {
            var data = new PlayerSaveData {
                MaxHealth = state.MaxHealth, Level = state.Level, Experience = state.Experience,
                Attack = state.Attack, Defense = state.Defense, Speed = state.Speed,
                Dexterity = state.Dexterity, Vitality = state.Vitality, Wisdom = state.Wisdom,
                Equipment = state.Equipment, Inventory = state.Inventory
            };
            DatabaseManager.SavePlayer(username, data);
        }
    }

    private int CreateNewRoom(string roomId) {
        if (!GameDataManager.Rooms.TryGetValue(roomId, out var rd)) return -1;
        int id = _rooms.Count;
        while(_rooms.ContainsKey(id)) id++;
        var room = new ServerRoom(id, rd, new Random().Next(), this, _playerStates);
        _rooms[id] = room;
        var rand = new Random();
        for (int i = 0; i < rd.SpawnerCount; i++) {
            var pos = new LastLight.Common.Vector2(rand.Next(200, Math.Max(300, rd.Width * 32 - 200)), rand.Next(200, Math.Max(300, rd.Height * 32 - 200)));
            if (room.World.IsWalkable(pos)) room.Spawners.CreateSpawner(pos, 100, 8);
        }
        if (room.Spawners.GetActiveSpawners().Count == 0) {
            room.Bosses.SpawnBoss(new LastLight.Common.Vector2(rd.Width * 16, rd.Height * 16), 1000);
        }
        return id;
    }

    public void SwitchPlayerToNexus(int playerId) => SwitchPlayerRoom(playerId, 0);

    private void SwitchPlayerRoom(int playerId, int roomId) {
        if (!_playerStates.TryGetValue(playerId, out var state)) return;
        if (!_rooms.TryGetValue(roomId, out var room)) return;
        
        state.RoomId = roomId; 

        var spawnPos = new LastLight.Common.Vector2(room.World.Width * 16, room.World.Height * 16);
        for (int i = 0; i < 100; i++) {
            var tp = new LastLight.Common.Vector2(new Random().Next(100, (room.World.Width - 2) * 32), new Random().Next(100, (room.World.Height - 2) * 32));
            if (room.World.IsWalkable(tp)) { spawnPos = tp; break; }
        }
        state.Position = spawnPos;

        EmitSignal(SignalName.WorldInitReceived, room.Seed, room.World.Width, room.World.Height, 32, (int)room.Style, room.ForceCleanupTimer ?? -1f);
        
        foreach (var p in room.Portals.Values) EmitSignal(SignalName.PortalSpawned, p.PortalId, new Godot.Vector2(p.Position.X, p.Position.Y), p.TargetRoomId, p.Name);
        foreach (var i in room.Items.GetActiveItems()) EmitSignal(SignalName.ItemSpawned, i.Id, new Godot.Vector2(i.Position.X, i.Position.Y), i.Info.Name);
        foreach (var e in room.Enemies.GetAllEnemies()) if (e.Active) EmitSignal(SignalName.EnemySpawned, e.Id, new Godot.Vector2(e.Position.X, e.Position.Y), e.MaxHealth, e.DataId);
        foreach (var s in room.Spawners.GetAllSpawners()) if (s.Active) EmitSignal(SignalName.SpawnerSpawned, s.Id, new Godot.Vector2(s.Position.X, s.Position.Y), s.MaxHealth);
        foreach (var b in room.Bosses.GetAllBosses()) if (b.Active) EmitSignal(SignalName.BossSpawned, b.Id, new Godot.Vector2(b.Position.X, b.Position.Y), b.MaxHealth, "boss");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        var activeRooms = _rooms.Values.ToList();
        
        foreach (var r in activeRooms) {
            r.Update(dt);
            if (r.IsMarkedForDeletion) {
                _rooms.Remove(r.Id);
            }
        }
        
        _broadcastTimer += dt; 
        if (_broadcastTimer >= _broadcastInterval) { 
            _broadcastTimer -= _broadcastInterval; 
            BroadcastUpdates(); 
        }
    }

    private void BroadcastUpdates() {
        if (_playerStates.TryGetValue(_localPlayerId, out var state) && _rooms.TryGetValue(state.RoomId, out var r)) {
            PlayerUpdateReceived?.Invoke(state);
            
            foreach (var e in r.Enemies.GetActiveEnemies()) EmitSignal(SignalName.EnemyUpdated, e.Id, new Godot.Vector2(e.Position.X, e.Position.Y), e.CurrentHealth);
            foreach (var s in r.Spawners.GetActiveSpawners()) EmitSignal(SignalName.SpawnerUpdated, s.Id, s.CurrentHealth);
            foreach (var b in r.Bosses.GetActiveBosses()) EmitSignal(SignalName.BossUpdated, b.Id, new Godot.Vector2(b.Position.X, b.Position.Y), b.CurrentHealth, b.Phase);
            
            var entries = r.RoomScores.Select(kvp => new LeaderboardEntry { PlayerId = kvp.Key, Username = _usernames.GetValueOrDefault(kvp.Key, "Player"), Score = kvp.Value }).OrderByDescending(e => e.Score).ToArray();
            if (entries.Length > 0) LeaderboardUpdated?.Invoke(new LeaderboardUpdate { Entries = entries });

            if (r.ForceCleanupTimer.HasValue) EmitSignal(SignalName.RoomStateUpdated, r.ForceCleanupTimer.Value);
        }
    }

    public void BroadcastPacket<T>(T packet, int roomId) where T : class
    {
        if (_playerStates[_localPlayerId].RoomId != roomId) return;

        // Route internal packets to Godot Signals
        if (packet is EnemySpawn es) EmitSignal(SignalName.EnemySpawned, es.EnemyId, new Godot.Vector2(es.Position.X, es.Position.Y), es.MaxHealth, es.DataId);
        else if (packet is EnemyDeath ed) EmitSignal(SignalName.EnemyDied, ed.EnemyId);
        else if (packet is SpawnerSpawn ss) EmitSignal(SignalName.SpawnerSpawned, ss.SpawnerId, new Godot.Vector2(ss.Position.X, ss.Position.Y), ss.MaxHealth);
        else if (packet is SpawnerDeath sd) EmitSignal(SignalName.SpawnerDied, sd.SpawnerId);
        else if (packet is BossSpawn bs) EmitSignal(SignalName.BossSpawned, bs.BossId, new Godot.Vector2(bs.Position.X, bs.Position.Y), bs.MaxHealth, bs.DataId);
        else if (packet is BossDeath bd) EmitSignal(SignalName.BossDied, bd.BossId);
        else if (packet is ItemSpawn ispn) EmitSignal(SignalName.ItemSpawned, ispn.ItemId, new Godot.Vector2(ispn.Position.X, ispn.Position.Y), ispn.Item.Name);
        else if (packet is ItemPickup ip) EmitSignal(SignalName.ItemPickedUp, ip.ItemId, ip.PlayerId);
        else if (packet is PortalSpawn ps) EmitSignal(SignalName.PortalSpawned, ps.PortalId, new Godot.Vector2(ps.Position.X, ps.Position.Y), ps.TargetRoomId, ps.Name);
        else if (packet is PortalDeath pd) EmitSignal(SignalName.PortalDied, pd.PortalId);
        else if (packet is SpawnBullet sb) EmitSignal(SignalName.BulletSpawned, sb.OwnerId, sb.BulletId, new Godot.Vector2(sb.Position.X, sb.Position.Y), new Godot.Vector2(sb.Velocity.X, sb.Velocity.Y));
        else if (packet is BulletHit bh) EmitSignal(SignalName.BulletHit, bh.BulletId, bh.TargetId, (int)bh.TargetType);
    }
}
