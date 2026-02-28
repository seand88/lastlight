using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class Bullet
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public Microsoft.Xna.Framework.Vector2 Position { get; set; }
    public Microsoft.Xna.Framework.Vector2 Velocity { get; set; }
    public bool Active { get; set; }
    public float LifeTime { get; set; }

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
        var color = OwnerId < 0 ? Color.Pink : Color.Yellow;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 4, (int)Position.Y - 4, 8, 8), color);
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

    public Bullet? Spawn(int id, int ownerId, Microsoft.Xna.Framework.Vector2 pos, Microsoft.Xna.Framework.Vector2 vel, float lifeTime = 5.0f)
    {
        foreach (var bullet in _bullets)
        {
            if (!bullet.Active)
            {
                bullet.Id = id;
                bullet.OwnerId = ownerId;
                bullet.Position = pos;
                bullet.Velocity = vel;
                bullet.Active = true;
                bullet.LifeTime = lifeTime;
                return bullet;
            }
        }
        return null;
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

    public void Destroy(int bulletId, ParticleManager? particles = null)
    {
        foreach (var bullet in _bullets)
        {
            if (bullet.Active && bullet.Id == bulletId)
            {
                bullet.Active = false;
                if (particles != null) particles.SpawnBurst(bullet.Position, 5, Color.Yellow, 80f, 0.3f, 3f);
                break;
            }
        }
    }
}