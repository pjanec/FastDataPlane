using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fdp.Kernel
{
    /// <summary>
    /// Static registry for component types.
    /// Assigns unique IDs to each component type at first access.
    /// Thread-safe via lock for registration.
    /// </summary>
    public static class ComponentType<T> where T : unmanaged
    {
        /// <summary>
        /// Gets the unique component type ID.
        /// Assigned on first access in registration order.
        /// JIT will inline this to a constant when T is known.
        /// </summary>
        public static int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ComponentTypeRegistry.GetOrRegister<T>();
        }
        
        /// <summary>
        /// Size of component in bytes.
        /// </summary>
        public static int Size => Unsafe.SizeOf<T>();
        
        /// <summary>
        /// Checks if this is a tag component (zero-size).
        /// Empty structs in C# are 1 byte, so we check for size == 1.
        /// </summary>
        public static bool IsTag => Size == 1;
    }
    
    /// <summary>
    /// Static registry for MANAGED component types (Tier 2).
    /// Uses the same ComponentTypeRegistry as unmanaged types.
    /// </summary>
    public static class ManagedComponentType<T> where T : class
    {
        /// <summary>
        /// Gets the unique component type ID.
        /// Uses the same ID space as unmanaged components.
        /// </summary>
        public static int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ComponentTypeRegistry.GetOrRegisterManaged(typeof(T));
        }
        
        /// <summary>
        /// Size is reference size (IntPtr).
        /// </summary>
        public static int Size => IntPtr.Size;
        
        /// <summary>
        /// Managed types are never tags.
        /// </summary>
        public static bool IsTag => false;
    }
    
    /// <summary>
    /// Global component type registry.
    /// Tracks all registered component types and assigns IDs.
    /// </summary>
    public static class ComponentTypeRegistry
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<Type, int> _typeToId = new Dictionary<Type, int>();
        private static readonly List<Type> _idToType = new List<Type>();
        private static readonly List<bool> _isSnapshotable = new List<bool>();
        private static readonly List<bool> _isRecordable = new List<bool>();
        private static readonly List<bool> _isSaveable = new List<bool>();
        private static readonly List<bool> _needsClone = new List<bool>();
        private static int _nextId = 0;
        
        /// <summary>
        /// Checks if a type is a C# record (immutable by design).
        /// Records have compiler-generated EqualityContract property.
        /// </summary>
        public static bool IsRecordType(Type type)
        {
            // C# records (both record class and record struct) have EqualityContract
            return type.GetProperty("EqualityContract", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) != null;
        }
        
        /// <summary>
        /// Registers a component type and returns its ID.
        /// Thread-safe via lock.
        /// </summary>
        internal static int GetOrRegister<T>() where T : unmanaged
        {
            return GetOrRegisterManaged(typeof(T));
        }
        
        /// <summary>
        /// Registers a managed component type and returns its ID.
        /// Thread-safe via lock. Used for both Tier 1 and Tier 2.
        /// </summary>
        internal static int GetOrRegisterManaged(Type type)
        {
            lock (_lock)
            {
                // Check if already registered
                if (_typeToId.TryGetValue(type, out int existingId))
                {
                    return existingId;
                }
                
                // Assign new ID
                if (_nextId >= FdpConfig.MAX_COMPONENT_TYPES)
                {
                    throw new InvalidOperationException(
                        $"Maximum component types ({FdpConfig.MAX_COMPONENT_TYPES}) exceeded");
                }
                
                int id = _nextId++;
                _typeToId[type] = id;
                _idToType.Add(type);
                _isSnapshotable.Add(true); // Default: snapshotable
                _isRecordable.Add(true);   // Default: recordable
                _isSaveable.Add(true);     // Default: saveable
                _needsClone.Add(false);    // Default: shallow copy
                
                return id;
            }
        }
        
        /// <summary>
        /// Sets whether a component type should be included in snapshots.
        /// Must be called AFTER registration.
        /// </summary>
        public static void SetSnapshotable(int typeId, bool snapshotable)
        {
            lock (_lock)
            {
                if (typeId < 0 || typeId >= _isSnapshotable.Count)
                    throw new ArgumentOutOfRangeException(nameof(typeId));
                
                _isSnapshotable[typeId] = snapshotable;
            }
        }
        
        /// <summary>
        /// Checks if a component type is snapshotable.
        /// Returns true by default for registered types.
        /// </summary>
        public static bool IsSnapshotable(int typeId)
        {
            lock (_lock)
            {
                if (typeId < 0 || typeId >= _isSnapshotable.Count)
                    return false;
                
                return _isSnapshotable[typeId];
            }
        }
        
        /// <summary>
        /// Gets all component type IDs that are snapshotable.
        /// </summary>
        public static int[] GetSnapshotableTypeIds()
        {
            lock (_lock)
            {
                var result = new List<int>();
                for (int i = 0; i < _isSnapshotable.Count; i++)
                {
                    if (_isSnapshotable[i])
                        result.Add(i);
                }
                return result.ToArray();
            }
        }

        /// <summary>
        /// Sets whether a component type should be included in FlightRecorder.
        /// </summary>
        public static void SetRecordable(int typeId, bool value)
        {
            lock (_lock)
            {
                if (typeId < 0 || typeId >= _isRecordable.Count)
                    throw new ArgumentOutOfRangeException(nameof(typeId));
                _isRecordable[typeId] = value;
            }
        }

        /// <summary>
        /// Checks if a component type is recordable.
        /// </summary>
        public static bool IsRecordable(int typeId)
        {
            lock (_lock)
            {
                if (typeId < 0 || typeId >= _isRecordable.Count)
                    return false;
                return _isRecordable[typeId];
            }
        }

        /// <summary>
        /// Gets all component type IDs that are recordable.
        /// </summary>
        public static IEnumerable<int> GetRecordableTypeIds()
        {
            lock (_lock)
            {
                for (int i = 0; i < _isRecordable.Count; i++)
                {
                    if (_isRecordable[i])
                        yield return i;
                }
            }
        }

        /// <summary>
        /// Sets whether a component type should be included in SaveGame.
        /// </summary>
        public static void SetSaveable(int typeId, bool value)
        {
            lock (_lock)
            {
                if (typeId < 0 || typeId >= _isSaveable.Count)
                    throw new ArgumentOutOfRangeException(nameof(typeId));
                _isSaveable[typeId] = value;
            }
        }

        /// <summary>
        /// Checks if a component type is saveable.
        /// </summary>
        public static bool IsSaveable(int typeId)
        {
            lock (_lock)
            {
                if (typeId < 0 || typeId >= _isSaveable.Count)
                    return false;
                return _isSaveable[typeId];
            }
        }

        /// <summary>
        /// Gets all component type IDs that are saveable.
        /// </summary>
        public static IEnumerable<int> GetSaveableTypeIds()
        {
            lock (_lock)
            {
                for (int i = 0; i < _isSaveable.Count; i++)
                {
                    if (_isSaveable[i])
                        yield return i;
                }
            }
        }

        /// <summary>
        /// Sets whether a component type needs deep cloning for snapshots.
        /// </summary>
        public static void SetNeedsClone(int typeId, bool value)
        {
            lock (_lock)
            {
                if (typeId < 0 || typeId >= _needsClone.Count)
                    throw new ArgumentOutOfRangeException(nameof(typeId));
                _needsClone[typeId] = value;
            }
        }

        /// <summary>
        /// Checks if a component type needs deep cloning.
        /// </summary>
        public static bool NeedsClone(int typeId)
        {
            lock (_lock)
            {
                if (typeId < 0 || typeId >= _needsClone.Count)
                    return false;
                return _needsClone[typeId];
            }
        }
        
        /// <summary>
        /// Registers a managed component type (no unmanaged constraint).
        /// Deprecated - use GetOrRegisterManaged instead.
        /// </summary>
        internal static int Register<T>(Type type)
        {
            return GetOrRegisterManaged(type);
        }
        
        /// <summary>
        /// Gets the component ID for a type (returns -1 if not registered).
        /// </summary>
        public static int GetId(Type type)
        {
            lock (_lock)
            {
                if (_typeToId.TryGetValue(type, out int id))
                    return id;
                return -1;
            }
        }
        
        /// <summary>
        /// Gets the component type for a given ID.
        /// </summary>
        public static Type? GetType(int id)
        {
            lock (_lock)
            {
                if (id < 0 || id >= _idToType.Count)
                    return null;
                
                return _idToType[id];
            }
        }
        
        /// <summary>
        /// Gets total number of registered component types.
        /// </summary>
        public static int RegisteredCount
        {
            get
            {
                lock (_lock)
                {
                    return _nextId;
                }
            }
        }
        
        /// <summary>
        /// Clears all registrations (for testing only).
        /// WARNING: Do not use in production - only for unit tests.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _typeToId.Clear();
                _idToType.Clear();
                _isSnapshotable.Clear();
                _isRecordable.Clear();
                _isSaveable.Clear();
                _needsClone.Clear();
                _nextId = 0;
            }
        }
        
        /// <summary>
        /// Returns all registered types ordered by their ID.
        /// Used for serialization to persist the ID-Type mapping.
        /// </summary>
        public static Type[] GetAllTypes()
        {
            lock (_lock)
            {
                return _idToType.ToArray();
            }
        }
    }
}
