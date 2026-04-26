using Godot;
using System;

public partial class Enemy : Node2D
{
    public int EnemyId { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }

    public void UpdateState(Godot.Vector2 position, int health)
    {
        GlobalPosition = position;
        CurrentHealth = health;
    }
}
