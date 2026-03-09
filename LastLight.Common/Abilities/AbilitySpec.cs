using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LastLight.Common.Abilities;

public class AbilitySpec
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("atlas")]
    public string Atlas { get; set; } = "Abilities";

    [JsonPropertyName("mana_cost")]
    public int ManaCost { get; set; }

    [JsonPropertyName("cooldown")]
    public float Cooldown { get; set; }

    [JsonPropertyName("delivery")]
    public DeliverySpec Delivery { get; set; } = new();

    [JsonPropertyName("effects")]
    public List<EffectSpec> Effects { get; set; } = new();
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ProjectileDelivery), "projectile")]
[JsonDerivedType(typeof(ChanneledDelivery), "channeled")]
[JsonDerivedType(typeof(ContactDelivery), "contact")]
[JsonDerivedType(typeof(InstantDelivery), "instant")]
public class DeliverySpec
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class ProjectileDelivery : DeliverySpec
{
    [JsonPropertyName("fire_rate")]
    public float FireRate { get; set; }
    [JsonPropertyName("speed")]
    public float Speed { get; set; }
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "straight";
    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
    [JsonPropertyName("range_tiles")]
    public float RangeTiles { get; set; }
    [JsonPropertyName("pierce")]
    public bool Pierce { get; set; }
    [JsonPropertyName("spread")]
    public float Spread { get; set; }
    [JsonPropertyName("shape")]
    public string Shape { get; set; } = "circle";
    [JsonPropertyName("width")]
    public float Width { get; set; }
    [JsonPropertyName("height")]
    public float Height { get; set; }
    [JsonPropertyName("color")]
    public string Color { get; set; } = "255,255,255";
}

public class ChanneledDelivery : DeliverySpec
{
    [JsonPropertyName("tick_rate")]
    public float TickRate { get; set; }
    [JsonPropertyName("mana_per_second")]
    public int ManaPerSecond { get; set; }
    [JsonPropertyName("length_tiles")]
    public float LengthTiles { get; set; }
    [JsonPropertyName("width_pixels")]
    public float WidthPixels { get; set; }
}

public class ContactDelivery : DeliverySpec
{
    [JsonPropertyName("knockback_force")]
    public float KnockbackForce { get; set; }
    [JsonPropertyName("cooldown_per_target")]
    public float CooldownPerTarget { get; set; }
}

public class InstantDelivery : DeliverySpec
{
    [JsonPropertyName("anchor")]
    public string Anchor { get; set; } = "caster"; // caster, mouse_cursor
    [JsonPropertyName("radius")]
    public float Radius { get; set; }
}

public class EffectSpec
{
    [JsonPropertyName("effect_name")]
    public string EffectName { get; set; } = string.Empty; // damage, heal, dot, hot, buff, debuff, mana_gain

    [JsonPropertyName("target_type")]
    public string TargetType { get; set; } = "enemies"; // caster, enemies, allies, all

    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public float Value { get; set; }

    [JsonPropertyName("damage_type")]
    public string? DamageType { get; set; }

    [JsonPropertyName("chance")]
    public float Chance { get; set; } = 1.0f;

    [JsonPropertyName("duration")]
    public float? Duration { get; set; }

    [JsonPropertyName("tick_rate")]
    public float? TickRate { get; set; }
}
