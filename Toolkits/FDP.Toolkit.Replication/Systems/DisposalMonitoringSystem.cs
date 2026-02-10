using System;
using Fdp.Kernel;
using FDP.Toolkit.Replication.Services;

namespace FDP.Toolkit.Replication.Systems
{
    public class DisposalMonitoringSystem : ComponentSystem
    {
        private NetworkEntityMap? _entityMap;
        
        protected override void OnUpdate()
        {
             if (_entityMap == null && World.HasSingletonManaged<NetworkEntityMap>())
                _entityMap = World.GetSingletonManaged<NetworkEntityMap>();

             if (_entityMap == null) return;
             
             // Detect dead entities and move them to graveyard
             _entityMap.PruneDeadEntities(World);
        }
    }
}
