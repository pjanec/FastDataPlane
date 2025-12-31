using System;
using Fdp.Kernel;

namespace Fdp.Examples.Showcase.Components
{
    public struct Position
    {
        public float X;
        public float Y;
    }

    public struct Velocity
    {
        public float X;
        public float Y;
    }

    public struct RenderSymbol
    {
        public char Symbol;
        public ConsoleColor Color;
    }

    public enum UnitType
    {
        Infantry,
        Tank,
        Aircraft
    }

    public struct UnitStats
    {
        public UnitType Type;
        public float Health;
        public float MaxHealth;
    }
    
    // Projectile component
    public struct Projectile
    {
        public Entity Owner;
        public float Damage;
        public float Speed;
        public float Lifetime; // Time until auto-destroy
    }
    
    // Visual hit flash effect
    public struct HitFlash
    {
        public float Duration;
        public ConsoleColor FlashColor;
        public ConsoleColor OriginalColor;
    }
    
    // Particle for explosions
    public struct Particle
    {
        public float Lifetime;
        public float FadeTime;
    }
    
    // Events for demonstration of Event Bus
    [EventId(100)]
    public struct CollisionEvent
    {
        public Entity EntityA;
        public Entity EntityB;
        public float ImpactForce;
    }
    
    [EventId(101)]
    public struct ProjectileFiredEvent
    {
        public Entity Shooter;
        public Entity Projectile;
        public UnitType ShooterType;
    }
    
    [EventId(102)]
    public struct ProjectileHitEvent
    {
        public Entity Projectile;
        public Entity Target;
        public float Damage;
    }
    
    [EventId(103)]
    public struct DamageEvent
    {
        public Entity Attacker;
        public Entity Target;
        public float Damage;
        public UnitType AttackerType;
    }
    
    [EventId(104)]
    public struct DeathEvent
    {
        public Entity Entity;
        public UnitType Type;
    }
}
