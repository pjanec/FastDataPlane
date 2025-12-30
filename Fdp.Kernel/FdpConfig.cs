namespace Fdp.Kernel
{
    /// <summary>
    /// Global configuration constants for the Fast Data Plane engine.
    /// These are compile-time constants for maximum JIT optimization.
    /// </summary>
    public static class FdpConfig
    {
        /// <summary>
        /// Maximum number of entities supported by the engine.
        /// Default: 1,000,000 entities
        /// </summary>
        public const int MAX_ENTITIES = 1_000_000;
        
        /// <summary>
        /// Size of each memory chunk in bytes.
        /// MUST be 64KB (65536) for Windows VirtualAlloc alignment.
        /// </summary>
        public const int CHUNK_SIZE_BYTES = 64 * 1024; // 64KB
        
        /// <summary>
        /// Maximum number of component types supported.
        /// Limited by BitMask256 capacity.
        /// </summary>
        public const int MAX_COMPONENT_TYPES = 256;
        
        /// <summary>
        /// Flight Recorder format version.
        /// Increment this whenever:
        /// - Any Tier 1 component struct layout changes
        /// - The .fdp file format structure is modified
        /// - Event serialization format changes
        /// Recordings are NOT backwards compatible - version must match exactly.
        /// </summary>
        public const uint FORMAT_VERSION = 1;
        
        /// <summary>
        /// Calculate chunk capacity for a given element size.
        /// This is a compile-time constant when T is known.
        /// </summary>
        public static int GetChunkCapacity<T>() where T : unmanaged
        {
            int elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
            
            if (elementSize > CHUNK_SIZE_BYTES)
            {
                throw new System.InvalidOperationException(
                    $"Type {typeof(T).Name} ({elementSize} bytes) exceeds chunk size ({CHUNK_SIZE_BYTES} bytes). " +
                    $"Consider using multi-part descriptors or managed components.");
            }
            
            return CHUNK_SIZE_BYTES / elementSize;
        }
        
        /// <summary>
        /// Calculate the number of chunks needed for MAX_ENTITIES of type T.
        /// </summary>
        public static int GetRequiredChunks<T>() where T : unmanaged
        {
            int capacity = GetChunkCapacity<T>();
            return (MAX_ENTITIES + capacity - 1) / capacity; // Ceiling division
        }
    }
}
