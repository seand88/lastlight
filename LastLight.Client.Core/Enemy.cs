using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common.Abilities;

namespace LastLight.Client.Core;

public class Enemy : LastLight.Common.Abilities.IEntity
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }

    // IEntity implementation
    LastLight.Common.Vector2 LastLight.Common.Abilities.IEntity.Position { 
        get => new LastLight.Common.Vector2(Position.X, Position.Y); 
        set => Position = new Microsoft.Xna.Framework.Vector2(value.X, value.Y); 
    }

    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public bool Active { get; set; }

    public int BaseDamage => 10;
    public float AttackSpeedBonus => 0f;
    public float RangeBonus => 0f;

    public void TakeDamage(int amount, IEntity? source)
    {
        CurrentHealth -= amount;
        if (CurrentHealth < 0) CurrentHealth = 0;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D atlas, Texture2D pixel)
    {
        if (!Active) return;

        // Source rectangle for enemy in atlas (32, 0, 32, 32)
        var sourceRect = new Rectangle(32, 0, 32, 32);
        var destRect = new Rectangle((int)Position.X - 16, (int)Position.Y - 16, 32, 32);

        spriteBatch.Draw(atlas, destRect, sourceRect, Color.White);

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
