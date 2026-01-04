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
            foreach (var kvp in _componentTables)
            {
                Type type = kvp.Key;
                IComponentTable myTable = kvp.Value;
                int typeId = myTable.ComponentTypeId;
                
                // Mask Filtering
                if (mask.HasValue && !mask.Value.IsSet(typeId))
                    continue;  // Skip filtered components
                
                // Get source table
                if (source._componentTables.TryGetValue(type, out var srcTable))
                {
                    // Delegate to table-specific sync which handles efficient dirty tracking
                    myTable.SyncFrom(srcTable);
                }
                // If source doesn't have the table, we assume it has no data for this component.
                // ideally we should clear our table, but avoiding that complexity for now 
                // as schema mismatch is not a primary supported case.
            }
            
            // 3. Sync global version
            // This ensures subsequent operations use the correct tick/version reference
            _globalVersion = source._globalVersion;
        }
    }
}
