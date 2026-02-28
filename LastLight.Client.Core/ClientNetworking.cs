using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using LastLight.Common;

namespace LastLight.Client.Core;

public class ClientNetworking : INetEventListener
{
    private readonly NetManager _netManager;
    private readonly NetPacketProcessor _packetProcessor;
    private NetPeer? _peer;
    private string _playerName = "PlayerOne";

    public ClientNetworking()
    {
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this);
        RegisterPackets();
    }

    public Action<AuthoritativePlayerUpdate>? OnPlayerUpdate;
    public Action<JoinResponse>? OnJoinResponse;
    public Action<SpawnBullet>? OnSpawnBullet;
    public Action<BulletHit>? OnBulletHit;
    public Action<EnemySpawn>? OnEnemySpawn;
    public Action<EnemyUpdate>? OnEnemyUpdate;
    public Action<EnemyDeath>? OnEnemyDeath;
    public Action<SpawnerSpawn>? OnSpawnerSpawn;
    public Action<SpawnerUpdate>? OnSpawnerUpdate;
    public Action<SpawnerDeath>? OnSpawnerDeath;
    public Action<BossSpawn>? OnBossSpawn;
    public Action<BossUpdate>? OnBossUpdate;
    public Action<BossDeath>? OnBossDeath;
    public Action<WorldInit>? OnWorldInit;
    public Action<ItemSpawn>? OnItemSpawn;
    public Action<ItemPickup>? OnItemPickup;
    public Action<PortalSpawn>? OnPortalSpawn;
    public Action<PortalDeath>? OnPortalDeath;
    public Action<LeaderboardUpdate>? OnLeaderboardUpdate;

    private void RegisterPackets()
    {
        _packetProcessor.RegisterNestedType((w, v) => { w.Put(v.X); w.Put(v.Y); }, r => new LastLight.Common.Vector2(r.GetFloat(), r.GetFloat()));
        _packetProcessor.RegisterNestedType<LeaderboardEntry>();
        
        _packetProcessor.SubscribeReusable<JoinResponse>((r) => OnJoinResponse?.Invoke(r));
        _packetProcessor.SubscribeReusable<WorldInit>((r) => OnWorldInit?.Invoke(r));
        _packetProcessor.SubscribeReusable<AuthoritativePlayerUpdate>((r) => OnPlayerUpdate?.Invoke(r));
        _packetProcessor.SubscribeReusable<SpawnBullet>((r) => OnSpawnBullet?.Invoke(r));
        _packetProcessor.SubscribeReusable<BulletHit>((r) => OnBulletHit?.Invoke(r));
        
        // Use manual field-copy lambda to avoid object reuse bug while keeping LiteNetLib happy
        _packetProcessor.SubscribeReusable<EnemySpawn>((r) => OnEnemySpawn?.Invoke(new EnemySpawn { EnemyId = r.EnemyId, Position = r.Position, MaxHealth = r.MaxHealth }));
        _packetProcessor.SubscribeReusable<EnemyUpdate>((r) => OnEnemyUpdate?.Invoke(r));
        _packetProcessor.SubscribeReusable<EnemyDeath>((r) => OnEnemyDeath?.Invoke(r));
        
        _packetProcessor.SubscribeReusable<SpawnerSpawn>((r) => OnSpawnerSpawn?.Invoke(new SpawnerSpawn { SpawnerId = r.SpawnerId, Position = r.Position, MaxHealth = r.MaxHealth }));
        _packetProcessor.SubscribeReusable<SpawnerUpdate>((r) => OnSpawnerUpdate?.Invoke(r));
        _packetProcessor.SubscribeReusable<SpawnerDeath>((r) => OnSpawnerDeath?.Invoke(r));
        
        _packetProcessor.SubscribeReusable<BossSpawn>((r) => OnBossSpawn?.Invoke(new BossSpawn { BossId = r.BossId, Position = r.Position, MaxHealth = r.MaxHealth }));
        _packetProcessor.SubscribeReusable<BossUpdate>((r) => OnBossUpdate?.Invoke(r));
        _packetProcessor.SubscribeReusable<BossDeath>((r) => OnBossDeath?.Invoke(r));
        
        _packetProcessor.SubscribeReusable<ItemSpawn>((r) => OnItemSpawn?.Invoke(new ItemSpawn { ItemId = r.ItemId, Position = r.Position, Type = r.Type }));
        _packetProcessor.SubscribeReusable<ItemPickup>((r) => OnItemPickup?.Invoke(r));
        
        _packetProcessor.SubscribeReusable<PortalSpawn>((r) => OnPortalSpawn?.Invoke(new PortalSpawn { PortalId = r.PortalId, Position = r.Position, TargetRoomId = r.TargetRoomId, Name = r.Name }));
        _packetProcessor.SubscribeReusable<PortalDeath>((r) => OnPortalDeath?.Invoke(r));
        
        _packetProcessor.SubscribeReusable<LeaderboardUpdate>((r) => OnLeaderboardUpdate?.Invoke(new LeaderboardUpdate { Entries = r.Entries }));
    }

    public void Connect(string host, int port, string playerName) { _playerName = playerName; _netManager.Start(); _peer = _netManager.Connect(host, port, "LastLightKey"); }
    public void PollEvents() => _netManager.PollEvents();
    public void Disconnect() => _netManager.Stop();
    
    public void SendPacket<T>(T packet, DeliveryMethod dm) where T : class, new()
    {
        if (_peer != null)
        {
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, packet);
            _peer.Send(writer, dm);
        }
    }

    public void SendJoinRequest(string name) => SendPacket(new JoinRequest { PlayerName = name }, DeliveryMethod.ReliableOrdered);
    public void OnPeerConnected(NetPeer peer) { _peer = peer; SendJoinRequest(_playerName); }
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) { }
    public void OnNetworkError(IPEndPoint ep, SocketError err) { }
    public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte ch, DeliveryMethod dm) => _packetProcessor.ReadAllPackets(r, p);
    public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader r, UnconnectedMessageType t) { }
    public void OnNetworkLatencyUpdate(NetPeer p, int l) { }
    public void OnConnectionRequest(ConnectionRequest r) => r.AcceptIfKey("LastLightKey");
    public void SendInputRequest(InputRequest r) => SendPacket(r, DeliveryMethod.Unreliable);
    public void SendFireRequest(FireRequest r) => SendPacket(r, DeliveryMethod.ReliableOrdered);
}