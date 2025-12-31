using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Fdp.Kernel.Internal;

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
        /// Parallel iteration over entities.
        /// Uses adaptive batching to balance overhead vs. granularity.
        /// Automatically handles chunk skipping and load balancing.
        /// Zero-allocation design via object pooling.
        /// Thread-safe as long as action doesn't modify shared state.
        /// </summary>
        /// <param name="action">Action to execute for each matching entity</param>
        /// <param name="hint">Workload hint for batch size optimization</param>
        public void ForEachParallel(Action<Entity> action, ParallelHint hint = ParallelHint.Light)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            
            var entityIndex = _repository.GetEntityIndex();
            int maxIndex = entityIndex.MaxIssuedIndex;
            int activeCount = entityIndex.ActiveCount;
            
            // 1. Fallback Threshold
            // Don't spin up threads for trivial counts where overhead exceeds benefit.
            // 1024 is the crossover point where parallelism beats single-threaded for light work.
            if (activeCount < 1024 && hint == ParallelHint.Light)
            {
                ForEach(action);
                return;
            }
            
            // 2. Resolve Batch Size from Hint
            // Tune to balance task scheduler overhead vs. load balancing granularity.
            int batchSize = hint switch
            {
                ParallelHint.VeryHeavy => 16,
                ParallelHint.Heavy => 64,
                ParallelHint.Medium => 256,
                _ => 1024 // Light
            };
            
            // 3. Adaptive Tuning for Light Workloads
            // For simple operations, adjust batch size based on entity count
            // to avoid excessive synchronization overhead.
            if (hint == ParallelHint.Light)
            {
                int coreCount = FdpConfig.MaxDegreeOfParallelism > 0 
                    ? FdpConfig.MaxDegreeOfParallelism 
                    : Environment.ProcessorCount;
                
                // Target ~2x batches per core.
                // 4x adds too much synchronization overhead for light work.
                int targetBatches = coreCount * 2;
                int calculatedSize = activeCount / targetBatches;
                
                // Clamp: Never go below 512 for light work, never go above 8192 (cache locality limit)
                batchSize = Math.Clamp(calculatedSize, 512, 8192);
            }
            
            // 4. Profiling Start
#if FDP_PROFILING
            long startTicks = Stopwatch.GetTimestamp();
#endif
            
            // 5. Build Work List (Zero-Alloc via Pooling)
            var workBatches = BatchListPool.Get();
            
            try
            {
                GenerateBatches(entityIndex, maxIndex, batchSize, workBatches);
                
                // DEBUG ASSERTION: Validate batches don't overlap
#if DEBUG
                ValidateBatches(workBatches, maxIndex);
#endif
                
                // 6. Execute Parallel Loop
                Parallel.ForEach(workBatches, FdpConfig.ParallelOptions, range =>
                {
                    // Tight inner loop with unsafe accessor for maximum performance
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        ref var header = ref entityIndex.GetHeaderUnsafe(i);
                        
                        if (header.IsActive && Matches(i, header))
                        {
                            action(new Entity(i, header.Generation));
                        }
                    }
                });
            }
            finally
            {
                // 7. Return to pool to prevent GC pressure
                BatchListPool.Return(workBatches);
            }
            
            // 8. Telemetry Output
#if FDP_PROFILING
            long endTicks = Stopwatch.GetTimestamp();
            double ms = (endTicks - startTicks) * 1000.0 / Stopwatch.Frequency;
            // Only log if slow to avoid console spam
            if (ms > 1.0)
            {
                Console.WriteLine($"[FDP Query] Parallel: {ms:F2}ms, Batches: {workBatches.Count}, Hint: {hint}");
            }
#endif
        }
        
        /// <summary>
        /// Generates work batches by slicing populated chunks into cache-friendly ranges.
        /// Skips empty chunks entirely for efficiency.
        /// </summary>
        private void GenerateBatches(EntityIndex index, int maxIndex, int batchSize, 
            System.Collections.Generic.List<(int Start, int End)> batches)
        {
            int totalChunks = index.GetTotalChunks();
            int chunkCapacity = index.GetChunkCapacity();
            
            for (int c = 0; c < totalChunks; c++)
            {
                // Chunk Skipping Optimization: Skip entire 64KB chunks with no entities
                if (index.GetChunkPopulation(c) == 0)
                    continue;
                
                int chunkStart = c * chunkCapacity;
                if (chunkStart > maxIndex)
                    break;
                
                int chunkEnd = Math.Min(chunkStart + chunkCapacity, maxIndex + 1);
                
                // Flattened Slicing: Break populated chunks into smaller batches
                // for load balancing across cores
                for (int b = chunkStart; b < chunkEnd; b += batchSize)
                {
                    int batchEnd = Math.Min(b + batchSize, chunkEnd);
                    batches.Add((b, batchEnd));
                }
            }
        }
        
        /// <summary>
        /// Debug validation to ensure batches are well-formed and don't overlap.
        /// Only active in DEBUG builds.
        /// </summary>
        [Conditional("DEBUG")]
        private void ValidateBatches(System.Collections.Generic.List<(int Start, int End)> batches, int maxIndex)
        {
            for (int i = 0; i < batches.Count; i++)
            {
                var b = batches[i];
                Debug.Assert(b.Start >= 0, "Batch start negative");
                Debug.Assert(b.End > b.Start, "Batch size zero or negative");
                Debug.Assert(b.End <= maxIndex + 1, "Batch out of bounds");
                
                // Ensure batches are ordered and non-overlapping
                if (i > 0)
                {
                    Debug.Assert(b.Start >= batches[i - 1].End, 
                        "Batches overlap or unordered - potential data race!");
                }
            }
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
