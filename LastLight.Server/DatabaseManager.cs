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
                Id TEXT PRIMARY KEY,
                Username TEXT UNIQUE NOT NULL,
                Data TEXT NOT NULL
            );
        ";
        command.ExecuteNonQuery();
    }

    public static PlayerSaveData? LoadPlayer(string username)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Data FROM Players WHERE Username = $username";
        command.Parameters.AddWithValue("$username", username);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var json = reader.GetString(0);
            return JsonSerializer.Deserialize<PlayerSaveData>(json) ?? new PlayerSaveData();
        }

        return null; // Return null if not found
    }

    public static void SavePlayer(string username, PlayerSaveData data)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var json = JsonSerializer.Serialize(data);
        var newId = Guid.NewGuid().ToString();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Players (Id, Username, Data)
            VALUES ($id, $username, $data)
            ON CONFLICT(Username) DO UPDATE SET Data = excluded.Data;
        ";
        command.Parameters.AddWithValue("$id", newId);
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$data", json);
        command.ExecuteNonQuery();
    }
}