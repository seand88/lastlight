using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using LastLight.Common;

namespace LastLight.Server;

public class ServerNetworking : INetEventListener
{
    private readonly NetManager _netManager;
    private readonly NetPacketProcessor _packetProcessor;
    private readonly int _port;

    private readonly Dictionary<int, AuthoritativePlayerUpdate> _playerStates = new();
    private readonly ServerBulletManager _bulletManager = new();
    private readonly ServerEnemyManager _enemyManager = new();
    private readonly ServerSpawnerManager _spawnerManager = new();
    private readonly ServerBossManager _bossManager = new();
    private readonly ServerItemManager _itemManager = new();
    private readonly WorldManager _worldManager = new();
    private float _broadcastTimer = 0f;
    private float _broadcastInterval = 0.05f;
    private float _moveSpeed = 200f;

    private int _serverBulletCounter = -1;

    public ServerNetworking(int port)
    {
        _port = port;
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this);
        _worldManager.GenerateWorld(12345, 100, 100, 32);
        RegisterPackets();

        _enemyManager.OnEnemySpawned += (enemy) => {
            var p = new EnemySpawn { EnemyId = enemy.Id, Position = enemy.Position, MaxHealth = enemy.MaxHealth };
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        _enemyManager.OnEnemyDied += (enemy) => {
            if (enemy.ParentSpawnerId != -1) _spawnerManager.NotifyEnemyDeath(enemy.ParentSpawnerId);
            int r = new Random().Next(100);
            if (r < 25) _itemManager.SpawnItem(ItemType.HealthPotion, enemy.Position);
            else if (r < 35) _itemManager.SpawnItem(ItemType.WeaponUpgrade, enemy.Position);
            var p = new EnemyDeath { EnemyId = enemy.Id };
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        _enemyManager.OnEnemyShoot += (enemy, pos, vel) => {
            int bid = _serverBulletCounter--;
            var p = new SpawnBullet { OwnerId = enemy.Id, BulletId = bid, Position = pos, Velocity = vel };
            _bulletManager.Spawn(bid, enemy.Id, pos, vel);
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        _spawnerManager.OnSpawnerCreated += (s) => {
            var p = new SpawnerSpawn { SpawnerId = s.Id, Position = s.Position, MaxHealth = s.MaxHealth };
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        _spawnerManager.OnSpawnerDied += (s) => {
            var p = new SpawnerDeath { SpawnerId = s.Id };
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        _spawnerManager.OnRequestEnemySpawn += (pos, sid) => _enemyManager.SpawnEnemy(pos, 100, sid);

        _bossManager.OnBossSpawned += (b) => {
            var p = new BossSpawn { BossId = b.Id, Position = b.Position, MaxHealth = b.MaxHealth };
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        _bossManager.OnBossDied += (b) => {
            var p = new BossDeath { BossId = b.Id };
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
            for(int i=0; i<5; i++) _itemManager.SpawnItem(ItemType.WeaponUpgrade, new Vector2(b.Position.X + i*10, b.Position.Y));
        };

        _bossManager.OnBossShoot += (b, pos, vel) => {
            int bid = _serverBulletCounter--;
            var p = new SpawnBullet { OwnerId = b.Id, BulletId = bid, Position = pos, Velocity = vel };
            _bulletManager.Spawn(bid, b.Id, pos, vel);
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        _itemManager.OnItemSpawned += (item) => {
            var p = new ItemSpawn { ItemId = item.Id, Position = item.Position, Type = item.Type };
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        _itemManager.OnItemPickedUp += (item, pid) => {
            if (_playerStates.TryGetValue(pid, out var state)) {
                if (item.Type == ItemType.HealthPotion) state.CurrentHealth = System.Math.Min(state.CurrentHealth + 25, state.MaxHealth);
                else if (item.Type == ItemType.WeaponUpgrade) {
                    state.CurrentWeapon = state.CurrentWeapon switch {
                        WeaponType.Single => WeaponType.Double, WeaponType.Double => WeaponType.Spread,
                        WeaponType.Spread => WeaponType.Rapid, _ => WeaponType.Rapid
                    };
                }
            }
            var p = new ItemPickup { ItemId = item.Id, PlayerId = pid };
            var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        };

        var rand = new Random();
        for (int i = 0; i < 15; i++) {
            Vector2 pos = new Vector2(800, 800);
            for (int j = 0; j < 50; j++) {
                var tp = new Vector2(rand.Next(200, 3000), rand.Next(200, 3000));
                if (_worldManager.IsWalkable(tp)) { pos = tp; break; }
            }
            _spawnerManager.CreateSpawner(pos, 500, 8);
        }

        _bossManager.SpawnBoss(new Vector2(1600, 1600), 5000);
    }

    private void RegisterPackets() {
        _packetProcessor.RegisterNestedType((w, v) => { w.Put(v.X); w.Put(v.Y); }, r => new LastLight.Common.Vector2(r.GetFloat(), r.GetFloat()));
        _packetProcessor.SubscribeReusable<JoinRequest, NetPeer>((req, peer) => {
            var res = new JoinResponse { Success = true, PlayerId = peer.Id, MaxHealth = 100, Level = 1, Experience = 0, CurrentWeapon = WeaponType.Single };
            _playerStates[peer.Id] = new AuthoritativePlayerUpdate { PlayerId = peer.Id, Position = new Vector2(400, 300), CurrentHealth = 100, MaxHealth = 100, Level = 1, Experience = 0, CurrentWeapon = WeaponType.Single };
            var w = new NetDataWriter(); _packetProcessor.Write(w, res); peer.Send(w, DeliveryMethod.ReliableOrdered);
            var wi = new WorldInit { Seed = 12345, Width = 100, Height = 100, TileSize = 32 };
            var ww = new NetDataWriter(); _packetProcessor.Write(ww, wi); peer.Send(ww, DeliveryMethod.ReliableOrdered);
            foreach (var i in _itemManager.GetActiveItems()) { var p = new ItemSpawn { ItemId = i.Id, Position = i.Position, Type = i.Type }; var iw = new NetDataWriter(); _packetProcessor.Write(iw, p); peer.Send(iw, DeliveryMethod.ReliableOrdered); }
            foreach (var e in _enemyManager.GetAllEnemies()) { if (e.Active) { var p = new EnemySpawn { EnemyId = e.Id, Position = e.Position, MaxHealth = e.MaxHealth }; var ew = new NetDataWriter(); _packetProcessor.Write(ew, p); peer.Send(ew, DeliveryMethod.ReliableOrdered); } }
            foreach (var s in _spawnerManager.GetAllSpawners()) { if (s.Active) { var p = new SpawnerSpawn { SpawnerId = s.Id, Position = s.Position, MaxHealth = s.MaxHealth }; var sw = new NetDataWriter(); _packetProcessor.Write(sw, p); peer.Send(sw, DeliveryMethod.ReliableOrdered); } }
            foreach (var b in _bossManager.GetAllBosses()) { if (b.Active) { var p = new BossSpawn { BossId = b.Id, Position = b.Position, MaxHealth = b.MaxHealth }; var bw = new NetDataWriter(); _packetProcessor.Write(bw, p); peer.Send(bw, DeliveryMethod.ReliableOrdered); } }
        });
        _packetProcessor.SubscribeReusable<InputRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var state)) {
                float dt = Math.Min(req.DeltaTime, 0.1f);
                state.Velocity = new Vector2(req.Movement.X * _moveSpeed, req.Movement.Y * _moveSpeed);
                var np = state.Position; np.X += state.Velocity.X * dt; if (!_worldManager.IsWalkable(np)) np.X = state.Position.X;
                np.Y += state.Velocity.Y * dt; if (!_worldManager.IsWalkable(np)) np.Y = state.Position.Y;
                state.Position = np; state.LastProcessedInputSequence = req.InputSequenceNumber;
            }
        });
        _packetProcessor.SubscribeReusable<FireRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var state)) {
                var vel = new Vector2(req.Direction.X * 500f, req.Direction.Y * 500f);
                _bulletManager.Spawn(req.BulletId, peer.Id, state.Position, vel);
                var p = new SpawnBullet { OwnerId = peer.Id, BulletId = req.BulletId, Position = state.Position, Velocity = vel };
                var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
            }
        });
    }

    public void Update(float dt) {
        _spawnerManager.Update(dt, _worldManager); _enemyManager.Update(dt, _playerStates, _worldManager);
        _bossManager.Update(dt, _playerStates);
        _itemManager.Update(_playerStates); _bulletManager.Update(dt); CheckCollisions();
        _broadcastTimer += dt; if (_broadcastTimer >= _broadcastInterval) { _broadcastTimer -= _broadcastInterval; BroadcastUpdates(); }
    }

    private void CheckCollisions() {
        foreach (var b in _bulletManager.GetActiveBullets()) {
            bool hit = false;
            // Wall Collision
            if (!_worldManager.IsShootable(b.Position)) { 
                _bulletManager.DestroyBullet(b); hit = true; 
                var w = new NetDataWriter(); _packetProcessor.Write(w, new BulletHit { BulletId = b.BulletId, TargetId = -1, TargetType = EntityType.Spawner }); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered); 
                continue; 
            }

            // Players take damage from AI Bullets (OwnerId < 0)
            foreach (var p in _playerStates.Values) {
                if (b.OwnerId == p.PlayerId || b.OwnerId >= 0) continue; // Skip if owned by player or same player
                if (Math.Abs(b.Position.X - p.Position.X) < 20 && Math.Abs(b.Position.Y - p.Position.Y) < 20) {
                    p.CurrentHealth -= 10; if (p.CurrentHealth <= 0) { p.CurrentHealth = p.MaxHealth; p.Position = new Vector2(400, 300); }
                    var w = new NetDataWriter(); _packetProcessor.Write(w, new BulletHit { BulletId = b.BulletId, TargetId = p.PlayerId, TargetType = EntityType.Player }); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
                    _bulletManager.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;

            // Spawners take damage from Player Bullets (OwnerId >= 0)
            foreach (var s in _spawnerManager.GetActiveSpawners()) {
                if (b.OwnerId < 0) continue; // AI bullets don't hurt AI
                if (Math.Abs(b.Position.X - s.Position.X) < 36 && Math.Abs(b.Position.Y - s.Position.Y) < 36) {
                    _spawnerManager.HandleDamage(s.Id, 25);
                    if (!_spawnerManager.GetAllSpawners().First(x => x.Id == s.Id).Active && _playerStates.TryGetValue(b.OwnerId, out var shooter)) GrantExp(shooter, 100);
                    var w = new NetDataWriter(); _packetProcessor.Write(w, new BulletHit { BulletId = b.BulletId, TargetId = s.Id, TargetType = EntityType.Spawner }); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
                    _bulletManager.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;

            // Enemies take damage from Player Bullets (OwnerId >= 0)
            foreach (var e in _enemyManager.GetActiveEnemies()) {
                if (b.OwnerId < 0) continue; // AI bullets don't hurt AI
                if (Math.Abs(b.Position.X - e.Position.X) < 20 && Math.Abs(b.Position.Y - e.Position.Y) < 20) {
                    _enemyManager.HandleDamage(e.Id, 25);
                    if (!_enemyManager.GetAllEnemies().First(x => x.Id == e.Id).Active && _playerStates.TryGetValue(b.OwnerId, out var shooter)) GrantExp(shooter, 20);
                    var w = new NetDataWriter(); _packetProcessor.Write(w, new BulletHit { BulletId = b.BulletId, TargetId = e.Id, TargetType = EntityType.Enemy }); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
                    _bulletManager.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;

            // Bosses take damage from Player Bullets (OwnerId >= 0)
            foreach (var boss in _bossManager.GetActiveBosses()) {
                if (b.OwnerId < 0) continue; // AI bullets don't hurt AI
                if (Math.Abs(b.Position.X - boss.Position.X) < 68 && Math.Abs(b.Position.Y - boss.Position.Y) < 68) {
                    _bossManager.HandleDamage(boss.Id, 25);
                    if (!_bossManager.GetAllBosses().First(x => x.Id == boss.Id).Active && _playerStates.TryGetValue(b.OwnerId, out var shooter)) GrantExp(shooter, 1000);
                    var w = new NetDataWriter(); _packetProcessor.Write(w, new BulletHit { BulletId = b.BulletId, TargetId = boss.Id, TargetType = EntityType.Boss }); _netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
                    _bulletManager.DestroyBullet(b); break;
                }
            }
        }
    }

    private void GrantExp(AuthoritativePlayerUpdate p, int amount) {
        p.Experience += amount; int needed = p.Level * 100;
        if (p.Experience >= needed) { p.Experience -= needed; p.Level++; p.MaxHealth += 20; p.CurrentHealth = p.MaxHealth; }
    }

    private void BroadcastUpdates() {
        foreach (var p in _playerStates.Values) { var w = new NetDataWriter(); _packetProcessor.Write(w, p); _netManager.SendToAll(w, DeliveryMethod.Unreliable); }
        foreach (var e in _enemyManager.GetActiveEnemies()) { var w = new NetDataWriter(); _packetProcessor.Write(w, new EnemyUpdate { EnemyId = e.Id, Position = e.Position, CurrentHealth = e.CurrentHealth }); _netManager.SendToAll(w, DeliveryMethod.Unreliable); }
        foreach (var s in _spawnerManager.GetActiveSpawners()) { var w = new NetDataWriter(); _packetProcessor.Write(w, new SpawnerUpdate { SpawnerId = s.Id, CurrentHealth = s.CurrentHealth }); _netManager.SendToAll(w, DeliveryMethod.Unreliable); }
        foreach (var boss in _bossManager.GetActiveBosses()) { var w = new NetDataWriter(); _packetProcessor.Write(w, new BossUpdate { BossId = boss.Id, Position = boss.Position, CurrentHealth = boss.CurrentHealth, Phase = boss.Phase }); _netManager.SendToAll(w, DeliveryMethod.Unreliable); }
    }

    public void Start() => _netManager.Start(_port);
    public void PollEvents() => _netManager.PollEvents();
    public void Stop() => _netManager.Stop();
    public void OnPeerConnected(NetPeer peer) => Console.WriteLine($"[Server] Peer connected: {peer}");
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) { Console.WriteLine($"[Server] Peer disconnected: {peer}. Reason: {info.Reason}"); _playerStates.Remove(peer.Id); }
    public void OnNetworkError(IPEndPoint ep, SocketError err) => Console.WriteLine($"[Server] Network error for {ep}: {err}");
    public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte ch, DeliveryMethod dm) => _packetProcessor.ReadAllPackets(r, p);
    public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader r, UnconnectedMessageType t) { }
    public void OnNetworkLatencyUpdate(NetPeer p, int l) { }
    public void OnConnectionRequest(ConnectionRequest r) => r.AcceptIfKey("LastLightKey");
}
