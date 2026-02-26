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

        _packetProcessor.SubscribeReusable<JoinResponse>((response) =>
        {
            Console.WriteLine($"[Client] Received Join Response: Success={response.Success}, Id={response.PlayerId}, Message={response.Message}");
            OnJoinResponse?.Invoke(response);
        });

        _packetProcessor.SubscribeReusable<AuthoritativePlayerUpdate>((update) =>
        {
            OnPlayerUpdate?.Invoke(update);
        });

        _packetProcessor.SubscribeReusable<SpawnBullet>((spawn) =>
        {
            OnSpawnBullet?.Invoke(spawn);
        });

        _packetProcessor.SubscribeReusable<BulletHit>((hit) =>
        {
            OnBulletHit?.Invoke(hit);
        });
    }

    public void Connect(string host, int port)
    {
        _netManager.Start();
        _peer = _netManager.Connect(host, port, "LastLightKey");
        Console.WriteLine($"[Client] Connecting to {host}:{port}...");
    }

    public void PollEvents()
    {
        _netManager.PollEvents();
    }

    public void Disconnect()
    {
        _netManager.Stop();
    }

    public void SendJoinRequest(string name)
    {
        if (_peer != null)
        {
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, new JoinRequest { PlayerName = name });
            _peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }

    // INetEventListener implementation
    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine($"[Client] Peer connected to server: {peer}");
        _peer = peer;
        SendJoinRequest("PlayerOne");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"[Client] Peer disconnected: {peer}. Reason: {disconnectInfo.Reason}");
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Console.WriteLine($"[Client] Network error for {endPoint}: {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        _packetProcessor.ReadAllPackets(reader, peer);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    public void OnConnectionRequest(ConnectionRequest request) { }

    public void SendInputRequest(LastLight.Common.InputRequest request)
    {
        if (_peer != null)
        {
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, request);
            _peer.Send(writer, DeliveryMethod.Unreliable); // Inputs are frequent, Unreliable is fine
        }
    }

    public void SendFireRequest(LastLight.Common.FireRequest request)
    {
        if (_peer != null)
        {
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, request);
            _peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}
