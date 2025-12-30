# Overview
Fast Data Plane (FDP) engine is a high-performance, data-oriented hybrid (managed/unmanaged) Entity Component System
targeting .NET 8, insired by Unity DOTS, built with a "Zero-Allocation" philosophy on the hot path,
leveraging unmanaged memory and direct OS memory allocation, hardware intrinsics (AVX2), a strict phase-based execution
model to ensure lock free data access and deterministic and cache-friendly simulation.

## **🚀 Key Features**

* **Hybrid Component Storage:**
  * **Tier 1 (Unmanaged):** Structs are stored in chunk table using raw `VirtualAlloc` memory. Guaranteed contiguous memory layout for maximum cache locality and SIMD usage.
  * **Tier 2 (Managed):** Classes/Strings are stored in efficient sparse arrays for data that requires Garbage Collection.
* **AVX2 Optimized Queries:** 256-bit component masks use optimized hardware intrinsics for O(1) filtering and rapid query matching.
* **Zero-Allocation Architecture:** Uses custom native memory allocator and pooling mechanisms. Iterators and structural changes are designed to generate zero GC pressure during the simulation loop.
* **Flight Recorder 🎥:** Built-in deterministic replay system.
  * Asynchronous, double-buffered snapshotting.
  * Delta compression support.
  * Automatic serialization of components via Expression Trees.
* **Strict Phase System:** Enforces read/write permissions (e.g., `ReadOnly`, `OwnedOnly`) depending on the execution phase (Simulation, NetworkReceive, Presentation) to prevent race conditions and logic errors.
* **Multithreading:** Supports parallel chunk iteration and thread-safe command recording.

