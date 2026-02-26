using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;

namespace LastLight.Server;

public class ServerEnemy
{
    public int Id { get; set; }
    public Vector2 Position;
    public Vector2 Velocity;
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public bool Active { get; set; }
    public float Speed { get; set; } = 100f;
    
    public void Update(float dt, Dictionary<int, AuthoritativePlayerUpdate> players)
    {
        if (!Active) return;

        // Simple AI: Find nearest player and move towards them
        AuthoritativePlayerUpdate? nearestPlayer = null;
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
        }
        else
        {
             Velocity = new Vector2(0, 0);
        }

        Position.X += Velocity.X * dt;
        Position.Y += Velocity.Y * dt;
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
    private int _nextEnemyId = 1;
    private Random _random = new();

    public Action<ServerEnemy>? OnEnemySpawned;
    public Action<ServerEnemy>? OnEnemyDied;

    public void SpawnEnemy(Vector2 position, int maxHealth = 100)
    {
        var enemy = new ServerEnemy
        {
            Id = _nextEnemyId++,
            Position = position,
            MaxHealth = maxHealth,
            CurrentHealth = maxHealth,
            Active = true
        };
        _enemies[enemy.Id] = enemy;
        OnEnemySpawned?.Invoke(enemy);
    }

    public void Update(float dt, Dictionary<int, AuthoritativePlayerUpdate> players)
    {
        foreach (var enemy in _enemies.Values.ToList()) // ToList to avoid modification during iteration if we remove later, though we just set Active=false
        {
            if (enemy.Active)
            {
                enemy.Update(dt, players);
                
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
