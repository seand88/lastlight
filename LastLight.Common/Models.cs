using LiteNetLib.Utils;

namespace LastLight.Common;

public struct Vector2
{
    public float X;
    public float Y;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public class JoinRequest
{
    public string PlayerName { get; set; } = string.Empty;
}

public class JoinResponse
{
    public bool Success { get; set; }
    public int PlayerId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MaxHealth { get; set; }
}

public class PlayerUpdate
{
    public int PlayerId { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Rotation { get; set; }
}

public class InputRequest
{
    public Vector2 Movement { get; set; } // The raw input direction (e.g., WASD)
    public float DeltaTime { get; set; } // Time elapsed for this input
    public int InputSequenceNumber { get; set; } // To match server response with client prediction
}

public class AuthoritativePlayerUpdate
{
    public int PlayerId { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public int LastProcessedInputSequence { get; set; } // Tell client which input this state is based on
    public int CurrentHealth { get; set; }
}

public class FireRequest
{
    public int BulletId { get; set; }
    public Vector2 Direction { get; set; }
}

public class SpawnBullet
{
    public int OwnerId { get; set; }
    public int BulletId { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
}

public enum EntityType : byte
{
    Player,
    Enemy,
    Spawner
}

public class BulletHit
{
    public int BulletId { get; set; }
    public int TargetId { get; set; }
    public EntityType TargetType { get; set; }
}

public class EnemySpawn
{
    public int EnemyId { get; set; }
    public Vector2 Position { get; set; }
    public int MaxHealth { get; set; }
}

public class EnemyUpdate
{
    public int EnemyId { get; set; }
    public Vector2 Position { get; set; }
    public int CurrentHealth { get; set; }
}

public class EnemyDeath
{
    public int EnemyId { get; set; }
}

public class SpawnerSpawn
{
    public int SpawnerId { get; set; }
    public Vector2 Position { get; set; }
    public int MaxHealth { get; set; }
}

public class SpawnerUpdate
{
    public int SpawnerId { get; set; }
    public int CurrentHealth { get; set; }
}

public class SpawnerDeath
{
    public int SpawnerId { get; set; }
}
