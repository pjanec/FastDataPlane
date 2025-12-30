using System;
using System.Collections.Generic;

namespace Fdp.Kernel
{
    /// <summary>
    /// Double-buffered event stream for managed (reference type) events.
    /// Uses locking for thread safety since List<T> is not thread-safe.
    /// Suitable for low-volume events (< 100/frame).
    /// </summary>
    /// <typeparam name="T">Managed event type (class)</typeparam>
    public class ManagedEventStream<T> where T : class
    {
        // Double buffers: front for reading, back for writing
        private List<T> _front = new List<T>();
        private List<T> _back = new List<T>();
        
        private readonly object _lock = new object();

        /// <summary>
        /// Writes an event to the stream.
        /// Thread-safe via locking.
        /// </summary>
        public void Write(T evt)
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            lock (_lock)
            {
                _back.Add(evt);
            }
        }

        /// <summary>
        /// Returns read-only list of events from previous frame.
        /// Safe to call during read phase (after swap).
        /// </summary>
        public IReadOnlyList<T> Read() => _front;

        /// <summary>
        /// Swaps read/write buffers.
        /// Called at end of frame in PostSimulation phase.
        /// </summary>
        public void Swap()
        {
            lock (_lock)
            {
                // Swap buffer references
                var temp = _front;
                _front = _back;
                _back = temp;

                // Clear the new write buffer (old read buffer)
                _back.Clear();
            }
        }

        /// <summary>
        /// Gets the pending (write) buffer for recording.
        /// Used by Flight Recorder to capture events before swap.
        /// </summary>
        public IReadOnlyList<T> GetPendingList()
        {
            lock (_lock)
            {
                // Return copy to avoid race conditions
                return new List<T>(_back);
            }
        }

        /// <summary>
        /// Gets current event count (for debugging).
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _back.Count;
                }
            }
        }
    }
}
