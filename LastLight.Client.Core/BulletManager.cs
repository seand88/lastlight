using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class Bullet
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string AbilityId { get; set; } = "";
    public Microsoft.Xna.Framework.Vector2 Position { get; set; }
    public Microsoft.Xna.Framework.Vector2 Velocity { get; set; }
    public bool Active { get; set; }
    public float LifeTime { get; set; }
    public Color Color { get; set; } = Color.Yellow;
    public float Size { get; set; } = 8f;

    public void Update(GameTime gameTime, WorldManager world, EnemyManager enemies, BossManager bosses, SpawnerManager spawners, ParticleManager particles)
    {
        if (!Active) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * dt;
        
        // Local wall collision prediction
        if (world != null && !world.IsShootable(new LastLight.Common.Vector2(Position.X, Position.Y)))
        {
            Active = false;
            particles.SpawnBurst(Position, 3, Color.Gray, 50f, 0.2f, 2f);
            return;
        }

        // Local Entity Collision Prediction (Hides the bullet instantly to fix "passing through" visual bug)
        if (OwnerId >= 0) { // If it's a player's bullet
            foreach(var e in enemies.GetAllEnemies()) {
                if (e.Active && System.Math.Abs(Position.X - e.Position.X) < 20 && System.Math.Abs(Position.Y - e.Position.Y) < 20) { 
                    Active = false; 
                    particles.SpawnBurst(Position, 5, Color.Yellow, 80f, 0.3f, 3f);
                    return; 
                }
            }
            foreach(var s in spawners.GetAllSpawners()) {
                if (s.Active && System.Math.Abs(Position.X - s.Position.X) < 36 && System.Math.Abs(Position.Y - s.Position.Y) < 36) { 
                    Active = false; 
                    particles.SpawnBurst(Position, 5, Color.Purple, 80f, 0.3f, 3f);
                    return; 
                }
            }
            foreach(var b in bosses.GetActiveBosses()) {
                if (b.Active && System.Math.Abs(Position.X - b.Position.X) < 68 && System.Math.Abs(Position.Y - b.Position.Y) < 68) { 
                    Active = false; 
                    particles.SpawnBurst(Position, 8, Color.DarkSlateBlue, 100f, 0.4f, 4f);
                    return; 
                }
            }
        }

        LifeTime -= dt;
        if (LifeTime <= 0) Active = false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!Active) return;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - (int)(Size/2), (int)Position.Y - (int)(Size/2), (int)Size, (int)Size), Color);
    }
}

public class BulletManager
{
    private readonly List<Bullet> _bullets = new();
    private const int MaxBullets = 2000;

    public BulletManager()
    {
        for (int i = 0; i < MaxBullets; i++)
        {
            _bullets.Add(new Bullet { Active = false });
        }
    }

    public Bullet? Spawn(int id, int ownerId, Microsoft.Xna.Framework.Vector2 pos, Microsoft.Xna.Framework.Vector2 vel, float lifeTime = 5.0f, string abilityId = "basic_attack")
    {
        foreach (var bullet in _bullets)
        {
            if (!bullet.Active)
            {
                bullet.Id = id;
                bullet.OwnerId = ownerId;
                bullet.AbilityId = abilityId;
                bullet.Position = pos;
                bullet.Velocity = vel;
                bullet.Active = true;
                bullet.LifeTime = lifeTime;

                if (GameDataManager.Abilities.TryGetValue(abilityId, out var spec) && spec.Delivery is LastLight.Common.Abilities.ProjectileDelivery proj)
                {
                    bullet.Color = ParseColor(proj.Color);
                    bullet.Size = proj.Width;
                }
                else
                {
                    bullet.Color = ownerId < 0 ? Color.Pink : Color.Yellow;
                    bullet.Size = 8f;
                }

                return bullet;
            }
        }
        return null;
    }

    private Color ParseColor(string rgb)
    {
        try {
            var parts = rgb.Split(',');
            if (parts.Length == 3) return new Color(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
        } catch { }
        return Color.White;
    }

    public void Update(GameTime gameTime, WorldManager world, EnemyManager enemies, BossManager bosses, SpawnerManager spawners, ParticleManager particles)
    {
        foreach (var bullet in _bullets)
        {
            bullet.Update(gameTime, world, enemies, bosses, spawners, particles);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var bullet in _bullets)
        {
            bullet.Draw(spriteBatch, pixel);
        }
    }

    public int Destroy(int bulletId, ParticleManager? particles = null)
    {
        foreach (var bullet in _bullets)
        {
            if (bullet.Active && bullet.Id == bulletId)
            {
                bullet.Active = false;
                if (particles != null) particles.SpawnBurst(bullet.Position, 5, Color.Yellow, 80f, 0.3f, 3f);
                return bullet.OwnerId;
            }
        }
        return -1;
    }
}