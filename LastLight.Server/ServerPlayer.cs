using LastLight.Common;
using LastLight.Common.Abilities;

namespace LastLight.Server;

public class ServerPlayer : IEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    private int _currentMana;
    public int CurrentMana { 
        get => _currentMana; 
        set => _currentMana = Math.Clamp(value, 0, 100); 
    }
    public int MaxMana { get; set; } = 100;
    public int Level { get; set; }
    public int Experience { get; set; }
    public int RunGold { get; set; }
    public int RoomId { get; set; }
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[5];
    public ItemInfo[] Toolbelt { get; set; } = new ItemInfo[8];
    public ItemInfo[] Stash { get; set; } = new ItemInfo[50];
    public ItemInfo[] DungeonLoot { get; set; } = new ItemInfo[50];
    public int ToolbeltSize { get; set; } = 3;

    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Dexterity { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }

    // IEntity implementation
    public int BaseDamage {
        get {
            var weapon = Equipment[0];
            if (!string.IsNullOrEmpty(weapon.DataId) && GameDataManager.Items.TryGetValue(weapon.DataId, out var data)) {
                return data.GetInt(weapon.CurrentTier, "base_damage");
            }
            return 0; // No weapon equipped
        }
    }

    public float AttackSpeedBonus {
        get {
            var weapon = Equipment[0];
            if (!string.IsNullOrEmpty(weapon.DataId) && GameDataManager.Items.TryGetValue(weapon.DataId, out var data)) {
                return data.GetFloat(weapon.CurrentTier, "attack_speed_mod");
            }
            return 0f;
        }
    }

    public float RangeBonus {
        get {
            var weapon = Equipment[0];
            if (!string.IsNullOrEmpty(weapon.DataId) && GameDataManager.Items.TryGetValue(weapon.DataId, out var data)) {
                return data.GetFloat(weapon.CurrentTier, "range_bonus");
            }
            return 0f;
        }
    }

    public int LastProcessedInputSequence { get; set; }

    public void TakeDamage(int amount, IEntity? source)
    {
        CurrentHealth -= amount;
        if (CurrentHealth < 0) CurrentHealth = 0;
    }

    public PlayerUpdate ToUpdatePacket()
    {
        return new PlayerUpdate
        {
            PlayerId = Id,
            Position = Position,
            Velocity = Velocity,
            LastProcessedInputSequence = LastProcessedInputSequence,
            CurrentHealth = CurrentHealth
        };
    }

    public PlayerSpawn ToSpawnPacket()
    {
        return new PlayerSpawn
        {
            PlayerId = Id,
            Username = Username,
            Position = Position,
            MaxHealth = MaxHealth,
            Level = Level,
            Equipment = Equipment
        };
    }

    public SelfStateUpdate ToSelfPacket() {
        return new SelfStateUpdate
        {
            CurrentMana = CurrentMana,
            MaxMana = MaxMana,
            Experience = Experience,
            RunGold = RunGold,
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Dexterity = Dexterity,
            Vitality = Vitality,
            Wisdom = Wisdom
        };
    }
}
