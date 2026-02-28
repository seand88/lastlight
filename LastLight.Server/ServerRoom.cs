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
            if (r < 25) Items.SpawnItem(new ItemInfo { ItemId = new Random().Next(1000, 9000), Category = ItemCategory.Consumable, Name = "Health Potion", StatBonus = 25 }, e.Position);
            else if (r < 35) Items.SpawnItem(new ItemInfo { ItemId = new Random().Next(1000, 9000), Category = ItemCategory.Weapon, Name = "Double Staff", WeaponType = WeaponType.Double, StatBonus = 5 }, e.Position);
        };
        Enemies.OnEnemyShoot += (e, p, v) => SpawnBullet(e.Id, p, v);
        Spawners.OnSpawnerCreated += (s) => Broadcast(new SpawnerSpawn { SpawnerId = s.Id, Position = s.Position, MaxHealth = s.MaxHealth });
        Spawners.OnSpawnerDied += (s) => Broadcast(new SpawnerDeath { SpawnerId = s.Id });
        Spawners.OnRequestEnemySpawn += (pos, sid) => Enemies.SpawnEnemy(pos, 100, sid);
        Bosses.OnBossSpawned += (b) => Broadcast(new BossSpawn { BossId = b.Id, Position = b.Position, MaxHealth = b.MaxHealth });
        Bosses.OnBossDied += (b) => {
            Broadcast(new BossDeath { BossId = b.Id });
            SpawnPortal(b.Position, 0, "Nexus Portal");
            for(int i=0; i<5; i++) Items.SpawnItem(new ItemInfo { ItemId = new Random().Next(1000, 9000), Category = ItemCategory.Weapon, Name = "Rapid Staff", WeaponType = WeaponType.Rapid, StatBonus = 10 }, new Vector2(b.Position.X + i*10, b.Position.Y));
        };
        Bosses.OnBossShoot += (b, p, v) => SpawnBullet(b.Id, p, v);
        Items.OnItemSpawned += (i) => Broadcast(new ItemSpawn { ItemId = i.Id, Position = i.Position, Item = i.Info });
        Items.OnItemPickedUp += (i, pid) => {
            if (_allPlayers.TryGetValue(pid, out var p)) {
                // Try to find an empty inventory slot
                for (int slot = 0; slot < p.Inventory.Length; slot++) {
                    if (p.Inventory[slot].ItemId == 0) {
                        p.Inventory[slot] = i.Info;
                        break;
                    }
                }
                // If inventory is full, we could ignore it, but for now we just overwrite or do nothing.
                // Let's just consume health potions instantly if not full health.
                if (i.Info.Category == ItemCategory.Consumable && i.Info.Name == "Health Potion" && p.CurrentHealth < p.MaxHealth) {
                    p.CurrentHealth = Math.Min(p.CurrentHealth + i.Info.StatBonus, p.MaxHealth);
                    // we remove it from inventory immediately if picked up
                    for (int slot = 0; slot < p.Inventory.Length; slot++) {
                        if (p.Inventory[slot].ItemId == i.Info.ItemId) { p.Inventory[slot] = new ItemInfo(); break; }
                    }
                }
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

        // Health Regen based on Vitality
        foreach(var p in players.Values) {
            if (p.CurrentHealth < p.MaxHealth) {
                float regenPerSec = 1.0f + (p.Vitality * 0.5f);
                p.CurrentHealth = (int)Math.Min(p.MaxHealth, p.CurrentHealth + (regenPerSec * dt));
            }
        }

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
            // Upgrade stats on level up
            player.Attack += 2; player.Defense += 1; player.Speed += 1;
            player.Dexterity += 1; player.Vitality += 1; player.Wisdom += 1;
        }
    }

    private void CheckCollisions()
    {
        var players = GetPlayersInRoom();
        foreach (var b in Bullets.GetActiveBullets())
        {
            bool hit = false;
            if (!World.IsShootable(b.Position)) { Bullets.DestroyBullet(b); hit = true; Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = -1, TargetType = EntityType.Spawner }); continue; }
            
            // Check Hit Players
            foreach (var p in players.Values) {
                if (b.OwnerId == p.PlayerId || b.OwnerId >= 0) continue;
                if (Math.Abs(b.Position.X - p.Position.X) < 20 && Math.Abs(b.Position.Y - p.Position.Y) < 20) {
                    int damage = Math.Max(1, 10 - (p.Defense / 2));
                    p.CurrentHealth -= damage; 
                    if (p.CurrentHealth <= 0) { 
                        p.CurrentHealth = p.MaxHealth; 
                        p.Position = new Vector2(World.Width*16, World.Height*16); 
                        // Penalty for death: lose some XP
                        p.Experience = (int)(p.Experience * 0.8f);
                    }
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = p.PlayerId, TargetType = EntityType.Player });
                    Bullets.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;

            // Check Hit Entities (Enemy/Spawner/Boss)
            if (b.OwnerId < 0) continue; // AI Bullets don't hit other AI
            if (!_allPlayers.TryGetValue(b.OwnerId, out var shooter)) continue;
            int baseDamage = 15 + (shooter.Attack * 2);

            foreach (var s in Spawners.GetActiveSpawners()) {
                if (Math.Abs(b.Position.X - s.Position.X) < 36 && Math.Abs(b.Position.Y - s.Position.Y) < 36) {
                    Spawners.HandleDamage(s.Id, baseDamage);
                    if (!s.Active) {
                        AddExperience(shooter, 100);
                        RoomScores[b.OwnerId] = RoomScores.GetValueOrDefault(b.OwnerId) + 100;
                    }
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = s.Id, TargetType = EntityType.Spawner });
                    Bullets.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;
            foreach (var e in Enemies.GetActiveEnemies()) {
                if (Math.Abs(b.Position.X - e.Position.X) < 20 && Math.Abs(b.Position.Y - e.Position.Y) < 20) {
                    Enemies.HandleDamage(e.Id, baseDamage);
                    if (!e.Active) {
                        AddExperience(shooter, 20);
                        RoomScores[b.OwnerId] = RoomScores.GetValueOrDefault(b.OwnerId) + 20;
                    }
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = e.Id, TargetType = EntityType.Enemy });
                    Bullets.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;
            foreach (var boss in Bosses.GetActiveBosses()) {
                if (Math.Abs(b.Position.X - boss.Position.X) < 68 && Math.Abs(b.Position.Y - boss.Position.Y) < 68) {
                    Bosses.HandleDamage(boss.Id, baseDamage);
                    if (!boss.Active) {
                        AddExperience(shooter, 1000);
                        RoomScores[b.OwnerId] = RoomScores.GetValueOrDefault(b.OwnerId) + 1000;
                    }
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = boss.Id, TargetType = EntityType.Boss });
                    Bullets.DestroyBullet(b); hit = true; break;
                }
            }
        }
    }
}