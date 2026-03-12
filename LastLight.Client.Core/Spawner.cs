using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LastLight.Client.Core;

public class Spawner
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public bool Active { get; set; }

    public void Draw(SpriteBatch spriteBatch, Texture2D atlas, Texture2D pixel)
    {
        if (!Active) return;

        var tex = Game1.AssetManager.GetAtlasTexture("Environment");
        var sourceRect = Game1.AssetManager.GetIconSourceRect("Environment", "spawner");
        var destRect = new Rectangle((int)Position.X - 32, (int)Position.Y - 32, 64, 64);

        spriteBatch.Draw(tex, destRect, sourceRect, Color.White);

        // Draw health bar
        if (MaxHealth > 0 && CurrentHealth < MaxHealth)
        {
            float healthPercent = (float)CurrentHealth / MaxHealth;
            int healthBarWidth = 64;
            int currentHealthWidth = (int)(healthBarWidth * healthPercent);
            
            // Background (red)
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 32, (int)Position.Y - 40, healthBarWidth, 6), Color.Red);
            // Foreground (green)
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 32, (int)Position.Y - 40, currentHealthWidth, 6), Color.LimeGreen);
        }
    }
}
