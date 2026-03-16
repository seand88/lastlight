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
    private Vector2 _lastKnownDirection = new Vector2(1, 0);

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
                _lastKnownDirection = new Vector2(dx / distance, dy / distance);

                if (_movement == "chase")
                {
                    entity.Velocity.X = _lastKnownDirection.X * entity.Speed;
                    entity.Velocity.Y = _lastKnownDirection.Y * entity.Speed;
                }
                else if (_movement == "kite")
                {
                    // If too close, move away; if too far, move closer
                    if (distance < 150)
                    {
                        entity.Velocity.X = -_lastKnownDirection.X * entity.Speed;
                        entity.Velocity.Y = -_lastKnownDirection.Y * entity.Speed;
                    }
                    else if (distance > 250)
                    {
                        entity.Velocity.X = _lastKnownDirection.X * entity.Speed;
                        entity.Velocity.Y = _lastKnownDirection.Y * entity.Speed;
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
        abilityManager.HandleEnemyAbility(entity, actionId, _lastKnownDirection, entity.RoomBullets);
    }

    public void OnDamaged(ServerEnemy entity, int damage, IEntity? source, ServerAbilityManager abilityManager) { }
}
