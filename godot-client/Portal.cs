using Godot;
using System;

public partial class Portal : Sprite2D
{
    public int PortalId { get; set; }
    public int TargetRoomId { get; set; }
    public string PortalName { get; set; } = "";

    public override void _Ready()
    {
        Texture = GD.Load<Texture2D>("res://portal.png");
        
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
