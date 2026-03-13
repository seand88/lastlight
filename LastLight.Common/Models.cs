using System.Collections.Generic;
using System.Text.Json.Serialization;
using LiteNetLib.Utils;

namespace LastLight.Common;

public struct Vector2
{
    public float X;
    public float Y;
    public Vector2(float x, float y) { X = x; Y = y; }
}

public class JoinRequest { 
    public string Username { get; set; } = string.Empty; 
}

public enum WeaponType : byte { Single, Double, Spread, Rapid }

public enum ItemCategory : byte { Weapon, Armor, Ring, Consumable }

public struct ItemInfo : INetSerializable {
    public int ItemId { get; set; }
    public string DataId { get; set; } = string.Empty;
    public int CurrentTier { get; set; }
    public List<string> SelectedPerkIds { get; set; }

    public ItemInfo() {
        ItemId = 0;
        DataId = string.Empty;
        CurrentTier = 0;
        SelectedPerkIds = new List<string>();
    }

    [JsonIgnore]
    public ItemCategory Category => !string.IsNullOrEmpty(DataId) && GameDataManager.Items.TryGetValue(DataId, out var d) ? d.Category : ItemCategory.Consumable;
    [JsonIgnore]
    public string Name => !string.IsNullOrEmpty(DataId) && GameDataManager.Items.TryGetValue(DataId, out var d) ? d.Name : "Unknown";
    [JsonIgnore]
    public int StatBonus => !string.IsNullOrEmpty(DataId) && GameDataManager.Items.TryGetValue(DataId, out var d) ? d.StatBonus : 0;
    [JsonIgnore]
    public WeaponType WeaponType => !string.IsNullOrEmpty(DataId) && GameDataManager.Items.TryGetValue(DataId, out var d) ? d.WeaponType : WeaponType.Single;
    [JsonIgnore]
    public string Atlas => !string.IsNullOrEmpty(DataId) && GameDataManager.Items.TryGetValue(DataId, out var d) ? d.Atlas : "Items";
    [JsonIgnore]
    public string Icon => !string.IsNullOrEmpty(DataId) && GameDataManager.Items.TryGetValue(DataId, out var d) ? d.Icon : "";
    
    public void Serialize(NetDataWriter writer) { 
        writer.Put(ItemId); 
        writer.Put(DataId ?? "");
        writer.Put(CurrentTier);
        
        int count = SelectedPerkIds?.Count ?? 0;
        writer.Put(count);
        if (SelectedPerkIds != null) {
            for (int i = 0; i < count; i++) {
                writer.Put(SelectedPerkIds[i] ?? "");
            }
        }
    }
    public void Deserialize(NetDataReader reader) { 
        ItemId = reader.GetInt(); 
        DataId = reader.GetString();
        CurrentTier = reader.GetInt();
        
        int count = reader.GetInt();
        SelectedPerkIds = new List<string>(count);
        for (int i = 0; i < count; i++) {
            SelectedPerkIds.Add(reader.GetString());
        }
    }
}

public class JoinResponse {
    public bool Success { get; set; }
    public int PlayerId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MaxHealth { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public ItemInfo[] Inventory { get; set; } = new ItemInfo[8];
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[3];
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Dexterity { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }
}

public class PlayerUpdate {
    public int PlayerId { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public int LastProcessedInputSequence { get; set; }
    public int CurrentHealth { get; set; }
}

public class PlayerSpawn {
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Vector2 Position { get; set; }
    public int MaxHealth { get; set; }
    public int Level { get; set; }
}

public class PlayerLeave {
    public int PlayerId { get; set; }
}

public class SelfStateUpdate {
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public int Experience { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Dexterity { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }
    public ItemInfo[] Inventory { get; set; } = new ItemInfo[8];
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[3];
}

public class InventoryUpdate {
    public int SlotIndex { get; set; } // 0-2 Equipment, 3-10 Inventory
    public ItemInfo Item { get; set; }
}

public class InputRequest {
    public Vector2 Movement { get; set; }
    public float DeltaTime { get; set; }
    public int InputSequenceNumber { get; set; }
}

public class FireRequest {
    public int BulletId { get; set; }
    public Vector2 Direction { get; set; }
}

public class SpawnBullet {
    public int OwnerId { get; set; }
    public int BulletId { get; set; }
    public string AbilityId { get; set; } = string.Empty;
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float LifeTime { get; set; }
}

public class PortalSpawn {
    public int PortalId { get; set; }
    public Vector2 Position { get; set; }
    public int TargetRoomId { get; set; }
    public string Name { get; set; } = "Portal";
}

public class PortalUseRequest { public int PortalId { get; set; } }
public class PortalDeath { public int PortalId { get; set; } }

public enum EntityType : byte { Player, Entity, Spawner, Portal }

public class EntitySpawn {
    public int EntityId { get; set; }
    public string DataId { get; set; } = "";
    public Vector2 Position { get; set; }
    public int MaxHealth { get; set; }
}

public class EntityUpdate {
    public int EntityId { get; set; }
    public Vector2 Position { get; set; }
    public int CurrentHealth { get; set; }
    public byte Phase { get; set; }
}

public class EntityDeath {
    public int EntityId { get; set; }
}

public class BulletHit {
    public int BulletId { get; set; }
    public int TargetId { get; set; }
    public EntityType TargetType { get; set; }
}

public class AbilityUseRequest {
    public string AbilityId { get; set; } = string.Empty;
    public Vector2 Direction { get; set; }
    public Vector2 TargetPosition { get; set; }
    public int ClientInstanceId { get; set; }
}

public class EffectEvent {
    public string EffectName { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public int SourceId { get; set; }
    public int SourceProjectileId { get; set; }
    public float Value { get; set; }
    public float Duration { get; set; }
    public Vector2 Position { get; set; }
    public string TemplateId { get; set; } = string.Empty;
}

public class SpawnerSpawn {
    public int SpawnerId { get; set; }
    public Vector2 Position { get; set; }
    public int MaxHealth { get; set; }
}

public class SpawnerUpdate {
    public int SpawnerId { get; set; }
    public int CurrentHealth { get; set; }
}

public class SpawnerDeath { public int SpawnerId { get; set; } }

public enum ItemType : byte { HealthPotion, WeaponUpgrade }

public class ItemSpawn {
    public int ItemId { get; set; }
    public ItemInfo Item { get; set; }
    public Vector2 Position { get; set; }
}

public class ItemPickup {
    public int ItemId { get; set; }
    public int PlayerId { get; set; }
}

public enum TileType : byte { Grass, Water, Wall, Sand }

public class WorldInit {
    public int RoomId { get; set; }
    public int Seed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int TileSize { get; set; }
    public WorldManager.GenerationStyle Style { get; set; }
    public float CleanupTimer { get; set; } = -1f;
}

public class RoomStateUpdate {
    public float CleanupTimer { get; set; }
}

public struct LeaderboardEntry : INetSerializable {
    public int PlayerId { get; set; }
    public string Username { get; set; }
    public int Score { get; set; }
    public void Serialize(NetDataWriter writer) { writer.Put(PlayerId); writer.Put(Username ?? "Guest"); writer.Put(Score); }
    public void Deserialize(NetDataReader reader) { PlayerId = reader.GetInt(); Username = reader.GetString(); Score = reader.GetInt(); }
}

public class LeaderboardUpdate {
    public LeaderboardEntry[] Entries { get; set; } = new LeaderboardEntry[0];
}

public class SwapItemRequest {
    public int FromIndex { get; set; } // 0-3 Equipment, 4-11 Inventory
    public int ToIndex { get; set; }
}

public class UseItemRequest {
    public int SlotIndex { get; set; } // 0-3 Equipment, 4-11 Inventory
}
