using Godot;
using System;

public partial class Portal : Node2D
{
    public int PortalId { get; set; }
    public int TargetRoomId { get; set; }
    public string PortalName { get; set; } = "";

    public override void _Ready()
    {
        var label = GetNode<Label>("NameLabel");
        label.Text = PortalName;

        var sprite = GetNode<Sprite2D>("Sprite2D");
        // Tint based on name
        if (PortalName.Contains("Forest")) sprite.SelfModulate = Colors.LimeGreen;
        else if (PortalName.Contains("Nexus")) sprite.SelfModulate = Colors.Cyan;
        else sprite.SelfModulate = Colors.MediumPurple;
    }
}
