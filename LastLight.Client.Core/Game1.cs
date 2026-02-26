using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LastLight.Client.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ClientNetworking _networking;
    private Texture2D _pixel;
    private Player _localPlayer;
    private Dictionary<int, Player> _otherPlayers = new();
    private BulletManager _bulletManager = new();
    private EnemyManager _enemyManager = new();
    private SpawnerManager _spawnerManager = new();
    private float _moveSpeed = 200f;
    private float _shootInterval = 0.1f;
    private float _shootTimer = 0f;
    private int _bulletCounter = 0;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _networking = new ClientNetworking();
        _localPlayer = new Player { IsLocal = true, Position = new Vector2(400, 300) };
        _networking.OnPlayerUpdate = HandlePlayerUpdate;
        _networking.OnJoinResponse = (response) => 
        {
            if (response.Success) 
            {
                _localPlayer.Id = response.PlayerId;
                _localPlayer.MaxHealth = response.MaxHealth;
                _localPlayer.CurrentHealth = response.MaxHealth;
            }
        };
        _networking.OnSpawnBullet = HandleSpawnBullet;
        _networking.OnBulletHit = HandleBulletHit;
        
        _networking.OnEnemySpawn = _enemyManager.HandleSpawn;
        _networking.OnEnemyUpdate = _enemyManager.HandleUpdate;
        _networking.OnEnemyDeath = _enemyManager.HandleDeath;

        _networking.OnSpawnerSpawn = _spawnerManager.HandleSpawn;
        _networking.OnSpawnerUpdate = _spawnerManager.HandleUpdate;
        _networking.OnSpawnerDeath = _spawnerManager.HandleDeath;
    }

    private void HandleBulletHit(LastLight.Common.BulletHit hit)
    {
        _bulletManager.Destroy(hit.BulletId);
        // We could also play a hit sound or particle effect here
    }

    private void HandleSpawnBullet(LastLight.Common.SpawnBullet spawn)
    {
        if (spawn.OwnerId == _localPlayer.Id) return;

        _bulletManager.Spawn(spawn.BulletId, spawn.OwnerId, new Vector2(spawn.Position.X, spawn.Position.Y), new Vector2(spawn.Velocity.X, spawn.Velocity.Y));
    }

    private void HandlePlayerUpdate(LastLight.Common.AuthoritativePlayerUpdate update)
    {
        if (update.PlayerId == _localPlayer.Id)
        {
            // Reconciliation
            _localPlayer.Position = new Vector2(update.Position.X, update.Position.Y);
            _localPlayer.CurrentHealth = update.CurrentHealth;
            
            // Remove processed inputs
            _localPlayer.PendingInputs.RemoveAll(i => i.InputSequenceNumber <= update.LastProcessedInputSequence);

            // Re-apply remaining pending inputs
            foreach (var input in _localPlayer.PendingInputs)
            {
                _localPlayer.ApplyInput(input, _moveSpeed);
            }
            return;
        }

        if (!_otherPlayers.TryGetValue(update.PlayerId, out var player))
        {
            player = new Player { Id = update.PlayerId, IsLocal = false, MaxHealth = 100 }; // Guess 100 for now or send in join
            _otherPlayers[update.PlayerId] = player;
        }

        player.Position = new Vector2(update.Position.X, update.Position.Y);
        player.Velocity = new Vector2(update.Velocity.X, update.Velocity.Y);
        player.CurrentHealth = update.CurrentHealth;
    }

    protected override void Initialize()
    {
        _networking.Connect("localhost", 5000);
        Exiting += (sender, args) => _networking.Disconnect();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    private float _networkTimer = 0f;
    private float _networkInterval = 0.05f; // 20 times a second

    private int _inputSequenceNumber = 0;

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Handle input and send to server
        var input = HandleInput(dt);
        if (input != null)
        {
            _localPlayer.PendingInputs.Add(input);
            _localPlayer.ApplyInput(input, _moveSpeed); // Client-side prediction
            _networking.SendInputRequest(input);
        }

        foreach (var player in _otherPlayers.Values)
        {
            player.Update(gameTime);
        }
        _bulletManager.Update(gameTime);
        _networking.PollEvents();

        base.Update(gameTime);
    }

    private LastLight.Common.InputRequest? HandleInput(float dt)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        Vector2 move = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.W)) move.Y -= 1;
        if (keyboard.IsKeyDown(Keys.S)) move.Y += 1;
        if (keyboard.IsKeyDown(Keys.A)) move.X -= 1;
        if (keyboard.IsKeyDown(Keys.D)) move.X += 1;

        if (move != Vector2.Zero)
        {
            move.Normalize();
        }

        _shootTimer += dt;
        if (mouse.LeftButton == ButtonState.Pressed && _shootTimer >= _shootInterval)
        {
            _shootTimer = 0;
            Shoot(mouse.Position.ToVector2());
        }

        if (move != Vector2.Zero || true) // Send even zero inputs to keep server updated, though optimization could skip zero sequences
        {
            return new LastLight.Common.InputRequest
            {
                Movement = new LastLight.Common.Vector2(move.X, move.Y),
                DeltaTime = dt,
                InputSequenceNumber = _inputSequenceNumber++
            };
        }
        return null;
    }

    private void Shoot(Vector2 targetPos)
    {
        var dir = targetPos - _localPlayer.Position;
        if (dir == Vector2.Zero) dir = new Vector2(1, 0);
        dir.Normalize();

        var vel = dir * 500f;
        int bulletId = _bulletCounter++;
        
        // Client-side prediction
        _bulletManager.Spawn(bulletId, _localPlayer.Id, _localPlayer.Position, vel);
        
        _networking.SendFireRequest(new LastLight.Common.FireRequest
        {
            BulletId = bulletId,
            Direction = new LastLight.Common.Vector2(dir.X, dir.Y)
        });
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        _spawnerManager.Draw(_spriteBatch, _pixel);
        _localPlayer.Draw(_spriteBatch, _pixel);
        foreach (var player in _otherPlayers.Values)
        {
            player.Draw(_spriteBatch, _pixel);
        }
        _enemyManager.Draw(_spriteBatch, _pixel);
        _bulletManager.Draw(_spriteBatch, _pixel);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
