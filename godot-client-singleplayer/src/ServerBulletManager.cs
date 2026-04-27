using System.Collections.Generic;
using LastLight.Common;

namespace LastLight.Server;

public class ServerBullet
{
    public int OwnerId { get; set; }
    public int BulletId { get; set; }
    public Vector2 Position;
    public Vector2 Velocity;
    public bool Active { get; set; }
    public float LifeTime { get; set; }

    public void Update(float dt)
    {
        if (!Active) return;

        Position.X += Velocity.X * dt;
        Position.Y += Velocity.Y * dt;
        LifeTime -= dt;
        if (LifeTime <= 0) Active = false;
    }
}

public class ServerBulletManager
{
    private readonly List<ServerBullet> _bullets = new();
    private const int MaxBullets = 2000;

    public ServerBulletManager()
    {
        for (int i = 0; i < MaxBullets; i++)
        {
            _bullets.Add(new ServerBullet { Active = false });
        }
    }

    public void Spawn(int bulletId, int ownerId, Vector2 pos, Vector2 vel, float lifeTime = 5.0f)
    {
        foreach (var bullet in _bullets)
        {
            if (!bullet.Active)
            {
                bullet.BulletId = bulletId;
                bullet.OwnerId = ownerId;
                bullet.Position = pos;
                bullet.Velocity = vel;
                bullet.Active = true;
                bullet.LifeTime = lifeTime;
                return;
            }
        }
    }

    public void Update(float dt)
    {
        foreach (var bullet in _bullets)
        {
            bullet.Update(dt);
        }
    }
    
    public IReadOnlyList<ServerBullet> GetActiveBullets()
    {
        var active = new List<ServerBullet>();
        foreach (var bullet in _bullets)
        {
            if (bullet.Active) active.Add(bullet);
        }
        return active;
    }
    
    public void DestroyBullet(ServerBullet bullet)
    {
        bullet.Active = false;
    }
}
