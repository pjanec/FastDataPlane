using System;
using System.Threading;
using Xunit;
using Fdp.Kernel;

namespace Fdp.Tests
{
    public class TimeSystemTests
    {
        [Fact]
        public void SetFrameTime_DeterministicMode_AdvancesTimeCorrectly()
        {
            var system = new TimeSystem();
            system.IsDeterministic = true;
            
            system.SetFrameTime(0.1);
            Assert.Equal(0.1, system.DeltaTime);
            Assert.Equal(0.1, system.TotalTime);
            Assert.Equal(1ul, system.CurrentTick);
            
            system.SetFrameTime(0.2);
            Assert.Equal(0.2, system.DeltaTime);
            Assert.Equal(0.3, system.TotalTime, precision: 6);
            Assert.Equal(2ul, system.CurrentTick);
        }
        
        [Fact]
        public void BeginFrame_RealTimeMode_ReadingClock()
        {
            var clock = new ManualTimeProvider();
            var system = new TimeSystem(clock);
            system.IsDeterministic = false;
            
            // Initial state (created at clock=0)
            
            // Advance clock by 100ms
            clock.Advance(TimeSpan.FromMilliseconds(100));
            
            system.BeginFrame();
            
            Assert.Equal(0.1, system.DeltaTime, precision: 4);
            Assert.Equal(0.1, system.TotalTime, precision: 4);
            Assert.Equal(1ul, system.CurrentTick);
            
            // Advance clock by 50ms
            clock.Advance(TimeSpan.FromMilliseconds(50));
            system.BeginFrame();
            
            Assert.Equal(0.05, system.DeltaTime, precision: 4);
            Assert.Equal(0.15, system.TotalTime, precision: 4);
            Assert.Equal(2ul, system.CurrentTick);
        }
        
        [Fact]
        public void HasTimeRemaining_ChecksBudget()
        {
            var clock = new ManualTimeProvider();
            var system = new TimeSystem(clock);
            
            // Start frame with 10ms budget
            system.IsDeterministic = false;
            system.BeginFrame(budgetMs: 10.0);
            
            // 0ms elapsed, requires 5ms
            Assert.True(system.HasTimeRemaining(5.0));
            
            // Advance clock by 6ms
            clock.Advance(TimeSpan.FromMilliseconds(6));
            
            // 6ms elapsed. 10 - 6 = 4ms remaining.
            
            // Try to use 5ms (should fail)
            Assert.False(system.HasTimeRemaining(5.0));
            
            // Try to use 3ms (should pass)
            Assert.True(system.HasTimeRemaining(3.0));
            
            // Advance by 5ms (total 11ms elapsed)
            clock.Advance(TimeSpan.FromMilliseconds(5));
            
            // Budget exceeded
            Assert.False(system.HasTimeRemaining(1.0));
        }
        
        [Fact]
        public void SetFrameTime_ResetsBudgetTimer()
        {
            var clock = new ManualTimeProvider();
            var system = new TimeSystem(clock);
            system.IsDeterministic = true;
            
            clock.Advance(TimeSpan.FromSeconds(1)); // Some time passed before frame
            
            // Start deterministic frame with 5ms budget
            system.SetFrameTime(0.016, budgetMs: 5.0);
            
            // Should verify that budgeting starts counting from NOW
            // Advance clock 2ms
            clock.Advance(TimeSpan.FromMilliseconds(2));
            
            Assert.True(system.HasTimeRemaining(2.0)); // 5 - 2 = 3 left
            
            clock.Advance(TimeSpan.FromMilliseconds(4)); // Total 6ms
            Assert.False(system.HasTimeRemaining(1.0));
        }
    }
    
    // Simple mock for TimeProvider
    public class ManualTimeProvider : TimeProvider
    {
        private long _ticks;
        private readonly long _frequency = TimeSpan.TicksPerSecond; // 10,000,000 ticks per sec
        
        public ManualTimeProvider()
        {
            _ticks = 0;
        }
        
        public override long GetTimestamp() => _ticks;
        
        public override long TimestampFrequency => _frequency;
        
        public void Advance(TimeSpan span)
        {
            _ticks += span.Ticks;
        }
        
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(_ticks, TimeSpan.Zero);
        }
    }
}
