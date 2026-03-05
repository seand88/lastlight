using System;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using LastLight.Common;

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
    private const string ConnectionString = "Data Source=lastlight.db";

    public static void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Players (
                Uuid TEXT PRIMARY KEY,
                Data TEXT NOT NULL
            );
        ";
        command.ExecuteNonQuery();
    }

    public static PlayerSaveData LoadPlayer(string uuid)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Data FROM Players WHERE Uuid = $uuid";
        command.Parameters.AddWithValue("$uuid", uuid);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var json = reader.GetString(0);
            return JsonSerializer.Deserialize<PlayerSaveData>(json) ?? new PlayerSaveData();
        }

        return null; // Return null if not found
    }

    public static void SavePlayer(string uuid, PlayerSaveData data)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var json = JsonSerializer.Serialize(data);

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Players (Uuid, Data)
            VALUES ($uuid, $data)
            ON CONFLICT(Uuid) DO UPDATE SET Data = excluded.Data;
        ";
        command.Parameters.AddWithValue("$uuid", uuid);
        command.Parameters.AddWithValue("$data", json);
        command.ExecuteNonQuery();
    }
}