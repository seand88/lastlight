using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class Item
{
    public int Id { get; set; }
    public ItemInfo Info { get; set; }
    public Microsoft.Xna.Framework.Vector2 Position { get; set; }
    public bool Active { get; set; }

    public void Draw(SpriteBatch spriteBatch, Texture2D atlas)
    {
        if (!Active) return;

        // Source rectangle based on Icon if available, else Category
        Rectangle sourceRect;
        if (!string.IsNullOrEmpty(Info.Icon) && Game1.IconRegions.TryGetValue(Info.Icon, out var region)) {
            sourceRect = region;
        } else {
            sourceRect = Info.Category switch {
                ItemCategory.Consumable => new Rectangle(8, 40, 16, 20),
                ItemCategory.Weapon => new Rectangle(40, 40, 16, 16),
                _ => new Rectangle(8, 40, 16, 20)
            };
        }
        
        var destRect = new Rectangle((int)Position.X - 16, (int)Position.Y - 16, 32, 32);

        spriteBatch.Draw(atlas, destRect, sourceRect, Color.White);
    }
}
