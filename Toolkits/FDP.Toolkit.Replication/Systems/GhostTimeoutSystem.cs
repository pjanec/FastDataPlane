using System;
using Fdp.Kernel;
using FDP.Toolkit.Replication.Components;

namespace FDP.Toolkit.Replication.Systems
{
    public class GhostTimeoutSystem : ComponentSystem
    {
        private const uint MAX_GHOST_AGE = 3600; // 60 seconds at 60Hz

        protected override void OnUpdate()
        {
            if (!World.HasSingletonUnmanaged<GlobalTime>()) return;
            var globalTime = World.GetSingletonUnmanaged<GlobalTime>();
            uint currentFrame = (uint)globalTime.FrameNumber;
            
            // Query all ghosts
            var query = World.Query()
                .WithManaged<BinaryGhostStore>()
                .Build();
            
            // Use EntityCommandBuffer to destroy entities while iterating
            using (var ecb = new EntityCommandBuffer())
            {
                foreach (var entity in query)
                {
                    var store = World.GetComponent<BinaryGhostStore>(entity);
                    if (store == null) continue;
                    
                    if (store.FirstSeenFrame == 0)
                    {
                        store.FirstSeenFrame = currentFrame;
                    }
                    else
                    {
                        uint age = currentFrame - store.FirstSeenFrame;
                        if (age > MAX_GHOST_AGE)
                        {
                            ecb.DestroyEntity(entity);
                        }
                    }
                }
                
                ecb.Playback(World);
            }
        }
    }
}
