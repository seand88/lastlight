namespace LastLight.Common.Abilities;

public interface IEntity
{
    int Id { get; }
    int CurrentHealth { get; set; }
    int MaxHealth { get; set; }
    int CurrentMana { get; set; }
    int MaxMana { get; set; }
    Vector2 Position { get; set; }
    int BaseDamage { get; }
    float AttackSpeedBonus { get; }
    float RangeBonus { get; }
    void TakeDamage(int amount, IEntity? source);
}

public static class EffectProcessor
{
    public delegate void EffectAppliedHandler(IEntity target, IEntity source, EffectSpec spec, float calculatedValue);
    public static event EffectAppliedHandler? OnEffectApplied;

    public static void ApplyEffect(IEntity target, IEntity source, EffectSpec spec)
    {
        // Probability check
        if (spec.Chance < 1.0f && new System.Random().NextDouble() > spec.Chance) return;

        float value = spec.Value;
        
        // If it's a damage effect and value is 0 (or we just want to prioritize multiplier), 
        // calculate based on BaseDamage
        if (spec.EffectName.ToLower() == "damage" || spec.EffectName.ToLower() == "dot")
        {
            // If multiplier is set (not default 1.0 or explicitly 1.0), use it.
            // For now, following spec: Final Damage = BaseDamage * Multiplier
            // We use Value as an override if it's non-zero and Multiplier is default? 
            // No, let's stick to the spec: Multiplier is the primary knob for scaling.
            if (spec.Multiplier != 0)
            {
                value = source.BaseDamage * spec.Multiplier;
            }
        }
        
        switch (spec.EffectName.ToLower())
        {
            case "damage":
                // In a real game, you'd apply defense/resistances here
                target.TakeDamage((int)value, source);
                break;
            case "heal":
                target.CurrentHealth = System.Math.Min(target.MaxHealth, target.CurrentHealth + (int)value);
                break;
            case "mana_gain":
                target.CurrentMana = System.Math.Min(target.MaxMana, target.CurrentMana + (int)value);
                break;
            case "dot":
            case "hot":
            case "buff":
            case "debuff":
                // These are handled by the StatusRegistry/Manager
                StatusManager.AddStatus(target, source, spec);
                break;
        }

        OnEffectApplied?.Invoke(target, source, spec, value);
    }
}
