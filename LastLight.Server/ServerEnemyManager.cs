using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;
using LastLight.Common.Abilities;

namespace LastLight.Server;

public class ServerEnemy : LastLight.Common.Abilities.IEntity
{
    public int Id { get; set; }
    public string DataId { get; set; } = "enemy_goblin";
    public int ParentSpawnerId { get; set; } = -1;
    public Vector2 Position;
    
    // IEntity implementation
    Vector2 LastLight.Common.Abilities.IEntity.Position { get => Position; set => Position = value; }

    public Vector2 Velocity;
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public bool Active { get; set; }
    public float Speed { get; set; } = 100f;
    public int BaseDamage { get; set; }
    public float AttackSpeedBonus { get; set; }
    public float RangeBonus { get; set; }
    public string PrimaryAbilityId { get; set; } = "";
    public string SpecialAbilityId { get; set; } = "";
    public string AiType { get; set; } = "chase";
    
    public Action<ServerEnemy, string, Vector2>? OnUseAbility;
    private float _shootTimer = 0f;
    
    public void Update(float dt, Dictionary<int, ServerPlayer> players, WorldManager worldManager)
    {
        if (!Active) return;

        // Simple AI: Find nearest player and move towards them
        ServerPlayer? nearestPlayer = null;
        float minDistanceSq = float.MaxValue;

        foreach (var player in players.Values)
        {
            float dx = player.Position.X - Position.X;
            float dy = player.Position.Y - Position.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                nearestPlayer = player;
            }
        }

        if (nearestPlayer != null)
        {
            float dx = nearestPlayer.Position.X - Position.X;
            float dy = nearestPlayer.Position.Y - Position.Y;
            
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            
            if (distance > 0)
            {
                Velocity.X = (dx / distance) * Speed;
                Velocity.Y = (dy / distance) * Speed;
            }
            else
            {
                Velocity = new Vector2(0, 0);
            }
            
            // Shooting logic
            _shootTimer += dt;
            
            // Try Special first
            if (!string.IsNullOrEmpty(SpecialAbilityId)) {
                if (GameDataManager.Abilities.TryGetValue(SpecialAbilityId, out var spec)) {
                    // Spec says: Final Fire Rate = AbilityBaseFireRate * (1 + EntityAttackSpeedBonus)
                    // Fire Rate is shots per second, so interval = 1.0 / FireRate.
                    // If it's a cooldown-based special, let's assume Cooldown is the base interval.
                    float interval = spec.Cooldown / (1.0f + AttackSpeedBonus);
                    if (_shootTimer >= interval) {
                        _shootTimer = 0;
                        OnUseAbility?.Invoke(this, SpecialAbilityId, new Vector2(dx / distance, dy / distance));
                        return; // Done for this frame
                    }
                }
            }

            // Try Primary
            if (!string.IsNullOrEmpty(PrimaryAbilityId)) {
                if (GameDataManager.Abilities.TryGetValue(PrimaryAbilityId, out var spec)) {
                    // Use Final Fire Rate Formula: Final Fire Rate = AbilityBaseFireRate * (1 + EntityAttackSpeedBonus)
                    float baseFireRate = spec.Delivery is ProjectileDelivery p ? p.FireRate : 1.0f;
                    float finalFireRate = baseFireRate * (1.0f + AttackSpeedBonus);
                    float interval = 1.0f / finalFireRate;
                    
                    if (_shootTimer >= interval) {
                        _shootTimer = 0;
                        OnUseAbility?.Invoke(this, PrimaryAbilityId, new Vector2(dx / distance, dy / distance));
                    }
                }
            }
        }
        else
        {
             Velocity = new Vector2(0, 0);
        }

        var newPos = Position;
        newPos.X += Velocity.X * dt;
        if (worldManager.IsWalkable(newPos))
        {
            Position.X = newPos.X;
        }
        else
        {
            newPos.X = Position.X;
        }
        
        newPos.Y += Velocity.Y * dt;
        if (worldManager.IsWalkable(newPos))
        {
            Position.Y = newPos.Y;
        }
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Active = false;
        }
    }
}

public class ServerEnemyManager
{
    private readonly Dictionary<int, ServerEnemy> _enemies = new();
    private int _nextEnemyId = -1; // Enemies use negative IDs
    private Random _random = new();

    public Action<ServerEnemy>? OnEnemySpawned;
    public Action<ServerEnemy>? OnEnemyDied;
    public Action<ServerEnemy, Vector2, Vector2>? OnEnemyShoot;
public void SpawnEnemy(Vector2 position, string dataId = "enemy_goblin", int parentSpawnerId = -1)
{
    int maxHealth = 100;
    float speed = 100f;
    int baseDamage = 10;
    float attackSpeedBonus = 0f;
    float rangeBonus = 0f;
    string primary = "";
    string special = "";
    string aiType = "chase";

    if (GameDataManager.Enemies.TryGetValue(dataId, out var ed)) {
        maxHealth = ed.MaxHealth;
        speed = ed.Speed;
        baseDamage = ed.BaseDamage;
        attackSpeedBonus = ed.AttackSpeedBonus;
        rangeBonus = ed.RangeBonus;
        primary = ed.PrimaryAbilityId;
        special = ed.SpecialAbilityId;
        aiType = ed.AiType;
    }

    var enemy = new ServerEnemy
    {
        Id = _nextEnemyId--, // Decrement for next
        DataId = dataId,
        ParentSpawnerId = parentSpawnerId,
        Position = position,
        MaxHealth = maxHealth,
        CurrentHealth = maxHealth,
        Speed = speed,
        BaseDamage = baseDamage,
        AttackSpeedBonus = attackSpeedBonus,
        RangeBonus = rangeBonus,
        PrimaryAbilityId = primary,
        SpecialAbilityId = special,
        AiType = aiType,
        Active = true
    };
    enemy.OnUseAbility = (e, id, dir) => OnEnemyUseAbility?.Invoke(e, id, dir);
    _enemies[enemy.Id] = enemy;
    OnEnemySpawned?.Invoke(enemy);
}

public Action<ServerEnemy, string, Vector2>? OnEnemyUseAbility;

    public void Update(float dt, Dictionary<int, ServerPlayer> players, WorldManager worldManager)
    {
        foreach (var enemy in _enemies.Values.ToList()) // ToList to avoid modification during iteration if we remove later, though we just set Active=false
        {
            if (enemy.Active)
            {
                enemy.Update(dt, players, worldManager);
                
                // For now, if health is 0, we can remove them eventually, but they are set inactive in TakeDamage
            }
        }
    }

    public void HandleDamage(int enemyId, int damage)
    {
        if (_enemies.TryGetValue(enemyId, out var enemy) && enemy.Active)
        {
            enemy.TakeDamage(damage);
            if (!enemy.Active)
            {
                OnEnemyDied?.Invoke(enemy);
            }
        }
    }

    public IReadOnlyCollection<ServerEnemy> GetActiveEnemies()
    {
        return _enemies.Values.Where(e => e.Active).ToList();
    }
    
    public IReadOnlyCollection<ServerEnemy> GetAllEnemies() // Needed for initial sync when player joins
    {
         return _enemies.Values;
    }
}
