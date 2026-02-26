using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class BossManager
{
    private readonly Dictionary<int, Boss> _bosses = new();

    public void HandleSpawn(BossSpawn spawn)
    {
        var boss = new Boss
        {
            Id = spawn.BossId,
            Position = new Microsoft.Xna.Framework.Vector2(spawn.Position.X, spawn.Position.Y),
            MaxHealth = spawn.MaxHealth,
            CurrentHealth = spawn.MaxHealth,
            Active = true,
            Phase = 1
        };
        _bosses[spawn.BossId] = boss;
    }

    public void HandleUpdate(BossUpdate update)
    {
        if (_bosses.TryGetValue(update.BossId, out var boss))
        {
            boss.Position = new Microsoft.Xna.Framework.Vector2(update.Position.X, update.Position.Y);
            boss.CurrentHealth = update.CurrentHealth;
            boss.Phase = update.Phase;
        }
    }

    public void HandleDeath(BossDeath death)
    {
        if (_bosses.TryGetValue(death.BossId, out var boss))
        {
            boss.Active = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D atlas, Texture2D pixel)
    {
        foreach (var boss in _bosses.Values)
        {
            boss.Draw(spriteBatch, atlas, pixel);
        }
    }

    public IEnumerable<Boss> GetActiveBosses()
    {
        foreach(var b in _bosses.Values) if(b.Active) yield return b;
    }
}
