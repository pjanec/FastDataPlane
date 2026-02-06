using System;
using System.Collections.Generic;

namespace Fdp.Kernel.FlightRecorder.Metadata
{
    [Serializable]
    public class RecordingMetadata
    {
        public int ProtocolVersion { get; set; } = 1;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string AppVersion { get; set; } = "1.0.0";
        public string Description { get; set; } = "";
        public int TotalFrames { get; set; } = 0;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public Dictionary<string, string> CustomTags { get; set; } = new Dictionary<string, string>();
    }
}
