using System.Collections.Generic;
using Fdp.Examples.NetworkDemo.Systems;
using Fdp.Examples.NetworkDemo.Modules;
using ModuleHost.Core.Abstractions;
using Fdp.Kernel;

namespace Fdp.Examples.NetworkDemo.Modules
{
    public class GameLogicModule : IModule
    {
        public string Name => "GameLogic";
        public ExecutionPolicy Policy => ExecutionPolicy.Synchronous();
        
        private readonly List<IModuleSystem> _systems = new();

        public void AddSystem(IModuleSystem sys) => _systems.Add(sys);

        public void Tick(ISimulationView view, float dt)
        {
            // Execute all game logic systems in order
            foreach (var sys in _systems)
            {
                sys.Execute(view, dt);
            }
        }
    }
}
