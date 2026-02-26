using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LastLight.Client.Core;

public class Boss
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public byte Phase { get; set; }
    public bool Active { get; set; }

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