using System.Runtime.InteropServices;

namespace Fdp.Kernel
{
    /// <summary>
    /// Global singleton component holding frame timing data.
    /// stored as a Singleton in EntityRepository.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GlobalTime
    {
        /// <summary>
        /// Time elapsed since last frame (in seconds).
        /// Scaled by TimeScale.
        /// </summary>
        public float DeltaTime;

        /// <summary>
        /// The unscaled, real-world time elapsed (in seconds).
        /// Useful for UI or inputs that shouldn't slow down with slo-mo.
        /// </summary>
        public float UnscaledDeltaTime;

        /// <summary>
        /// Total time elapsed since application start (in seconds).
        /// Affected by TimeScale.
        /// </summary>
        public double TotalTime;

        /// <summary>
        /// Total real-world time elapsed (in seconds).
        /// </summary>
        public double UnscaledTotalTime;

        /// <summary>
        /// Current Frame/Tick count.
        /// </summary>
        public ulong FrameCount;

        /// <summary>
        /// Global time multiplier (1.0 = Normal, 0.5 = Slow Mo, 0.0 = Paused).
        /// </summary>
        public float TimeScale;
    }
}
