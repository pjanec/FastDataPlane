using Xunit;
using Fdp.Kernel;
using System;

namespace Fdp.Tests
{
    public class EntityRepositorySyncTests
    {
        // Define components
        struct Pos { public float X; }
        struct Vel { public float X; }
        
        record Tag(string Label);

        [Fact]
        public void FullSync_CopiesAllDirtyChunks()
        {
             using var source = new EntityRepository();
             using var dest = new EntityRepository();
             
             source.RegisterComponent<Pos>();
             source.RegisterComponent<Vel>();
             dest.RegisterComponent<Pos>();
             dest.RegisterComponent<Vel>();
             
             var e = source.CreateEntity();
             source.AddComponent(e, new Pos { X=1 });
             source.Tick(); // GlobalVersion++
             
             dest.SyncFrom(source);
             
             var destE = new Entity(e.Index, e.Generation);
             Assert.True(dest.IsAlive(destE));
             Assert.Equal(1f, dest.GetComponentRO<Pos>(destE).X);
             Assert.Equal(source.GlobalVersion, dest.GlobalVersion);
        }

        [Fact]
         public void FilteredSync_CopiesOnlyMaskedComponents()
         {
             using var source = new EntityRepository();
             using var dest = new EntityRepository();
             
             source.RegisterComponent<Pos>();
             source.RegisterComponent<Vel>();
             dest.RegisterComponent<Pos>();
             dest.RegisterComponent<Vel>();
             
             var e = source.CreateEntity();
             source.AddComponent(e, new Pos { X=10 });
             source.AddComponent(e, new Vel { X=20 });
             
             // Create mask for Pos only
             var mask = new BitMask256();
             mask.SetBit(ComponentType<Pos>.ID);
             
             dest.SyncFrom(source, mask);
             
             var destE = new Entity(e.Index, e.Generation);
             Assert.True(dest.HasComponent<Pos>(destE));
             Assert.False(dest.HasComponent<Vel>(destE)); 
         }
        
        [Fact]
        public void Tier2_ShallowCopy_Works()
        {
             using var source = new EntityRepository();
             using var dest = new EntityRepository();
             source.RegisterManagedComponent<Tag>();
             dest.RegisterManagedComponent<Tag>();
             
             var e = source.CreateEntity();
             var tag = new Tag("Test");
             source.AddComponent(e, tag);
             
             dest.SyncFrom(source);
             
             var destE = new Entity(e.Index, e.Generation);
             var destTag = dest.GetComponentRO<Tag>(destE);
             
             Assert.Same(tag, destTag);
        }
        
        [Fact]
        public void EmptySource_ClearsDestination()
        {
            using var source = new EntityRepository();
            using var dest = new EntityRepository();
            source.RegisterComponent<Pos>();
            dest.RegisterComponent<Pos>();
            
            // Dest has data
            var e = dest.CreateEntity();
            dest.AddComponent(e, new Pos { X=1 });
            
            // Source has no entities (we just key it clean)
            // But structural sync needs Empty source repository?
            // "Source" is empty by default (no entities).
            // But wait, SyncFrom syncs chunks.
            // If Source keys are empty, it syncs empty keys.
            // But if Dest keys are not empty, they should be cleared?
            // EntityIndex.SyncEmpty clears Dest.
            // SyncDirtyChunks on tables clears chunks.
            
            // Fresh source
            using var freshSource = new EntityRepository();
            freshSource.RegisterComponent<Pos>();
            
            dest.SyncFrom(freshSource);
            
            // Check
            Assert.Equal(0, dest.EntityCount);
            Assert.False(dest.IsAlive(e));
        }

        [Fact]
        public void Performance_MeetsTarget()
        {
             using var source = new EntityRepository();
             using var dest = new EntityRepository();
             // Register some components
             source.RegisterComponent<Pos>();
             dest.RegisterComponent<Pos>();
             
             // 100K entities
             int count = 100_000;
             for(int i=0; i<count; i++)
             {
                 var e = source.CreateEntity();
                 if (i % 2 == 0) source.AddComponent(e, new Pos{X=i});
             }
             
             var sw = System.Diagnostics.Stopwatch.StartNew();
             dest.SyncFrom(source);
             sw.Stop();
             
             // Allowing 50ms for cold start + full sync. 
             // Target is 2ms BUT for "30% dirty". This is 100% dirty.
             Assert.True(sw.ElapsedMilliseconds < 50, $"Time: {sw.ElapsedMilliseconds}");
        }
    }
}
