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
        private static int _nextId = 0;
        
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
                
                return id;
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
