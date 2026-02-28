using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LastLight.Server;

public class ServerRoom
{
    public int Id { get; }
    public string Name { get; }
    public int Seed { get; }
    public WorldManager.GenerationStyle Style { get; }
    public int ParentRoomId { get; set; } = -1;
    public int ParentPortalId { get; set; } = -1;
    public float EmptyTimer { get; private set; } = 0f;
    public bool IsMarkedForDeletion { get; private set; } = false;
    public WorldManager World { get; } = new();
    public ServerEnemyManager Enemies { get; } = new();
    public ServerSpawnerManager Spawners { get; } = new();
    public ServerBossManager Bosses { get; } = new();
    public ServerItemManager Items { get; } = new();
    public ServerBulletManager Bullets { get; } = new();
    public Dictionary<int, PortalSpawn> Portals { get; } = new();
    public Dictionary<int, int> RoomScores { get; } = new();
    
    private readonly NetPacketProcessor _packetProcessor;
    private readonly ServerNetworking _networking;
    private readonly Dictionary<int, AuthoritativePlayerUpdate> _allPlayers;

    public ServerRoom(int id, string name, int seed, int width, int height, WorldManager.GenerationStyle style, NetPacketProcessor processor, ServerNetworking networking, Dictionary<int, AuthoritativePlayerUpdate> allPlayers)
    {
        Id = id; Name = name; Seed = seed; Style = style;
        _packetProcessor = processor; _networking = networking; _allPlayers = allPlayers;
        World.GenerateWorld(seed, width, height, 32, Style);
        
        Enemies.OnEnemySpawned += (e) => { var p = new EnemySpawn { EnemyId = e.Id, Position = e.Position, MaxHealth = e.MaxHealth }; Broadcast(p); };
        Enemies.OnEnemyDied += (e) => {
            if (e.ParentSpawnerId != -1) Spawners.NotifyEnemyDeath(e.ParentSpawnerId);
            Broadcast(new EnemyDeath { EnemyId = e.Id });
            int r = new Random().Next(100);
            if (r < 25) Items.SpawnItem(ItemType.HealthPotion, e.Position);
            else if (r < 35) Items.SpawnItem(ItemType.WeaponUpgrade, e.Position);
        };
        Enemies.OnEnemyShoot += (e, p, v) => SpawnBullet(e.Id, p, v);
        Spawners.OnSpawnerCreated += (s) => Broadcast(new SpawnerSpawn { SpawnerId = s.Id, Position = s.Position, MaxHealth = s.MaxHealth });
        Spawners.OnSpawnerDied += (s) => Broadcast(new SpawnerDeath { SpawnerId = s.Id });
        Spawners.OnRequestEnemySpawn += (pos, sid) => Enemies.SpawnEnemy(pos, 100, sid);
        Bosses.OnBossSpawned += (b) => Broadcast(new BossSpawn { BossId = b.Id, Position = b.Position, MaxHealth = b.MaxHealth });
        Bosses.OnBossDied += (b) => {
            Broadcast(new BossDeath { BossId = b.Id });
            SpawnPortal(b.Position, 0, "Nexus Portal");
            for(int i=0; i<5; i++) Items.SpawnItem(ItemType.WeaponUpgrade, new Vector2(b.Position.X + i*10, b.Position.Y));
        };
        Bosses.OnBossShoot += (b, p, v) => SpawnBullet(b.Id, p, v);
        Items.OnItemSpawned += (i) => Broadcast(new ItemSpawn { ItemId = i.Id, Position = i.Position, Type = i.Type });
        Items.OnItemPickedUp += (i, pid) => {
            if (_allPlayers.TryGetValue(pid, out var p)) {
                if (i.Type == ItemType.HealthPotion) p.CurrentHealth = Math.Min(p.CurrentHealth + 25, p.MaxHealth);
                else if (i.Type == ItemType.WeaponUpgrade) p.CurrentWeapon = p.CurrentWeapon switch { WeaponType.Single => WeaponType.Double, WeaponType.Double => WeaponType.Spread, WeaponType.Spread => WeaponType.Rapid, _ => WeaponType.Rapid };
            }
            Broadcast(new ItemPickup { ItemId = i.Id, PlayerId = pid });
        };
    }

    public void SpawnPortal(Vector2 pos, int targetRoomId, string name, int? forcedId = null)
    {
        int pid = forcedId ?? -(3000 + Portals.Count);
        var p = new PortalSpawn { PortalId = pid, Position = pos, TargetRoomId = targetRoomId, Name = name };
        Portals[pid] = p;
        Broadcast(p);
    }

    private void SpawnBullet(int ownerId, Vector2 pos, Vector2 vel)
    {
        int bid = -(10000 + new Random().Next(1000000));
        Bullets.Spawn(bid, ownerId, pos, vel);
        Broadcast(new SpawnBullet { OwnerId = ownerId, BulletId = bid, Position = pos, Velocity = vel });
    }

    public void Update(float dt)
    {
        var players = GetPlayersInRoom();
        if (Id != 0 && players.Count == 0) {
            EmptyTimer += dt;
            if (EmptyTimer > 30f) IsMarkedForDeletion = true;
        } else EmptyTimer = 0f;
        Spawners.Update(dt, World);
        Enemies.Update(dt, players, World);
        Bosses.Update(dt, players);
        Items.Update(players);
        Bullets.Update(dt);
        CheckCollisions();
    }

    public Dictionary<int, AuthoritativePlayerUpdate> GetPlayersInRoom() => _allPlayers.Where(p => p.Value.RoomId == Id).ToDictionary(p => p.Key, p => p.Value);

    public void Broadcast<T>(T packet, DeliveryMethod dm = DeliveryMethod.ReliableOrdered) where T : class, new()
    {
        var writer = new NetDataWriter(); _packetProcessor.Write(writer, packet);
        foreach (var p in GetPlayersInRoom()) _networking.GetPeer(p.Key)?.Send(writer, dm);
    }

    private void AddExperience(AuthoritativePlayerUpdate player, int amount) {
        player.Experience += amount;
        int threshold = player.Level * 100;
        if (player.Experience >= threshold) {
            player.Experience -= threshold;
            player.Level++;
            player.MaxHealth += 20;
            player.CurrentHealth = player.MaxHealth;
        }
    }

    private void CheckCollisions()
    {
        var players = GetPlayersInRoom();
        foreach (var b in Bullets.GetActiveBullets())
        {
            bool hit = false;
            if (!World.IsShootable(b.Position)) { Bullets.DestroyBullet(b); hit = true; Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = -1, TargetType = EntityType.Spawner }); continue; }
            foreach (var p in players.Values) {
                if (b.OwnerId == p.PlayerId || b.OwnerId >= 0) continue;
                if (Math.Abs(b.Position.X - p.Position.X) < 20 && Math.Abs(b.Position.Y - p.Position.Y) < 20) {
                    p.CurrentHealth -= 10; if (p.CurrentHealth <= 0) { p.CurrentHealth = p.MaxHealth; p.Position = new Vector2(World.Width*16, World.Height*16); }
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = p.PlayerId, TargetType = EntityType.Player });
                    Bullets.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;
            foreach (var s in Spawners.GetActiveSpawners()) {
                if (b.OwnerId < 0) continue;
                if (Math.Abs(b.Position.X - s.Position.X) < 36 && Math.Abs(b.Position.Y - s.Position.Y) < 36) {
                    Spawners.HandleDamage(s.Id, 25);
                    if (!s.Active && _allPlayers.TryGetValue(b.OwnerId, out var shooter)) {
                        AddExperience(shooter, 100);
                        RoomScores[b.OwnerId] = RoomScores.GetValueOrDefault(b.OwnerId) + 100;
                    }
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = s.Id, TargetType = EntityType.Spawner });
                    Bullets.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;
            foreach (var e in Enemies.GetActiveEnemies()) {
                if (b.OwnerId < 0) continue;
                if (Math.Abs(b.Position.X - e.Position.X) < 20 && Math.Abs(b.Position.Y - e.Position.Y) < 20) {
                    Enemies.HandleDamage(e.Id, 25);
                    if (!e.Active && _allPlayers.TryGetValue(b.OwnerId, out var shooter)) {
                        AddExperience(shooter, 20);
                        RoomScores[b.OwnerId] = RoomScores.GetValueOrDefault(b.OwnerId) + 20;
                    }
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = e.Id, TargetType = EntityType.Enemy });
                    Bullets.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;
            foreach (var boss in Bosses.GetActiveBosses()) {
                if (b.OwnerId < 0) continue;
                if (Math.Abs(b.Position.X - boss.Position.X) < 68 && Math.Abs(b.Position.Y - boss.Position.Y) < 68) {
                    Bosses.HandleDamage(boss.Id, 25);
                    if (!boss.Active && _allPlayers.TryGetValue(b.OwnerId, out var shooter)) {
                        AddExperience(shooter, 1000);
                        RoomScores[b.OwnerId] = RoomScores.GetValueOrDefault(b.OwnerId) + 1000;
                    }
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = boss.Id, TargetType = EntityType.Boss });
                    Bullets.DestroyBullet(b); break;
                }
            }
        }
    }
}