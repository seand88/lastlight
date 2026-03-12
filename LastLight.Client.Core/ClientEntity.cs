using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common;
using LastLight.Common.Abilities;

namespace LastLight.Client.Core;

public class ClientEntity : IEntity
{
    public int Id { get; set; }
    public string DataId { get; set; } = "";
    public Microsoft.Xna.Framework.Vector2 Position { get; set; }

    // IEntity implementation
    LastLight.Common.Vector2 IEntity.Position { 
        get => new LastLight.Common.Vector2(Position.X, Position.Y); 
        set => Position = new Microsoft.Xna.Framework.Vector2(value.X, value.Y); 
    }

    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public byte Phase { get; set; }
    public bool Active { get; set; }

    // Visual helper properties from JSON data
    public string Name => GameDataManager.Enemies.TryGetValue(DataId, out var d) ? d.Name : "Unknown";
    public string Animation => GameDataManager.Enemies.TryGetValue(DataId, out var d) ? d.Animation : "";
    public string EnemyType => GameDataManager.Enemies.TryGetValue(DataId, out var d) ? d.EnemyType : "enemy";
    public int Width => GameDataManager.Enemies.TryGetValue(DataId, out var d) ? d.Width : 32;
    public int Height => GameDataManager.Enemies.TryGetValue(DataId, out var d) ? d.Height : 32;

    public int BaseDamage => 10;
    public float AttackSpeedBonus => 0f;
    public float RangeBonus => 0f;

    public void TakeDamage(int amount, IEntity? source)
    {
        // Handled by authoritative server updates
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!Active) return;

        // 1. Draw Health Bar
        if (MaxHealth > 0 && CurrentHealth < MaxHealth)
        {
            float healthPercent = (float)CurrentHealth / MaxHealth;
            int barWidth = Width;
            int currentHealthWidth = (int)(barWidth * healthPercent);
            int barHeight = (EnemyType == "boss") ? 8 : 4;
            int yOffset = (Height / 2) + barHeight + 4;
            
            var barRect = new Rectangle((int)Position.X - (barWidth/2), (int)Position.Y - yOffset, barWidth, barHeight);
            
            // Background (red)
            spriteBatch.Draw(pixel, barRect, Color.DarkRed);
            // Foreground (green)
            spriteBatch.Draw(pixel, new Rectangle(barRect.X, barRect.Y, currentHealthWidth, barHeight), Color.LimeGreen);
        }
    }
}
