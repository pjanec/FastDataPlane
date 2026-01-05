using System;
using System.Collections.Generic;
using Fdp.Kernel.Internal;

namespace Fdp.Kernel
{
    public sealed partial class EntityRepository
    {
        /// <summary>
        /// Synchronizes this repository from a source repository.
        /// Supports full synchronization (GDB/Backup) or filtered synchronization (SoD/Replication).
        /// </summary>
        /// <param name="source">The source repository to copy from.</param>
        /// <param name="mask">Optional mask to filter specific component types. If null, all components are synced.</param>
        public void SyncFrom(EntityRepository source, BitMask256? mask = null)
        {
            // 1. Sync EntityIndex
            // This copies critical structural data (Generations, IsActive, Masks)
            _entityIndex.SyncFrom(source._entityIndex);
            if (mask.HasValue)
            {
                _entityIndex.ApplyComponentFilter(mask.Value);
            }
            
            // 2. Sync component tables (with optional filtering)
            foreach (var kvp in source._componentTables)
            {
                Type type = kvp.Key;
                IComponentTable srcTable = kvp.Value;
                int typeId = srcTable.ComponentTypeId;
                
                // Mask Filtering
                if (mask.HasValue && !mask.Value.IsSet(typeId))
                    continue;  // Skip filtered components
                
                // Get or Create destination table
                if (!_componentTables.TryGetValue(type, out var myTable))
                {
                    // Schema Mismatch: Destination missing table.
                    // Automatically register component to match schema.
                    // Use Reflection to invoke generic RegisterComponent<T>
                    var method = typeof(EntityRepository).GetMethod(nameof(RegisterComponent))
                        ?.MakeGenericMethod(type);
                    
                    if (method != null)
                    {
                        method.Invoke(this, null);
                        myTable = _componentTables[type];
                    }
                    else
                    {
                        // Should not happen, but safe fallback
                        continue;
                    }
                }
                
                // Sync data
                myTable.SyncFrom(srcTable);
            }
            
            // 3. Sync global version
            // This ensures subsequent operations use the correct tick/version reference
            _globalVersion = source._globalVersion;
        }
    }
}
