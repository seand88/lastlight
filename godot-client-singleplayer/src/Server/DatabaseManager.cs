using System;
using System.IO;
using System.Text.Json;
using LastLight.Common;
using Godot;

namespace LastLight.Server;

public class PlayerSaveData
{
    public int MaxHealth { get; set; } = 100;
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 0;
    public int Speed { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Vitality { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[3];
    public ItemInfo[] Inventory { get; set; } = new ItemInfo[8];
}

public static class DatabaseManager
{
    private static string GetSavePath(string username)
    {
        string userDir = OS.GetUserDataDir();
        if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
        return Path.Combine(userDir, $"{username}_save.json");
    }

    public static void Initialize()
    {
        // No init needed for JSON files
    }

    public static PlayerSaveData? LoadPlayer(string username)
    {
        try
        {
            string path = GetSavePath(username);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<PlayerSaveData>(json);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error loading player: {ex.Message}");
        }

        return null;
    }

    public static void SavePlayer(string username, PlayerSaveData data)
    {
        try
        {
            string path = GetSavePath(username);
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error saving player: {ex.Message}");
        }
    }
}