using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class SpawnerManager
{
    private readonly Dictionary<int, Spawner> _spawners = new();

    public void HandleSpawn(SpawnerSpawn spawn)
    {
        var spawner = new Spawner
        {
            Id = spawn.SpawnerId,
            Position = new Microsoft.Xna.Framework.Vector2(spawn.Position.X, spawn.Position.Y),
            MaxHealth = spawn.MaxHealth,
            CurrentHealth = spawn.MaxHealth, // Assume full health on spawn
            Active = true
        };
        _spawners[spawn.SpawnerId] = spawner;
    }

    public void HandleUpdate(SpawnerUpdate update)
    {
        if (_spawners.TryGetValue(update.SpawnerId, out var spawner))
        {
            spawner.CurrentHealth = update.CurrentHealth;
        }
    }

    public void HandleDeath(SpawnerDeath death, ParticleManager? particles = null)
    {
        if (_spawners.TryGetValue(death.SpawnerId, out var spawner))
        {
            if (spawner.Active && particles != null)
            {
                particles.SpawnBurst(spawner.Position, 25, Color.Purple, 150f, 0.8f, 8f);
            }
            spawner.Active = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D atlas, Texture2D pixel)
    {
        foreach (var spawner in _spawners.Values)
        {
            spawner.Draw(spriteBatch, atlas, pixel);
        }
    }

    public IEnumerable<Spawner> GetAllSpawners() => _spawners.Values;
}
