using Fdp.Kernel;
using Fdp.Examples.Showcase.Components;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fdp.Examples.Showcase.Systems
{
    public class SpatialMap
    {
        private readonly Dictionary<int, List<Entity>> _buckets = new();
        private const int CellSize = 10; // Bucket size (10x10 units)

        public void Clear()
        {
            foreach (var list in _buckets.Values) list.Clear();
        }

        public void Add(Entity entity, Position pos)
        {
            int key = GetKey(pos.X, pos.Y);
            if (!_buckets.TryGetValue(key, out var list))
            {
                list = new List<Entity>(32); // Pre-allocate
                _buckets[key] = list;
            }
            list.Add(entity);
        }

        public void Query(Position pos, float radius, List<Entity> results)
        {
            results.Clear();
            int minX = (int)(pos.X - radius) / CellSize;
            int maxX = (int)(pos.X + radius) / CellSize;
            int minY = (int)(pos.Y - radius) / CellSize;
            int maxY = (int)(pos.Y + radius) / CellSize;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int key = (x * 73856093) ^ (y * 19349663); // Simple spatial hash
                    if (_buckets.TryGetValue(key, out var list))
                    {
                        results.AddRange(list);
                    }
                }
            }
        }

        private int GetKey(float x, float y)
        {
            int cellX = (int)x / CellSize;
            int cellY = (int)y / CellSize;
            return (cellX * 73856093) ^ (cellY * 19349663);
        }
    }

    public class SpatialSystem : ComponentSystem
    {
        public SpatialMap Map { get; } = new SpatialMap();
        private EntityQuery _query = null!;

        public SpatialSystem(EntityRepository repo)
        {
            Create(repo);
        }

        protected override void OnCreate()
        {
            // Only map things that can collide or be shot (Position + UnitStats)
            _query = World.Query().With<Position>().With<UnitStats>().Build();
        }

        protected override void OnUpdate()
        {
            Map.Clear();
            // This can't be parallelized easily without concurrent collections (slow)
            _query.ForEach(e => 
            {
                ref readonly var pos = ref World.GetComponentRO<Position>(e);
                Map.Add(e, pos);
            });
        }
    }

    public class MovementSystem : ComponentSystem
    {
        private EntityQuery _query = null!;

        public MovementSystem(EntityRepository repo)
        {
            Create(repo);
        }

        protected override void OnCreate()
        {
            _query = World.Query()
                .With<Position>()
                .With<Velocity>()
                .Build();
        }

        protected override void OnUpdate()
        {
            ref var time = ref World.GetSingletonUnmanaged<GlobalTime>();
            float dt = time.DeltaTime * time.TimeScale;

            _query.ForEachParallel(entity =>
            {
                ref var pos = ref World.GetComponentRW<Position>(entity);
                ref var vel = ref World.GetComponentRW<Velocity>(entity);

                pos.X += vel.X * dt;
                pos.Y += vel.Y * dt;
            });
        }
    }

    public class PatrolSystem : ComponentSystem
    {
        private const float MinX = 0f;
        private const float MaxX = 80f; // Console width roughly
        private const float MinY = 0f;
        private const float MaxY = 24f;
        
        private EntityQuery _query = null!;

        public PatrolSystem(EntityRepository repo)
        {
            Create(repo);
        }

        protected override void OnCreate()
        {
            _query = World.Query()
                .With<Position>()
                .With<Velocity>()
                .With<UnitStats>()
                .Build();
        }

        protected override void OnUpdate()
        {
             _query.ForEachParallel(entity =>
             {
                 ref var pos = ref World.GetComponentRW<Position>(entity);
                 ref var vel = ref World.GetComponentRW<Velocity>(entity);
                 
                 // Bounce logic
                 if (pos.X < MinX || pos.X > MaxX) vel.X = -vel.X;
                 if (pos.Y < MinY || pos.Y > MaxY) vel.Y = -vel.Y;
             });
        }
    }
    
    public class CollisionSystem : ComponentSystem
    {
        private const float CollisionRadius = 2.0f;
        private EntityQuery _query = null!;
        private FdpEventBus _eventBus = null!;
        private SpatialSystem _spatial;
        private List<Entity> _nearbyCache = new List<Entity>(64);

        public CollisionSystem(EntityRepository repo, FdpEventBus eventBus, SpatialSystem spatial)
        {
            _spatial = spatial;
            _eventBus = eventBus;
            Create(repo);
        }

        protected override void OnCreate()
        {
            _query = World.Query()
                .With<Position>()
                .With<Velocity>()
                .Build();
        }

        protected override void OnUpdate()
        {
            // _spatial.Map has been updated by SpatialSystem just before this system runs

            _query.ForEach(entityA =>
            {
                ref readonly var posA = ref World.GetComponentRO<Position>(entityA);
                
                // Use Spatial Map to get candidates
                _spatial.Map.Query(posA, CollisionRadius, _nearbyCache);

                foreach (var entityB in _nearbyCache)
                {
                    if (entityA == entityB) continue;

                    ref readonly var posB = ref World.GetComponentRO<Position>(entityB);

                    float dx = posA.X - posB.X;
                    float dy = posA.Y - posB.Y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq < CollisionRadius * CollisionRadius)
                    {
                         // Collision detected! Fire event
                        _eventBus.Publish(new CollisionEvent 
                        { 
                            EntityA = entityA, 
                            EntityB = entityB, 
                            ImpactForce = (float)System.Math.Sqrt(distSq) 
                        });
                        
                        // Simple elastic collision response
                        if (World.HasComponent<Velocity>(entityA) && World.HasComponent<Velocity>(entityB))
                        {
                            ref var velA = ref World.GetComponentRW<Velocity>(entityA);
                            ref var velB = ref World.GetComponentRW<Velocity>(entityB);
                            
                            float tempX = velA.X;
                            float tempY = velA.Y;
                            velA.X = velB.X;
                            velA.Y = velB.Y;
                            velB.X = tempX;
                            velB.Y = tempY;
                        }
                    }
                }
            });
        }
    }
    
    public class CombatSystem : ComponentSystem
    {
        private const float CombatRange = 15.0f; // Detection range
        private const float CombatCooldown = 1.5f; // Seconds between shots
        private EntityQuery _query = null!;
        private System.Collections.Generic.Dictionary<Entity, double> _lastAttackTime = new();
        private FdpEventBus _eventBus = null!;
        private SpatialSystem _spatial;
        private List<Entity> _nearbyCache = new List<Entity>(64);

        public CombatSystem(EntityRepository repo, FdpEventBus eventBus, SpatialSystem spatial)
        {
            _spatial = spatial;
            _eventBus = eventBus;
            Create(repo);
        }

        protected override void OnCreate()
        {
            _query = World.Query()
                .With<Position>()
                .With<UnitStats>()
                .Build();
        }

        protected override void OnUpdate()
        {
            ref var time = ref World.GetSingletonUnmanaged<GlobalTime>();
            var entities = new System.Collections.Generic.List<Entity>();
            _query.ForEach(e => entities.Add(e));

            for (int i = 0; i < entities.Count; i++)
            {
                Entity shooter = entities[i];
                ref var shooterStats = ref World.GetComponentRW<UnitStats>(shooter);
                
                // Skip dead units
                if (shooterStats.Health <= 0) continue;
                
                // Check cooldown
                if (_lastAttackTime.TryGetValue(shooter, out var lastTime))
                {
                    if (time.TotalTime - lastTime < CombatCooldown) continue;
                }
                
                ref readonly var shooterPos = ref World.GetComponentRO<Position>(shooter);
                
                // Find targets in range using Spatial Map
                _spatial.Map.Query(shooterPos, CombatRange, _nearbyCache);

                foreach (var target in _nearbyCache)
                {
                    if (shooter == target) continue;

                    ref var targetStats = ref World.GetComponentRW<UnitStats>(target);
                    
                    // Skip dead or same type
                    if (targetStats.Health <= 0) continue;
                    if (targetStats.Type == shooterStats.Type) continue; // Friendly fire off
                    
                    ref readonly var targetPos = ref World.GetComponentRO<Position>(target);
                    
                    float dx = targetPos.X - shooterPos.X;
                    float dy = targetPos.Y - shooterPos.Y;
                    float distSq = dx * dx + dy * dy;
                    
                    if (distSq < CombatRange * CombatRange && distSq > 0.01f)
                    {
                        // FIRE PROJECTILE!
                        float damage = CalculateDamage(shooterStats.Type, targetStats.Type);
                        float dist = (float)System.Math.Sqrt(distSq);
                        
                        // Spawn projectile
                        var projectile = World.CreateEntity();
                        World.AddComponent(projectile, new Position { X = shooterPos.X, Y = shooterPos.Y });
                        
                        // Calculate velocity towards target
                        float speed = 20f; // Units per second
                        World.AddComponent(projectile, new Velocity 
                        { 
                            X = (dx / dist) * speed, 
                            Y = (dy / dist) * speed 
                        });
                        
                        World.AddComponent(projectile, new Projectile
                        {
                            Owner = shooter,
                            Damage = damage,
                            Speed = speed,
                            Lifetime = 3.0f // 3 seconds max
                        });
                        
                        // Visual: small colored bullet
                        char symbol = '*';
                        System.ConsoleColor color = shooterStats.Type switch
                        {
                            UnitType.Tank => System.ConsoleColor.Yellow,
                            UnitType.Aircraft => System.ConsoleColor.Cyan,
                            _ => System.ConsoleColor.White
                        };
                        World.AddComponent(projectile, new RenderSymbol { Symbol = symbol, Color = color });
                        
                        // Fire event
                        _eventBus.Publish(new ProjectileFiredEvent
                        {
                            Shooter = shooter,
                            Projectile = projectile,
                            ShooterType = shooterStats.Type
                        });
                        
                        _lastAttackTime[shooter] = time.TotalTime;
                        break; // One shot per frame
                    }
                }
            }
        }
        
        private float CalculateDamage(UnitType attacker, UnitType target)
        {
            // Simple rock-paper-scissors damage system
            return (attacker, target) switch
            {
                (UnitType.Tank, UnitType.Infantry) => 25f,      // Tanks strong vs Infantry
                (UnitType.Infantry, UnitType.Aircraft) => 15f,  // Infantry can shoot aircraft
                (UnitType.Aircraft, UnitType.Tank) => 30f,      // Aircraft strong vs Tanks
                (UnitType.Tank, UnitType.Aircraft) => 5f,       // Tanks weak vs Aircraft
                (UnitType.Aircraft, UnitType.Infantry) => 20f,  // Aircraft vs Infantry
                (UnitType.Infantry, UnitType.Tank) => 5f,       // Infantry weak vs Tanks
                _ => 10f
            };
        }
    }
    
    public class ProjectileSystem : ComponentSystem
    {
        private const float HitRadius = 1.5f;
        private EntityQuery _projectileQuery = null!;
        private FdpEventBus _eventBus = null!;
        private LifecycleSystem _lifecycle = null!;
        private SpatialSystem _spatial;
        private List<Entity> _nearbyCache = new List<Entity>(64);

        public ProjectileSystem(EntityRepository repo, FdpEventBus eventBus, LifecycleSystem lifecycle, SpatialSystem spatial)
        {
            _spatial = spatial;
            _eventBus = eventBus;
            _lifecycle = lifecycle;
            Create(repo);
        }

        protected override void OnCreate()
        {
            _projectileQuery = World.Query()
                .With<Position>()
                .With<Projectile>()
                .Build();
        }

        protected override void OnUpdate()
        {
            ref var time = ref World.GetSingletonUnmanaged<GlobalTime>();
            float dt = time.DeltaTime;
            
            var toDestroy = new System.Collections.Generic.List<Entity>();
            
            // Collect all projectiles
            var projectiles = new System.Collections.Generic.List<Entity>();
            _projectileQuery.ForEach(e => projectiles.Add(e));
            
            // Update all projectiles
            foreach (var proj in projectiles)
            {
                ref var projData = ref World.GetComponentRW<Projectile>(proj);
                ref readonly var projPos = ref World.GetComponentRO<Position>(proj);
                
                // Capture values needed in nested loop
                Entity owner = projData.Owner;
                float damage = projData.Damage;
                Position position = projPos;
                
                // Age the projectile
                projData.Lifetime -= dt;
                if (projData.Lifetime <= 0)
                {
                    toDestroy.Add(proj);
                    continue;
                }
                
                // Check for hits against units using Spatial Map
                _spatial.Map.Query(projPos, HitRadius, _nearbyCache);

                foreach (var unit in _nearbyCache)
                {
                    if (unit == owner) continue; // Can't hit self
                    
                    ref var unitStats = ref World.GetComponentRW<UnitStats>(unit);
                    if (unitStats.Health <= 0) continue; // Already dead
                    
                    ref readonly var unitPos = ref World.GetComponentRO<Position>(unit);
                    
                    float dx = position.X - unitPos.X;
                    float dy = position.Y - unitPos.Y;
                    float distSq = dx * dx + dy * dy;
                    
                    if (distSq < HitRadius * HitRadius)
                    {
                        // HIT!
                        unitStats.Health -= damage;
                        
                        // Add hit flash effect
                        if (World.HasComponent<RenderSymbol>(unit))
                        {
                            var render = World.GetComponentRO<RenderSymbol>(unit);
                            World.AddComponent(unit, new HitFlash
                            {
                                Duration = 0.3f,
                                FlashColor = System.ConsoleColor.Red,
                                OriginalColor = render.Color
                            });
                        }
                        
                        // Fire events
                        _eventBus.Publish(new ProjectileHitEvent
                        {
                            Projectile = proj,
                            Target = unit,
                            Damage = damage
                        });
                        
                        _eventBus.Publish(new DamageEvent
                        {
                            Attacker = owner,
                            Target = unit,
                            Damage = damage,
                            AttackerType = World.HasComponent<UnitStats>(owner) 
                                ? World.GetComponentRO<UnitStats>(owner).Type 
                                : UnitType.Infantry
                        });
                        
                        // Check for death
                        if (unitStats.Health <= 0)
                        {
                            unitStats.Health = 0;
                            _eventBus.Publish(new DeathEvent
                            {
                                Entity = unit,
                                Type = unitStats.Type
                            });
                            
                            // Stop the entity from moving (remove velocity)
                            if (World.HasComponent<Velocity>(unit))
                            {
                                World.RemoveComponent<Velocity>(unit);
                            }
                            
                            // Visual death indicator (shows 'x' until corpse timer expires)
                            if (World.HasComponent<RenderSymbol>(unit))
                            {
                                ref var render = ref World.GetComponentRW<RenderSymbol>(unit);
                                render.Color = System.ConsoleColor.DarkGray;
                                render.Symbol = 'x';
                            }
                            
                            // Mark as corpse - will be removed after 1 second
                            // This gives time for particle effects to complete
                            World.AddComponent(unit, new Corpse { TimeRemaining = 1.0f });
                        }
                        
                        toDestroy.Add(proj);
                        break; // Projectile hit something, stop checking
                    }
                }
            }
            
            // Queue destroyed projectiles for cleanup at end of frame
            // Don't destroy immediately - let the barrier handle it
            foreach (var proj in toDestroy)
            {
                _lifecycle.CommandBuffer.DestroyEntity(proj);
            }
        }
    }
    
    public class HitFlashSystem : ComponentSystem
    {
        private EntityQuery _query = null!;

        public HitFlashSystem(EntityRepository repo)
        {
            Create(repo);
        }

        protected override void OnCreate()
        {
            _query = World.Query()
                .With<HitFlash>()
                .With<RenderSymbol>()
                .Build();
        }

        protected override void OnUpdate()
        {
            ref var time = ref World.GetSingletonUnmanaged<GlobalTime>();
            float dt = time.DeltaTime;
            
            var toRemoveQueue = new ConcurrentQueue<Entity>();
            
            _query.ForEachParallel(entity =>
            {
                ref var flash = ref World.GetComponentRW<HitFlash>(entity);
                ref var render = ref World.GetComponentRW<RenderSymbol>(entity);
                
                // Update flash timer
                flash.Duration -= dt;
                
                if (flash.Duration > 0)
                {
                    // Flash to hit color
                    render.Color = flash.FlashColor;
                }
                else
                {
                    // Restore original color
                    render.Color = flash.OriginalColor;
                    toRemoveQueue.Enqueue(entity);
                }
            });
            
            // Remove expired flash components
            while (toRemoveQueue.TryDequeue(out var entity))
            {
                if (World.HasComponent<HitFlash>(entity))
                {
                    World.RemoveComponent<HitFlash>(entity);
                }
            }
        }
    }
    
    public class ParticleSystem : ComponentSystem
    {
        private EntityQuery _query = null!;
        private FdpEventBus _eventBus = null!;
        private LifecycleSystem _lifecycle = null!;

        public ParticleSystem(EntityRepository repo, FdpEventBus eventBus, LifecycleSystem lifecycle)
        {
            _eventBus = eventBus;
            _lifecycle = lifecycle;
            Create(repo);
        }

        protected override void OnCreate()
        {
            _query = World.Query()
                .With<Particle>()
                .With<Position>()
                .Build();
        }

        protected override void OnUpdate()
        {
            ref var time = ref World.GetSingletonUnmanaged<GlobalTime>();
            float dt = time.DeltaTime;
            
            var toDestroy = new System.Collections.Generic.List<Entity>();
            
            // Process death events - spawn explosions
            var deathEvents = _eventBus.Consume<DeathEvent>();
            foreach (ref readonly var deathEvt in deathEvents)
            {
                // Get death position
                if (World.IsAlive(deathEvt.Entity) && World.HasComponent<Position>(deathEvt.Entity))
                {
                    ref readonly var pos = ref World.GetComponentRO<Position>(deathEvt.Entity);
                    SpawnExplosion(pos.X, pos.Y, deathEvt.Type);
                }
            }
            
            // Update particles
            _query.ForEach(entity =>
            {
                ref var particle = ref World.GetComponentRW<Particle>(entity);
                
                particle.Lifetime -= dt;
                
                if (particle.Lifetime <= 0)
                {
                    toDestroy.Add(entity);
                }
                else if (World.HasComponent<RenderSymbol>(entity))
                {
                    // Fade effect
                    ref var render = ref World.GetComponentRW<RenderSymbol>(entity);
                    if (particle.Lifetime < particle.FadeTime * 0.3f)
                    {
                        render.Color = System.ConsoleColor.DarkGray;
                    }
                }
            });
            
            // Queue expired particles for cleanup at end of frame
            foreach (var entity in toDestroy)
            {
                _lifecycle.CommandBuffer.DestroyEntity(entity);
            }
        }
        
        private void SpawnExplosion(float x, float y, UnitType unitType)
        {
            var rand = new System.Random();
            int particleCount = rand.Next(8, 13); // 8-12 particles
            
            System.ConsoleColor explosionColor = unitType switch
            {
                UnitType.Tank => System.ConsoleColor.DarkYellow,
                UnitType.Aircraft => System.ConsoleColor.DarkCyan,
                _ => System.ConsoleColor.Gray
            };
            
            for (int i = 0; i < particleCount; i++)
            {
                float angle = (float)(rand.NextDouble() * System.Math.PI * 2);
                float speed = (float)(rand.NextDouble() * 8 + 4); // 4-12 units/sec
                
                var particle = World.CreateEntity();
                World.AddComponent(particle, new Position { X = x, Y = y });
                World.AddComponent(particle, new Velocity 
                { 
                    X = (float)System.Math.Cos(angle) * speed,
                    Y = (float)System.Math.Sin(angle) * speed
                });
                
                float lifetime = (float)(rand.NextDouble() * 0.5 + 0.5); // 0.5-1.0 seconds
                World.AddComponent(particle, new Particle 
                { 
                    Lifetime = lifetime,
                    FadeTime = lifetime
                });
                
                char[] symbols = new[] { '.', '`', '+', 'o' };
                World.AddComponent(particle, new RenderSymbol 
                { 
                    Symbol = symbols[rand.Next(symbols.Length)],
                    Color = explosionColor
                });
            }
        }
    }
    
    /// <summary>
    /// Lifecycle System - The "End Frame Barrier"
    /// Owns the shared EntityCommandBuffer that other systems write to.
    /// Runs LAST to apply all structural changes (destroy, create, add/remove components)
    /// after all logic systems have completed, ensuring a consistent world view.
    /// </summary>
    public class LifecycleSystem : ComponentSystem
    {
        // The shared buffer - other systems will access this
        public EntityCommandBuffer CommandBuffer { get; private set; }
        private EntityQuery _corpseQuery = null!;

        public LifecycleSystem(EntityRepository repo)
        {
            Create(repo);
            // Initialize with reasonable capacity to avoid resizing
            CommandBuffer = new EntityCommandBuffer(4096);
        }

        protected override void OnCreate()
        {
            _corpseQuery = World.Query()
                .With<Corpse>()
                .Build();
        }

        protected override void OnUpdate()
        {
            ref var time = ref World.GetSingletonUnmanaged<GlobalTime>();
            float dt = time.DeltaTime;
            
            // Process corpses - count down their timers
            var toRemove = new System.Collections.Generic.List<Entity>();
            _corpseQuery.ForEach(entity =>
            {
                ref var corpse = ref World.GetComponentRW<Corpse>(entity);
                corpse.TimeRemaining -= dt;
                
                if (corpse.TimeRemaining <= 0)
                {
                    // Timer expired - queue for destruction
                    toRemove.Add(entity);
                }
            });
            
            // Queue expired corpses for destruction
            foreach (var entity in toRemove)
            {
                CommandBuffer.DestroyEntity(entity);
            }
            
            // Apply all queued commands from this frame
            // This is the ONLY place where structural changes happen
            if (!CommandBuffer.IsEmpty)
            {
                CommandBuffer.Playback(World);
                // Playback clears the buffer automatically in FDP Kernel
            }
        }
        
        // Cleanup
        protected override void OnDestroy()
        {
            CommandBuffer.Dispose();
        }
    }
}
