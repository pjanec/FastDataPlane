using System;

namespace Fdp.Kernel
{
    /// <summary>
    /// Stores managed components (classes, strings, etc.) in GC-managed arrays.
    /// Tier 2 storage for "cold" data that doesn't need the performance of native memory.
    /// Uses standard .NET arrays, so GC manages all memory.
    /// </summary>
    public sealed class ManagedComponentTable<T> : IComponentTable where T : class
    {
        private T?[][] _chunks;
        private readonly int _chunkSize;
        private readonly int _componentTypeId;
        private uint[] _chunkVersions;
        
        public ManagedComponentTable(int chunkSize = 16384)
        {
            _chunkSize = chunkSize;
            _componentTypeId = ManagedComponentType<T>.ID;
            
            // Pre-allocate chunk array (but not the actual data arrays yet - lazy)
            int maxChunks = (FdpConfig.MAX_ENTITIES / chunkSize) + 1;
            _chunks = new T[maxChunks][];
            _chunkVersions = new uint[maxChunks];
        }
        
        public int ComponentTypeId => _componentTypeId;
        public Type ComponentType => typeof(T);
        public int ComponentSize => IntPtr.Size; // Reference size
        public int ChunkCapacity => _chunkSize;
        
        /// <summary>
        /// Gets or sets component for an entity. Lazy-allocates chunks.
        /// </summary>
        public T? this[int entityIndex]
        {
            get
            {
                int chunkIndex = entityIndex / _chunkSize;
                int localIndex = entityIndex % _chunkSize;
                
                var chunk = _chunks[chunkIndex];
                if (chunk == null)
                    return null; // Not allocated yet
                
                return chunk[localIndex];
            }
            set
            {
                int chunkIndex = entityIndex / _chunkSize;
                int localIndex = entityIndex % _chunkSize;
                
                // Lazy allocate chunk if needed
                if (_chunks[chunkIndex] == null)
                {
                    _chunks[chunkIndex] = new T[_chunkSize];
                }
                
                _chunks[chunkIndex][localIndex] = value;
            }
        }
        
        /// <summary>
        /// Gets reference for read/write access. Allocates chunk if needed.
        /// </summary>
        public ref T? GetRW(int entityIndex, uint version)
        {
            int chunkIndex = entityIndex / _chunkSize;
            int localIndex = entityIndex % _chunkSize;
            
            // Lazy allocate chunk if needed
            if (_chunks[chunkIndex] == null)
            {
                _chunks[chunkIndex] = new T[_chunkSize];
            }
            
            // Update version
            _chunkVersions[chunkIndex] = version;
            
            return ref _chunks[chunkIndex][localIndex];
        }
        
        /// <summary>
        /// Gets value for read-only access. Does NOT allocate.
        /// Returns null if chunk not allocated.
        /// </summary>
        public T? GetRO(int entityIndex)
        {
            int chunkIndex = entityIndex / _chunkSize;
            int localIndex = entityIndex % _chunkSize;
            
            var chunk = _chunks[chunkIndex];
            if (chunk == null)
                return null;
            
            return chunk[localIndex];
        }
        
        /// <summary>
        /// Sets component value with version tracking.
        /// </summary>
        public void Set(int entityIndex, T? value, uint version)
        {
            int chunkIndex = entityIndex / _chunkSize;
            int localIndex = entityIndex % _chunkSize;
            
            // Lazy allocate chunk if needed
            if (_chunks[chunkIndex] == null)
            {
                _chunks[chunkIndex] = new T[_chunkSize];
            }
            
            _chunks[chunkIndex][localIndex] = value;
            _chunkVersions[chunkIndex] = version;
        }

        /// <summary>
        /// Sets a full chunk of data. Used by PlaybackSystem.
        /// </summary>
        public void SetChunk(int chunkIndex, T?[] data, uint version)
        {
             // Ensure capacity
             if (chunkIndex >= _chunks.Length)
             {
                 // Resize logic? Or assume pre-allocated?
                 // Constructor allocates fixed size.
                 // We should probably check bounds.
                 if (chunkIndex >= _chunks.Length) throw new IndexOutOfRangeException("Chunk index out of bounds");
             }
             
             _chunks[chunkIndex] = data;
             _chunkVersions[chunkIndex] = version;
        }
        
        /// <summary>
        /// Gets the version of the chunk containing the entity.
        /// </summary>
        public uint GetVersionForEntity(int entityId)
        {
            int chunkIndex = entityId / _chunkSize;
            return _chunkVersions[chunkIndex];
        }
        
        /// <summary>
        /// Gets chunk version by index (for query system).
        /// </summary>
        public uint GetChunkVersion(int chunkIndex)
        {
            if (chunkIndex >= _chunkVersions.Length)
                return 0;
            return _chunkVersions[chunkIndex];
        }
        
        /// <summary>
        /// Sets component from raw bytes. For managed types, this deserializes the object.
        /// Used by EntityCommandBuffer for type-erased operations.
        /// </summary>
        public unsafe void SetRaw(int entityIndex, IntPtr dataPtr, int size, uint version)
        {
            // For managed types, we can't use raw memory copy
            // The caller needs to have already deserialized the object
            // This is a limitation of the raw API for managed types
            throw new NotSupportedException(
                "SetRaw is not supported for managed components. " +
                "Use Set() with the deserialized object instead.");
        }
        
        /// <summary>
        /// Checks if chunk is allocated.
        /// </summary>
        public bool IsChunkAllocated(int chunkIndex)
        {
            return chunkIndex < _chunks.Length && _chunks[chunkIndex] != null;
        }
        
        /// <summary>
        /// Clears a component value (sets to null).
        /// </summary>
        public void Clear(int entityIndex)
        {
            int chunkIndex = entityIndex / _chunkSize;
            int localIndex = entityIndex % _chunkSize;
            
            if (_chunks[chunkIndex] != null)
            {
                _chunks[chunkIndex][localIndex] = null;
            }
        }
        
        public void Dispose()
        {
            // Let GC handle cleanup
            // We could null out arrays to help GC, but it's not strictly necessary
            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i] = null!;
            }
        }
        public byte[] Serialize(EntityRepository repo, MessagePack.MessagePackSerializerOptions options)
        {
            var dataToSave = new System.Collections.Generic.List<EntityComponentPair<T>>();
            var index = repo.GetEntityIndex();
            int max = index.MaxIssuedIndex;
            
            for (int i = 0; i <= max; i++)
            {
                 ref var header = ref index.GetHeader(i);
                 if (header.IsActive && header.ComponentMask.IsSet(_componentTypeId))
                 {
                      var val = this[i];
                      if (val != null)
                      {
                          dataToSave.Add(new EntityComponentPair<T> { EntityId = i, Value = val });
                      }
                 }
            }

            return MessagePack.MessagePackSerializer.Serialize(dataToSave, options);
        }

        public void Deserialize(EntityRepository repo, byte[] data, MessagePack.MessagePackSerializerOptions options)
        {
            var loadedData = MessagePack.MessagePackSerializer.Deserialize<System.Collections.Generic.List<EntityComponentPair<T>>>(data, options);
            
            foreach (var item in loadedData)
            {
                // Set with version 0 (fresh load)
                this.Set(item.EntityId, item.Value, 0); 
                
                // Update component mask
                ref var header = ref repo.GetHeader(item.EntityId);
                header.ComponentMask.SetBit(_componentTypeId);
            }
        }
    }
}
