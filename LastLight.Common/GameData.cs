using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LastLight.Common;

public class ItemData {
    public string Id { get; set; } = "";
    public ItemCategory Category { get; set; }
    public string Name { get; set; } = "";
    public int StatBonus { get; set; }
    public WeaponType WeaponType { get; set; }
    public string Icon { get; set; } = "";
}

public class EnemyData {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int MaxHealth { get; set; }
    public int BaseDamage { get; set; }
    public float Speed { get; set; }
    public string SpecialAbility { get; set; } = "";
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
        
        Console.WriteLine($"[GameDataManager] Loaded {Items.Count} items, {Enemies.Count} enemies, {Rooms.Count} rooms. (Path: {resolvedPath})");
    }
}