using Godot;
using System;

public partial class HUD : CanvasLayer
{
    private Label _statsLabel = null!;
    private ProgressBar _hpBar = null!;

    public override void _Ready()
    {
        var panel = new Panel();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.Size = new Godot.Vector2(200, 100);
        panel.Position = new Godot.Vector2(10, 10);
        AddChild(panel);

        _statsLabel = new Label();
        _statsLabel.Position = new Godot.Vector2(10, 10);
        panel.AddChild(_statsLabel);

        _hpBar = new ProgressBar();
        _hpBar.Position = new Godot.Vector2(10, 70);
        _hpBar.Size = new Godot.Vector2(180, 20);
        _hpBar.ShowPercentage = true;
        panel.AddChild(_hpBar);
    }

    public void UpdateStatus(int health, int maxHealth, int level)
    {
        _statsLabel.Text = $"Level: {level}\nHP: {health}/{maxHealth}";
        _hpBar.MaxValue = maxHealth;
        _hpBar.Value = health;
    }
}