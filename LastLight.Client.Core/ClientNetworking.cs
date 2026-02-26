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

    private void RegisterPackets()
    {
        _packetProcessor.RegisterNestedType((w, v) => { w.Put(v.X); w.Put(v.Y); }, r => new LastLight.Common.Vector2(r.GetFloat(), r.GetFloat()));
        _packetProcessor.SubscribeReusable<JoinResponse>((r) => OnJoinResponse?.Invoke(r));
        _packetProcessor.SubscribeReusable<WorldInit>((r) => OnWorldInit?.Invoke(r));
        _packetProcessor.SubscribeReusable<AuthoritativePlayerUpdate>((r) => OnPlayerUpdate?.Invoke(r));
        _packetProcessor.SubscribeReusable<SpawnBullet>((r) => OnSpawnBullet?.Invoke(r));
        _packetProcessor.SubscribeReusable<BulletHit>((r) => OnBulletHit?.Invoke(r));
        _packetProcessor.SubscribeReusable<EnemySpawn>((r) => OnEnemySpawn?.Invoke(r));
        _packetProcessor.SubscribeReusable<EnemyUpdate>((r) => OnEnemyUpdate?.Invoke(r));
        _packetProcessor.SubscribeReusable<EnemyDeath>((r) => OnEnemyDeath?.Invoke(r));
        _packetProcessor.SubscribeReusable<SpawnerSpawn>((r) => OnSpawnerSpawn?.Invoke(r));
        _packetProcessor.SubscribeReusable<SpawnerUpdate>((r) => OnSpawnerUpdate?.Invoke(r));
        _packetProcessor.SubscribeReusable<SpawnerDeath>((r) => OnSpawnerDeath?.Invoke(r));
        _packetProcessor.SubscribeReusable<BossSpawn>((r) => OnBossSpawn?.Invoke(r));
        _packetProcessor.SubscribeReusable<BossUpdate>((r) => OnBossUpdate?.Invoke(r));
        _packetProcessor.SubscribeReusable<BossDeath>((r) => OnBossDeath?.Invoke(r));
        _packetProcessor.SubscribeReusable<ItemSpawn>((r) => OnItemSpawn?.Invoke(r));
        _packetProcessor.SubscribeReusable<ItemPickup>((r) => OnItemPickup?.Invoke(r));
        _packetProcessor.SubscribeReusable<PortalSpawn>((r) => OnPortalSpawn?.Invoke(r));
        _packetProcessor.SubscribeReusable<PortalDeath>((r) => OnPortalDeath?.Invoke(r));
    }

    public void Connect(string host, int port) { _netManager.Start(); _peer = _netManager.Connect(host, port, "LastLightKey"); }
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
    public void OnPeerConnected(NetPeer peer) { _peer = peer; SendJoinRequest("PlayerOne"); }
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) { }
    public void OnNetworkError(IPEndPoint ep, SocketError err) { }
    public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte ch, DeliveryMethod dm) => _packetProcessor.ReadAllPackets(r, p);
    public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader r, UnconnectedMessageType t) { }
    public void OnNetworkLatencyUpdate(NetPeer p, int l) { }
    public void OnConnectionRequest(ConnectionRequest r) { }
    public void SendInputRequest(InputRequest r) => SendPacket(r, DeliveryMethod.Unreliable);
    public void SendFireRequest(FireRequest r) => SendPacket(r, DeliveryMethod.ReliableOrdered);
}
