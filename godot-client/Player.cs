using Godot;
using System;
using System.Collections.Generic;
using LastLight.Common;

public partial class Player : CharacterBody2D
{
	[Export] public int PlayerId { get; set; }
	[Export] public bool IsLocal { get; set; }

	public int SpeedStat { get; set; } = 10;
	public List<InputRequest> PendingInputs = new();

	private Sprite2D _sprite = null!;
	private Label _nameLabel = null!;

		public override void _Ready()
		{
			_sprite = GetNode<Sprite2D>("Sprite2D");
			_nameLabel = GetNode<Label>("NameLabel");
			
			_nameLabel.Text = IsLocal ? "You" : $"Player {PlayerId}";
	
			if (IsLocal)
			{
				var camera = GetNode<Camera2D>("Camera2D");
				camera.Enabled = true;
			}
		}
		public void ApplyInput(InputRequest input, WorldManager world)
	{
		float speed = 100f + (SpeedStat * 5f);
		Godot.Vector2 vel = new Godot.Vector2(input.Movement.X, input.Movement.Y) * speed;
		
		var newPos = GlobalPosition;
		newPos.X += vel.X * input.DeltaTime;
		
		// In Godot, you can just do this or use collision. We mimic MonoGame for perfect sync.
		if (world != null && !world.IsWalkable(new LastLight.Common.Vector2(newPos.X, newPos.Y)))
		{
			newPos.X = GlobalPosition.X;
		}

		newPos.Y += vel.Y * input.DeltaTime;
		if (world != null && !world.IsWalkable(new LastLight.Common.Vector2(newPos.X, newPos.Y)))
		{
			newPos.Y = GlobalPosition.Y;
		}

		GlobalPosition = newPos;
	}

	public void UpdateState(Godot.Vector2 position, Godot.Vector2 velocity)
	{
		if (IsLocal)
		{
			// Server reconciliation happens in Main.cs via PendingInputs
			GlobalPosition = position;
		}
		else
		{
			GlobalPosition = position;
			Velocity = velocity;
		}
	}

	public override void _Process(double delta)
	{
		if (!IsLocal)
		{
			// Simple dead reckoning for remote players
			GlobalPosition += Velocity * (float)delta;
		}
	}
}
