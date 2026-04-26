using Godot;
using System;
using System.Collections.Generic;
using LastLight.Common;

public partial class Main : Node
{
	private Networking _networking = null!;
	private Node2D _entities = null!;
	private World _world = null!;
    private HUD _hud = null!;
	private Dictionary<int, Player> _players = new();
	private int _localPlayerId = -1;

	public override void _Ready()
	{
		GD.Print("LastLight Godot Client Starting...");
		
		// Load Game Data
		string dataPath = ProjectSettings.GlobalizePath("res://Data");
		GameDataManager.Load(dataPath);

		// Generate Atlas
		TextureManager.GenerateAtlas();

		// World
		_world = new World();
		_world.Name = "World";
		AddChild(_world);

        _hud = new HUD();
        _hud.Name = "HUD";
        AddChild(_hud);

		// Entities Container
		_entities = new Node2D();
		_entities.Name = "Entities";
		AddChild(_entities);

		// Create Networking Node
		_networking = new Networking();
		_networking.Name = "Networking";
		AddChild(_networking);

		// Connect Signals
		_networking.JoinResponseReceived += OnJoinResponse;
		_networking.WorldInitReceived += OnWorldInit;
		_networking.PlayerUpdateReceived += OnPlayerUpdate;
		_networking.BulletSpawned += OnBulletSpawned;
		_networking.BulletHit += OnBulletHit;
		_networking.EnemySpawned += OnEnemySpawned;
		_networking.EnemyUpdated += OnEnemyUpdated;
		_networking.EnemyDied += OnEnemyDied;
		_networking.PortalSpawned += OnPortalSpawned;
		_networking.PortalDied += OnPortalDied;
		_networking.Disconnected += OnDisconnected;

		// Start Connection
		_networking.Connect("127.0.0.1", 5000, "GodotUser_" + GD.Randi() % 1000);
	}

	public override void _Process(double delta)
	{
		if (_localPlayerId != -1 && _players.TryGetValue(_localPlayerId, out var localPlayer))
		{
			HandleMovement((float)delta);
			HandleShooting((float)delta);
		}
	}

	private void HandleMovement(float dt)
	{
		Godot.Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		_networking.SendPacket(new InputRequest 
		{ 
			Movement = new LastLight.Common.Vector2(inputDir.X, inputDir.Y),
			DeltaTime = dt,
			InputSequenceNumber = 0 
		}, LiteNetLib.DeliveryMethod.Unreliable);
	}

	private float _shootTimer = 0f;
	private float _shootInterval = 0.2f;

	private void HandleShooting(float dt)
	{
		_shootTimer += dt;
		if (Input.IsMouseButtonPressed(MouseButton.Left) && _shootTimer >= _shootInterval)
		{
			_shootTimer = 0f;
			var mousePos = _players[_localPlayerId].GetGlobalMousePosition();
			var dir = (mousePos - _players[_localPlayerId].GlobalPosition).Normalized();
			_networking.SendPacket(new FireRequest 
			{ 
				BulletId = 0, // Server handles IDs
				Direction = new LastLight.Common.Vector2(dir.X, dir.Y)
			}, LiteNetLib.DeliveryMethod.ReliableOrdered);
		}

		if (Input.IsActionJustPressed("ui_accept")) // Space by default
		{
			foreach (var portal in _portals.Values)
			{
				if (portal.GlobalPosition.DistanceTo(_players[_localPlayerId].GlobalPosition) < 60)
				{
					_networking.SendPacket(new PortalUseRequest { PortalId = portal.PortalId }, LiteNetLib.DeliveryMethod.ReliableOrdered);
					break;
				}
			}
		}
	}

	private void OnJoinResponse(bool success, int playerId, string message)
	{
		GD.Print($"Join Response: {success}, ID: {playerId}, Message: {message}");
		if (success)
		{
			_localPlayerId = playerId;
			SpawnPlayer(playerId, true);
		}
	}

	private void OnWorldInit(int seed, int width, int height, int tileSize, int style, float cleanupTimer)
	{
		GD.Print($"World Init: Seed={seed}, Size={width}x{height}, Style={(WorldManager.GenerationStyle)style}");
		_world.Generate(seed, width, height, (WorldManager.GenerationStyle)style);
	}

	private void OnPlayerUpdate(int playerId, Godot.Vector2 position, Godot.Vector2 velocity, int currentHealth, int maxHealth, int level)
	{
		if (!_players.TryGetValue(playerId, out var player))
		{
			player = SpawnPlayer(playerId, playerId == _localPlayerId);
		}
		player.UpdateState(position, velocity);
        if (playerId == _localPlayerId)
        {
            _hud.UpdateStatus(currentHealth, maxHealth, level);
        }
	}

	private Player SpawnPlayer(int playerId, bool isLocal)
	{
		GD.Print($"Spawning player {playerId} (Local: {isLocal})");
		var player = new Player();
		player.PlayerId = playerId;
		player.IsLocal = isLocal;
		player.Name = $"Player_{playerId}";
		_entities.AddChild(player);
		_players[playerId] = player;
		return player;
	}

	private Dictionary<int, Bullet> _bullets = new();

	private void OnBulletSpawned(int ownerId, int bulletId, Godot.Vector2 position, Godot.Vector2 velocity)
	{
		var bullet = new Bullet();
		bullet.OwnerId = ownerId;
		bullet.BulletId = bulletId;
		bullet.GlobalPosition = position;
		bullet.Velocity = velocity;
		_entities.AddChild(bullet);
		_bullets[bulletId] = bullet;
	}

	private void OnBulletHit(int bulletId, int targetId, int targetType)
	{
		if (_bullets.TryGetValue(bulletId, out var bullet))
		{
			bullet.QueueFree();
			_bullets.Remove(bulletId);
		}
	}

	private Dictionary<int, Enemy> _enemies = new();
	private Dictionary<int, Portal> _portals = new();

	private void OnEnemySpawned(int enemyId, Godot.Vector2 position, int maxHealth, string dataId)
	{
		var enemy = new Enemy();
		enemy.EnemyId = enemyId;
		enemy.MaxHealth = maxHealth;
		enemy.CurrentHealth = maxHealth;
		enemy.GlobalPosition = position;
		_entities.AddChild(enemy);
		_enemies[enemyId] = enemy;
	}

	private void OnEnemyUpdated(int enemyId, Godot.Vector2 position, int currentHealth)
	{
		if (_enemies.TryGetValue(enemyId, out var enemy))
		{
			enemy.UpdateState(position, currentHealth);
		}
	}

	private void OnEnemyDied(int enemyId)
	{
		if (_enemies.TryGetValue(enemyId, out var enemy))
		{
			enemy.QueueFree();
			_enemies.Remove(enemyId);
		}
	}

	private void OnPortalSpawned(int portalId, Godot.Vector2 position, int targetRoomId, string name)
	{
		var portal = new Portal();
		portal.PortalId = portalId;
		portal.GlobalPosition = position;
		portal.TargetRoomId = targetRoomId;
		portal.PortalName = name;
		_entities.AddChild(portal);
		_portals[portalId] = portal;
	}

	private void OnPortalDied(int portalId)
	{
		if (_portals.TryGetValue(portalId, out var portal))
		{
			portal.QueueFree();
			_portals.Remove(portalId);
		}
	}

	private void OnDisconnected(string reason)
	{
		GD.Print($"Disconnected: {reason}");
	}
}