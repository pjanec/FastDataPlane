using System.Collections.Generic;
using FDP.Toolkit.Replication.Systems;
using ModuleHost.Core.Abstractions;
using Fdp.Kernel;

namespace FDP.Toolkit.Replication
{
    public class ReplicationLogicModule : IModule
    {
        public string Name => "ReplicationLogic";
        public ExecutionPolicy Policy => ExecutionPolicy.Synchronous(); // Runs every frame
        
        private readonly List<ComponentSystem> _componentSystems = new();
        private readonly List<IModuleSystem> _moduleSystems = new();

        public ReplicationLogicModule(EntityRepository world)
        {
            // Standard Replication Pipeline (Component Systems)
            AddComponentSystem(new GhostCreationSystem(), world);
            AddComponentSystem(new GhostPromotionSystem(), world);
            AddComponentSystem(new OwnershipIngressSystem(), world);
            AddComponentSystem(new OwnershipEgressSystem(), world);
            AddComponentSystem(new SmartEgressSystem(), world);
            AddComponentSystem(new DisposalMonitoringSystem(), world);
            AddComponentSystem(new SubEntityCleanupSystem(), world);
        }
        
        private void AddComponentSystem(ComponentSystem sys, EntityRepository world)
        {
            sys.Create(world);
            _componentSystems.Add(sys);
        }

        // Allow injecting extra systems if needed
        public void AddSystem(IModuleSystem sys) => _moduleSystems.Add(sys);

        public void Tick(ISimulationView view, float dt)
        {
            // Execute component systems
            foreach (var sys in _componentSystems)
            {
                sys.Run();
            }

            // Execute module systems
            foreach (var sys in _moduleSystems)
            {
                sys.Execute(view, dt);
            }
        }
    }
}
