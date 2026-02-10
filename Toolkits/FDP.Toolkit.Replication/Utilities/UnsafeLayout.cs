using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FDP.Toolkit.Replication.Utilities
{
    /// <summary>
    /// Provides zero-overhead access to EntityId field via unsafe pointer arithmetic.
    /// Type initializer runs once per generic type instantiation.
    /// </summary>
    /// <typeparam name="T">Struct type containing EntityId field</typeparam>
    public static class UnsafeLayout<T> where T : unmanaged
    {
        /// <summary>
        /// Byte offset from start of struct to EntityId field. -1 if field not found.
        /// </summary>
        public static readonly int EntityIdOffset;
        
        /// <summary>
        /// True if type has valid EntityId field (long or ulong).
        /// </summary>
        public static readonly bool IsValid;

        static UnsafeLayout()
        {
            // One-time reflection at type initialization
            var field = typeof(T).GetField("EntityId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (field != null && (field.FieldType == typeof(long) || field.FieldType == typeof(ulong)))
            {
                EntityIdOffset = (int)Marshal.OffsetOf<T>("EntityId");
                IsValid = true;
            }
            else
            {
                EntityIdOffset = -1;
                IsValid = false;
            }
        }

        /// <summary>
        /// Reads EntityId from struct via pointer arithmetic (Zero overhead).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long ReadId(T* ptr)
        {
            byte* bytePtr = (byte*)ptr;
            return *(long*)(bytePtr + EntityIdOffset);
        }

        /// <summary>
        /// Writes EntityId to struct via pointer arithmetic (Zero overhead).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteId(T* ptr, long id)
        {
            byte* bytePtr = (byte*)ptr;
            *(long*)(bytePtr + EntityIdOffset) = id;
        }
    }
}
