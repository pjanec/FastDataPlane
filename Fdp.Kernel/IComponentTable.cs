using System;

namespace Fdp.Kernel
{
    /// <summary>
    /// Base interface for component tables.
    /// Allows polymorphic storage of different component types.
    /// </summary>
    public interface IComponentTable : IDisposable
    {
        /// <summary>
        /// Component type ID.
        /// </summary>
        int ComponentTypeId { get; }
        
        /// <summary>
        /// Component type.
        /// </summary>
        Type ComponentType { get; }
        
        /// <summary>
        /// Size of component in bytes.
        /// </summary>
        int ComponentSize { get; }
        
        /// <summary>
        /// Gets the version of the chunk containing the entity.
        /// </summary>
        uint GetVersionForEntity(int entityId);
        
        /// <summary>
        /// Sets component from raw bytes (used by EntityCommandBuffer).
        /// </summary>
        /// <summary>
        /// Sets component from raw bytes (used by EntityCommandBuffer).
        /// </summary>
        unsafe void SetRaw(int entityIndex, IntPtr dataPtr, int size, uint version);

        // Serialization
        byte[] Serialize(EntityRepository repo, MessagePack.MessagePackSerializerOptions options);
        void Deserialize(EntityRepository repo, byte[] data, MessagePack.MessagePackSerializerOptions options);

        /// <summary>
        /// Synchronizes data from a source table of the same type.
        /// </summary>
        void SyncFrom(IComponentTable source);
    }
}
