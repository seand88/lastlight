using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using LastLight.Common.Abilities;

namespace LastLight.Common;

public class ItemData {
    public string Id { get; set; } = "";
    public ItemCategory Category { get; set; }
    public string Name { get; set; } = "";
    public int StatBonus { get; set; }
    public WeaponType WeaponType { get; set; }
    public string Atlas { get; set; } = "Items";
    public string Icon { get; set; } = "";
    public List<WeaponTier> Tiers { get; set; } = new();
}

public class WeaponTier {
    public int Tier { get; set; }
    public int BaseDamage { get; set; }
    public float AttackSpeedMod { get; set; }
    public float RangeBonus { get; set; }
    public List<string> UnlockedAbilities { get; set; } = new();
    public List<PerkOption> PerkOptions { get; set; } = new();
}

public class PerkOption {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Desc { get; set; } = "";
}

public class EnemyData {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string EnemyType { get; set; } = "enemy";
    public int Width { get; set; } = 32;
    public int Height { get; set; } = 32;
    public int MaxHealth { get; set; }
    public float Speed { get; set; }
    public int BaseDamage { get; set; }
    public float AttackSpeedBonus { get; set; }
    public float RangeBonus { get; set; }
    
    // AI v2.0
    public string AiDriver { get; set; } = "standard";
    public JsonElement AiConfig { get; set; }

    // Legacy AI v1.0
    public string PrimaryAbilityId { get; set; } = "";
    public string SpecialAbilityId { get; set; } = "";
    public string AiType { get; set; } = "chase";
    
    public string Animation { get; set; } = "";
}

public class RoomData {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public WorldManager.GenerationStyle Style { get; set; }
    public string[] AllowedEnemies { get; set; } = Array.Empty<string>();
    public int SpawnerCount { get; set; } = 15;
}

public static class GameDataManager {
    public static Dictionary<string, ItemData> Items { get; private set; } = new();
    public static Dictionary<string, EnemyData> Enemies { get; private set; } = new();
    public static Dictionary<string, RoomData> Rooms { get; private set; } = new();
    public static Dictionary<string, AbilitySpec> Abilities { get; private set; } = new();
    public static Dictionary<string, EffectTemplate> EffectTemplates { get; private set; } = new();

    public static void Load(string dataDirectory) {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());

        // Fallback for 'dotnet run' which might set the working directory to the project root
        string resolvedPath = dataDirectory;
        if (!Directory.Exists(resolvedPath)) {
            resolvedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dataDirectory);
        }

        if (File.Exists(Path.Combine(resolvedPath, "Items.json"))) {
            var itemsList = JsonSerializer.Deserialize<List<ItemData>>(File.ReadAllText(Path.Combine(resolvedPath, "Items.json")), options);
            if (itemsList != null) foreach (var item in itemsList) Items[item.Id] = item;
        }
        if (File.Exists(Path.Combine(resolvedPath, "Enemies.json"))) {
            var enemiesList = JsonSerializer.Deserialize<List<EnemyData>>(File.ReadAllText(Path.Combine(resolvedPath, "Enemies.json")), options);
            if (enemiesList != null) foreach (var enemy in enemiesList) Enemies[enemy.Id] = enemy;
        }
        if (File.Exists(Path.Combine(resolvedPath, "Rooms.json"))) {
            var roomsList = JsonSerializer.Deserialize<List<RoomData>>(File.ReadAllText(Path.Combine(resolvedPath, "Rooms.json")), options);
            if (roomsList != null) foreach (var room in roomsList) Rooms[room.Id] = room;
        }
        if (File.Exists(Path.Combine(resolvedPath, "Abilities.json"))) {
            var abilitiesList = JsonSerializer.Deserialize<List<AbilitySpec>>(File.ReadAllText(Path.Combine(resolvedPath, "Abilities.json")), options);
            if (abilitiesList != null) foreach (var ability in abilitiesList) Abilities[ability.Id] = ability;
        }
        if (File.Exists(Path.Combine(resolvedPath, "EffectTemplates.json"))) {
            var templatesList = JsonSerializer.Deserialize<List<EffectTemplate>>(File.ReadAllText(Path.Combine(resolvedPath, "EffectTemplates.json")), options);
            if (templatesList != null) foreach (var template in templatesList) EffectTemplates[template.Id] = template;
        }

        Console.WriteLine($"[GameDataManager] Loaded {Items.Count} items, {Enemies.Count} enemies, {Rooms.Count} rooms, {Abilities.Count} abilities, {EffectTemplates.Count} templates. (Path: {resolvedPath})");
    }
}

public class EffectTemplate {
    public string Id { get; set; } = "";
    public string UiIcon { get; set; } = "";
    public string FctColor { get; set; } = "255,255,255";
    public string SfxOnApply { get; set; } = "";
    public string SfxLoop { get; set; } = "";
    public string ParticleFx { get; set; } = "";
    public string VfxAnchor { get; set; } = "center";
    public string ScreenTint { get; set; } = "";
}