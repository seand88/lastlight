using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LastLight.Client.Core;

public class Enemy
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public bool Active { get; set; }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!Active) return;

        // Draw a simple green square for enemy
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 16, (int)Position.Y - 16, 32, 32), Color.Green);

        // Draw health bar
        if (MaxHealth > 0 && CurrentHealth < MaxHealth)
        {
            float healthPercent = (float)CurrentHealth / MaxHealth;
            int healthBarWidth = 32;
            int currentHealthWidth = (int)(healthBarWidth * healthPercent);
            
            // Background (red)
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 16, (int)Position.Y - 24, healthBarWidth, 4), Color.Red);
            // Foreground (green)
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 16, (int)Position.Y - 24, currentHealthWidth, 4), Color.LimeGreen);
        }
    }
}
