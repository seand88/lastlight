using LiteNetLib.Utils;

namespace LastLight.Common;

public struct Vector2
{
    public float X;
    public float Y;
    public Vector2(float x, float y) { X = x; Y = y; }
}

public class JoinRequest { public string PlayerName { get; set; } = string.Empty; }

public enum WeaponType : byte { Single, Double, Spread, Rapid }

public enum ItemCategory : byte { Weapon, Armor, Accessory, Ability, Consumable }

public struct ItemInfo : INetSerializable {
    public int ItemId { get; set; }
    public ItemCategory Category { get; set; }
    public string Name { get; set; }
    public int StatBonus { get; set; }
    public WeaponType WeaponType { get; set; } // If it's a weapon
    
    public void Serialize(NetDataWriter writer) { 
        writer.Put(ItemId); writer.Put((byte)Category); writer.Put(Name ?? ""); writer.Put(StatBonus); writer.Put((byte)WeaponType); 
    }
    public void Deserialize(NetDataReader reader) { 
        ItemId = reader.GetInt(); Category = (ItemCategory)reader.GetByte(); Name = reader.GetString(); StatBonus = reader.GetInt(); WeaponType = (WeaponType)reader.GetByte(); 
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
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[4];
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Dexterity { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }
}

public class AuthoritativePlayerUpdate {
    public int PlayerId { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public int LastProcessedInputSequence { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int RoomId { get; set; }
    public ItemInfo[] Inventory { get; set; } = new ItemInfo[8];
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[4];
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Dexterity { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }
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
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
}

public class PortalSpawn {
    public int PortalId { get; set; }
    public Vector2 Position { get; set; }
    public int TargetRoomId { get; set; }
    public string Name { get; set; } = "Portal";
}

public class PortalUseRequest { public int PortalId { get; set; } }
public class PortalDeath { public int PortalId { get; set; } }

public enum EntityType : byte { Player, Enemy, Spawner, Boss, Portal }

public class BulletHit {
    public int BulletId { get; set; }
    public int TargetId { get; set; }
    public EntityType TargetType { get; set; }
}

public class BossSpawn {
    public int BossId { get; set; }
    public Vector2 Position { get; set; }
    public int MaxHealth { get; set; }
}

public class BossUpdate {
    public int BossId { get; set; }
    public Vector2 Position { get; set; }
    public int CurrentHealth { get; set; }
    public byte Phase { get; set; }
}

public class BossDeath { public int BossId { get; set; } }

public class EnemySpawn {
    public int EnemyId { get; set; }
    public Vector2 Position { get; set; }
    public int MaxHealth { get; set; }
}

public class EnemyUpdate {
    public int EnemyId { get; set; }
    public Vector2 Position { get; set; }
    public int CurrentHealth { get; set; }
}

public class EnemyDeath { public int EnemyId { get; set; } }

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
    public int Seed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int TileSize { get; set; }
    public WorldManager.GenerationStyle Style { get; set; }
}

public struct LeaderboardEntry : INetSerializable {
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int Score { get; set; }
    public void Serialize(NetDataWriter writer) { writer.Put(PlayerId); writer.Put(PlayerName ?? "Guest"); writer.Put(Score); }
    public void Deserialize(NetDataReader reader) { PlayerId = reader.GetInt(); PlayerName = reader.GetString(); Score = reader.GetInt(); }
}

public class LeaderboardUpdate {
    public LeaderboardEntry[] Entries { get; set; } = new LeaderboardEntry[0];
}

public class SwapItemRequest {
    public int FromIndex { get; set; } // 0-3 Equipment, 4-11 Inventory
    public int ToIndex { get; set; }
}
