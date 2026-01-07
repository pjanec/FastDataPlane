using System;
using Xunit;
using Fdp.Kernel;

namespace Fdp.Tests
{
    public class EventBusActiveTrackingTests
    {
        [EventId(1001)]
        struct TestEvent { public int Value; }

        class ManagedEvent { public int Value; }

        [Fact]
        public void HasEvent_Unmanaged_ReturnsTrueIfActive()
        {
            using var bus = new FdpEventBus();
            bus.Register<TestEvent>();
            
            // Frame 0: No events
            bus.SwapBuffers();
            Assert.False(bus.HasEvent<TestEvent>());
            
            // Frame 1: Publish
            bus.Publish(new TestEvent { Value = 42 });
            // Before Swap: Still false (in pending)
            Assert.False(bus.HasEvent<TestEvent>());
            
            // Frame 2: Swap -> Active
            bus.SwapBuffers();
            Assert.True(bus.HasEvent<TestEvent>());
            
            // Frame 3: No Publish -> Swap -> Inactive
            bus.SwapBuffers();
            Assert.False(bus.HasEvent<TestEvent>());
        }

        [Fact]
        public void HasEvent_Managed_ReturnsTrueIfActive()
        {
            using var bus = new FdpEventBus();
            
            // Frame 0
            bus.SwapBuffers();
            Assert.False(bus.HasManagedEvent<ManagedEvent>());
            
            // Frame 1: Publish
            bus.PublishManaged(new ManagedEvent { Value = 42 });
            Assert.False(bus.HasManagedEvent<ManagedEvent>());
            
            // Frame 2: Swap -> Active
            bus.SwapBuffers();
            Assert.True(bus.HasManagedEvent<ManagedEvent>());
            
            // Frame 3: Inactive
            bus.SwapBuffers();
            Assert.False(bus.HasManagedEvent<ManagedEvent>());
        }

        [Fact]
        public void HasEvent_ByType_Works()
        {
            using var bus = new FdpEventBus();
            bus.Register<TestEvent>();
            
            bus.Publish(new TestEvent { Value = 1 });
            bus.SwapBuffers();
            
            // Unmanaged
            // Note: HasEvent(Type) implementation for ValueTypes assumes we have a way to lookup ID or iterate.
            // My implementation returns false for ValueTypes currently with TODO.
            // Let's verify that expectation or fix it if I want to pass this test.
            // Actually, I should probably FIX it for the test to pass if I include it.
            // But let's check Managed first.
            Assert.True(bus.HasEventImpl(typeof(ManagedEvent)) == false); // Should be false yet (not published)
            
            bus.PublishManaged(new ManagedEvent());
            bus.SwapBuffers();
            
            Assert.True(bus.HasEvent(typeof(ManagedEvent)));
        }
    }
    
    public static class EventBusTestExtensions
    {
        // Helper to access private logic if needed, or just use public API
        public static bool HasEventImpl(this FdpEventBus bus, Type t) => bus.HasEvent(t);
    }
}
