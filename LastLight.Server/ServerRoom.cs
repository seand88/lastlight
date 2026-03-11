using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;
using LastLight.Common.Abilities;
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
    public float? ForceCleanupTimer { get; private set; } = null;
    public bool IsMarkedForDeletion { get; private set; } = false;
    public WorldManager World { get; } = new();
    public ServerEnemyManager Enemies { get; } = new();
    public ServerSpawnerManager Spawners { get; } = new();
    public ServerItemManager Items { get; } = new();
    public ServerBulletManager Bullets { get; } = new();
    public Dictionary<int, PortalSpawn> Portals { get; } = new();
    public Dictionary<int, int> RoomScores { get; } = new();
    
    private readonly NetPacketProcessor _packetProcessor;
    private readonly ServerNetworking _networking;
    private readonly ServerAbilityManager _abilityManager;
    private readonly Dictionary<int, ServerPlayer> _allPlayers;

    public RoomData Data { get; private set; }

    public ServerRoom(int id, RoomData data, int seed, NetPacketProcessor processor, ServerNetworking networking, ServerAbilityManager abilityManager, Dictionary<int, ServerPlayer> allPlayers)
    {
        Id = id; Data = data; Name = data.Name; Seed = seed; Style = data.Style;
        _packetProcessor = processor; _networking = networking; _abilityManager = abilityManager; _allPlayers = allPlayers;
        World.GenerateWorld(seed, data.Width, data.Height, 32, Style);
        Enemies.RoomBullets = Bullets;
        
        Enemies.OnEnemySpawned += (e) => { 
            if (e.DataId.StartsWith("boss_")) {
                Broadcast(new BossSpawn { BossId = e.Id, Position = e.Position, MaxHealth = e.MaxHealth, DataId = e.DataId });
            } else {
                Broadcast(new EnemySpawn { EnemyId = e.Id, Position = e.Position, MaxHealth = e.MaxHealth, DataId = e.DataId }); 
            }
        };
        Enemies.OnEnemyDied += (e) => {
            if (e.DataId.StartsWith("boss_")) {
                Broadcast(new BossDeath { BossId = e.Id });
                SpawnPortal(e.Position, 0, "Nexus Portal");
                for(int i=0; i<5; i++) Items.SpawnItem(new ItemInfo { ItemId = new Random().Next(1000, 9000), DataId = "weapon_rapid_staff" }, new Vector2(e.Position.X + i*10, e.Position.Y));
                
                // Room is successfully completed, distribute XP to everyone then save players
                foreach (var p in GetPlayersInRoom().Values) {
                    AddExperience(p, 1000);
                    _networking.SavePlayer(p.Id);
                }
                
                ForceCleanupTimer = 60f; // 1 minute cleanup timer
            } else {
                if (e.ParentSpawnerId != -1) Spawners.NotifyEnemyDeath(e.ParentSpawnerId);
                Broadcast(new EnemyDeath { EnemyId = e.Id });
                int r = new Random().Next(100);
                if (r < 25) Items.SpawnItem(new ItemInfo { ItemId = new Random().Next(1000, 9000), DataId = "potion_health" }, e.Position);
                else if (r < 35) Items.SpawnItem(new ItemInfo { ItemId = new Random().Next(1000, 9000), DataId = "weapon_double_staff" }, e.Position);
            }
        };
        Enemies.OnEnemyUseAbility += (e, id, dir) => {
            _abilityManager.HandleEnemyAbility(e, id, dir, Bullets);
        };
        Spawners.OnSpawnerCreated += (s) => Broadcast(new SpawnerSpawn { SpawnerId = s.Id, Position = s.Position, MaxHealth = s.MaxHealth });
        Spawners.OnSpawnerDied += (s) => {
            Broadcast(new SpawnerDeath { SpawnerId = s.Id });
            if (Id != 0 && Spawners.GetActiveSpawners().Count == 0) {
                Enemies.SpawnEnemy(new Vector2(Data.Width * 16, Data.Height * 16), "boss_overlord");
            }
        };
        Spawners.OnRequestEnemySpawn += (pos, sid) => {
            string enemyId = "enemy_goblin";
            if (Data.AllowedEnemies != null && Data.AllowedEnemies.Length > 0) {
                enemyId = Data.AllowedEnemies[new Random().Next(Data.AllowedEnemies.Length)];
            }
            Enemies.SpawnEnemy(pos, enemyId, sid);
        };
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
        if (ForceCleanupTimer.HasValue) {
            ForceCleanupTimer -= dt;
            if (ForceCleanupTimer.Value <= 0) {
                IsMarkedForDeletion = true;
                
                // Force any remaining players out to Nexus
                foreach (var p in GetPlayersInRoom().Keys) {
                    _networking.SavePlayer(p);
                    _networking.SwitchPlayerToNexus(p);
                }
            }
        }

        var players = GetPlayersInRoom();
        if (Id != 0 && players.Count == 0 && !ForceCleanupTimer.HasValue) {
            EmptyTimer += dt;
            if (EmptyTimer > 30f) IsMarkedForDeletion = true;
        } else EmptyTimer = 0f;

        // Health Regen based on Vitality
        foreach(var p in players.Values) {
            if (p.CurrentHealth < p.MaxHealth) {
                float regenPerSec = 1.0f + (p.Vitality * 0.5f);
                p.CurrentHealth = (int)Math.Min(p.MaxHealth, p.CurrentHealth + (regenPerSec * dt));
            }
            
            // Mana Regen based on Wisdom
            if (p.CurrentMana < p.MaxMana) {
                float manaRegenPerSec = 1.0f + (p.Wisdom * 0.5f);
                // We add it as a float accumulator internally or just do the math
                p.CurrentMana = (int)Math.Min(p.MaxMana, p.CurrentMana + (manaRegenPerSec * dt));
            }
        }

        Spawners.Update(dt, World);
        Enemies.Update(dt, players, World, _abilityManager);
        Items.Update(players);
        Bullets.Update(dt);
        StatusManager.Update(dt);
        CheckCollisions();
        CheckAllDeaths();
    }

    private void CheckAllDeaths()
    {
        var players = GetPlayersInRoom();
        foreach (var p in players.Values) if (p.CurrentHealth <= 0) HandlePlayerDeath(p);
        foreach (var e in Enemies.GetActiveEnemies()) if (e.CurrentHealth <= 0) Enemies.HandleDamage(e.Id, 0);
    }

    public Dictionary<int, ServerPlayer> GetPlayersInRoom() => _allPlayers.Where(p => p.Value.RoomId == Id).ToDictionary(p => p.Key, p => p.Value);

    public void Broadcast<T>(T packet, DeliveryMethod dm = DeliveryMethod.ReliableOrdered) where T : class, new()
    {
        var writer = new NetDataWriter(); _packetProcessor.Write(writer, packet);
        foreach (var p in GetPlayersInRoom()) _networking.GetPeer(p.Key)?.Send(writer, dm);
    }

    private void AddExperience(ServerPlayer player, int amount) {
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
            if (!World.IsShootable(b.Position)) { Bullets.DestroyBullet(b); hit = true; continue; }
            
            // Look up ability spec for this bullet
            if (!GameDataManager.Abilities.TryGetValue(b.AbilityId, out var ability)) {
                // If it's an old bullet or missing ID, use a default damage logic for fallback
                ProcessOldCollision(b, players);
                continue;
            }

            IEntity? shooter = null;
            if (b.OwnerId >= 0) {
                if (_allPlayers.TryGetValue(b.OwnerId, out var sp)) shooter = sp;
            }
            else {
                // Check Enemy/Boss as source
                var enemy = Enemies.GetAllEnemies().FirstOrDefault(e => e.Id == b.OwnerId);
                if (enemy != null) shooter = enemy;
                
            }

            // 1. Check Players
            foreach (var p in players.Values) {
                if (b.OwnerId == p.Id || (b.OwnerId >= 0 && p.Id >= 0)) continue; // Don't hit self or allies
                if (Math.Abs(b.Position.X - p.Position.X) < 20 && Math.Abs(b.Position.Y - p.Position.Y) < 20) {
                    ApplyAbilityEffects(ability, p, shooter, b.Position, b.CorrelationId);
                    if (p.CurrentHealth <= 0) HandlePlayerDeath(p);
                    Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = p.Id, TargetType = EntityType.Player });
                    Bullets.DestroyBullet(b); hit = true; break;
                }
            }
            if (hit) continue;

            // 2. Check Enemies/Bosses
            if (b.OwnerId >= 0) { // Only player bullets hit AI
                foreach (var e in Enemies.GetActiveEnemies()) {
                    if (Math.Abs(b.Position.X - e.Position.X) < 20 && Math.Abs(b.Position.Y - e.Position.Y) < 20) {
                        ApplyAbilityEffects(ability, e, shooter, b.Position, b.CorrelationId);
                        if (e.CurrentHealth <= 0) Enemies.HandleDamage(e.Id, 0); 
                        if (!e.Active && b.OwnerId >= 0) {
                            if (_allPlayers.TryGetValue(b.OwnerId, out var p)) AddExperience(p, 20);
                            RoomScores[b.OwnerId] = RoomScores.GetValueOrDefault(b.OwnerId) + 20;
                        }
                        Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = e.Id, TargetType = EntityType.Enemy });
                        Bullets.DestroyBullet(b); hit = true; break;
                    }
                }
                if (hit) continue;


                // Special case for Spawner (doesn't implement IEntity yet, we'll just handle damage)
                foreach (var s in Spawners.GetActiveSpawners()) {
                    if (Math.Abs(b.Position.X - s.Position.X) < 36 && Math.Abs(b.Position.Y - s.Position.Y) < 36) {
                        // Calculate damage using multiplier since Spawner isn't an IEntity
                        var dmgEffect = ability.Effects.FirstOrDefault(ef => ef.EffectName == "damage");
                        float multiplier = dmgEffect?.Multiplier ?? 1.0f;
                        int damage = (int)((shooter?.BaseDamage ?? 10) * multiplier);
                        
                        Spawners.HandleDamage(s.Id, damage);
                        if (!s.Active && _allPlayers.TryGetValue(b.OwnerId, out var p)) {
                            AddExperience(p, 100);
                            RoomScores[b.OwnerId] = RoomScores.GetValueOrDefault(b.OwnerId) + 100;
                        }
                        Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = s.Id, TargetType = EntityType.Spawner });
                        Bullets.DestroyBullet(b); hit = true; break;
                    }
                }
            }
        }
    }

    private void ApplyAbilityEffects(AbilitySpec ability, IEntity target, IEntity? source, Vector2 pos, int sourceProjectileId)
    {
        foreach (var effect in ability.Effects) {
            IEntity? actualTarget = null;

            if (effect.TargetType == "caster") {
                actualTarget = source;
            } else {
                // Filter other target types
                bool valid = effect.TargetType switch {
                    "enemies" => (source?.Id >= 0 && target.Id < 0) || (source?.Id < 0 && target.Id >= 0),
                    "allies" => (source?.Id >= 0 && target.Id >= 0) || (source?.Id < 0 && target.Id < 0),
                    _ => true
                };
                if (valid) actualTarget = target;
            }

            if (actualTarget != null) {
                EffectProcessor.ApplyEffect(actualTarget, source ?? actualTarget, effect);
                
                // Broadcast event
                Broadcast(new EffectEvent {
                    EffectName = effect.EffectName,
                    TargetId = actualTarget.Id,
                    SourceId = source?.Id ?? 0,
                    SourceProjectileId = sourceProjectileId,
                    Value = effect.Value,
                    Position = pos,
                    TemplateId = effect.TemplateId,
                    Duration = effect.Duration ?? 0f
                });
            }
        }
    }

    private void HandlePlayerDeath(ServerPlayer p)
    {
        p.CurrentHealth = p.MaxHealth;
        p.Position = new Vector2(World.Width * 16, World.Height * 16);
        // Penalty for death: lose some XP
        p.Experience = (int)(p.Experience * 0.8f);
        // Lose inventory on death
        for (int i = 0; i < p.Inventory.Length; i++) {
            p.Inventory[i] = new ItemInfo();
        }
        // If they were in a dungeon, maybe kick them to Nexus? 
        // For now, just respawn at center of room.
    }

    private void ProcessOldCollision(ServerBullet b, Dictionary<int, ServerPlayer> players)
    {
        // Simple fallback for non-ability bullets (like AI for now)
        foreach (var p in players.Values) {
            if (b.OwnerId == p.Id || b.OwnerId >= 0) continue;
            if (Math.Abs(b.Position.X - p.Position.X) < 20 && Math.Abs(b.Position.Y - p.Position.Y) < 20) {
                p.CurrentHealth -= 10;
                if (p.CurrentHealth <= 0) HandlePlayerDeath(p);
                Broadcast(new BulletHit { BulletId = b.BulletId, TargetId = p.Id, TargetType = EntityType.Player });
                Bullets.DestroyBullet(b); break;
            }
        }
    }
}
