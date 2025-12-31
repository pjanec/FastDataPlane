using Fdp.Kernel;
using Fdp.Examples.Showcase.Components;

namespace Fdp.Examples.Showcase.Systems
{
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
}
