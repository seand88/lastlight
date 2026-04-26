using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using LastLight.Common;

public partial class Networking : Node, INetEventListener
{
    private NetManager _netManager = null!;
    private NetPacketProcessor _packetProcessor = null!;
    private NetPeer? _peer;
    private string _username = "GodotPlayer";

    [Signal] public delegate void JoinResponseReceivedEventHandler(bool success, int playerId, string message);
    [Signal] public delegate void WorldInitReceivedEventHandler(int seed, int width, int height, int tileSize, int style, float cleanupTimer);
    [Signal] public delegate void PlayerUpdateReceivedEventHandler(int playerId, Godot.Vector2 position, Godot.Vector2 velocity, int currentHealth, int maxHealth, int level);
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
    [Signal] public delegate void LeaderboardUpdatedEventHandler();
    [Signal] public delegate void RoomStateUpdatedEventHandler(float cleanupTimer);
    [Signal] public delegate void DisconnectedEventHandler(string reason);

    public override void _Ready()
    {
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this);
        RegisterPackets();
    }

    public override void _Process(double delta)
    {
        _netManager.PollEvents();
    }

    private void RegisterPackets()
    {
        _packetProcessor.RegisterNestedType((w, v) => { w.Put(v.X); w.Put(v.Y); }, r => new LastLight.Common.Vector2(r.GetFloat(), r.GetFloat()));
        _packetProcessor.RegisterNestedType<LeaderboardEntry>();
        _packetProcessor.RegisterNestedType<ItemInfo>();

        _packetProcessor.SubscribeReusable<JoinResponse>((r) => EmitSignal(SignalName.JoinResponseReceived, r.Success, r.PlayerId, r.Message));
        _packetProcessor.SubscribeReusable<WorldInit>((r) => EmitSignal(SignalName.WorldInitReceived, r.Seed, r.Width, r.Height, r.TileSize, (int)r.Style, r.CleanupTimer));
        _packetProcessor.SubscribeReusable<AuthoritativePlayerUpdate>((r) => EmitSignal(SignalName.PlayerUpdateReceived, r.PlayerId, new Godot.Vector2(r.Position.X, r.Position.Y), new Godot.Vector2(r.Velocity.X, r.Velocity.Y), r.CurrentHealth, r.MaxHealth, r.Level));
        
        _packetProcessor.SubscribeReusable<SpawnBullet>((r) => EmitSignal(SignalName.BulletSpawned, r.OwnerId, r.BulletId, new Godot.Vector2(r.Position.X, r.Position.Y), new Godot.Vector2(r.Velocity.X, r.Velocity.Y)));
        _packetProcessor.SubscribeReusable<BulletHit>((r) => EmitSignal(SignalName.BulletHit, r.BulletId, r.TargetId, (int)r.TargetType));
        
        _packetProcessor.SubscribeReusable<EnemySpawn>((r) => EmitSignal(SignalName.EnemySpawned, r.EnemyId, new Godot.Vector2(r.Position.X, r.Position.Y), r.MaxHealth, r.DataId));
        _packetProcessor.SubscribeReusable<EnemyUpdate>((r) => EmitSignal(SignalName.EnemyUpdated, r.EnemyId, new Godot.Vector2(r.Position.X, r.Position.Y), r.CurrentHealth));
        _packetProcessor.SubscribeReusable<EnemyDeath>((r) => EmitSignal(SignalName.EnemyDied, r.EnemyId));

        _packetProcessor.SubscribeReusable<SpawnerSpawn>((r) => EmitSignal(SignalName.SpawnerSpawned, r.SpawnerId, new Godot.Vector2(r.Position.X, r.Position.Y), r.MaxHealth));
        _packetProcessor.SubscribeReusable<SpawnerUpdate>((r) => EmitSignal(SignalName.SpawnerUpdated, r.SpawnerId, r.CurrentHealth));
        _packetProcessor.SubscribeReusable<SpawnerDeath>((r) => EmitSignal(SignalName.SpawnerDied, r.SpawnerId));

        _packetProcessor.SubscribeReusable<PortalSpawn>((r) => EmitSignal(SignalName.PortalSpawned, r.PortalId, new Godot.Vector2(r.Position.X, r.Position.Y), r.TargetRoomId, r.Name));
        _packetProcessor.SubscribeReusable<PortalDeath>((r) => EmitSignal(SignalName.PortalDied, r.PortalId));

        _packetProcessor.SubscribeReusable<BossSpawn>((r) => EmitSignal(SignalName.BossSpawned, r.BossId, new Godot.Vector2(r.Position.X, r.Position.Y), r.MaxHealth, r.DataId));
        _packetProcessor.SubscribeReusable<BossUpdate>((r) => EmitSignal(SignalName.BossUpdated, r.BossId, new Godot.Vector2(r.Position.X, r.Position.Y), r.CurrentHealth, (int)r.Phase));
        _packetProcessor.SubscribeReusable<BossDeath>((r) => EmitSignal(SignalName.BossDied, r.BossId));

        _packetProcessor.SubscribeReusable<ItemSpawn>((r) => EmitSignal(SignalName.ItemSpawned, r.ItemId, new Godot.Vector2(r.Position.X, r.Position.Y), r.Item.Name));
        _packetProcessor.SubscribeReusable<ItemPickup>((r) => EmitSignal(SignalName.ItemPickedUp, r.ItemId, r.PlayerId));

        _packetProcessor.SubscribeReusable<LeaderboardUpdate>((r) => EmitSignal(SignalName.LeaderboardUpdated));
        _packetProcessor.SubscribeReusable<RoomStateUpdate>((r) => EmitSignal(SignalName.RoomStateUpdated, r.CleanupTimer));
    }

    public void Connect(string host, int port, string username)
    {
        _username = username;
        if (!_netManager.IsRunning) {
            _netManager.Start();
        }
        _peer = _netManager.Connect(host, port, "LastLightKey");
        GD.Print($"Connecting to {host}:{port} as {username} (Manager running: {_netManager.IsRunning})...");
    }

    public void Disconnect()
    {
        _netManager.Stop();
        _peer = null;
    }

    public void SendPacket<T>(T packet, DeliveryMethod dm) where T : class, new()
    {
        if (_peer != null)
        {
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            _peer.Send(writer, dm);
        }
    }

    // INetEventListener implementation
    public void OnPeerConnected(NetPeer peer) 
    { 
        _peer = peer; 
        GD.Print($"Connected to server! Peer ID: {peer.Id}");
        SendPacket(new JoinRequest { Username = _username }, DeliveryMethod.ReliableOrdered);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) 
    { 
        EmitSignal(SignalName.Disconnected, info.Reason.ToString());
        _peer = null;
    }

    public void OnNetworkError(IPEndPoint ep, SocketError err) 
    { 
        EmitSignal(SignalName.Disconnected, err.ToString());
    }

    public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte ch, DeliveryMethod dm) 
    { 
        _packetProcessor.ReadAllPackets(r, p); 
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader r, UnconnectedMessageType t) { }
    public void OnNetworkLatencyUpdate(NetPeer p, int l) { }
    public void OnConnectionRequest(ConnectionRequest r) => r.AcceptIfKey("LastLightKey");
}
