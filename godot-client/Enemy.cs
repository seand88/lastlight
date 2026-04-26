using Godot;
using System;

public partial class Enemy : Sprite2D
{
    public int EnemyId { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }

    public override void _Ready()
    {
        // Enemy texture at (32, 0, 32, 32)
        Texture = TextureManager.GetTexture(new Rect2(32, 0, 32, 32));
    }

    public void UpdateState(Godot.Vector2 position, int health)
    {
        GlobalPosition = position;
        CurrentHealth = health;
    }
}
