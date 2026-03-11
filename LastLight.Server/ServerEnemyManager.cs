using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;
using LastLight.Common.Abilities;
using LastLight.Server.AI;

namespace LastLight.Server;

public class ServerEnemy : LastLight.Common.Abilities.IEntity, ITimerRegistry
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
    
    // AI v2.0
    public IAiDriver Driver { get; set; } = new StandardAiDriver();
    public ServerAbilityManager AbilityManager { get; set; } = null!; // Set upon creation
    public ServerBulletManager RoomBullets { get; set; } = null!; // Set upon creation
    private Dictionary<string, float> _logicTimers = new();
    private Dictionary<string, float> _logicIntervals = new();

    // Legacy fields
    public string PrimaryAbilityId { get; set; } = "";
    public string SpecialAbilityId { get; set; } = "";
    public string AiType { get; set; } = "chase";
    
    public Action<ServerEnemy, string, Vector2>? OnUseAbility;
    private float _shootTimer = 0f;
    
    public void RegisterTimer(string actionId, float interval)
    {
        _logicIntervals[actionId] = interval;
        _logicTimers[actionId] = interval; // Start full
    }

    public void UnregisterTimer(string actionId)
    {
        _logicTimers.Remove(actionId);
        _logicIntervals.Remove(actionId);
    }

    public void ClearTimers()
    {
        _logicTimers.Clear();
        _logicIntervals.Clear();
    }

    public void Update(float dt, Dictionary<int, ServerPlayer> players, WorldManager worldManager, ServerAbilityManager abilityManager)
    {
        if (!Active) return;

        // 1. Delegate movement and state to the Driver
        Driver.OnUpdate(dt, this, players, abilityManager);

        // 2. Process active timers
        var keys = _logicTimers.Keys.ToList();
        foreach (var key in keys)
        {
            _logicTimers[key] -= dt;
            if (_logicTimers[key] <= 0)
            {
                // Pulse the driver
                Driver.OnTimerTick(key, this, abilityManager);
                
                // Reset timer based on interval (which might have been updated by stat changes)
                // Need to re-calculate interval if it's based on AttackSpeedBonus?
                // For now, just reset to original registered interval
                _logicTimers[key] = _logicIntervals[key]; 
            }
        }

        // Apply Velocity to Position (Physics)
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

    public void TakeDamage(int amount, IEntity? source)
    {
        CurrentHealth -= amount;

        // Reactive Driver Hook handles everything including phases
        if (AbilityManager != null) {
            Driver.OnDamaged(this, amount, source, AbilityManager);
        }

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            // Removed Active = false here to allow manager to process death event.
        }
    }
}

public class ServerEnemyManager
{
    private readonly Dictionary<int, ServerEnemy> _enemies = new();
    private int _nextEnemyId = -1; // Enemies use negative IDs
    private Random _random = new();

    public ServerBulletManager RoomBullets { get; set; } = null!;

    public Action<ServerEnemy>? OnEnemySpawned;
    public Action<ServerEnemy>? OnEnemyDied;
    public Action<ServerEnemy, Vector2, Vector2>? OnEnemyShoot;
    public Action<ServerEnemy, string, Vector2>? OnEnemyUseAbility;

    public void SpawnEnemy(Vector2 position, string dataId = "enemy_goblin", int parentSpawnerId = -1)
    {
        int maxHealth = 100;
        float speed = 100f;
        int baseDamage = 10;
        float attackSpeedBonus = 0f;
        float rangeBonus = 0f;
        
        string aiMode = "standard";
        System.Text.Json.JsonElement aiConfig = default;

        string primary = "";
        string special = "";
        string aiType = "chase";

        if (GameDataManager.Enemies.TryGetValue(dataId, out var ed)) {
            maxHealth = ed.MaxHealth;
            speed = ed.Speed;
            baseDamage = ed.BaseDamage;
            attackSpeedBonus = ed.AttackSpeedBonus;
            rangeBonus = ed.RangeBonus;
            
            aiMode = ed.AiDriver;
            aiConfig = ed.AiConfig;

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
            RoomBullets = RoomBullets, // Inject RoomBullets
            Active = true
        };
        
        enemy.Driver = AiDriverFactory.Create(aiMode);
        enemy.Driver.Initialize(aiConfig, enemy, enemy);

        enemy.OnUseAbility = (e, id, dir) => OnEnemyUseAbility?.Invoke(e, id, dir);
        _enemies[enemy.Id] = enemy;
        OnEnemySpawned?.Invoke(enemy);
    }

    public void Update(float dt, Dictionary<int, ServerPlayer> players, WorldManager worldManager, ServerAbilityManager abilityManager)
    {
        foreach (var enemy in _enemies.Values.ToList()) 
        {
            if (enemy.Active)
            {
                enemy.AbilityManager = abilityManager; // Ensure AbilityManager is set
                enemy.Update(dt, players, worldManager, abilityManager);
            }
        }
    }

    public void HandleDamage(int enemyId, int damage, IEntity? source = null)
    {
        if (_enemies.TryGetValue(enemyId, out var enemy) && enemy.Active)
        {
            enemy.TakeDamage(damage, source);
            if (enemy.CurrentHealth <= 0)
            {
                enemy.Active = false; // Manager disables the entity now
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
