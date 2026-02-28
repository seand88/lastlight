using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;

namespace LastLight.Client.Core;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Microsoft.Xna.Framework.Vector2 Position { get; set; }
    public Microsoft.Xna.Framework.Vector2 Velocity { get; set; }
    public bool IsLocal { get; set; }
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public ItemInfo[] Inventory { get; set; } = new ItemInfo[8];
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[4];
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Dexterity { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }
    public int RoomId { get; set; }

    public List<InputRequest> PendingInputs = new();

    public void ApplyInput(InputRequest input, float speed, WorldManager world)
    {
        Velocity = new Microsoft.Xna.Framework.Vector2(input.Movement.X, input.Movement.Y) * speed;
        
        var newPos = Position;
        newPos.X += Velocity.X * input.DeltaTime;
        if (world != null && !world.IsWalkable(new LastLight.Common.Vector2(newPos.X, newPos.Y)))
        {
            newPos.X = Position.X;
        }

        newPos.Y += Velocity.Y * input.DeltaTime;
        if (world != null && !world.IsWalkable(new LastLight.Common.Vector2(newPos.X, newPos.Y)))
        {
            newPos.Y = Position.Y;
        }

        Position = newPos;
    }

    public void Update(GameTime gameTime, WorldManager world)
    {
        if (IsLocal)
        {
            // Local player updates are handled in Game1 via HandleInput
            return; 
        }

        // For remote players, we just apply velocity (simple dead reckoning)
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var newPos = Position;
        newPos.X += Velocity.X * dt;
        if (world != null && !world.IsWalkable(new LastLight.Common.Vector2(newPos.X, newPos.Y)))
        {
            newPos.X = Position.X;
        }

        newPos.Y += Velocity.Y * dt;
        if (world != null && !world.IsWalkable(new LastLight.Common.Vector2(newPos.X, newPos.Y)))
        {
            newPos.Y = Position.Y;
        }

        Position = newPos;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D atlas, Texture2D pixel)
    {
        // Source rectangle for player in atlas (0, 0, 32, 32)
        var sourceRect = new Rectangle(0, 0, 32, 32);
        var destRect = new Rectangle((int)Position.X - 16, (int)Position.Y - 16, 32, 32);
        
        spriteBatch.Draw(atlas, destRect, sourceRect, IsLocal ? Color.White : Color.Red);

        // Draw health bar
        if (MaxHealth > 0 && CurrentHealth < MaxHealth)
        {
            float healthPercent = (float)CurrentHealth / MaxHealth;
            int healthBarWidth = 32;
            int currentHealthWidth = (int)(healthBarWidth * healthPercent);
            
            // Background (red)
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 16, (int)Position.Y - 24, healthBarWidth, 4), Color.Red);
            // Foreground (green/cyan)
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X - 16, (int)Position.Y - 24, currentHealthWidth, 4), IsLocal ? Color.Cyan : Color.LimeGreen);
        }
    }
}
