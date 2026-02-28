using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LastLight.Client.Core;

public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color Color;
    public float LifeTime;
    public float MaxLifeTime;
    public float Scale;
    public bool Active;

    public void Update(float dt)
    {
        if (!Active) return;
        Position += Velocity * dt;
        LifeTime -= dt;
        if (LifeTime <= 0) Active = false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!Active) return;
        float alpha = LifeTime / MaxLifeTime;
        spriteBatch.Draw(pixel, Position, null, Color * alpha, 0f, new Vector2(0.5f, 0.5f), Scale, SpriteEffects.None, 0f);
    }
}
