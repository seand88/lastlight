using Godot;
using System;

public partial class Spawner : Node2D
{
    public int SpawnerId { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }

    public void UpdateState(int health)
    {
        CurrentHealth = health;
    }
}
