using Fdp.Kernel;
using ModuleHost.Core.Abstractions;

namespace Fdp.Interfaces
{
    /// <summary>
    /// Translates between network descriptors and ECS components.
    /// </summary>
    public interface IDescriptorTranslator
    {
        /// <summary>
        /// Unique identifier for this descriptor type.
        /// </summary>
        long DescriptorOrdinal { get; }
        
        /// <summary>
        /// DDS topic name.
        /// </summary>
        string TopicName { get; }
        
        /// <summary>
        /// Processes incoming network data and updates ECS entities.
        /// </summary>
        void PollIngress(IEntityCommandBuffer cmd, ISimulationView view);
        
        /// <summary>
        /// Scans ECS entities and publishes updates to the network.
        /// </summary>
        void ScanAndPublish(ISimulationView view);
        
        /// <summary>
        /// Applies descriptor data to an entity (used during ghost promotion).
        /// </summary>
        void ApplyToEntity(Entity entity, object data, EntityRepository repo);

        void Dispose(long networkEntityId);
    }
}
