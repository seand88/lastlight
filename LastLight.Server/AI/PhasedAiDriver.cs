using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LastLight.Common;
using LastLight.Common.Abilities;

namespace LastLight.Server.AI;

public class PhasedAiDriver : IAiDriver
{
    private List<PhaseData> _phases = new();
    private PhaseData? _currentPhase = null;
    private ITimerRegistry? _registry;

    private Vector2 _lastKnownDirection = new Vector2(1, 0);

    public void Initialize(JsonElement config, ServerEnemy entity, ITimerRegistry registry)
    {
        _registry = registry;
        
        if (config.ValueKind == JsonValueKind.Object && config.TryGetProperty("phases", out var phasesProp) && phasesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in phasesProp.EnumerateArray())
            {
                var phase = new PhaseData();
                if (p.TryGetProperty("threshold", out var tProp)) phase.Threshold = tProp.GetSingle();
                if (p.TryGetProperty("movement", out var mProp)) phase.Movement = mProp.GetString() ?? "stationary";
                if (p.TryGetProperty("speed", out var sProp)) phase.Speed = sProp.GetSingle();
                if (p.TryGetProperty("base_damage", out var bdProp)) phase.BaseDamage = bdProp.GetInt32();
                
                if (p.TryGetProperty("behaviors", out var bProp) && bProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var b in bProp.EnumerateArray())
                    {
                        var behavior = new BehaviorData();
                        if (b.TryGetProperty("trigger", out var trProp)) behavior.Trigger = trProp.GetString() ?? "";
                        if (b.TryGetProperty("action", out var aProp)) behavior.ActionId = aProp.GetString() ?? "";
                        if (b.TryGetProperty("interval", out var iProp)) behavior.Interval = iProp.GetSingle();
                        if (b.TryGetProperty("hp", out var hpProp)) behavior.HpThreshold = hpProp.GetSingle();
                        if (b.TryGetProperty("event", out var evProp)) behavior.EventName = evProp.GetString();
                        
                        phase.Behaviors.Add(behavior);
                    }
                }
                _phases.Add(phase);
            }
            
            // Sort phases by threshold descending (highest HP first)
            _phases = _phases.OrderByDescending(p => p.Threshold).ToList();
        }

        // Initialize first phase based on current health (usually 1.0)
        float currentHpPercent = entity.MaxHealth > 0 ? (float)entity.CurrentHealth / entity.MaxHealth : 1.0f;
        var startPhase = _phases.LastOrDefault(p => currentHpPercent <= p.Threshold) ?? _phases.FirstOrDefault();
        
        if (startPhase != null)
        {
            TransitionToPhase(startPhase, entity);
        }
    }

    public void OnUpdate(float dt, ServerEnemy entity, Dictionary<int, ServerPlayer> players, ServerAbilityManager abilityManager)
    {
        if (_currentPhase == null || _currentPhase.Movement == "stationary")
        {
            entity.Velocity = new Vector2(0, 0);
            return;
        }

        // Simple Targeting Logic
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
                
                if (_currentPhase.Movement == "chase")
                {
                    entity.Velocity.X = _lastKnownDirection.X * entity.Speed;
                    entity.Velocity.Y = _lastKnownDirection.Y * entity.Speed;
                }
                else if (_currentPhase.Movement == "kite")
                {
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
                        entity.Velocity = new Vector2(0, 0);
                    }
                }
                else if (_currentPhase.Movement == "spiral")
                {
                    // Placeholder for spiral movement logic
                    entity.Velocity = new Vector2(0, 0);
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
        // Fire the ability using the cached direction
        abilityManager.HandleEnemyAbility(entity, actionId, _lastKnownDirection, entity.RoomBullets);
    }

    public void OnDamaged(ServerEnemy entity, int damage, IEntity? source, ServerAbilityManager abilityManager)
    {
        if (_currentPhase == null) return;

        // 1. Determine direction for reactive abilities
        Vector2 reactionDirection = _lastKnownDirection;
        if (source != null)
        {
            float dx = source.Position.X - entity.Position.X;
            float dy = source.Position.Y - entity.Position.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist > 0) reactionDirection = new Vector2(dx / dist, dy / dist);
        }

        // 2. Execute explicit "on_damaged" counter-attacks for the CURRENT phase
        foreach (var behavior in _currentPhase.Behaviors.Where(b => b.Trigger == "on_damaged"))
        {
            abilityManager.HandleEnemyAbility(entity, behavior.ActionId, reactionDirection, entity.RoomBullets);
        }

        // 3. Evaluate Phase Transitions (The "on_hp_below" logic)
        float currentHpPercent = entity.MaxHealth > 0 ? (float)entity.CurrentHealth / entity.MaxHealth : 0f;
        
        // Find the lowest threshold phase that we now qualify for
        var targetPhase = _phases.LastOrDefault(p => currentHpPercent <= p.Threshold);
        
        if (targetPhase != null && targetPhase != _currentPhase)
        {
            // Execute any "on_hp_below" one-time actions defined in the NEW phase
            foreach(var behavior in targetPhase.Behaviors.Where(b => b.Trigger == "on_hp_below" && currentHpPercent <= b.HpThreshold))
            {
                abilityManager.HandleEnemyAbility(entity, behavior.ActionId, reactionDirection, entity.RoomBullets);
            }
            
            // Lock in the transition
            TransitionToPhase(targetPhase, entity);
        }

        // 4. Handle Death logic if HP reached 0
        if (entity.CurrentHealth <= 0)
        {
            foreach (var behavior in _currentPhase.Behaviors.Where(b => b.Trigger == "on_death"))
            {
                abilityManager.HandleEnemyAbility(entity, behavior.ActionId, reactionDirection, entity.RoomBullets);
            }
        }
    }

    private void TransitionToPhase(PhaseData newPhase, ServerEnemy entity)
    {
        // Clear old timers
        _registry?.ClearTimers();

        _currentPhase = newPhase;
        
        // Apply stats overrides to the body
        if (newPhase.Speed.HasValue) entity.Speed = newPhase.Speed.Value;
        if (newPhase.BaseDamage.HasValue) entity.BaseDamage = newPhase.BaseDamage.Value;

        // Register new timers for the new phase
        foreach (var behavior in newPhase.Behaviors.Where(b => b.Trigger == "on_timer"))
        {
            if (behavior.Interval > 0)
            {
                _registry?.RegisterTimer(behavior.ActionId, behavior.Interval);
            }
        }
    }

    private class PhaseData
    {
        public float Threshold { get; set; } = 1.0f;
        public string Movement { get; set; } = "stationary";
        public float? Speed { get; set; }
        public int? BaseDamage { get; set; }
        public List<BehaviorData> Behaviors { get; set; } = new();
    }

    private class BehaviorData
    {
        public string Trigger { get; set; } = "";
        public string ActionId { get; set; } = "";
        public float Interval { get; set; } = 1.0f;
        public float HpThreshold { get; set; } = 0f;
        public string? EventName { get; set; }
    }
}
