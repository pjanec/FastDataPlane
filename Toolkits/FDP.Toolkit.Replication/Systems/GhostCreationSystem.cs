using System;
using Fdp.Kernel;
using FDP.Toolkit.Replication.Components;
using FDP.Toolkit.Replication.Services;

namespace FDP.Toolkit.Replication.Systems
{
    public class GhostCreationSystem : ComponentSystem
    {
        private NetworkEntityMap? _entityMap;
        
        protected override void OnCreate()
        {
            if (World.HasSingletonManaged<NetworkEntityMap>())
            {
                _entityMap = World.GetSingletonManaged<NetworkEntityMap>();
            }
        }

        protected override void OnUpdate()
        {
            // Re-check for singleton if missing
            if (_entityMap == null && World.HasSingletonManaged<NetworkEntityMap>())
            {
                _entityMap = World.GetSingletonManaged<NetworkEntityMap>();
            }
        }
        
        /// <summary>
        /// Creates a new ghost entity for the given network ID.
        /// </summary>
        public Entity CreateGhost(long networkId)
        {
             if (_entityMap == null)
                throw new InvalidOperationException("NetworkEntityMap not found registered in World singletons.");

            // 1. Allocate Entity
            Entity entity = World.CreateEntity();
            
            // 2. Add NetworkIdentity
            World.AddComponent(entity, new NetworkIdentity(networkId));
            
            // 3. Add BinaryGhostStore
            uint currentFrame = 0;
            if (World.HasSingletonUnmanaged<GlobalTime>())
            {
                currentFrame = (uint)World.GetSingletonUnmanaged<GlobalTime>().FrameNumber;
            }

            World.AddComponent(entity, new BinaryGhostStore 
            { 
                FirstSeenFrame = currentFrame
            });
            
            // 4. Register
            _entityMap.Register(networkId, entity);
            
            return entity;
        }
    }
}
