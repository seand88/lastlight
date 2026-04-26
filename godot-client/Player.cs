using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public int PlayerId { get; set; }
	[Export] public bool IsLocal { get; set; }

	private Sprite2D _sprite = null!;
	private Label _nameLabel = null!;

	public override void _Ready()
	{
		_sprite = new Sprite2D();
		AddChild(_sprite);

		_sprite.Texture = TextureManager.GetTexture(new Rect2(0, 0, 32, 32)); // Player is roughly in first 32x32
        // Actually, let's be more precise
        _sprite.Texture = TextureManager.GetTexture(new Rect2(0, 0, 32, 32));
        _sprite.RegionEnabled = true;
        _sprite.RegionRect = new Rect2(0, 0, 32, 32);

		_nameLabel = new Label();
		_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_nameLabel.Position = new Godot.Vector2(-50, -40);
		_nameLabel.Size = new Godot.Vector2(100, 20);
		_nameLabel.Text = IsLocal ? "You" : $"Player {PlayerId}";
		AddChild(_nameLabel);

		if (IsLocal)
		{
			var camera = new Camera2D();
			AddChild(camera);
		}
	}

	public void UpdateState(Godot.Vector2 position, Godot.Vector2 velocity)
	{
		if (IsLocal)
		{
			// Prediction/Interpolation would go here
			// For now, just snap to authoritative position if it's too far
			if (GlobalPosition.DistanceTo(position) > 50)
			{
				GlobalPosition = position;
			}
		}
		else
		{
			GlobalPosition = position;
			Velocity = velocity;
		}
	}
}
