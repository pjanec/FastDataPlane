using System;
using System.Runtime.CompilerServices;

namespace Fdp.Kernel
{
    /// <summary>
    /// Defines a query for entities with specific component requirements.
    /// Uses BitMask for O(1) filtering via SIMD (when AVX2 enabled).
    /// Immutable after construction for thread-safety.
    /// </summary>
    public sealed class EntityQuery
    {
        private readonly BitMask256 _includeMask;
        private readonly BitMask256 _excludeMask;
        private readonly BitMask256 _authorityIncludeMask;
        private readonly BitMask256 _authorityExcludeMask;
        private readonly EntityRepository _repository;
        private readonly bool _hasDisFilter;
        private readonly ulong _disFilterValue; // The target ID
        private readonly ulong _disFilterMask;  // Which bytes to check

        internal EntityQuery(EntityRepository repository, BitMask256 includeMask, BitMask256 excludeMask, BitMask256 authorityIncludeMask, BitMask256 authorityExcludeMask, bool hasDisFilter, ulong disFilterValue, ulong disFilterMask)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _includeMask = includeMask;
            _excludeMask = excludeMask;
            _authorityIncludeMask = authorityIncludeMask;
            _authorityExcludeMask = authorityExcludeMask;
            _hasDisFilter = hasDisFilter;
            _disFilterValue = disFilterValue;
            _disFilterMask = disFilterMask;
        }

        /// <summary>
        /// Iterates over all entities matching this query.
        /// Calls action for each matching entity.
        /// Performance: Skips chunks with no matching entities.
        /// </summary>
        public void ForEach(Action<Entity> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            var entityIndex = _repository.GetEntityIndex();
            int maxIndex = entityIndex.MaxIssuedIndex;
            
            for (int i = 0; i <= maxIndex; i++)
            {
                ref var header = ref entityIndex.GetHeader(i);
                
                // Match Check
                if (header.IsActive && Matches(i, header))
                {
                    action(new Entity(i, header.Generation));
                }
            }
        }
        
        /// <summary>
        /// Counts entities matching this query.
        /// Optimized to avoid allocation.
        /// </summary>
        public int Count()
        {
            var entityIndex = _repository.GetEntityIndex();
            int maxIndex = entityIndex.MaxIssuedIndex;
            int count = 0;
            
            for (int i = 0; i <= maxIndex; i++)
            {
                ref var header = ref entityIndex.GetHeader(i);
                
                if (header.IsActive && Matches(i, header))
                {
                     count++;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Checks if any entities match this query.
        /// Short-circuits on first match.
        /// </summary>
        public bool Any()
        {
            var entityIndex = _repository.GetEntityIndex();
            int maxIndex = entityIndex.MaxIssuedIndex;
            
            for (int i = 0; i <= maxIndex; i++)
            {
                ref var header = ref entityIndex.GetHeader(i);
                
                if (header.IsActive && Matches(i, header))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the first entity matching this query.
        /// Returns Entity.Null if no matches.
        /// </summary>
        public Entity FirstOrNull()
        {
            var entityIndex = _repository.GetEntityIndex();
            int maxIndex = entityIndex.MaxIssuedIndex;
            
            for (int i = 0; i <= maxIndex; i++)
            {
                ref var header = ref entityIndex.GetHeader(i);
                
                if (header.IsActive && Matches(i, header))
                {
                    return new Entity(i, header.Generation);
                }
            }
            
            return Entity.Null;
        }

        /// <summary>
        /// Checks if an entity's component mask matches this query.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(int entityIndex, in EntityHeader header)
        {
            // Component Mask
            if (!BitMask256.HasAll(header.ComponentMask, _includeMask)) return false;
            if (BitMask256.HasAny(header.ComponentMask, _excludeMask)) return false;

            // Authority Mask
            if (!BitMask256.HasAll(header.AuthorityMask, _authorityIncludeMask)) return false;
            if (BitMask256.HasAny(header.AuthorityMask, _authorityExcludeMask)) return false;
                
            // NEW: Single instruction check
            if (_hasDisFilter)
            {
                if ((header.DisType.Value & _disFilterMask) != _disFilterValue)
                    return false;
            }
            
            return true;
        }
        
        // ================================================
        // CHUNK-AWARE ITERATION (Stage 8)
        // ================================================
        
        /// <summary>
        /// Iterates with chunk skipping optimization.
        /// Skips entire chunks if they have no active entities.
        /// Better cache locality than ForEach.
        /// </summary>
        public void ForEachChunked(Action<Entity> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            var entityIndex = _repository.GetEntityIndex();
            int totalChunks = entityIndex.GetTotalChunks();
            int chunkCapacity = entityIndex.GetChunkCapacity();
            
            for (int chunkIdx = 0; chunkIdx < totalChunks; chunkIdx++)
            {
                // Skip chunks with no entities
                int population = entityIndex.GetChunkPopulation(chunkIdx);
                if (population == 0)
                    continue;
                
                int startIndex = chunkIdx * chunkCapacity;
                int endIndex = Math.Min(startIndex + chunkCapacity, entityIndex.MaxIssuedIndex + 1);
                
                // Iterate through chunk
                for (int i = startIndex; i < endIndex; i++)
                {
                    ref var header = ref entityIndex.GetHeader(i);
                    
                    if (!header.IsActive)
                        continue;
                    
                    if (!Matches(i, header))
                        continue;
                    
                    var entity = new Entity(i, header.Generation);
                    action(entity);
                }
            }
        }
        
        /// <summary>
        /// Parallel iteration over chunks.
        /// Each chunk is processed independently.
        /// Thread-safe as long as action doesn't modify shared state.
        /// </summary>
        public void ForEachParallel(Action<Entity> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            var entityIndex = _repository.GetEntityIndex();
            int totalChunks = entityIndex.GetTotalChunks();
            int chunkCapacity = entityIndex.GetChunkCapacity();
            
            System.Threading.Tasks.Parallel.For(0, totalChunks, chunkIdx =>
            {
                // Skip chunks with no entities
                int population = entityIndex.GetChunkPopulation(chunkIdx);
                if (population == 0)
                    return;
                
                int startIndex = chunkIdx * chunkCapacity;
                int endIndex = Math.Min(startIndex + chunkCapacity, entityIndex.MaxIssuedIndex + 1);
                
                // Iterate through chunk
                for (int i = startIndex; i < endIndex; i++)
                {
                    ref var header = ref entityIndex.GetHeader(i);
                    
                    if (!header.IsActive)
                        continue;
                    
                    if (!Matches(i, header))
                        continue;
                    
                    var entity = new Entity(i, header.Generation);
                    action(entity);
                }
            });
        }
        
        /// <summary>
        /// Gets the include mask (for advanced usage).
        /// </summary>
        public BitMask256 IncludeMask => _includeMask;
        
        /// <summary>
        /// Gets the exclude mask (for advanced usage).
        /// </summary>
        public BitMask256 ExcludeMask => _excludeMask;
    }
}
