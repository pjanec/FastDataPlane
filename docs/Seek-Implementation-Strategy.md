# Seek Implementation Strategy: Leveraging Efficient Event Skipping

## Overview
Recent updates to the Flight Recorder file format (Version 2) and the `PlaybackSystem` have laid the critical infrastructure required to implement a high-performance **"Seek"** capability.

This document explains why "Managed Event Skipping" was the prerequisite for efficient seeking and outlines the algorithm for implementing `PlaybackController.Seek()`.

## The Challenge: Cost of Deserialization
In the Fast Data Plane (FDP) architecture, "Seeking" to a specific point in time (e.g., Tick 5000) involves:
1.  Locating the nearest previous **Keyframe** (e.g., Tick 4800).
2.  Loading that Keyframe to restore the base state.
3.  Sequentially applying every **Delta Frame** from 4801 to 5000 to evolve the state.

### The Bottleneck
Previously, applying a Delta Frame required reading **all** data within it.
*   **Entity Components**: Fast (raw memory copy).
*   **Unmanaged Events**: Fast (raw copy).
*   **Managed Events**: **Extremely Slow** (Requires `BinaryReader`, `Reflection`, `Activator.CreateInstance`, and string parsing).

If a user wanted to jump from Tick 0 to Tick 5000, the system had to fully deserialize 5000 frames' worth of Managed Events (e.g., chat logs, complex AI states, debug strings), even though that data would be immediately discarded because the user only cares about the *final* state at Tick 5000.

This made "Seeking" essentially as slow as "Playing Fast Forward".

## The Solution: Managed Event Skipping (Format v2)
We introduced a **Block Size Tracking** mechanism in Format Version 2.

### File Format Change
Every block of Managed Events in the recording is now prefixed with its total byte size:
```
[BlockSize: int32] [Type Name] [Count] [Serialized Data...]
```

### Zero-Cost Skipping
When the `PlaybackSystem` applies a frame with `processEvents = false`:
1.  It reads the `BlockSize`.
2.  It executes `stream.Seek(BlockSize, SeekOrigin.Current)`.
3.  **Result**: The file pointer effectively "teleports" over the complex managed data. No reflection, no allocation, no GC pressure.

## Implementing `PlaybackController.Seek(targetTick)`

With this foundation, the `Seek` feature can now be implemented efficiently.

### Algorithm
```csharp
public void Seek(ulong targetTick)
{
    // 1. Pause Playback
    _isPlaying = false;

    // 2. Find Nearest Keyframe
    // (Assuming we have an index of keyframes)
    long keyframeOffset = _keyframeIndex.FindClosestBefore(targetTick);
    _stream.Position = keyframeOffset;

    // 3. Fast-Forward Loop
    while (_currentTick < targetTick)
    {
        // READ NEXT FRAME
        // Crucial: Pass 'processEvents = false'
        _playbackSystem.ApplyFrame(
            _repo, 
            _reader, 
            eventBus: null,       // No event bus needed for intermediate frames
            processEvents: false  // ENABLE SKIPPING
        );
        
        // At this point:
        // - Entity State is updated (Fast Memcpy)
        // - Events are skipped (Instant Seek)
    }

    // 4. Final Frame (Target Tick)
    // Optionally apply the final frame with events if we want to show them immediately
    _playbackSystem.ApplyFrame(_repo, _reader, _eventBus, processEvents: true);
}
```

## Performance Implications
*   **Before**: Seeking time was dominated by `FdpAutoSerializer.Deserialize()`, creating massive GC pressure and CPU load.
*   **After**: Seeking time is dominated only by File I/O bandwidth and raw memory copies.

This enables "Scrubbing" (dragging the timeline slider) to feel responsive even in recordings containing megabytes of text or complex event data.
