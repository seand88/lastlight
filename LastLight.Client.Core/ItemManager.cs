using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class ItemManager
{
    private readonly Dictionary<int, Item> _items = new();

    public void HandleSpawn(ItemSpawn spawn)
    {
        var item = new Item
        {
            Id = spawn.ItemId,
            Info = spawn.Item,
            Position = new Microsoft.Xna.Framework.Vector2(spawn.Position.X, spawn.Position.Y),
            Active = true
        };
        _items[spawn.ItemId] = item;
    }

    public void HandlePickup(ItemPickup pickup)
    {
        if (_items.TryGetValue(pickup.ItemId, out var item))
        {
            item.Active = false;
            // Remove from list or just keep inactive
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D atlas)
    {
        foreach (var item in _items.Values)
        {
            item.Draw(spriteBatch, atlas);
        }
    }
}
