using Xunit;
using Fdp.Kernel;

using FDP.Toolkit.Time.Controllers;
using FDP.Toolkit.Time.Messages;

namespace FDP.Toolkit.Time.Tests
{
    public class GlobalTimeTests
    {
        [Fact]
        public void GlobalTime_IsPaused_ReturnsTrueWhenScaleIsZero()
        {
            var time = new GlobalTime { TimeScale = 0.0f };
            Assert.True(time.IsPaused);
        }
        
        [Fact]
        public void GlobalTime_IsPaused_ReturnsFalseWhenScaleIsNonZero()
        {
            var time = new GlobalTime { TimeScale = 1.0f };
            Assert.False(time.IsPaused);
        }
    }
}
