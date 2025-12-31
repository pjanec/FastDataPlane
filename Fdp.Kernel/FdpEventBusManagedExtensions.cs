using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdp.Kernel
{
    /// <summary>
    /// Extension methods for FdpEventBus - managed event support for Flight Recorder.
    /// </summary>
    public static class FdpEventBusManagedExtensions
    {
        /// <summary>
        /// Returns all managed event streams that have pending events.
        /// Used by Flight Recorder during PostSimulation to capture managed events.
        /// </summary>
        public static IEnumerable<IManagedEventStreamInfo> GetAllPendingManagedStreams(this FdpEventBus eventBus)
        {
            // Access private _managedStreams field via reflection
            var managedStreamsField = typeof(FdpEventBus).GetField("_managedStreams",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (managedStreamsField == null) yield break;
            
            var managedStreams = managedStreamsField.GetValue(eventBus) as System.Collections.IDictionary;
            if (managedStreams == null) yield break;
            
            foreach (System.Collections.DictionaryEntry kvp in managedStreams)
            {
                var typeId = (int)kvp.Key;
                var streamObj = kvp.Value;
                
                if (streamObj == null) continue;
                
                // Use reflection to get stream type and pending events
                var streamType = streamObj.GetType();
                var getPendingMethod = streamType.GetMethod(nameof(ManagedEventStream<object>.GetPendingList))!;
                
                var pendingList = getPendingMethod.Invoke(streamObj, null) as System.Collections.IList;
                if (pendingList != null && pendingList.Count > 0)
                {
                    // Get generic type T from ManagedEventStream<T>
                    var eventType = streamType.GetGenericArguments()[0];
                    
                    yield return new ManagedEventStreamInfo
                    {
                        TypeId = typeId,
                        EventType = eventType,
                        PendingEvents = pendingList
                    };
                }
            }
        }

        /// <summary>
        /// Injects managed events into the Current (read) buffer.
        /// Used during replay - auto-creates stream if needed.
        /// </summary>
        public static void InjectManagedIntoCurrent(this FdpEventBus eventBus, int typeId, Type eventType, IEnumerable<object> events)
        {
            // Access private _managedStreams field via reflection
            var managedStreamsField = typeof(FdpEventBus).GetField("_managedStreams",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (managedStreamsField == null) return;
            
            var managedStreams = managedStreamsField.GetValue(eventBus) as System.Collections.Concurrent.ConcurrentDictionary<int, object>;
            if (managedStreams == null) return;
            
            // Get or create stream
            if (!managedStreams.TryGetValue(typeId, out var streamObj))
            {
                // Create stream dynamically
                var streamType = typeof(ManagedEventStream<>).MakeGenericType(eventType);
                streamObj = Activator.CreateInstance(streamType)!;
                managedStreams.TryAdd(typeId, streamObj);
            }
            
            // Inject events into the FRONT buffer (read buffer) using reflection
            var frontField = streamObj.GetType().GetField("_front", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (frontField != null)
            {
                var frontList = frontField.GetValue(streamObj) as System.Collections.IList;
                if (frontList != null)
                {
                    frontList.Clear();
                    foreach (var evt in events)
                    {
                        frontList.Add(evt);
                    }
                }
            }
        }
    }
}
