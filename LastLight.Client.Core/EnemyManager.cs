using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class EnemyManager
{
    private readonly Dictionary<int, Enemy> _enemies = new();

    public void HandleSpawn(EnemySpawn spawn)
    {
        var enemy = new Enemy
        {
            Id = spawn.EnemyId,
            Position = new Microsoft.Xna.Framework.Vector2(spawn.Position.X, spawn.Position.Y),
            MaxHealth = spawn.MaxHealth,
            CurrentHealth = spawn.MaxHealth, // Assume full health on spawn
            Active = true
        };
        _enemies[spawn.EnemyId] = enemy;
    }

    public void HandleUpdate(EnemyUpdate update)
    {
        if (_enemies.TryGetValue(update.EnemyId, out var enemy))
        {
            // Simple snapping for now, could interpolate later
            enemy.Position = new Microsoft.Xna.Framework.Vector2(update.Position.X, update.Position.Y);
            enemy.CurrentHealth = update.CurrentHealth;
        }
    }

    public void HandleDeath(EnemyDeath death)
    {
        if (_enemies.TryGetValue(death.EnemyId, out var enemy))
        {
            enemy.Active = false;
            // Optionally remove from dictionary, or keep if we want respawns or death animations
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D atlas, Texture2D pixel)
    {
        foreach (var enemy in _enemies.Values)
        {
            enemy.Draw(spriteBatch, atlas, pixel);
        }
    }
}
