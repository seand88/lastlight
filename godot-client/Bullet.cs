using Godot;
using System;

public partial class Bullet : Sprite2D
{
    public int BulletId { get; set; }
    public int OwnerId { get; set; }
    public Godot.Vector2 Velocity { get; set; }

    public override void _Ready()
    {
        // Use a simple white pixel/small rect for bullets
        // TextureManager could provide a bullet texture too
        Texture = TextureManager.GetTexture(new Rect2(128, 128, 2, 2)); // Placeholder white pixel
        Scale = new Godot.Vector2(4, 4);
    }

    public override void _Process(double delta)
    {
        GlobalPosition += Velocity * (float)delta;

        // Simple cleanup if it goes too far (though server should handle death)
        // But for now, let's just let it fly or die on signal
    }
}
