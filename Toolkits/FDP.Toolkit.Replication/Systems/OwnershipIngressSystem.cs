using System;
using Fdp.Kernel;
using Fdp.Interfaces;
using ModuleHost.Core.Abstractions;
using FDP.Toolkit.Replication.Components;
using FDP.Toolkit.Replication.Messages;
using FDP.Toolkit.Replication.Services;

namespace FDP.Toolkit.Replication.Systems
{
    public class OwnershipIngressSystem : ComponentSystem
    {
        private NetworkEntityMap? _entityMap;
        private INetworkTopology? _topology;

        protected override void OnUpdate()
        {
            if (_entityMap == null && World.HasSingletonManaged<NetworkEntityMap>())
                _entityMap = World.GetSingletonManaged<NetworkEntityMap>();

            if (_topology == null && World.HasSingletonManaged<INetworkTopology>())
                _topology = World.GetSingletonManaged<INetworkTopology>();

            if (_entityMap == null) return;
            
            int localNodeId = _topology?.LocalNodeId ?? 0;

            // Consume events (destructive read)
            var updates = ((ISimulationView)World).ConsumeEvents<OwnershipUpdate>();
            
            foreach (var update in updates)
            {
                if (!_entityMap.TryGetEntity(update.NetworkId.Value, out Entity entity))
                {
                    continue;
                }

                if (!World.IsAlive(entity)) continue;

                DescriptorOwnership ownership;
                // Use GetComponent for managed types too via shim if GetManagedComponent is missing
                if (World.HasManagedComponent<DescriptorOwnership>(entity))
                {
                    ownership = World.GetComponent<DescriptorOwnership>(entity);
                }
                else
                {
                    ownership = new DescriptorOwnership();
                    World.SetManagedComponent(entity, ownership);
                }

                ownership.Map[update.PackedKey] = update.NewOwnerNodeId;

                var (typeId, _) = ModuleHost.Core.Network.OwnershipExtensions.UnpackKey(update.PackedKey);
                bool isAuth = (localNodeId != 0 && update.NewOwnerNodeId == localNodeId);
                
                try 
                {
                   World.SetAuthority(entity, (int)typeId, isAuth);
                } 
                catch (Exception) 
                { 
                    // Component might not be present or TypeID invalid for mask
                }

                if (localNodeId != 0 && update.NewOwnerNodeId == localNodeId)
                {
                    World.Bus.Publish(new FDP.Toolkit.Replication.Messages.DescriptorAuthorityChanged
                    {
                        Entity = entity,
                        PackedKey = update.PackedKey,
                        IsAuthoritative = true
                    });
                }
            }
        }
    }
}
