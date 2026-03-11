using System;
using System.Collections.Generic;
using LastLight.Common;
using LastLight.Common.Abilities;

namespace LastLight.Server;

public class ServerAbilityManager
{
    private readonly Dictionary<int, Dictionary<string, float>> _playerCooldowns = new();

    public Action<int, int, Vector2, Vector2, float, string>? OnBulletSpawned;

    public void HandleAbilityRequest(ServerPlayer player, AbilityUseRequest request, ServerBulletManager bulletManager)
    {
        if (!GameDataManager.Abilities.TryGetValue(request.AbilityId, out var spec)) return;

        // Cooldown check
        if (!_playerCooldowns.ContainsKey(player.Id))
            _playerCooldowns[player.Id] = new Dictionary<string, float>();

        if (_playerCooldowns[player.Id].TryGetValue(request.AbilityId, out var lastUsed))
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0f < lastUsed + spec.Cooldown)
                return; // Still on cooldown
        }

        // Mana check
        if (player.CurrentMana < spec.ManaCost) return;
        player.CurrentMana -= spec.ManaCost;

        // Update cooldown
        _playerCooldowns[player.Id][request.AbilityId] = (float)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0f);
        
        ExecuteAbility(player, spec, request.Direction, bulletManager, request.ClientInstanceId);
    }

    public void HandleEnemyAbility(IEntity caster, string abilityId, Vector2 direction, ServerBulletManager bulletManager)
    {
        if (!GameDataManager.Abilities.TryGetValue(abilityId, out var spec)) return;

        // Cooldown check for AI
        if (!_playerCooldowns.ContainsKey(caster.Id))
            _playerCooldowns[caster.Id] = new Dictionary<string, float>();

        if (_playerCooldowns[caster.Id].TryGetValue(abilityId, out var lastUsed))
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0f < lastUsed + spec.Cooldown)
                return; // Still on cooldown
        }

        // Update cooldown
        _playerCooldowns[caster.Id][abilityId] = (float)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0f);

        ExecuteAbility(caster, spec, direction, bulletManager, 0);
    }
    private void ExecuteAbility(IEntity caster, AbilitySpec spec, Vector2 direction, ServerBulletManager bulletManager, int correlationId)
    {
        // Execute Delivery
        if (spec.Delivery is ProjectileDelivery proj)
        {
            ExecuteProjectileDelivery(caster, spec, proj, direction, bulletManager, correlationId);
        }
        // Handle other delivery types here
    }

    private void ExecuteProjectileDelivery(IEntity caster, AbilitySpec spec, ProjectileDelivery proj, Vector2 direction, ServerBulletManager bulletManager, int correlationId)
    {
        int count = proj.Count;
        float baseAngle = (float)Math.Atan2(direction.Y, direction.X);

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle;
            
            if (proj.Pattern.ToLower() == "circle")
            {
                angle = (float)(i * Math.PI * 2 / count);
            }
            else if (proj.Pattern.ToLower() == "cone" && count > 1)
            {
                float spread = proj.Spread > 0 ? proj.Spread : 0.5f; // Default spread
                angle = (baseAngle - spread / 2f) + (spread / (count - 1)) * i;
            }

            int bulletId = new Random().Next(); // Unique ID for this bullet instance
            Vector2 velocity = new Vector2((float)Math.Cos(angle) * proj.Speed, (float)Math.Sin(angle) * proj.Speed);
            
            // Final Range = AbilityBaseRange + EntityRangeBonus
            float finalRangeTiles = proj.RangeTiles + caster.RangeBonus;
            float lifeTime = (finalRangeTiles * 32.0f) / proj.Speed;

            bulletManager.Spawn(bulletId, caster.Id, caster.Position, velocity, lifeTime, spec.Id, correlationId);
            
            // Notify clients to spawn visual bullet
            OnBulletSpawned?.Invoke(caster.Id, bulletId, caster.Position, velocity, lifeTime, spec.Id);
        }
    }
}
