using System.Collections.Generic;
using System.Linq;
using LastLight.Common;

namespace LastLight.Server;

public class ServerItem
{
    public int Id { get; set; }
    public ItemInfo Info { get; set; }
    public Vector2 Position { get; set; }
    public bool Active { get; set; }
}

public class ServerItemManager
{
    private readonly Dictionary<int, ServerItem> _items = new();
    private int _nextItemId = 1;

    public System.Action<ServerItem>? OnItemSpawned;
    public System.Action<ServerItem, int>? OnItemPickedUp;

    public void SpawnItem(ItemInfo info, Vector2 position)
    {
        var item = new ServerItem
        {
            Id = _nextItemId++,
            Info = info,
            Position = position,
            Active = true
        };
        _items[item.Id] = item;
        OnItemSpawned?.Invoke(item);
    }

    public void Update(Dictionary<int, AuthoritativePlayerUpdate> players)
    {
        foreach (var item in _items.Values.ToList())
        {
            if (!item.Active) continue;

            foreach (var player in players.Values)
            {
                float dx = System.Math.Abs(item.Position.X - player.Position.X);
                float dy = System.Math.Abs(item.Position.Y - player.Position.Y);

                if (dx < 20 && dy < 20) // Collision check for pickup
                {
                    item.Active = false;
                    OnItemPickedUp?.Invoke(item, player.PlayerId);
                    break;
                }
            }
        }
    }

    public IReadOnlyCollection<ServerItem> GetActiveItems() => _items.Values.Where(i => i.Active).ToList();
}
