using System.Collections.Generic;
using FDP.Toolkit.Replication.Systems;
using ModuleHost.Core.Abstractions;
using Fdp.Kernel;
using System;

namespace FDP.Toolkit.Replication
{
    public class ReplicationLogicModule : IModule
    {
        public string Name => "ReplicationLogic";
        // Runs every frame on main thread
        public ExecutionPolicy Policy => ExecutionPolicy.Synchronous();
        
        // No constructor args needed - systems are wrappers
        public void RegisterSystems(ISystemRegistry registry)
        {
            registry.RegisterSystem(new SimWrapper<GhostCreationSystem>());
            registry.RegisterSystem(new SimWrapper<GhostPromotionSystem>());
            registry.RegisterSystem(new SimWrapper<OwnershipIngressSystem>());
            registry.RegisterSystem(new SimWrapper<OwnershipEgressSystem>());
            registry.RegisterSystem(new SimWrapper<SmartEgressSystem>());
            registry.RegisterSystem(new SimWrapper<SubEntityCleanupSystem>());
        }

        public void Tick(ISimulationView view, float dt) { }

        // Wrapper to bridge legacy ComponentSystem to IModuleSystem for Simulation Phase
        [UpdateInPhase(SystemPhase.Simulation)]
        private class SimWrapper<T> : IModuleSystem where T : ComponentSystem, new()
        {
            private readonly T _sys = new T();
            private bool _init;
            
            public void Execute(ISimulationView view, float dt)
            {
                if (!_init)
                {
                    _sys.Create((EntityRepository)view);
                    _init = true;
                }
                _sys.Run();
            }
        }
    }
}
