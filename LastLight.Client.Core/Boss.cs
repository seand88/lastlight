using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common.Abilities;

namespace LastLight.Client.Core;

public class Boss : LastLight.Common.Abilities.IEntity
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
    public byte Phase { get; set; }
    public bool Active { get; set; }

    public int BaseDamage => 25;
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

        // Boss is 128x128. Atlas source at (128, 0, 128, 128)
        var sourceRect = new Rectangle(128, 0, 128, 128);
        var destRect = new Rectangle((int)Position.X - 64, (int)Position.Y - 64, 128, 128);

        // Flash red if Phase 3
        Color color = Phase == 3 ? (Math.Sin(Game1.TotalTime * 10) > 0 ? Color.Red : Color.White) : Color.White;
        spriteBatch.Draw(atlas, destRect, sourceRect, color);
    }
}