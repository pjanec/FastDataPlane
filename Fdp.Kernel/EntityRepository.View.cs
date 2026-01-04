using System;
using Fdp.Kernel;
using Fdp.Kernel.Internal;
using ModuleHost.Core.Abstractions;

namespace Fdp.Kernel
{
    public sealed partial class EntityRepository : ISimulationView
    {
        // Properties
        uint ISimulationView.Tick => _globalVersion;
        
        float ISimulationView.Time => _simulationTime;
        
        // Methods
        
        ref readonly T ISimulationView.GetComponentRO<T>(Entity e)
        {
            // Delegate to existing internal methods via UnsafeShim or direct if accessible
            // Since we are in EntityRepository, we can call internal methods directly if we know them.
            // But GetComponentRO logic differs for managed/unmanaged.
            // ISimulationView splits them.
            
            // For unmanaged T:
            return ref GetUnmanagedComponentRO<T>(e);
        }
        
        T ISimulationView.GetManagedComponentRO<T>(Entity e)
        {
            // Call internal method directly
            var val = GetManagedComponentRO<T>(e);
            if (val == null) throw new InvalidOperationException($"Entity {e} missing component {typeof(T).Name}");
            return val;
        }
        
        bool ISimulationView.IsAlive(Entity e)
        {
            return IsAlive(e);
        }
        
        ReadOnlySpan<T> ISimulationView.ConsumeEvents<T>()
        {
            return Bus.Consume<T>();
        }
        
        QueryBuilder ISimulationView.Query()
        {
            return Query();
        }
    }
}
