using System;

namespace Fdp.Kernel
{
    /// <summary>
    /// Manages time tracking, frame deltas, and execution budgeting.
    /// Supports both deterministic (fixed step) and real-time (clock-based) modes.
    /// Uses System.TimeProvider for abstracting time sources (testing, real-time).
    /// </summary>
    public class TimeSystem
    {
        private readonly TimeProvider _timeProvider;
        private long _frameStartTimestamp;
        private double _frameBudgetMs;
        private long _lastFrameTimestamp;

        /// <summary>
        /// Gets the time elapsed since the last frame in seconds.
        /// </summary>
        public double DeltaTime { get; private set; }

        /// <summary>
        /// Gets the total time accumulated since start in seconds.
        /// </summary>
        public double TotalTime { get; private set; }

        /// <summary>
        /// Gets the current frame/tick count.
        /// </summary>
        public ulong CurrentTick { get; private set; }

        /// <summary>
        /// If true, automatic clock reading is disabled and time must be injected via SetFrameTime.
        /// </summary>
        public bool IsDeterministic { get; set; }

        /// <summary>
        /// Initializes a new instance of the TimeSystem.
        /// </summary>
        /// <param name="timeProvider">Optional time source. Defaults to System.TimeProvider.System.</param>
        public TimeSystem(TimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? TimeProvider.System;
            Reset();
        }

        /// <summary>
        /// Resets the time system state (TotalTime, Tick, etc).
        /// </summary>
        public void Reset()
        {
            TotalTime = 0;
            CurrentTick = 0;
            DeltaTime = 0;
            _lastFrameTimestamp = _timeProvider.GetTimestamp();
            _frameStartTimestamp = _lastFrameTimestamp;
            _frameBudgetMs = double.PositiveInfinity;
        }

        /// <summary>
        /// Injects a specific time delta for the current frame.
        /// Enforces deterministic mode logic.
        /// Resets the frame budget timer.
        /// </summary>
        /// <param name="deltaSeconds">The time step to advance.</param>
        /// <param name="budgetMs">Optional execution budget for this frame in milliseconds.</param>
        public void SetFrameTime(double deltaSeconds, double budgetMs = double.PositiveInfinity)
        {
            // Deterministic mode: Time is advanced explicitly
            DeltaTime = deltaSeconds;
            TotalTime += deltaSeconds;
            CurrentTick++;
            
            // Mark start of frame for budgeting purposes
            _frameStartTimestamp = _timeProvider.GetTimestamp();
            _frameBudgetMs = budgetMs;
        }

        /// <summary>
        /// Advances the frame based on the attached clock (real-time mode).
        /// Should typically be used when IsDeterministic is false.
        /// </summary>
        /// <param name="budgetMs">Optional execution budget for this frame in milliseconds.</param>
        public void BeginFrame(double budgetMs = double.PositiveInfinity)
        {
            if (IsDeterministic)
            {
                // In deterministic mode, BeginFrame conceptually just resets the budget timer
                // assuming SetFrameTime was called or will be called.
                // However, usually SetFrameTime handles the advance.
                // We'll just reset the budget anchors here to be safe without advancing time if not asked.
                _frameStartTimestamp = _timeProvider.GetTimestamp();
                _frameBudgetMs = budgetMs;
                return;
            }

            long now = _timeProvider.GetTimestamp();
            TimeSpan elapsed = _timeProvider.GetElapsedTime(_lastFrameTimestamp, now);
            
            DeltaTime = elapsed.TotalSeconds;
            TotalTime += DeltaTime;
            CurrentTick++;

            _lastFrameTimestamp = now;
            _frameStartTimestamp = now;
            _frameBudgetMs = budgetMs;
        }

        /// <summary>
        /// Checks if there is enough budget remaining to perform an operation.
        /// </summary>
        /// <param name="msRequired">Estimated milliseconds required for the operation.</param>
        /// <returns>True if the operation fits in the remaining budget.</returns>
        public bool HasTimeRemaining(double msRequired)
        {
            if (double.IsPositiveInfinity(_frameBudgetMs))
                return true;

            long now = _timeProvider.GetTimestamp();
            double elapsedMs = _timeProvider.GetElapsedTime(_frameStartTimestamp, now).TotalMilliseconds;
            double remaining = _frameBudgetMs - elapsedMs;

            return remaining >= msRequired;
        }
        
        /// <summary>
        /// Gets the elapsed milliseconds spent in the current frame so far.
        /// </summary>
        public double GetFrameElapsedMs()
        {
            long now = _timeProvider.GetTimestamp();
            return _timeProvider.GetElapsedTime(_frameStartTimestamp, now).TotalMilliseconds;
        }
    }
}
