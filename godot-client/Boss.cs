using Godot;
using System;

public partial class Boss : Node2D
{
    public int BossId { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }
    public int Phase { get; set; }

    public void UpdateState(Godot.Vector2 position, int health, int phase)
    {
        GlobalPosition = position;
        CurrentHealth = health;
        Phase = phase;
    }
}
