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
    private readonly Dictionary<int, NetPeer> _peers = new();
    private readonly Dictionary<int, string> _playerNames = new();
    private readonly Dictionary<int, ServerRoom> _rooms = new();
    private readonly Dictionary<int, float> _playerFireCooldowns = new();
    
    private float _broadcastTimer = 0f;
    private float _broadcastInterval = 0.05f;
    private float _moveSpeed = 200f;
    private int _serverBulletCounter = -1;

    public ServerNetworking(int port)
    {
        _port = port;
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this);
        RegisterPackets();

        var nexus = new ServerRoom(0, "Nexus Social Hub", 12345, 30, 30, WorldManager.GenerationStyle.Nexus, _packetProcessor, this, _playerStates);
        _rooms[0] = nexus;

        nexus.SpawnPortal(new Vector2(350, 480), -1, "Forest Realm", -3000);
        nexus.SpawnPortal(new Vector2(610, 480), -2, "Dungeon Realm", -3001);
    }

    public NetPeer? GetPeer(int id) => _peers.TryGetValue(id, out var p) ? p : null;

    public void SendPacket<T>(NetPeer peer, T packet, DeliveryMethod dm) where T : class, new() {
        var w = new NetDataWriter(); _packetProcessor.Write(w, packet); peer.Send(w, dm);
    }

    private void RegisterPackets()
    {
        _packetProcessor.RegisterNestedType((w, v) => { w.Put(v.X); w.Put(v.Y); }, r => new LastLight.Common.Vector2(r.GetFloat(), r.GetFloat()));
        _packetProcessor.RegisterNestedType<LeaderboardEntry>();

        _packetProcessor.SubscribeReusable<JoinRequest, NetPeer>((req, peer) => {
            _peers[peer.Id] = peer;
            _playerNames[peer.Id] = string.IsNullOrWhiteSpace(req.PlayerName) ? "Guest" : req.PlayerName;
            var res = new JoinResponse { Success = true, PlayerId = peer.Id, MaxHealth = 100, Level = 1, Experience = 0, CurrentWeapon = WeaponType.Single };
            _playerStates[peer.Id] = new AuthoritativePlayerUpdate { PlayerId = peer.Id, Position = new Vector2(480, 480), CurrentHealth = 100, MaxHealth = 100, Level = 1, Experience = 0, CurrentWeapon = WeaponType.Single, RoomId = 0 };
            SendPacket(peer, res, DeliveryMethod.ReliableOrdered);
            SwitchPlayerRoom(peer, 0);
        });

        _packetProcessor.SubscribeReusable<InputRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var state) && _rooms.TryGetValue(state.RoomId, out var room)) {
                float dt = Math.Min(req.DeltaTime, 0.1f);
                state.Velocity = new Vector2(req.Movement.X * _moveSpeed, req.Movement.Y * _moveSpeed);
                var np = state.Position;
                np.X += state.Velocity.X * dt; if (!room.World.IsWalkable(np)) np.X = state.Position.X;
                np.Y += state.Velocity.Y * dt; if (!room.World.IsWalkable(np)) np.Y = state.Position.Y;
                state.Position = np; state.LastProcessedInputSequence = req.InputSequenceNumber;
            }
        });

        _packetProcessor.SubscribeReusable<FireRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var state) && _rooms.TryGetValue(state.RoomId, out var room)) {
                if (state.RoomId == 0) return;

                // 1. Fire Rate Enforcement (Server Authoritative)
                float now = (float)Globals.Stopwatch.Elapsed.TotalSeconds;
                _playerFireCooldowns.TryGetValue(peer.Id, out float lastFire);
                float interval = state.CurrentWeapon == WeaponType.Rapid ? 0.05f : 0.1f;
                if (now - lastFire < interval * 0.9f) return; 
                _playerFireCooldowns[peer.Id] = now;

                // 2. Authoritative Weapon Pattern Spawning
                float baseAngle = (float)Math.Atan2(req.Direction.Y, req.Direction.X);
                void ServerFire(float angle) {
                    var d = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    var v = new Vector2(d.X * 500f, d.Y * 500f);
                    int bid = _serverBulletCounter--;
                    room.Bullets.Spawn(bid, peer.Id, state.Position, v);
                    room.Broadcast(new SpawnBullet { OwnerId = peer.Id, BulletId = bid, Position = state.Position, Velocity = v });
                }

                switch (state.CurrentWeapon) {
                    case WeaponType.Single: ServerFire(baseAngle); break;
                    case WeaponType.Double: ServerFire(baseAngle - 0.05f); ServerFire(baseAngle + 0.05f); break;
                    case WeaponType.Spread: ServerFire(baseAngle - 0.2f); ServerFire(baseAngle); ServerFire(baseAngle + 0.2f); break;
                    case WeaponType.Rapid: ServerFire(baseAngle); break;
                }
            }
        });

        _packetProcessor.SubscribeReusable<PortalUseRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var state) && _rooms.TryGetValue(state.RoomId, out var room)) {
                foreach(var portal in room.Portals.Values) {
                    if (Math.Abs(state.Position.X - portal.Position.X) < 60 && Math.Abs(state.Position.Y - portal.Position.Y) < 60) {
                        int tid = portal.TargetRoomId;
                        if (tid < 0 || !_rooms.ContainsKey(tid)) {
                            if (portal.Name.Contains("Forest")) tid = CreateNewRoom("Forest World Instance", WorldManager.GenerationStyle.Biomes);
                            else tid = CreateNewRoom("Dungeon World Instance", WorldManager.GenerationStyle.Dungeon);
                            portal.TargetRoomId = tid;
                        }
                        SwitchPlayerRoom(peer, tid);
                        break;
                    }
                }
            }
        });
    }

    private int CreateNewRoom(string name, WorldManager.GenerationStyle style) {
        int id = _rooms.Count;
        while(_rooms.ContainsKey(id)) id++;
        var room = new ServerRoom(id, name, new Random().Next(), 100, 100, style, _packetProcessor, this, _playerStates);
        _rooms[id] = room;
        var rand = new Random();
        for (int i = 0; i < 15; i++) {
            Vector2 pos = new Vector2(rand.Next(200, 3000), rand.Next(200, 3000));
            if (room.World.IsWalkable(pos)) room.Spawners.CreateSpawner(pos, 500, 8);
        }
        room.Bosses.SpawnBoss(new Vector2(1600, 1600), 5000);
        return id;
    }

    private void SwitchPlayerRoom(NetPeer peer, int roomId) {
        if (!_playerStates.TryGetValue(peer.Id, out var state)) return;
        if (!_rooms.TryGetValue(roomId, out var room)) return;
        
        int oldRoomId = state.RoomId;
        state.RoomId = roomId; 
        
        if (_rooms.TryGetValue(oldRoomId, out var oldRoom)) {
            var leaveW = new NetDataWriter(); _packetProcessor.Write(leaveW, state);
            foreach(var p in oldRoom.GetPlayersInRoom()) if(p.Key != peer.Id) _netManager.GetPeerById(p.Key).Send(leaveW, DeliveryMethod.ReliableOrdered);
        }

        Vector2 spawnPos = new Vector2(room.World.Width * 16, room.World.Height * 16);
        for (int i = 0; i < 100; i++) {
            var tp = new Vector2(new Random().Next(100, (room.World.Width - 2) * 32), new Random().Next(100, (room.World.Height - 2) * 32));
            if (room.World.IsWalkable(tp)) { spawnPos = tp; break; }
        }
        state.Position = spawnPos;

        SendPacket(peer, new WorldInit { Seed = room.Seed, Width = room.World.Width, Height = room.World.Height, TileSize = 32, Style = room.Style }, DeliveryMethod.ReliableOrdered);
        foreach (var p in room.Portals.Values) SendPacket(peer, p, DeliveryMethod.ReliableOrdered);
        foreach (var i in room.Items.GetActiveItems()) SendPacket(peer, new ItemSpawn { ItemId = i.Id, Position = i.Position, Type = i.Type }, DeliveryMethod.ReliableOrdered);
        foreach (var e in room.Enemies.GetAllEnemies()) if (e.Active) SendPacket(peer, new EnemySpawn { EnemyId = e.Id, Position = e.Position, MaxHealth = e.MaxHealth }, DeliveryMethod.ReliableOrdered);
        foreach (var s in room.Spawners.GetAllSpawners()) if (s.Active) SendPacket(peer, new SpawnerSpawn { SpawnerId = s.Id, Position = s.Position, MaxHealth = s.MaxHealth }, DeliveryMethod.ReliableOrdered);
        foreach (var b in room.Bosses.GetAllBosses()) if (b.Active) SendPacket(peer, new BossSpawn { BossId = b.Id, Position = b.Position, MaxHealth = b.MaxHealth }, DeliveryMethod.ReliableOrdered);
        foreach (var other in room.GetPlayersInRoom().Values) if(other.PlayerId != peer.Id) SendPacket(peer, other, DeliveryMethod.ReliableOrdered);
    }

    public void Update(float dt) {
        var ids = _rooms.Keys.ToList();
        foreach (var id in ids) {
            var r = _rooms[id]; r.Update(dt);
            if (r.IsMarkedForDeletion) {
                Console.WriteLine($"[Server] Deleting room {r.Id}");
                if (r.ParentRoomId != -1 && _rooms.TryGetValue(r.ParentRoomId, out var pr)) {
                    if (pr.Portals.Remove(r.ParentPortalId)) pr.Broadcast(new PortalDeath { PortalId = r.ParentPortalId });
                }
                _rooms.Remove(r.Id);
            }
        }
        _broadcastTimer += dt; if (_broadcastTimer >= _broadcastInterval) { _broadcastTimer -= _broadcastInterval; BroadcastUpdates(); }
    }

    private void BroadcastUpdates() {
        foreach (var r in _rooms.Values) {
            var players = r.GetPlayersInRoom();
            if (players.Count == 0) continue;
            foreach (var p in players.Values) {
                var w = new NetDataWriter(); _packetProcessor.Write(w, p);
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
            foreach (var e in r.Enemies.GetActiveEnemies()) {
                var w = new NetDataWriter(); _packetProcessor.Write(w, new EnemyUpdate { EnemyId = e.Id, Position = e.Position, CurrentHealth = e.CurrentHealth });
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
            foreach (var s in r.Spawners.GetActiveSpawners()) {
                var w = new NetDataWriter(); _packetProcessor.Write(w, new SpawnerUpdate { SpawnerId = s.Id, CurrentHealth = s.CurrentHealth });
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
            foreach (var b in r.Bosses.GetActiveBosses()) {
                var w = new NetDataWriter(); _packetProcessor.Write(w, new BossUpdate { BossId = b.Id, Position = b.Position, CurrentHealth = b.CurrentHealth, Phase = b.Phase });
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
            
            var entries = r.RoomScores.Select(kvp => new LeaderboardEntry { PlayerId = kvp.Key, PlayerName = _playerNames.GetValueOrDefault(kvp.Key, "Guest"), Score = kvp.Value }).OrderByDescending(e => e.Score).ToArray();
            if (entries.Length > 0) {
                var w = new NetDataWriter(); _packetProcessor.Write(w, new LeaderboardUpdate { Entries = entries });
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
        }
    }

    public void Start() {
        if (Environment.GetEnvironmentVariable("FLY_APP_NAME") != null) {
            try {
                var ips = Dns.GetHostAddresses("fly-global-services");
                var ipv4 = ips.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork) ?? IPAddress.Any;
                var ipv6 = ips.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetworkV6) ?? IPAddress.IPv6Any;
                _netManager.Start(ipv4, ipv6, _port);
                Console.WriteLine($"[Server] Bound to Fly.io global services. IPv4: {ipv4}, IPv6: {ipv6}");
                return;
            } catch (Exception ex) {
                Console.WriteLine($"[Server] Error binding to fly-global-services: {ex.Message}");
            }
        }
        _netManager.Start(_port);
    }
    public void PollEvents() => _netManager.PollEvents();
    public void Stop() => _netManager.Stop();
    public void OnPeerConnected(NetPeer p) => Console.WriteLine($"Connected: {p}");
    public void OnPeerDisconnected(NetPeer p, DisconnectInfo info) { _playerStates.Remove(p.Id); _peers.Remove(p.Id); _playerNames.Remove(p.Id); }
    public void OnNetworkError(IPEndPoint ep, SocketError err) { }
    public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte ch, DeliveryMethod dm) => _packetProcessor.ReadAllPackets(r, p);
    public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader r, UnconnectedMessageType t) { }
    public void OnNetworkLatencyUpdate(NetPeer p, int l) { }
    public void OnConnectionRequest(ConnectionRequest r) => r.AcceptIfKey("LastLightKey");
}