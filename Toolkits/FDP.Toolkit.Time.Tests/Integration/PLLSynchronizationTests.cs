using System;
using Xunit;
using FDP.Toolkit.Time.Controllers;
using FDP.Toolkit.Time.Messages;
using ModuleHost.Core.Time;
using Fdp.Kernel;
using System.Diagnostics;

namespace FDP.Toolkit.Time.Tests.Integration
{
    public class PLLSynchronizationTests
    {
        [Fact]
        public void PLL_SmoothsTime_WhenPulsesJitter()
        {
            var bus = new FdpEventBus();
            // Increase SnapThreshold because we step 1.0s at a time, which exceeds default 500ms threshold
            var config = new TimeConfig { PLLGain = 0.5, SnapThresholdMs = 2000 }; 
            
            long currentTicks = 0;
            Func<long> tickSource = () => currentTicks;
            
            var slave = new SlaveTimeController(bus, config, tickSource); // Calls internal constructor
            
            // Initial State: T=0
            slave.SeedState(new GlobalTime { TotalTime = 0, TimeScale = 1.0f });
            
            // Advance Wall Clock by 1.0s
            long ticksPerSec = Stopwatch.Frequency;
            currentTicks += ticksPerSec; // +1s
            
            // Publish Master Pulse for T=1.0 (perfect sync first)
            // TimePulseDescriptor uses SimTimeSnapshot for TotalTime
            bus.Publish(new TimePulseDescriptor 
            { 
                SimTimeSnapshot = 1.0, 
                SequenceId = 1, 
                TimeScale = 1.0f,
                MasterWallTicks = currentTicks 
            });
            bus.SwapBuffers();
            
            // Update
            var time1 = slave.Update();
            Assert.Equal(1.0, time1.TotalTime, 0.05); // Should match closely
            
            // Advance Wall Clock by 1.0s (T=2.0 locally)
            currentTicks += ticksPerSec;
            
            // Master Pulse arrives indicating T=2.2 (Master is ahead 0.2s)
            // Master says at Wall=2.0s, Time was 2.2s.
            bus.Publish(new TimePulseDescriptor 
            { 
                SimTimeSnapshot = 2.2, 
                SequenceId = 2, 
                TimeScale = 1.0f,
                MasterWallTicks = currentTicks 
            });
            bus.SwapBuffers();
            
            var time2 = slave.Update();
            
            // Expectation: TotalTime should be > 2.0 (physics advanced)
            // But NOT full 2.2 (smoothing error)
            Assert.True(time2.TotalTime > 2.0, $"Time {time2.TotalTime} should be > 2.0");
            Assert.True(time2.TotalTime < 2.2, $"Time {time2.TotalTime} should be < 2.2 (smoothed)");
            
            // Verify TimeScale increased > 1.0 to catch up
            // Assert.True(slave.GetTimeScale() > 1.0f, "TimeScale should increase to catch up");
            // PLL adjusts DeltaTime, not the persistent TimeScale property (which mirrors Master)
            Assert.True(time2.DeltaTime > 1.0, $"DeltaTime {time2.DeltaTime} should be > 1.0 (PLL catchup)");
        }
    }
}
