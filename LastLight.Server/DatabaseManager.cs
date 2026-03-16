using System;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using LastLight.Common;

namespace LastLight.Server;

public class PlayerSaveData
{
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int MaxHealth { get; set; } = 100;

    // Stats
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 0;
    public int Speed { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Vitality { get; set; } = 10;
    public int Wisdom { get; set; } = 10;

    // Progression
    public int ToolbeltSize { get; set; } = 3;

    // Collections
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[5];
    public ItemInfo[] Toolbelt { get; set; } = new ItemInfo[8];
    public ItemInfo[] Stash { get; set; } = new ItemInfo[50];
}

public static class DatabaseManager
{
    private const string ConnectionString = "Data Source=lastlight.db";

    public static void Initialize(bool resetDb = false)
    {
        bool dbExists = System.IO.File.Exists("lastlight.db");
        
        if (resetDb && dbExists)
        {
            System.IO.File.Delete("lastlight.db");
            dbExists = false;
            Console.WriteLine("[Server] Database reset requested. Deleted existing lastlight.db.");
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        if (!dbExists)
        {
            Console.WriteLine("[Server] Creating and seeding new database...");
            string schemaPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "schema.sql");
            if (System.IO.File.Exists(schemaPath))
            {
                var schema = System.IO.File.ReadAllText(schemaPath);
                var command = connection.CreateCommand();
                command.CommandText = schema;
                command.ExecuteNonQuery();
                Console.WriteLine("[Server] Database seeded successfully.");
            }
            else
            {
                // Fallback for some execution environments where CWD is root
                schemaPath = System.IO.Path.Combine("LastLight.Server", "Database", "schema.sql");
                if (System.IO.File.Exists(schemaPath))
                {
                    var schema = System.IO.File.ReadAllText(schemaPath);
                    var command = connection.CreateCommand();
                    command.CommandText = schema;
                    command.ExecuteNonQuery();
                    Console.WriteLine("[Server] Database seeded successfully (via fallback path).");
                }
                else
                {
                    Console.WriteLine($"[Server] WARNING: Could not find schema script at {schemaPath}");
                }
            }
        }
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