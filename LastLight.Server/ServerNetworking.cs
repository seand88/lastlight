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

    public ServerNetworking(int port)
    {
        _port = port;
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this);
        
        RegisterPackets();
    }

    private readonly Dictionary<int, AuthoritativePlayerUpdate> _playerStates = new();
    private readonly ServerBulletManager _bulletManager = new();
    private float _broadcastTimer = 0f;
    private float _broadcastInterval = 0.05f;
    private float _moveSpeed = 200f; // Must match client speed

    private void RegisterPackets()
    {
        _packetProcessor.RegisterNestedType((w, v) => 
        {
            w.Put(v.X);
            w.Put(v.Y);
        }, r => 
        {
            return new LastLight.Common.Vector2(r.GetFloat(), r.GetFloat());
        });

        _packetProcessor.SubscribeReusable<JoinRequest, NetPeer>((request, peer) =>
        {
            Console.WriteLine($"[Server] Join request from {request.PlayerName} (Peer: {peer})");
            var response = new JoinResponse
            {
                Success = true,
                PlayerId = peer.Id,
                Message = "Welcome to LastLight!"
            };
            
            // Initialize player state
            _playerStates[peer.Id] = new AuthoritativePlayerUpdate 
            { 
                PlayerId = peer.Id, 
                Position = new LastLight.Common.Vector2(400, 300) 
            };

            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, response);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        });

        _packetProcessor.SubscribeReusable<InputRequest, NetPeer>((request, peer) =>
        {
            if (_playerStates.TryGetValue(peer.Id, out var state))
            {
                // Basic validation: ensure DeltaTime isn't absurdly high to prevent speed hacks
                float dt = Math.Min(request.DeltaTime, 0.1f); 
                
                // Calculate new position (Server simulation)
                state.Velocity = new LastLight.Common.Vector2(request.Movement.X * _moveSpeed, request.Movement.Y * _moveSpeed);
                var newPos = state.Position;
                newPos.X += state.Velocity.X * dt;
                newPos.Y += state.Velocity.Y * dt;
                state.Position = newPos;
                
                state.LastProcessedInputSequence = request.InputSequenceNumber;
            }
        });

        _packetProcessor.SubscribeReusable<FireRequest, NetPeer>((request, peer) =>
        {
            if (_playerStates.TryGetValue(peer.Id, out var state))
            {
                var vel = new LastLight.Common.Vector2(request.Direction.X * 500f, request.Direction.Y * 500f);
                var spawn = new SpawnBullet
                {
                    OwnerId = peer.Id,
                    BulletId = request.BulletId,
                    Position = state.Position,
                    Velocity = vel
                };
                
                _bulletManager.Spawn(request.BulletId, peer.Id, state.Position, vel);
                
                var writer = new NetDataWriter();
                _packetProcessor.Write(writer, spawn);
                _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered); // Send to all, including shooter
            }
        });
    }

    public void Update(float dt)
    {
        _bulletManager.Update(dt);
        CheckCollisions();

        _broadcastTimer += dt;
        if (_broadcastTimer >= _broadcastInterval)
        {
            _broadcastTimer -= _broadcastInterval;
            BroadcastUpdates();
        }
    }

    private void CheckCollisions()
    {
        var bullets = _bulletManager.GetActiveBullets();
        foreach (var bullet in bullets)
        {
            foreach (var playerState in _playerStates.Values)
            {
                if (bullet.OwnerId == playerState.PlayerId) continue; // Don't shoot yourself

                // Simple circle collision (radius ~16 for players)
                float dx = bullet.Position.X - playerState.Position.X;
                float dy = bullet.Position.Y - playerState.Position.Y;
                float distanceSquared = dx * dx + dy * dy;

                if (distanceSquared < 16 * 16)
                {
                    Console.WriteLine($"[Server] Player {playerState.PlayerId} hit by Bullet {bullet.BulletId} from Player {bullet.OwnerId}");
                    
                    var hit = new BulletHit
                    {
                        BulletId = bullet.BulletId,
                        TargetId = playerState.PlayerId
                    };
                    var writer = new NetDataWriter();
                    _packetProcessor.Write(writer, hit);
                    _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);

                    _bulletManager.DestroyBullet(bullet);
                    break; // Bullet destroyed, stop checking other players
                }
            }
        }
    }

    private void BroadcastUpdates()
    {
        if (_playerStates.Count == 0) return;

        foreach (var player in _playerStates.Values)
        {
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, player);
            _netManager.SendToAll(writer, DeliveryMethod.Unreliable);
        }
    }

    public void Start()
    {
        _netManager.Start(_port);
        Console.WriteLine($"[Server] Started on port {_port}");
    }

    public void PollEvents()
    {
        _netManager.PollEvents();
    }

    public void Stop()
    {
        _netManager.Stop();
    }

    // INetEventListener implementation
    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine($"[Server] Peer connected: {peer}");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"[Server] Peer disconnected: {peer}. Reason: {disconnectInfo.Reason}");
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Console.WriteLine($"[Server] Network error for {endPoint}: {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        _packetProcessor.ReadAllPackets(reader, peer);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey("LastLightKey");
    }
}
