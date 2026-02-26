using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LastLight.Client.Core;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool IsLocal { get; set; }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * dt;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Draw a simple square for now
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 16, (int)Position.Y - 16, 32, 32), IsLocal ? Color.White : Color.Red);
    }
}
