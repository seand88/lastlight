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
    private readonly Dictionary<int, ServerRoom> _rooms = new();
    
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

        var nexus = new ServerRoom(0, "Nexus", 12345, 100, 100, _packetProcessor, this, _playerStates);
        _rooms[0] = nexus;

        var rand = new Random();
        for (int i = 0; i < 15; i++) {
            Vector2 pos = new Vector2(rand.Next(200, 3000), rand.Next(200, 3000));
            if (nexus.World.IsWalkable(pos)) nexus.Spawners.CreateSpawner(pos, 500, 8);
        }
    }

    public NetPeer? GetPeer(int id) => _peers.TryGetValue(id, out var p) ? p : null;

    private void SendPacket<T>(NetPeer peer, T packet, DeliveryMethod dm) where T : class, new() {
        var w = new NetDataWriter(); _packetProcessor.Write(w, packet); peer.Send(w, dm);
    }

    private void RegisterPackets()
    {
        _packetProcessor.RegisterNestedType((w, v) => { w.Put(v.X); w.Put(v.Y); }, r => new LastLight.Common.Vector2(r.GetFloat(), r.GetFloat()));

        _packetProcessor.SubscribeReusable<JoinRequest, NetPeer>((req, peer) => {
            _peers[peer.Id] = peer;
            var res = new JoinResponse { Success = true, PlayerId = peer.Id, MaxHealth = 100, Level = 1, Experience = 0, CurrentWeapon = WeaponType.Single };
            _playerStates[peer.Id] = new AuthoritativePlayerUpdate { PlayerId = peer.Id, Position = new Vector2(400, 300), CurrentHealth = 100, MaxHealth = 100, Level = 1, Experience = 0, CurrentWeapon = WeaponType.Single, RoomId = 0 };
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
                var vel = new Vector2(req.Direction.X * 500f, req.Direction.Y * 500f);
                int bid = _serverBulletCounter--;
                room.Bullets.Spawn(bid, peer.Id, state.Position, vel);
                room.Broadcast(new SpawnBullet { OwnerId = peer.Id, BulletId = bid, Position = state.Position, Velocity = vel });
            }
        });

        _packetProcessor.SubscribeReusable<PortalUseRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var state) && _rooms.TryGetValue(state.RoomId, out var room)) {
                foreach(var portal in room.Portals.Values) {
                    if (Math.Abs(state.Position.X - portal.Position.X) < 50 && Math.Abs(state.Position.Y - portal.Position.Y) < 50) {
                        int tid = portal.TargetRoomId;
                        if (tid == -1) {
                            tid = CreateDungeon(state.RoomId, portal.PortalId);
                            portal.TargetRoomId = tid; // Save link
                        }
                        SwitchPlayerRoom(peer, tid);
                        break;
                    }
                }
            }
        });
    }

    private int CreateDungeon(int parentRoomId, int parentPortalId) {
        int id = _rooms.Count;
        while(_rooms.ContainsKey(id)) id++;
        var room = new ServerRoom(id, "Dungeon", new Random().Next(), 50, 50, _packetProcessor, this, _playerStates) {
            ParentRoomId = parentRoomId,
            ParentPortalId = parentPortalId
        };
        _rooms[id] = room;
        room.Bosses.SpawnBoss(new Vector2(800, 800), 2500);
        return id;
    }

    private void SwitchPlayerRoom(NetPeer peer, int roomId) {
        if (!_playerStates.TryGetValue(peer.Id, out var state)) return;
        state.RoomId = roomId; 
        
        var room = _rooms[roomId];

        // Find a safe spawn point in the new room
        Vector2 spawnPos = new Vector2(room.World.Width * 16, room.World.Height * 16); // Default to middle
        for (int i = 0; i < 100; i++) {
            var testPos = new Vector2(new Random().Next(100, (room.World.Width - 2) * 32), new Random().Next(100, (room.World.Height - 2) * 32));
            if (room.World.IsWalkable(testPos)) {
                spawnPos = testPos;
                break;
            }
        }
        state.Position = spawnPos;

        // Send correct Seed and room dimensions
        SendPacket(peer, new WorldInit { 
            Seed = room.Seed, 
            Width = room.World.Width, 
            Height = room.World.Height, 
            TileSize = 32,
            Style = (roomId == 0 ? WorldManager.GenerationStyle.Biomes : WorldManager.GenerationStyle.Dungeon)
        }, DeliveryMethod.ReliableOrdered);
        foreach (var i in room.Items.GetActiveItems()) SendPacket(peer, new ItemSpawn { ItemId = i.Id, Position = i.Position, Type = i.Type }, DeliveryMethod.ReliableOrdered);
        foreach (var e in room.Enemies.GetAllEnemies()) if (e.Active) SendPacket(peer, new EnemySpawn { EnemyId = e.Id, Position = e.Position, MaxHealth = e.MaxHealth }, DeliveryMethod.ReliableOrdered);
        foreach (var s in room.Spawners.GetAllSpawners()) if (s.Active) SendPacket(peer, new SpawnerSpawn { SpawnerId = s.Id, Position = s.Position, MaxHealth = s.MaxHealth }, DeliveryMethod.ReliableOrdered);
        foreach (var b in room.Bosses.GetAllBosses()) if (b.Active) SendPacket(peer, new BossSpawn { BossId = b.Id, Position = b.Position, MaxHealth = b.MaxHealth }, DeliveryMethod.ReliableOrdered);
        foreach (var p in room.Portals.Values) SendPacket(peer, p, DeliveryMethod.ReliableOrdered);
    }

    public void Update(float dt) {
        var ids = _rooms.Keys.ToList();
        foreach (var id in ids) {
            var r = _rooms[id];
            r.Update(dt);
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
                foreach(var targetId in players.Keys) if(_peers.TryGetValue(targetId, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
            foreach (var e in r.Enemies.GetActiveEnemies()) {
                var w = new NetDataWriter(); _packetProcessor.Write(w, new EnemyUpdate { EnemyId = e.Id, Position = e.Position, CurrentHealth = e.CurrentHealth });
                foreach(var targetId in players.Keys) if(_peers.TryGetValue(targetId, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
            foreach (var s in r.Spawners.GetActiveSpawners()) {
                var w = new NetDataWriter(); _packetProcessor.Write(w, new SpawnerUpdate { SpawnerId = s.Id, CurrentHealth = s.CurrentHealth });
                foreach(var targetId in players.Keys) if(_peers.TryGetValue(targetId, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
            foreach (var b in r.Bosses.GetActiveBosses()) {
                var w = new NetDataWriter(); _packetProcessor.Write(w, new BossUpdate { BossId = b.Id, Position = b.Position, CurrentHealth = b.CurrentHealth, Phase = b.Phase });
                foreach(var targetId in players.Keys) if(_peers.TryGetValue(targetId, out var peer)) peer.Send(w, DeliveryMethod.Unreliable);
            }
        }
    }

    public void Start() => _netManager.Start(_port);
    public void PollEvents() => _netManager.PollEvents();
    public void Stop() => _netManager.Stop();
    public void OnPeerConnected(NetPeer p) => Console.WriteLine($"Connected: {p}");
    public void OnPeerDisconnected(NetPeer p, DisconnectInfo info) { _playerStates.Remove(p.Id); _peers.Remove(p.Id); }
    public void OnNetworkError(IPEndPoint ep, SocketError err) { }
    public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte ch, DeliveryMethod dm) => _packetProcessor.ReadAllPackets(r, p);
    public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader r, UnconnectedMessageType t) { }
    public void OnNetworkLatencyUpdate(NetPeer p, int l) { }
    public void OnConnectionRequest(ConnectionRequest r) => r.AcceptIfKey("LastLightKey");
}
