namespace LastLight.Common.Abilities;

public interface IEntity
{
    int Id { get; }
    int CurrentHealth { get; set; }
    int MaxHealth { get; set; }
    int CurrentMana { get; set; }
    int MaxMana { get; set; }
    Vector2 Position { get; set; }
    // Other common stats can be added here
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
        
        switch (spec.EffectName.ToLower())
        {
            case "damage":
                // In a real game, you'd apply defense/resistances here
                target.CurrentHealth -= (int)value;
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
