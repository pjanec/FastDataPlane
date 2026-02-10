using System;
using System.Numerics;
using Fdp.Examples.NetworkDemo.Components;
using Fdp.Examples.NetworkDemo.Events;
using Fdp.Kernel;
using FDP.Toolkit.Replication.Components;
using ModuleHost.Core.Abstractions;

namespace Fdp.Examples.NetworkDemo.Systems
{
    [UpdateInPhase(SystemPhase.Simulation)]
    public class RadarSystem : IModuleSystem
    {
        private readonly IEventBus _eventBus;
        
        public RadarSystem(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        
        public void Execute(ISimulationView view, float dt)
        {
            var query = view.Query()
                .With<NetworkIdentity>()
                .With<DemoPosition>()
                .Build();
            
            foreach (var entity in query) {
                var pos = view.GetComponentRO<DemoPosition>(entity);
                if (Vector3.Distance(pos.Value, Vector3.Zero) < 1000f) {
                    _eventBus.Publish(new RadarContactEvent {
                        EntityId = view.GetComponentRO<NetworkIdentity>(entity).Value,
                        Position = pos.Value,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }
    }
}
