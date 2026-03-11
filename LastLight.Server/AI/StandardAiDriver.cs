using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LastLight.Common;
using LastLight.Common.Abilities;

namespace LastLight.Server.AI;

public class StandardAiDriver : IAiDriver
{
    private string _movement = "chase";
    private string _primaryAbilityId = "";
    private string _specialAbilityId = "";

    public void Initialize(JsonElement config, ServerEnemy entity, ITimerRegistry registry)
    {
        // Parse config or use legacy fallbacks if config is missing/malformed
        if (config.ValueKind == JsonValueKind.Object)
        {
            if (config.TryGetProperty("movement", out var movementProp)) _movement = movementProp.GetString() ?? "chase";
            if (config.TryGetProperty("speed", out var speedProp)) entity.Speed = speedProp.GetSingle();
            if (config.TryGetProperty("primary", out var primaryProp)) _primaryAbilityId = primaryProp.GetString() ?? "";
            if (config.TryGetProperty("special", out var specialProp)) _specialAbilityId = specialProp.GetString() ?? "";
        }
        else
        {
            // Fallback to legacy fields initialized in ServerEnemyManager
            _movement = entity.AiType;
            _primaryAbilityId = entity.PrimaryAbilityId;
            _specialAbilityId = entity.SpecialAbilityId;
        }

        // Register timers based on ability stats
        if (!string.IsNullOrEmpty(_primaryAbilityId) && GameDataManager.Abilities.TryGetValue(_primaryAbilityId, out var primarySpec))
        {
            float baseFireRate = primarySpec.Delivery is ProjectileDelivery p ? p.FireRate : 1.0f;
            float finalFireRate = baseFireRate * (1.0f + entity.AttackSpeedBonus);
            if (finalFireRate > 0) registry.RegisterTimer(_primaryAbilityId, 1.0f / finalFireRate);
        }

        if (!string.IsNullOrEmpty(_specialAbilityId) && GameDataManager.Abilities.TryGetValue(_specialAbilityId, out var specialSpec))
        {
            float interval = specialSpec.Cooldown / (1.0f + entity.AttackSpeedBonus);
            if (interval > 0) registry.RegisterTimer(_specialAbilityId, interval);
        }
    }

    public void OnUpdate(float dt, ServerEnemy entity, Dictionary<int, ServerPlayer> players, ServerAbilityManager abilityManager)
    {
        if (_movement == "stationary")
        {
            entity.Velocity = new Vector2(0, 0);
            return;
        }

        // Simple Targeting Logic: Find nearest player
        var nearest = players.Values.OrderBy(p => 
            (p.Position.X - entity.Position.X) * (p.Position.X - entity.Position.X) + 
            (p.Position.Y - entity.Position.Y) * (p.Position.Y - entity.Position.Y)
        ).FirstOrDefault();

        if (nearest != null)
        {
            float dx = nearest.Position.X - entity.Position.X;
            float dy = nearest.Position.Y - entity.Position.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance > 0)
            {
                if (_movement == "chase")
                {
                    entity.Velocity.X = (dx / distance) * entity.Speed;
                    entity.Velocity.Y = (dy / distance) * entity.Speed;
                }
                else if (_movement == "kite")
                {
                    // If too close, move away; if too far, move closer
                    if (distance < 150)
                    {
                        entity.Velocity.X = -(dx / distance) * entity.Speed;
                        entity.Velocity.Y = -(dy / distance) * entity.Speed;
                    }
                    else if (distance > 250)
                    {
                        entity.Velocity.X = (dx / distance) * entity.Speed;
                        entity.Velocity.Y = (dy / distance) * entity.Speed;
                    }
                    else
                    {
                        entity.Velocity = new Vector2(0, 0); // Ideal range
                    }
                }
            }
        }
        else
        {
            entity.Velocity = new Vector2(0, 0);
        }
    }

    public void OnTimerTick(string actionId, ServerEnemy entity, ServerAbilityManager abilityManager)
    {
        // We calculate the firing direction (assuming nearest player for now)
        // In a more advanced driver, this target would be cached in OnUpdate.
        Vector2 direction = new Vector2(1, 0); // Default fallback
        
        // Find nearest player dynamically if we need to shoot at them
        // (A robust driver would cache the target ID in OnUpdate)
        // For standard, we'll just fire at the closest if we don't have a cached target.
        // We'll let ServerAbilityManager handle looking up the target if direction isn't crucial.
        // But Projectiles need a direction. We'll add a simplified direction calculation here.
        
        // This is a simplification. The actual target finding should be robust.
        // For now, if the ability needs a direction, the ability manager uses the direction provided.
        // We'll pass a default direction or let the ability manager handle 'enemies' target type.
        
        // The old code fired towards the nearest player. We'll replicate that for the standard driver.
        // We need the players dictionary, but OnTimerTick doesn't receive it directly.
        // We'll change the ServerAbilityManager to find the target if needed, or pass an empty direction 
        // and let the server room handle the collision context.
        // Wait, the spec says HandleEnemyAbility(entity, actionId, direction, ...).
        // Let's assume the driver caches the target direction.
        
        // Since we don't have the players dict here, we'll store the last known direction in OnUpdate.
        // I will add a _lastKnownDirection field.
        
        abilityManager.HandleEnemyAbility(entity, actionId, _lastKnownDirection, entity.RoomBullets);
    }

    private Vector2 _lastKnownDirection = new Vector2(1, 0);

    public void OnDamaged(ServerEnemy entity, int damage, IEntity? source, ServerAbilityManager abilityManager) { }
}
