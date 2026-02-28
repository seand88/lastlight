using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LastLight.Client.Core;

public class ParticleManager
{
    private readonly List<Particle> _particles = new();
    private readonly Random _random = new();
    private const int MaxParticles = 1000;

    public ParticleManager()
    {
        for (int i = 0; i < MaxParticles; i++)
        {
            _particles.Add(new Particle { Active = false });
        }
    }

    private Particle? GetFreeParticle()
    {
        foreach (var p in _particles)
        {
            if (!p.Active) return p;
        }
        return null;
    }

    public void SpawnBurst(Vector2 position, int count, Color color, float speed = 100f, float life = 0.5f, float scale = 4f)
    {
        for (int i = 0; i < count; i++)
        {
            var p = GetFreeParticle();
            if (p == null) break;

            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float s = (float)(_random.NextDouble() * speed);
            
            p.Position = position;
            p.Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * s;
            p.Color = color;
            p.MaxLifeTime = life;
            p.LifeTime = life;
            p.Scale = (float)(_random.NextDouble() * scale + scale * 0.5f);
            p.Active = true;
        }
    }

    public void Update(float dt)
    {
        foreach (var p in _particles)
        {
            p.Update(dt);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var p in _particles)
        {
            p.Draw(spriteBatch, pixel);
        }
    }
}
