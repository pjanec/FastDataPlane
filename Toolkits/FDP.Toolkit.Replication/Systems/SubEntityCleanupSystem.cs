using Fdp.Kernel;
using FDP.Toolkit.Replication.Components;

namespace FDP.Toolkit.Replication.Systems
{
    public class SubEntityCleanupSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            // 1. Cleanup orphans (Children whose parents are dead)
            var query = World.Query()
                .With<PartMetadata>()
                .Build();

            using (var ecb = new EntityCommandBuffer())
            {
                foreach (var entity in query)
                {
                    var meta = World.GetComponent<PartMetadata>(entity);
                    if (!World.IsAlive(meta.ParentEntity))
                    {
                        ecb.DestroyEntity(entity);
                    }
                    // Also check if parent is disposing?
                    // Typically IsAlive covers it after the frame boundaries.
                }
                
                // 2. Unlink dead children from parents?
                // This is expensive to scan all parents. 
                // Suggest relying on lazy checks in systems using ChildMap.
                
                ecb.Playback(World);
            }
        }
    }
}
