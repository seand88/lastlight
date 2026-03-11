using System.Collections.Generic;
using System.Text.Json;
using LastLight.Common.Abilities;

namespace LastLight.Server.AI;

public interface IAiDriver
{
    void Initialize(JsonElement config, ServerEnemy entity, ITimerRegistry registry);
    void OnUpdate(float dt, ServerEnemy entity, Dictionary<int, ServerPlayer> players, ServerAbilityManager abilityManager);
    void OnTimerTick(string actionId, ServerEnemy entity, ServerAbilityManager abilityManager);
    void OnDamaged(ServerEnemy entity, int damage, IEntity? source, ServerAbilityManager abilityManager);
}
