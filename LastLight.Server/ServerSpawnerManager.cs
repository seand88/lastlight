using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;

namespace LastLight.Server;

public class ServerSpawner
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public bool Active { get; set; }
    
    public int MaxEnemies { get; set; } = 5;
    public int SpawnedEnemies { get; set; } = 0;
    
    private float _spawnTimer = 0f;
    private float _spawnInterval = 3f; // Spawn an enemy every 3 seconds
    
    public Action<ServerSpawner, Vector2>? OnSpawnEnemy;

    public void Update(float dt)
    {
        if (!Active) return;

        if (SpawnedEnemies < MaxEnemies)
        {
            _spawnTimer += dt;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnedEnemies++;
                
                // Spawn nearby
                var spawnPos = new Vector2(Position.X + (new Random().Next(-50, 50)), Position.Y + (new Random().Next(-50, 50)));
                OnSpawnEnemy?.Invoke(this, spawnPos);
            }
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

public class ServerSpawnerManager
{
    private readonly Dictionary<int, ServerSpawner> _spawners = new();
    private int _nextSpawnerId = 1000; // Distinct ID range for spawners

    public Action<ServerSpawner>? OnSpawnerCreated;
    public Action<ServerSpawner>? OnSpawnerDied;
    public Action<Vector2, int>? OnRequestEnemySpawn;

    public void CreateSpawner(Vector2 position, int maxHealth = 200, int maxEnemies = 5)
    {
        var spawner = new ServerSpawner
        {
            Id = _nextSpawnerId++,
            Position = position,
            MaxHealth = maxHealth,
            CurrentHealth = maxHealth,
            Active = true,
            MaxEnemies = maxEnemies
        };
        
        spawner.OnSpawnEnemy = (s, pos) => OnRequestEnemySpawn?.Invoke(pos, s.Id);
        
        _spawners[spawner.Id] = spawner;
        OnSpawnerCreated?.Invoke(spawner);
    }

    public void NotifyEnemyDeath(int spawnerId)
    {
        if (_spawners.TryGetValue(spawnerId, out var spawner))
        {
            spawner.SpawnedEnemies--;
            if (spawner.SpawnedEnemies < 0) spawner.SpawnedEnemies = 0;
        }
    }

    public void Update(float dt)
    {
        foreach (var spawner in _spawners.Values.ToList())
        {
            if (spawner.Active)
            {
                spawner.Update(dt);
            }
        }
    }

    public void HandleDamage(int spawnerId, int damage)
    {
        if (_spawners.TryGetValue(spawnerId, out var spawner) && spawner.Active)
        {
            spawner.TakeDamage(damage);
            if (!spawner.Active)
            {
                OnSpawnerDied?.Invoke(spawner);
            }
        }
    }

    public IReadOnlyCollection<ServerSpawner> GetActiveSpawners()
    {
        return _spawners.Values.Where(s => s.Active).ToList();
    }
    
    public IReadOnlyCollection<ServerSpawner> GetAllSpawners()
    {
         return _spawners.Values;
    }
}
