using Godot;
using System;

public partial class Bullet : Sprite2D
{
    public int BulletId { get; set; }
    public int OwnerId { get; set; }
    public Godot.Vector2 Velocity { get; set; }

    public override void _Ready()
    {
        Texture = GD.Load<Texture2D>("res://bullet.png");
    }

    public override void _Process(double delta)
    {
        GlobalPosition += Velocity * (float)delta;

        // Simple cleanup if it goes too far (though server should handle death)
        // But for now, let's just let it fly or die on signal
    }
}
