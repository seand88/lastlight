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

    private readonly Dictionary<int, PlayerUpdate> _playerStates = new();
    private float _broadcastTimer = 0f;
    private float _broadcastInterval = 0.05f;

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
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, response);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        });

        _packetProcessor.SubscribeReusable<PlayerUpdate, NetPeer>((update, peer) =>
        {
            // Simple validation and update
            update.PlayerId = peer.Id;
            _playerStates[peer.Id] = update;
        });
    }

    public void Update(float dt)
    {
        _broadcastTimer += dt;
        if (_broadcastTimer >= _broadcastInterval)
        {
            _broadcastTimer -= _broadcastInterval;
            BroadcastUpdates();
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
