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

        // Mana check (Future)
        // if (player.CurrentMana < spec.ManaCost) return;

        // Update cooldown
        _playerCooldowns[player.Id][request.AbilityId] = (float)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0f);
        
        // Execute Delivery
        if (spec.Delivery is ProjectileDelivery proj)
        {
            ExecuteProjectileDelivery(player, spec, proj, request, bulletManager);
        }
        // Handle other delivery types here
    }

    private void ExecuteProjectileDelivery(ServerPlayer player, AbilitySpec spec, ProjectileDelivery proj, AbilityUseRequest request, ServerBulletManager bulletManager)
    {
        // For now, simple straight shot using Direction
        // In the future, handle patterns (cone, circle, etc)
        
        int bulletId = new Random().Next(); // Unique ID for this bullet instance
        Vector2 velocity = new Vector2(request.Direction.X * proj.Speed, request.Direction.Y * proj.Speed);
        float lifeTime = (proj.RangeTiles * 32.0f) / proj.Speed;

        bulletManager.Spawn(bulletId, player.Id, player.Position, velocity, lifeTime, spec.Id, request.ClientInstanceId);
        
        // Notify clients to spawn visual bullet
        OnBulletSpawned?.Invoke(player.Id, bulletId, player.Position, velocity, lifeTime, spec.Id);
    }
}
