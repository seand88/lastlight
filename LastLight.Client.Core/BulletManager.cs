using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LastLight.Client.Core;

public class Bullet
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool Active { get; set; }
    public float LifeTime { get; set; }

    public void Update(GameTime gameTime)
    {
        if (!Active) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * dt;
        LifeTime -= dt;
        if (LifeTime <= 0) Active = false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!Active) return;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 4, (int)Position.Y - 4, 8, 8), Color.Yellow);
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

    public Bullet? Spawn(int id, int ownerId, Vector2 pos, Vector2 vel, float lifeTime = 5.0f)
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

    public void Update(GameTime gameTime)
    {
        foreach (var bullet in _bullets)
        {
            bullet.Update(gameTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var bullet in _bullets)
        {
            bullet.Draw(spriteBatch, pixel);
        }
    }

    public void Destroy(int bulletId)
    {
        foreach (var bullet in _bullets)
        {
            if (bullet.Active && bullet.Id == bulletId)
            {
                bullet.Active = false;
                break;
            }
        }
    }
}
