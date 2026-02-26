using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Microsoft.Xna.Framework.Vector2 Position { get; set; }
    public Microsoft.Xna.Framework.Vector2 Velocity { get; set; }
    public bool IsLocal { get; set; }

    public List<InputRequest> PendingInputs = new();

    public void ApplyInput(InputRequest input, float speed)
    {
        Velocity = new Microsoft.Xna.Framework.Vector2(input.Movement.X, input.Movement.Y) * speed;
        Position += Velocity * input.DeltaTime;
    }

    public void Update(GameTime gameTime)
    {
        if (IsLocal)
        {
            // Local player updates are handled in Game1 via HandleInput
            return; 
        }

        // For remote players, we just apply velocity (simple dead reckoning)
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += Velocity * dt;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Draw a simple square for now
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 16, (int)Position.Y - 16, 32, 32), IsLocal ? Color.White : Color.Red);
    }
}
