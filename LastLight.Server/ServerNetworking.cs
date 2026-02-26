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
    private float _broadcastTimer = 0f;
    private float _broadcastInterval = 0.05f;
    private float _moveSpeed = 200f; // Must match client speed

    private int _serverBulletCounter = -1; // Negative IDs for server spawned bullets

    public ServerNetworking(int port)
    {
        _port = port;
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this);
        
        RegisterPackets();

        _enemyManager.OnEnemySpawned += (enemy) => 
        {
            var spawn = new EnemySpawn { EnemyId = enemy.Id, Position = enemy.Position, MaxHealth = enemy.MaxHealth };
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, spawn);
            _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        };

        _enemyManager.OnEnemyDied += (enemy) =>
        {
            var death = new EnemyDeath { EnemyId = enemy.Id };
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, death);
            _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        };

        _enemyManager.OnEnemyShoot += (enemy, pos, vel) =>
        {
            int bulletId = _serverBulletCounter--;
            var spawn = new SpawnBullet
            {
                OwnerId = enemy.Id,
                BulletId = bulletId,
                Position = pos,
                Velocity = vel
            };
            
            _bulletManager.Spawn(bulletId, enemy.Id, pos, vel);
            
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, spawn);
            _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        };

        _spawnerManager.OnSpawnerCreated += (spawner) =>
        {
            var spawn = new SpawnerSpawn { SpawnerId = spawner.Id, Position = spawner.Position, MaxHealth = spawner.MaxHealth };
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, spawn);
            _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        };

        _spawnerManager.OnSpawnerDied += (spawner) =>
        {
            var death = new SpawnerDeath { SpawnerId = spawner.Id };
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, death);
            _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        };

        _spawnerManager.OnRequestEnemySpawn += (pos) =>
        {
            _enemyManager.SpawnEnemy(pos);
        };

        // Spawn a test spawner
        _spawnerManager.CreateSpawner(new LastLight.Common.Vector2(400, 100), 200, 10);
    }

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
                Message = "Welcome to LastLight!",
                MaxHealth = 100
            };
            
            // Initialize player state
            _playerStates[peer.Id] = new AuthoritativePlayerUpdate 
            { 
                PlayerId = peer.Id, 
                Position = new LastLight.Common.Vector2(400, 300),
                CurrentHealth = 100
            };

            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, response);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            // Send existing enemies to the new player
            foreach (var enemy in _enemyManager.GetAllEnemies())
            {
                if (enemy.Active)
                {
                    var enemySpawn = new EnemySpawn { EnemyId = enemy.Id, Position = enemy.Position, MaxHealth = enemy.MaxHealth };
                    var spawnWriter = new NetDataWriter();
                    _packetProcessor.Write(spawnWriter, enemySpawn);
                    peer.Send(spawnWriter, DeliveryMethod.ReliableOrdered);
                }
            }

            // Send existing spawners
            foreach (var spawner in _spawnerManager.GetAllSpawners())
            {
                if (spawner.Active)
                {
                    var spawnerSpawn = new SpawnerSpawn { SpawnerId = spawner.Id, Position = spawner.Position, MaxHealth = spawner.MaxHealth };
                    var spawnWriter = new NetDataWriter();
                    _packetProcessor.Write(spawnWriter, spawnerSpawn);
                    peer.Send(spawnWriter, DeliveryMethod.ReliableOrdered);
                }
            }
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
        _spawnerManager.Update(dt);
        _enemyManager.Update(dt, _playerStates);
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
            bool hitSomething = false;

            // Check players
            foreach (var playerState in _playerStates.Values)
            {
                if (bullet.OwnerId == playerState.PlayerId) continue; // Don't shoot yourself
                if (bullet.OwnerId > 0) continue; // Players don't shoot players (Co-op)

                // AABB collision (Player is 32x32, Bullet is 8x8)
                float dx = Math.Abs(bullet.Position.X - playerState.Position.X);
                float dy = Math.Abs(bullet.Position.Y - playerState.Position.Y);

                if (dx < 20 && dy < 20) // 16 (player half-width) + 4 (bullet half-width)
                {
                    Console.WriteLine($"[Server] Player {playerState.PlayerId} hit by Bullet {bullet.BulletId} from Enemy {bullet.OwnerId}");
                    
                    playerState.CurrentHealth -= 10; // Hardcode player damage
                    if (playerState.CurrentHealth <= 0)
                    {
                        Console.WriteLine($"[Server] Player {playerState.PlayerId} died! Respawning.");
                        playerState.CurrentHealth = 100;
                        playerState.Position = new LastLight.Common.Vector2(400, 300); // Respawn
                        // We might want to send a specific Respawn packet or just let Authoritative update snap them
                    }

                    var hit = new BulletHit
                    {
                        BulletId = bullet.BulletId,
                        TargetId = playerState.PlayerId,
                        TargetType = EntityType.Player
                    };
                    var writer = new NetDataWriter();
                    _packetProcessor.Write(writer, hit);
                    _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);

                    _bulletManager.DestroyBullet(bullet);
                    hitSomething = true;
                    break; // Bullet destroyed, stop checking other players
                }
            }

            if (hitSomething) continue;

            // Check spawners
            foreach (var spawner in _spawnerManager.GetActiveSpawners())
            {
                if (bullet.OwnerId < 0) continue; // Enemies/Spawners don't shoot spawners

                // AABB collision (Spawner is 64x64, Bullet is 8x8)
                float dx = Math.Abs(bullet.Position.X - spawner.Position.X);
                float dy = Math.Abs(bullet.Position.Y - spawner.Position.Y);

                if (dx < 36 && dy < 36) // 32 (spawner half-width) + 4 (bullet half-width)
                {
                    Console.WriteLine($"[Server] Spawner {spawner.Id} hit by Bullet {bullet.BulletId} from Player {bullet.OwnerId}");
                    
                    _spawnerManager.HandleDamage(spawner.Id, 25); // Hardcode 25 damage

                    var hit = new BulletHit
                    {
                        BulletId = bullet.BulletId,
                        TargetId = spawner.Id,
                        TargetType = EntityType.Spawner
                    };
                    var writer = new NetDataWriter();
                    _packetProcessor.Write(writer, hit);
                    _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);

                    _bulletManager.DestroyBullet(bullet);
                    hitSomething = true;
                    break; // Bullet destroyed
                }
            }

            if (hitSomething) continue;

            // Check enemies
            foreach (var enemy in _enemyManager.GetActiveEnemies())
            {
                if (bullet.OwnerId < 0) continue; // Enemies don't shoot enemies

                // AABB collision (Enemy is 32x32, Bullet is 8x8)
                float dx = Math.Abs(bullet.Position.X - enemy.Position.X);
                float dy = Math.Abs(bullet.Position.Y - enemy.Position.Y);

                if (dx < 20 && dy < 20) // 16 (enemy half-width) + 4 (bullet half-width)
                {
                    Console.WriteLine($"[Server] Enemy {enemy.Id} hit by Bullet {bullet.BulletId} from Player {bullet.OwnerId}");
                    
                    _enemyManager.HandleDamage(enemy.Id, 25); // Hardcode 25 damage for now

                    var hit = new BulletHit
                    {
                        BulletId = bullet.BulletId,
                        TargetId = enemy.Id,
                        TargetType = EntityType.Enemy
                    };
                    var writer = new NetDataWriter();
                    _packetProcessor.Write(writer, hit);
                    _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);

                    _bulletManager.DestroyBullet(bullet);
                    break; // Bullet destroyed
                }
            }
        }
    }

    private void BroadcastUpdates()
    {
        if (_playerStates.Count > 0) 
        {
            foreach (var player in _playerStates.Values)
            {
                var writer = new NetDataWriter();
                _packetProcessor.Write(writer, player);
                _netManager.SendToAll(writer, DeliveryMethod.Unreliable);
            }
        }

        foreach (var enemy in _enemyManager.GetActiveEnemies())
        {
            var update = new EnemyUpdate
            {
                EnemyId = enemy.Id,
                Position = enemy.Position,
                CurrentHealth = enemy.CurrentHealth
            };
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, update);
            _netManager.SendToAll(writer, DeliveryMethod.Unreliable);
        }

        foreach (var spawner in _spawnerManager.GetActiveSpawners())
        {
            var update = new SpawnerUpdate
            {
                SpawnerId = spawner.Id,
                CurrentHealth = spawner.CurrentHealth
            };
            var writer = new NetDataWriter();
            _packetProcessor.Write(writer, update);
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
