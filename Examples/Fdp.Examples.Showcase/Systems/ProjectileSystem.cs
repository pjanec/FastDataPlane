using System.Collections.Generic;
using Fdp.Kernel;
using Fdp.Examples.Showcase.Components;

namespace Fdp.Examples.Showcase.Systems
{
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
}
