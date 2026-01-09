using System;
using Xunit;
using Fdp.Kernel;

namespace Fdp.Tests
{
    public class TransientComponentAttributeTests
    {
        [TransientComponent]
        private struct TransientStructComponent { public int Value; }

        private struct NormalStructComponent { public int Value; }

        [TransientComponent]
        private class TransientManagedComponent { public string Name; }

        private class NormalManagedComponent { public string Name; }

        [Fact]
        public void RegisterComponent_AutoDetectsTransientAttribute_Struct()
        {
            var repo = new EntityRepository();
            repo.RegisterComponent<TransientStructComponent>();

            int id = ComponentType<TransientStructComponent>.ID;
            Assert.False(ComponentTypeRegistry.IsSnapshotable(id));
        }

        [Fact]
        public void RegisterComponent_AutoDetectsTransientAttribute_Managed()
        {
            var repo = new EntityRepository();
            repo.RegisterComponent<TransientManagedComponent>();

            int id = ManagedComponentType<TransientManagedComponent>.ID;
            Assert.False(ComponentTypeRegistry.IsSnapshotable(id));
        }

        [Fact]
        public void RegisterComponent_NormalComponent_IsSnapshotable()
        {
            var repo = new EntityRepository();
            repo.RegisterComponent<NormalStructComponent>();

            int id = ComponentType<NormalStructComponent>.ID;
            Assert.True(ComponentTypeRegistry.IsSnapshotable(id));
        }

        [Fact]
        public void RegisterComponent_ExplicitOverride_ForceSnapshotable()
        {
            var repo = new EntityRepository();
            // Even though it has attribute, we force it true
            repo.RegisterComponent<TransientStructComponent>(snapshotable: true);

            int id = ComponentType<TransientStructComponent>.ID;
            Assert.True(ComponentTypeRegistry.IsSnapshotable(id));
        }

        [Fact]
        public void RegisterComponent_ExplicitOverride_ForceTransient()
        {
            var repo = new EntityRepository();
            // Normal component forced to transient
            repo.RegisterComponent<NormalStructComponent>(snapshotable: false);

            int id = ComponentType<NormalStructComponent>.ID;
            Assert.False(ComponentTypeRegistry.IsSnapshotable(id));
        }
        // Records for testing
        public record TestPlayerStats(int Health, int Score);

        [TransientComponent]
        public record TestDebugData(string Message);

        // Classes for testing
        public class TestGameState  // No attribute (should error)
        {
            public int Value;
        }

        [TransientComponent]
        public class TestUICache  // With attribute (OK)
        {
            public Dictionary<int, string> Cache = new Dictionary<int, string>();
        }

        [Fact]
        public void RegisterManagedComponent_Record_AutoSnapshotable()
        {
            var repo = new EntityRepository();
            repo.RegisterManagedComponent<TestPlayerStats>();
            
            int id = ManagedComponentType<TestPlayerStats>.ID;
            Assert.True(ComponentTypeRegistry.IsSnapshotable(id));
        }

        [Fact]
        public void RegisterManagedComponent_ClassWithoutAttribute_Throws()
        {
            var repo = new EntityRepository();
            
            // Should throw with helpful error message because it's a class without attribute
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                repo.RegisterManagedComponent<TestGameState>();
            });
            
            Assert.Contains("[TransientComponent]", ex.Message);
            Assert.Contains("Convert to 'record'", ex.Message);
        }

        [Fact]
        public void RegisterManagedComponent_ClassWithAttribute_MarksTransient()
        {
            var repo = new EntityRepository();
            repo.RegisterManagedComponent<TestUICache>();
            
            int id = ManagedComponentType<TestUICache>.ID;
            Assert.False(ComponentTypeRegistry.IsSnapshotable(id));
        }

        [Fact]
        public void RegisterManagedComponent_RecordWithAttribute_AttributeWins()
        {
            var repo = new EntityRepository();
            repo.RegisterManagedComponent<TestDebugData>();
            
            int id = ManagedComponentType<TestDebugData>.ID;
            Assert.False(ComponentTypeRegistry.IsSnapshotable(id));
        }

        [Fact]
        public void RegisterManagedComponent_ClassWithoutAttribute_ExplicitOverride_Works()
        {
            var repo = new EntityRepository();
            // Force snapshotable=true even though it's unsafe (escape hatch)
            // Or force false, which is safe.
            // Let's test force false to satisfy "snapshotable: false" solution suggestion
            repo.RegisterManagedComponent<TestGameState>(snapshotable: false);
            
            int id = ManagedComponentType<TestGameState>.ID;
            Assert.False(ComponentTypeRegistry.IsSnapshotable(id));
        }
    }
}
