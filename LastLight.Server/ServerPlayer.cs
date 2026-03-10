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
    public int RoomId { get; set; }
    public ItemInfo[] Inventory { get; set; } = new ItemInfo[8];
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[3];
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Dexterity { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }
    public int LastProcessedInputSequence { get; set; }

    public AuthoritativePlayerUpdate ToPacket()
    {
        return new AuthoritativePlayerUpdate
        {
            PlayerId = Id,
            Position = Position,
            Velocity = Velocity,
            LastProcessedInputSequence = LastProcessedInputSequence,
            CurrentHealth = CurrentHealth,
            MaxHealth = MaxHealth,
            Level = Level,
            RoomId = RoomId
        };
    }

    public SelfStateUpdate ToSelfPacket()
    {
        return new SelfStateUpdate
        {
            CurrentMana = CurrentMana,
            MaxMana = MaxMana,
            Experience = Experience,
            Attack = Attack,
            Defense = Defense,
            Speed = Speed,
            Dexterity = Dexterity,
            Vitality = Vitality,
            Wisdom = Wisdom,
            Inventory = Inventory,
            Equipment = Equipment
        };
    }
}
