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

    private readonly Dictionary<int, ServerPlayer> _playerStates = new();
    private readonly Dictionary<int, NetPeer> _peers = new();
    private readonly Dictionary<int, string> _usernames = new();
    private readonly Dictionary<int, ServerRoom> _rooms = new();
    private readonly ServerAbilityManager _abilityManager = new();
    
    private float _broadcastTimer = 0f;
    private float _broadcastInterval = 0.05f;
    private float _moveSpeed = 200f;

    public ServerNetworking(int port)
    {
        _port = port;
        _packetProcessor = new NetPacketProcessor();
        _netManager = new NetManager(this);
        RegisterPackets();

        var nexusData = GameDataManager.Rooms.TryGetValue("room_nexus", out var nd) ? nd : new RoomData { Id = "room_nexus", Name = "Nexus Social Hub", Width = 30, Height = 30, Style = WorldManager.GenerationStyle.Nexus };
        var nexus = new ServerRoom(0, nexusData, 12345, _packetProcessor, this, _abilityManager, _playerStates);
        _rooms[0] = nexus;

        nexus.SpawnPortal(new Vector2(350, 480), -1, "Forest Realm", -3000);
        nexus.SpawnPortal(new Vector2(610, 480), -2, "Dungeon Realm", -3001);

        _abilityManager.OnBulletSpawned = (ownerId, bulletId, pos, vel, lifeTime, abilityId) => {
            ServerRoom? room = null;
            if (ownerId >= 0) {
                if (_playerStates.TryGetValue(ownerId, out var player)) _rooms.TryGetValue(player.RoomId, out room);
            } else {
                // If it's an AI, we need to find which room they are in.
                // We can iterate rooms to find the owner, but it's more efficient to have the Room handle the event.
                // For now, let's find the room that contains this entity ID.
                room = _rooms.Values.FirstOrDefault(r => 
                    r.Enemies.GetAllEnemies().Any(b => b.Id == ownerId));
            }

            if (room != null) {
                room.Broadcast(new SpawnBullet { OwnerId = ownerId, BulletId = bulletId, AbilityId = abilityId, Position = pos, Velocity = vel });
            }
        };
    }

    public NetPeer? GetPeer(int id) => _peers.TryGetValue(id, out var p) ? p : null;

    public void SendPacket<T>(NetPeer peer, T packet, DeliveryMethod dm) where T : class, new() {
        var w = new NetDataWriter(); _packetProcessor.Write(w, packet); peer.Send(w, dm);
    }

    private void RegisterPackets()
    {
        _packetProcessor.RegisterNestedType((w, v) => { w.Put(v.X); w.Put(v.Y); }, r => new LastLight.Common.Vector2(r.GetFloat(), r.GetFloat()));
        _packetProcessor.RegisterNestedType<LeaderboardEntry>();
        _packetProcessor.RegisterNestedType<ItemInfo>();

        _packetProcessor.SubscribeReusable<JoinRequest, NetPeer>((req, peer) => {
            _peers[peer.Id] = peer;
            _usernames[peer.Id] = string.IsNullOrWhiteSpace(req.Username) ? "Guest" : req.Username;
            
            string username = req.Username;
            if (string.IsNullOrWhiteSpace(username)) {
                username = "Guest";
            }
            _usernames[peer.Id] = username;

            var dbPlayer = DatabaseManager.LoadPlayer(username);
            if (dbPlayer == null) {
                dbPlayer = new PlayerSaveData();
                var starterWeapon = new ItemInfo { ItemId = 1, DataId = "weapon_basic_staff" };
                dbPlayer.Equipment[0] = starterWeapon;
            }
            
            var res = new JoinResponse { Success = true, PlayerId = peer.Id, MaxHealth = dbPlayer.MaxHealth, Level = dbPlayer.Level, Experience = dbPlayer.Experience, Attack = dbPlayer.Attack, Defense = dbPlayer.Defense, Speed = dbPlayer.Speed, Dexterity = dbPlayer.Dexterity, Vitality = dbPlayer.Vitality, Wisdom = dbPlayer.Wisdom, Equipment = dbPlayer.Equipment, Inventory = dbPlayer.Inventory };
            _playerStates[peer.Id] = new ServerPlayer { Id = peer.Id, Position = new Vector2(480, 480), CurrentHealth = dbPlayer.MaxHealth, MaxHealth = dbPlayer.MaxHealth, Level = dbPlayer.Level, Experience = dbPlayer.Experience, RoomId = 0, Attack = dbPlayer.Attack, Defense = dbPlayer.Defense, Speed = dbPlayer.Speed, Dexterity = dbPlayer.Dexterity, Vitality = dbPlayer.Vitality, Wisdom = dbPlayer.Wisdom, Equipment = dbPlayer.Equipment, Inventory = dbPlayer.Inventory };
            SendPacket(peer, res, DeliveryMethod.ReliableOrdered);
            SwitchPlayerRoom(peer, 0);
        });

        _packetProcessor.SubscribeReusable<InputRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var player) && _rooms.TryGetValue(player.RoomId, out var room)) {
                float dt = Math.Min(req.DeltaTime, 0.1f);
                float speed = 100f + (player.Speed * 5f);
                player.Velocity = new Vector2(req.Movement.X * speed, req.Movement.Y * speed);
                var np = player.Position;
                np.X += player.Velocity.X * dt; if (!room.World.IsWalkable(np)) np.X = player.Position.X;
                np.Y += player.Velocity.Y * dt; if (!room.World.IsWalkable(np)) np.Y = player.Position.Y;
                player.Position = np; player.LastProcessedInputSequence = req.InputSequenceNumber;
            }
        });

        _packetProcessor.SubscribeReusable<AbilityUseRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var player) && _rooms.TryGetValue(player.RoomId, out var room)) {
                if (player.RoomId == 0) return;
                _abilityManager.HandleAbilityRequest(player, req, room.Bullets);
            }
        });

        _packetProcessor.SubscribeReusable<PortalUseRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var player) && _rooms.TryGetValue(player.RoomId, out var room)) {
                foreach(var portal in room.Portals.Values) {
                    if (Math.Abs(player.Position.X - portal.Position.X) < 60 && Math.Abs(player.Position.Y - portal.Position.Y) < 60) {
                        int tid = portal.TargetRoomId;
                        if (tid < 0 || !_rooms.ContainsKey(tid)) {
                            if (portal.Name.Contains("Forest")) tid = CreateNewRoom("room_forest");
                            else tid = CreateNewRoom("room_dungeon");
                            portal.TargetRoomId = tid;
                        }
                        // Save player state if they are leaving a combat room (not the Nexus)
                        if (player.RoomId != 0) {
                            SavePlayer(peer.Id);
                        }
                        SwitchPlayerRoom(peer, tid);
                        break;
                    }
                }
            }
        });

        _packetProcessor.SubscribeReusable<SwapItemRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var p)) {
                ItemInfo[] fromArray = req.FromIndex < 3 ? p.Equipment : p.Inventory;
                int fromIdx = req.FromIndex < 3 ? req.FromIndex : req.FromIndex - 3;
                
                ItemInfo[] toArray = req.ToIndex < 3 ? p.Equipment : p.Inventory;
                int toIdx = req.ToIndex < 3 ? req.ToIndex : req.ToIndex - 3;

                if (fromIdx >= 0 && fromIdx < fromArray.Length && toIdx >= 0 && toIdx < toArray.Length) {
                    ItemInfo item = fromArray[fromIdx];
                    
                    // Validation for Equipment Slots
                    if (req.ToIndex < 3 && item.ItemId != 0) {
                        if (req.ToIndex == 0 && item.Category != ItemCategory.Weapon) return;
                        if (req.ToIndex == 1 && item.Category != ItemCategory.Armor) return;
                        if (req.ToIndex == 2 && item.Category != ItemCategory.Ring) return;
                    }

                    // Swap
                    fromArray[fromIdx] = toArray[toIdx];
                    toArray[toIdx] = item;
                }
            }
        });

        _packetProcessor.SubscribeReusable<UseItemRequest, NetPeer>((req, peer) => {
            if (_playerStates.TryGetValue(peer.Id, out var p)) {
                ItemInfo[] slots = req.SlotIndex < 3 ? p.Equipment : p.Inventory;
                int idx = req.SlotIndex < 3 ? req.SlotIndex : req.SlotIndex - 3;

                if (idx >= 0 && idx < slots.Length) {
                    ItemInfo item = slots[idx];
                    if (item.ItemId != 0 && item.Category == ItemCategory.Consumable) {
                        if (item.Name.Contains("Health Potion")) {
                            p.CurrentHealth = Math.Min(p.MaxHealth, p.CurrentHealth + item.StatBonus);
                            slots[idx] = new ItemInfo(); // Consume
                        }
                    }
                }
            }
        });
    }

    public void SavePlayer(int playerId) {
        if (_usernames.TryGetValue(playerId, out var username) && _playerStates.TryGetValue(playerId, out var player)) {
            var data = new PlayerSaveData {
                MaxHealth = player.MaxHealth,
                Level = player.Level,
                Experience = player.Experience,
                Attack = player.Attack,
                Defense = player.Defense,
                Speed = player.Speed,
                Dexterity = player.Dexterity,
                Vitality = player.Vitality,
                Wisdom = player.Wisdom,
                Equipment = player.Equipment,
                Inventory = player.Inventory
            };
            DatabaseManager.SavePlayer(username, data);
        }
    }

    private int CreateNewRoom(string roomId) {
        if (!GameDataManager.Rooms.TryGetValue(roomId, out var rd)) return -1;
        int id = _rooms.Count;
        while(_rooms.ContainsKey(id)) id++;
        var room = new ServerRoom(id, rd, new Random().Next(), _packetProcessor, this, _abilityManager, _playerStates);
        _rooms[id] = room;
        var rand = new Random();
        for (int i = 0; i < rd.SpawnerCount; i++) {
            Vector2 pos = new Vector2(rand.Next(200, Math.Max(300, rd.Width * 32 - 200)), rand.Next(200, Math.Max(300, rd.Height * 32 - 200)));
            if (room.World.IsWalkable(pos)) room.Spawners.CreateSpawner(pos, 100, 8);
        }
        if (room.Spawners.GetActiveSpawners().Count == 0) {
            room.Enemies.SpawnEnemy(new Vector2(rd.Width * 16, rd.Height * 16), "boss_overlord");
        }
        return id;
    }

    public void SwitchPlayerToNexus(int playerId) {
        if (_peers.TryGetValue(playerId, out var peer)) {
            SwitchPlayerRoom(peer, 0);
        }
    }

    private void SwitchPlayerRoom(NetPeer peer, int roomId) {
        if (!_playerStates.TryGetValue(peer.Id, out var player)) return;
        if (!_rooms.TryGetValue(roomId, out var room)) return;
        
        int oldRoomId = player.RoomId;
        player.RoomId = roomId; 
        
        if (_rooms.TryGetValue(oldRoomId, out var oldRoom)) {
            var leaveW = new NetDataWriter(); _packetProcessor.Write(leaveW, player.ToPacket());
            foreach(var p in oldRoom.GetPlayersInRoom()) if(p.Key != peer.Id) _netManager.GetPeerById(p.Key).Send(leaveW, DeliveryMethod.ReliableOrdered);
        }

        Vector2 spawnPos = new Vector2(room.World.Width * 16, room.World.Height * 16);
        for (int i = 0; i < 100; i++) {
            var tp = new Vector2(new Random().Next(100, (room.World.Width - 2) * 32), new Random().Next(100, (room.World.Height - 2) * 32));
            if (room.World.IsWalkable(tp)) { spawnPos = tp; break; }
        }
        player.Position = spawnPos;

        SendPacket(peer, new WorldInit { Seed = room.Seed, Width = room.World.Width, Height = room.World.Height, TileSize = 32, Style = room.Style, CleanupTimer = room.ForceCleanupTimer ?? -1f }, DeliveryMethod.ReliableOrdered);
        foreach (var p in room.Portals.Values) SendPacket(peer, p, DeliveryMethod.ReliableOrdered);
        foreach (var i in room.Items.GetActiveItems()) SendPacket(peer, new ItemSpawn { ItemId = i.Id, Position = i.Position, Item = i.Info }, DeliveryMethod.ReliableOrdered);
        foreach (var e in room.Enemies.GetAllEnemies()) if (e.Active) { if (e.DataId.StartsWith("boss_")) SendPacket(peer, new BossSpawn { BossId = e.Id, Position = e.Position, MaxHealth = e.MaxHealth, DataId = e.DataId }, DeliveryMethod.ReliableOrdered); else SendPacket(peer, new EnemySpawn { EnemyId = e.Id, Position = e.Position, MaxHealth = e.MaxHealth, DataId = e.DataId }, DeliveryMethod.ReliableOrdered); }
        foreach (var s in room.Spawners.GetAllSpawners()) if (s.Active) SendPacket(peer, new SpawnerSpawn { SpawnerId = s.Id, Position = s.Position, MaxHealth = s.MaxHealth }, DeliveryMethod.ReliableOrdered);
        foreach (var other in room.GetPlayersInRoom().Values) if(other.Id != peer.Id) SendPacket(peer, other.ToPacket(), DeliveryMethod.ReliableOrdered);
    }

    public void Update(float dt) {
        var activeRooms = _rooms.Values.ToList();
        
        // Multithreaded physics/AI update for each room
        Parallel.ForEach(activeRooms, r => {
            r.Update(dt);
        });

        // Handle deletions synchronously after all updates to avoid modifying _rooms concurrently
        foreach (var r in activeRooms) {
            if (r.IsMarkedForDeletion) {
                Console.WriteLine($"[Server] Deleting room {r.Id}");
                if (r.ParentRoomId != -1 && _rooms.TryGetValue(r.ParentRoomId, out var pr)) {
                    if (pr.Portals.Remove(r.ParentPortalId)) pr.Broadcast(new PortalDeath { PortalId = r.ParentPortalId });
                }
                _rooms.Remove(r.Id);
            }
        }
        
        _broadcastTimer += dt; 
        if (_broadcastTimer >= _broadcastInterval) { 
            _broadcastTimer -= _broadcastInterval; 
            BroadcastUpdates(); 
        }
    }

    private void BroadcastUpdates() {
        var activeRooms = _rooms.Values.ToList();

        Parallel.ForEach(activeRooms, r => {
            var players = r.GetPlayersInRoom();
            if (players.Count == 0) return;

            // Using thread-local DataWriters to avoid cross-thread corruption in LiteNetLib
            var playerWriter = new NetDataWriter();
            var enemyWriter = new NetDataWriter();
            var spawnerWriter = new NetDataWriter();
            var bossWriter = new NetDataWriter();
            var leaderboardWriter = new NetDataWriter();
            var roomStateWriter = new NetDataWriter();

            if (r.ForceCleanupTimer.HasValue) {
                roomStateWriter.Reset(); _packetProcessor.Write(roomStateWriter, new RoomStateUpdate { CleanupTimer = r.ForceCleanupTimer.Value });
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(roomStateWriter, DeliveryMethod.Unreliable);
            }

            foreach (var p in players.Values) {
                // 1. Broadcast Public Data (Pos, HP, Level) to everyone
                playerWriter.Reset(); _packetProcessor.Write(playerWriter, p.ToPacket());
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(playerWriter, DeliveryMethod.Unreliable);

                // 2. Send Private Data (Mana, XP, Stats, Inventory) only to the owner
                if (_peers.TryGetValue(p.Id, out var selfPeer)) {
                    var selfWriter = new NetDataWriter();
                    _packetProcessor.Write(selfWriter, p.ToSelfPacket());
                    selfPeer.Send(selfWriter, DeliveryMethod.Unreliable);
                }
            }
            foreach (var e in r.Enemies.GetActiveEnemies()) {
                if (e.DataId.StartsWith("boss_")) {
                    bossWriter.Reset(); _packetProcessor.Write(bossWriter, new BossUpdate { BossId = e.Id, Position = e.Position, CurrentHealth = e.CurrentHealth, Phase = 1 });
                    foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(bossWriter, DeliveryMethod.Unreliable);
                } else {
                    enemyWriter.Reset(); _packetProcessor.Write(enemyWriter, new EnemyUpdate { EnemyId = e.Id, Position = e.Position, CurrentHealth = e.CurrentHealth });
                    foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(enemyWriter, DeliveryMethod.Unreliable);
                }
            }
            foreach (var s in r.Spawners.GetActiveSpawners()) {
                spawnerWriter.Reset(); _packetProcessor.Write(spawnerWriter, new SpawnerUpdate { SpawnerId = s.Id, CurrentHealth = s.CurrentHealth });
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(spawnerWriter, DeliveryMethod.Unreliable);
            }
            
            var entries = r.RoomScores.Select(kvp => new LeaderboardEntry { PlayerId = kvp.Key, Username = _usernames.GetValueOrDefault(kvp.Key, "Guest"), Score = kvp.Value }).OrderByDescending(e => e.Score).ToArray();
            if (entries.Length > 0) {
                leaderboardWriter.Reset(); _packetProcessor.Write(leaderboardWriter, new LeaderboardUpdate { Entries = entries });
                foreach(var tid in players.Keys) if(_peers.TryGetValue(tid, out var peer)) peer.Send(leaderboardWriter, DeliveryMethod.Unreliable);
            }
        });
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
    public void OnPeerDisconnected(NetPeer p, DisconnectInfo info) { _playerStates.Remove(p.Id); _peers.Remove(p.Id); _usernames.Remove(p.Id); }
    public void OnNetworkError(IPEndPoint ep, SocketError err) { }
    public void OnNetworkReceive(NetPeer p, NetPacketReader r, byte ch, DeliveryMethod dm) => _packetProcessor.ReadAllPackets(r, p);
    public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader r, UnconnectedMessageType t) { }
    public void OnNetworkLatencyUpdate(NetPeer p, int l) { }
    public void OnConnectionRequest(ConnectionRequest r) => r.AcceptIfKey("LastLightKey");
}
