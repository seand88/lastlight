using System;
using System.Collections.Generic;

namespace LastLight.Common.Abilities;

public class StatusInstance
{
    public EffectSpec Spec { get; set; } = new();
    public IEntity Target { get; set; } = null!;
    public IEntity Source { get; set; } = null!;
    public float DurationRemaining { get; set; }
    public float TickTimer { get; set; }
}

public static class StatusManager
{
    private static readonly Dictionary<int, List<StatusInstance>> _activeStatuses = new();

    public static void AddStatus(IEntity target, IEntity source, EffectSpec spec)
    {
        if (!spec.Duration.HasValue) return;

        var status = new StatusInstance
        {
            Spec = spec,
            Target = target,
            Source = source,
            DurationRemaining = spec.Duration.Value,
            TickTimer = spec.TickRate ?? 0f
        };

        if (!_activeStatuses.ContainsKey(target.Id))
            _activeStatuses[target.Id] = new List<StatusInstance>();

        _activeStatuses[target.Id].Add(status);
    }

    public static void Update(float dt)
    {
        foreach (var entityStatuses in _activeStatuses.Values)
        {
            for (int i = entityStatuses.Count - 1; i >= 0; i--)
            {
                var status = entityStatuses[i];
                status.DurationRemaining -= dt;

                if (status.Spec.TickRate.HasValue)
                {
                    status.TickTimer -= dt;
                    if (status.TickTimer <= 0)
                    {
                        // Re-trigger the effect logic for a tick
                        ApplyTick(status);
                        status.TickTimer = status.Spec.TickRate.Value;
                    }
                }

                if (status.DurationRemaining <= 0)
                {
                    entityStatuses.RemoveAt(i);
                }
            }
        }
    }

    private static void ApplyTick(StatusInstance status)
    {
        // For DOT/HOT, we apply the damage/heal value on each tick
        if (status.Spec.EffectName == "dot")
        {
            status.Target.CurrentHealth -= (int)status.Spec.Value;
        }
        else if (status.Spec.EffectName == "hot")
        {
            status.Target.CurrentHealth = Math.Min(status.Target.MaxHealth, status.Target.CurrentHealth + (int)status.Spec.Value);
        }
    }

    public static void ClearStatuses(int entityId)
    {
        if (_activeStatuses.ContainsKey(entityId))
            _activeStatuses.Remove(entityId);
    }
}
