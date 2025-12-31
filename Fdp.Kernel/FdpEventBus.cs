using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fdp.Kernel
{
    /// <summary>
    /// Central event bus for transient, one-frame events.
    /// Supports both unmanaged (Tier 1) and managed (Tier 2) events.
    /// Uses double-buffering: events published in Frame N are consumed in Frame N+1.
    /// </summary>
    public class FdpEventBus : IDisposable
    {
        // Separate dictionaries for native and managed streams
        // ConcurrentDictionary allows lock-free lazy initialization
        private readonly ConcurrentDictionary<int, INativeEventStream> _nativeStreams = new();
        private readonly ConcurrentDictionary<int, object> _managedStreams = new();

        private bool _disposed;

        /// <summary>
        /// Publishes an unmanaged event to the bus.
        /// Thread-safe: multiple threads can publish concurrently.
        /// Event will be visible in the next frame (after SwapBuffers).
        /// </summary>
        /// <typeparam name="T">Unmanaged event type with [EventId] attribute</typeparam>
        /// <param name="evt">Event to publish</param>
        public void Publish<T>(T evt) where T : unmanaged
        {
            var stream = GetOrCreateNativeStream<T>();
            stream.Write(evt);
        }

        /// <summary>
        /// Publishes a managed event to the bus.
        /// Thread-safe via locking.
        /// Event will be visible in the next frame (after SwapBuffers).
        /// </summary>
        /// <typeparam name="T">Managed event type (class)</typeparam>
        /// <param name="evt">Event to publish</param>
        public void PublishManaged<T>(T evt) where T : class
        {
            var stream = GetOrCreateManagedStream<T>();
            stream.Write(evt);
        }

        /// <summary>
        /// Consumes all events of type T from the previous frame.
        /// Returns empty span if no events were published.
        /// This is how systems "subscribe" to events - by calling Consume in their Update.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type</typeparam>
        /// <returns>Read-only span of events from previous frame</returns>
        public unsafe ReadOnlySpan<T> Consume<T>() where T : unmanaged
        {
            if (_nativeStreams.TryGetValue(EventType<T>.Id, out var stream))
            {
                // Check if it's a typed stream (normal case)
                if (stream is NativeEventStream<T> typedStream)
                {
                    return typedStream.Read();
                }
                
                // Otherwise it's an untyped stream (replay case)
                // Read raw bytes and reinterpret as T*
                var rawBytes = stream.GetRawBytes();
                fixed (byte* ptr = rawBytes)
                {
                    return new ReadOnlySpan<T>(ptr, rawBytes.Length / sizeof(T));
                }
            }
            return ReadOnlySpan<T>.Empty;
        }

        /// <summary>
        /// Consumes all managed events of type T from the previous frame.
        /// Returns empty list if no events were published.
        /// </summary>
        /// <typeparam name="T">Managed event type</typeparam>
        /// <returns>Read-only list of events from previous frame</returns>
        public IReadOnlyList<T> ConsumeManaged<T>() where T : class
        {
            if (_managedStreams.TryGetValue(GetManagedTypeId<T>(), out var stream))
            {
                return ((ManagedEventStream<T>)stream).Read();
            }
            return Array.Empty<T>();
        }

        /// <summary>
        /// Swaps all event buffers.
        /// MUST be called at the end of each frame (PostSimulation phase).
        /// After swap:
        /// - Events from current frame become readable
        /// - Previous frame's events are cleared
        /// </summary>
        public void SwapBuffers()
        {
            // Swap native streams
            foreach (var stream in _nativeStreams.Values)
            {
                stream.Swap();  // Swap handles clearing the write buffer internally
            }

            // Swap managed streams
            foreach (var streamObj in _managedStreams.Values)
            {
                // Dynamic dispatch (we don't know T at compile time)
                var swapMethod = streamObj.GetType().GetMethod(nameof(ManagedEventStream<object>.Swap));
                swapMethod?.Invoke(streamObj, null);
            }
        }

        /// <summary>
        /// Returns all active native event streams.
        /// Used by recorder for serialization (future feature).
        /// </summary>
        public IEnumerable<INativeEventStream> GetAllActiveStreams()
        {
            return _nativeStreams.Values;
        }

        // ========== FLIGHT RECORDER INTEGRATION ==========

        /// <summary>
        /// Returns all active native event streams that have pending events.
        /// Used by Flight Recorder during PostSimulation to capture events.
        /// </summary>
        public IEnumerable<INativeEventStream> GetAllPendingStreams()
        {
            foreach (var stream in _nativeStreams.Values)
            {
                // Only return streams that have data in their pending (write) buffer
                if (stream.GetPendingBytes().Length > 0)
                {
                    yield return stream;
                }
            }
        }

        /// <summary>
        /// Gets debug inspectors for all active event streams.
        /// Use this to populate an Event Inspector UI.
        /// </summary>
        public IEnumerable<IEventStreamInspector> GetDebugInspectors()
        {
            // 1. Native Streams
            foreach (var stream in _nativeStreams.Values)
            {
                if (stream is IEventStreamInspector inspector)
                    yield return inspector;
            }

            // 2. Managed Streams
            foreach (var stream in _managedStreams.Values)
            {
                if (stream is IEventStreamInspector inspector)
                    yield return inspector;
            }
        }

        /// <summary>
        /// Clears all Current (read) buffers.
        /// Must be called before injecting replay data to prevent mixing old/new events.
        /// </summary>
        public void ClearCurrentBuffers()
        {
            // Clear native streams
            foreach (var stream in _nativeStreams.Values)
            {
                stream.ClearCurrent();
            }
            
            // Clear managed streams
            foreach (var streamObj in _managedStreams.Values)
            {
                var method = streamObj.GetType().GetMethod(nameof(ManagedEventStream<object>.ClearCurrent));
                method?.Invoke(streamObj, null);
            }
        }

        /// <summary>
        /// Injects event data directly into the Current (read) buffer of a specific event type.
        /// Used by Flight Recorder during replay to bypass normal Publish/Swap flow.
        /// NOTE: The event type must have been registered by calling Publish<T>() at least once,
        /// or the stream won't exist and the data will be silently ignored.
        /// </summary>
        /// <param name="typeId">Event type ID</param>
        /// <param name="data">Raw event bytes from recording</param>
        public void InjectIntoCurrent(int typeId, ReadOnlySpan<byte> data)
        {
            if (_nativeStreams.TryGetValue(typeId, out var stream))
            {
                stream.InjectIntoCurrent(data);
            }
            else
            {
                // Stream doesn't exist - this event type was never published on replay side
                // We can't create it without knowing the generic type T
                // This is expected behavior: if you want to replay events, you must
                // call Publish<EventType>() at least once before playback to register the stream
                //
                // Alternative: Maintain a type registry, but that's more complex
                // For now, document this requirement
            }
        }

        /// <summary>
        /// Injects event data with element size - creates stream dynamically if needed.
        /// This is the CORRECT way to inject during replay - NO pre-registration required!
        /// </summary>
        /// <param name="typeId">Event type ID</param>
        /// <param name="elementSize">Size of each event in bytes</param>
        /// <param name="data">Raw event bytes from recording</param>
        public void InjectIntoCurrentBySize(int typeId, int elementSize, ReadOnlySpan<byte> data)
        {
            // Try to get existing stream
            if (_nativeStreams.TryGetValue(typeId, out var stream))
            {
                // Stream exists - verify size matches
                if (stream.ElementSize != elementSize)
                {
                    throw new InvalidOperationException(
                        $"Element size mismatch for event {typeId}: " +
                        $"stream has {stream.ElementSize}, data has {elementSize}");
                }
                
                stream.InjectIntoCurrent(data);
            }
            else
            {
                // Stream doesn't exist - create untyped stream dynamically
                var untypedStream = new UntypedNativeEventStream(typeId, elementSize);
                _nativeStreams.TryAdd(typeId, untypedStream);
                untypedStream.InjectIntoCurrent(data);
            }
        }

        /// <summary>
        /// Gets or creates a native event stream for type T.
        /// Thread-safe via ConcurrentDictionary.
        /// </summary>
        private NativeEventStream<T> GetOrCreateNativeStream<T>() where T : unmanaged
        {
            int typeId = EventType<T>.Id; // Validates [EventId] attribute
            
            var stream = _nativeStreams.GetOrAdd(typeId, _ => new NativeEventStream<T>());
            return (NativeEventStream<T>)stream;
        }

        /// <summary>
        /// Gets or creates a managed event stream for type T.
        /// Thread-safe via ConcurrentDictionary.
        /// </summary>
        private ManagedEventStream<T> GetOrCreateManagedStream<T>() where T : class
        {
            int typeId = GetManagedTypeId<T>();
            
            var stream = _managedStreams.GetOrAdd(typeId, _ => new ManagedEventStream<T>());
            return (ManagedEventStream<T>)stream;
        }

        /// <summary>
        /// Gets a stable type ID for managed types.
        /// Uses hash of full type name (stable across sessions).
        /// </summary>
        private int GetManagedTypeId<T>()
        {
            // Use hash of full name for stable ID
            // Note: This is simpler than requiring [EventId] on managed types
            return typeof(T).FullName!.GetHashCode() & 0x7FFFFFFF;
        }

        /// <summary>
        /// Disposes the event bus and all streams.
        /// </summary>
        /// <summary>
        /// Populates the provided list with active native event streams that have pending events.
        /// Zero-allocation if list capacity is sufficient.
        /// </summary>
        public void PopulatePendingStreams(List<INativeEventStream> target)
        {
            target.Clear();
            foreach (var kvp in _nativeStreams)
            {
                var stream = kvp.Value;
                if (stream.GetPendingBytes().Length > 0)
                {
                    target.Add(stream);
                }
            }
        }
        
        /// <summary>
        /// Populates the provided list with active managed event streams that have pending events.
        /// Zero-allocation if list capacity is sufficient.
        /// </summary>
        public void PopulatePendingManagedStreams(List<IManagedEventStreamInfo> target)
        {
            target.Clear();
            foreach (var kvp in _managedStreams)
            {
                var streamObj = kvp.Value;
                if (streamObj is IManagedEventStreamInfo info && info.PendingEvents.Count > 0)
                {
                    target.Add(info);
                }
            }
        }
        
        /// <summary>
        /// Injects managed events into the current buffer.
        /// </summary>
        public void InjectManagedIntoCurrent(int typeId, Type eventType, IReadOnlyList<object> events)
        {
            if (eventType == null) return;
            
            // Dynamic dispatch to helper
            var method = typeof(FdpEventBus).GetMethod(nameof(InjectManagedInternal), 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(eventType);
            
            method.Invoke(this, new object[] { typeId, events });
        }
        
        private void InjectManagedInternal<T>(int typeId, IReadOnlyList<object> events) where T : class
        {
            var stream = GetOrCreateManagedStream<T>(); // Ensure stream exists
            // Verify ID matches? (should match hash)
            
            // Use internal access to inject
            stream.InjectIntoCurrent(events.Cast<T>());
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Dispose native streams
            foreach (var stream in _nativeStreams.Values)
            {
                if (stream is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _nativeStreams.Clear();
            _managedStreams.Clear();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
    

}
