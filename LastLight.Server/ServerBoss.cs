using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;

namespace LastLight.Server;

public class ServerBoss
{
    public int Id { get; set; }
    public Vector2 Position;
    public Vector2 Velocity;
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public bool Active { get; set; }
    public byte Phase { get; set; } = 1;
    public float Speed { get; set; } = 50f;

    public Action<ServerBoss, Vector2, Vector2>? OnShoot;
    private float _shootTimer = 0f;
    private float _shootInterval = 1.0f;
    private float _phaseTimer = 0f;

    public void Update(float dt, Dictionary<int, AuthoritativePlayerUpdate> players)
    {
        if (!Active) return;

        UpdatePhase();

        // Target nearest player
        AuthoritativePlayerUpdate? target = players.Values.OrderBy(p => 
            Math.Pow(p.Position.X - Position.X, 2) + Math.Pow(p.Position.Y - Position.Y, 2)).FirstOrDefault();

        if (target != null)
        {
            float dx = target.Position.X - Position.X;
            float dy = target.Position.Y - Position.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            // Phase logic for movement
            if (Phase == 1)
            {
                if (dist > 200)
                {
                    Velocity.X = (dx / dist) * Speed;
                    Velocity.Y = (dy / dist) * Speed;
                }
                else Velocity = new Vector2(0, 0);
            }
            else if (Phase == 2)
            {
                Velocity.X = (dx / dist) * Speed * 1.5f;
                Velocity.Y = (dy / dist) * Speed * 1.5f;
            }
            else // Phase 3: Erratic movement
            {
                _phaseTimer += dt;
                Velocity.X = (float)Math.Cos(_phaseTimer * 2) * Speed * 2f;
                Velocity.Y = (float)Math.Sin(_phaseTimer * 2) * Speed * 2f;
            }

            // Shooting logic
            _shootTimer += dt;
            if (_shootTimer >= _shootInterval)
            {
                _shootTimer = 0;
                ExecuteAttack(target);
            }
        }

        Position.X += Velocity.X * dt;
        Position.Y += Velocity.Y * dt;
    }

    private void UpdatePhase()
    {
        float healthPerc = (float)CurrentHealth / MaxHealth;
        if (healthPerc > 0.66f) Phase = 1;
        else if (healthPerc > 0.33f) Phase = 2;
        else Phase = 3;

        _shootInterval = Phase switch {
            1 => 1.0f,
            2 => 0.5f,
            3 => 0.1f,
            _ => 1.0f
        };
    }

    private void ExecuteAttack(AuthoritativePlayerUpdate target)
    {
        float dx = target.Position.X - Position.X;
        float dy = target.Position.Y - Position.Y;
        float angle = (float)Math.Atan2(dy, dx);

        if (Phase == 1)
        {
            // Triple Aimed Shot
            for (int i = -1; i <= 1; i++)
            {
                float a = angle + (i * 0.2f);
                var vel = new Vector2((float)Math.Cos(a) * 300f, (float)Math.Sin(a) * 300f);
                OnShoot?.Invoke(this, Position, vel);
            }
        }
        else if (Phase == 2)
        {
            // 12-way Radial
            for (int i = 0; i < 12; i++)
            {
                float a = (float)(i * Math.PI * 2 / 12);
                var vel = new Vector2((float)Math.Cos(a) * 250f, (float)Math.Sin(a) * 250f);
                OnShoot?.Invoke(this, Position, vel);
            }
        }
        else
        {
            // Rapid Spiral
            _phaseTimer += 0.5f;
            float a = _phaseTimer;
            var vel = new Vector2((float)Math.Cos(a) * 400f, (float)Math.Sin(a) * 400f);
            OnShoot?.Invoke(this, Position, vel);
        }
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Active = false;
        }
    }
}

public class ServerBossManager
{
    private readonly Dictionary<int, ServerBoss> _bosses = new();
    private int _nextBossId = 2000;

    public Action<ServerBoss>? OnBossSpawned;
    public Action<ServerBoss>? OnBossDied;
    public Action<ServerBoss, Vector2, Vector2>? OnBossShoot;

    public void SpawnBoss(Vector2 position, int health = 5000)
    {
        var boss = new ServerBoss {
            Id = _nextBossId++,
            Position = position,
            MaxHealth = health,
            CurrentHealth = health,
            Active = true
        };
        boss.OnShoot = (b, p, v) => OnBossShoot?.Invoke(b, p, v);
        _bosses[boss.Id] = boss;
        OnBossSpawned?.Invoke(boss);
    }

    public void Update(float dt, Dictionary<int, AuthoritativePlayerUpdate> players)
    {
        foreach (var boss in _bosses.Values.ToList())
        {
            if (boss.Active) boss.Update(dt, players);
        }
    }

    public void HandleDamage(int id, int damage)
    {
        if (_bosses.TryGetValue(id, out var boss) && boss.Active)
        {
            boss.TakeDamage(damage);
            if (!boss.Active) OnBossDied?.Invoke(boss);
        }
    }

    public IReadOnlyCollection<ServerBoss> GetActiveBosses() => _bosses.Values.Where(b => b.Active).ToList();
    public IReadOnlyCollection<ServerBoss> GetAllBosses() => _bosses.Values.ToList();
}
