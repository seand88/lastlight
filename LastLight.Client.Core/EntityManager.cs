using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class EntityManager
{
    private readonly Dictionary<int, ClientEntity> _entities = new();

    public void HandleSpawn(EntitySpawn spawn)
    {
        var entity = new ClientEntity
        {
            Id = spawn.EntityId,
            DataId = spawn.DataId,
            Position = new Microsoft.Xna.Framework.Vector2(spawn.Position.X, spawn.Position.Y),
            MaxHealth = spawn.MaxHealth,
            CurrentHealth = spawn.MaxHealth,
            Active = true
        };
        _entities[spawn.EntityId] = entity;
    }

    public void HandleUpdate(EntityUpdate update)
    {
        if (_entities.TryGetValue(update.EntityId, out var entity))
        {
            entity.Position = new Microsoft.Xna.Framework.Vector2(update.Position.X, update.Position.Y);
            entity.CurrentHealth = update.CurrentHealth;
            entity.Phase = update.Phase;
        }
    }

    public void HandleDeath(EntityDeath death, ParticleManager? particles = null)
    {
        if (_entities.TryGetValue(death.EntityId, out var entity))
        {
            if (entity.Active && particles != null)
            {
                // Play specific death effects based on type
                if (entity.EnemyType == "boss") {
                    particles.SpawnBurst(entity.Position, 100, Color.DarkSlateBlue, 250f, 1.5f, 12f);
                    particles.SpawnBurst(entity.Position, 50, Color.Yellow, 200f, 1.0f, 6f);
                } else {
                    particles.SpawnBurst(entity.Position, 15, new Color(139, 0, 0), 120f, 0.6f, 5f);
                }
            }
            entity.Active = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var entity in _entities.Values)
        {
            entity.Draw(spriteBatch, pixel);
        }
    }

    public IEnumerable<ClientEntity> GetAllEntities() => _entities.Values;
}
