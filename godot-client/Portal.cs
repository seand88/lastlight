using Godot;
using System;

public partial class Portal : Sprite2D
{
    public int PortalId { get; set; }
    public int TargetRoomId { get; set; }
    public string PortalName { get; set; } = "";

    public override void _Ready()
    {
        // Portal texture at (64, 128, 32, 32)
        Texture = TextureManager.GetTexture(new Rect2(64, 128, 32, 32));
        
        var label = new Label();
        label.Text = PortalName;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = new Godot.Vector2(-50, -40);
        label.Size = new Godot.Vector2(100, 20);
        AddChild(label);

        // Tint based on name
        if (PortalName.Contains("Forest")) SelfModulate = Colors.LimeGreen;
        else if (PortalName.Contains("Nexus")) SelfModulate = Colors.Cyan;
        else SelfModulate = Colors.MediumPurple;
    }
}
