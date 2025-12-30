using Xunit;
using Fdp.Kernel;
using Fdp.Kernel.Tkb;
using System;

namespace Fdp.Tests
{
    public class TkbTests
    {
        // Test Components
        public struct Position
        {
            public float X, Y, Z;
        }

        public struct Health
        {
            public int Current;
            public int Max;
        }

        public class Inventory
        {
            public string[] Items = Array.Empty<string>();
        }

        [Fact]
        public void Spawn_UnmanagedComponents_AreCopied()
        {
            using var repo = new EntityRepository();
            repo.RegisterComponent<Position>();
            repo.RegisterComponent<Health>();

            // 1. Define Template
            var tkb = new TkbDatabase();
            var template = new TkbTemplate("Grunt");
            
            template.AddComponent(new Position { X = 10, Y = 20, Z = 30 });
            template.AddComponent(new Health { Current = 100, Max = 100 });
            
            tkb.AddTemplate(template);

            // 2. Spawn Entity
            var entity = tkb.Spawn("Grunt", repo);

            // 3. Verify
            Assert.True(repo.HasComponent<Position>(entity));
            Assert.True(repo.HasComponent<Health>(entity));

            ref var pos = ref repo.GetComponentRW<Position>(entity);
            Assert.Equal(10, pos.X);
            Assert.Equal(20, pos.Y);
            
            ref var hp = ref repo.GetComponentRW<Health>(entity);
            Assert.Equal(100, hp.Current);
        }

        [Fact]
        public void Spawn_ManagedComponents_UseFactory()
        {
            using var repo = new EntityRepository();
            repo.RegisterComponent<Inventory>();

            // 1. Define Template
            var tkb = new TkbDatabase();
            var template = new TkbTemplate("Hero");

            // Use factory to create new list for each hero
            template.AddManagedComponent(() => new Inventory 
            { 
                Items = new[] { "Sword", "Shield" } 
            });

            tkb.AddTemplate(template);

            // 2. Spawn Two Entities
            var e1 = tkb.Spawn("Hero", repo);
            var e2 = tkb.Spawn("Hero", repo);

            // 3. Verify Data
            var inv1 = repo.GetComponentRW<Inventory>(e1);
            var inv2 = repo.GetComponentRW<Inventory>(e2);

            Assert.Equal("Sword", inv1.Items[0]);
            Assert.Equal("Sword", inv2.Items[0]);

            // 4. Verify Independence (Modify one, other should stay same)
            inv1.Items[0] = "Broken Sword";
            
            Assert.Equal("Broken Sword", inv1.Items[0]);
            Assert.Equal("Sword", inv2.Items[0]); // Should be distinct instance
        }

        [Fact]
        public void Spawn_UnknownTemplate_Throws()
        {
            using var repo = new EntityRepository();
            var tkb = new TkbDatabase();

            Assert.Throws<KeyNotFoundException>(() => 
            {
                tkb.Spawn("MissingNo", repo);
            });
        }

        [Fact]
        public void AddTemplate_DuplicateName_Throws()
        {
            var tkb = new TkbDatabase();
            var t1 = new TkbTemplate("Same");
            var t2 = new TkbTemplate("Same");

            tkb.AddTemplate(t1);
            
            Assert.Throws<InvalidOperationException>(() => 
            {
                tkb.AddTemplate(t2);
            });
        }
        
        [Fact]
        public void Spawn_AppliesSequentialIDs()
        {
             // Verify that multiple spawns gets unique entities
             using var repo = new EntityRepository();
             repo.RegisterComponent<Position>();
             
             var tkb = new TkbDatabase();
             var t = new TkbTemplate("Prop");
             t.AddComponent(new Position());
             tkb.AddTemplate(t);
             
             var e1 = tkb.Spawn("Prop", repo);
             var e2 = tkb.Spawn("Prop", repo);
             
             Assert.NotEqual(e1, e2);
             Assert.NotEqual(e1.Index, e2.Index);
        }
    }
}
