using System;
using System.Collections.Generic;
using CycloneDDS.Runtime;
using Fdp.Kernel;
using FDP.Kernel.Logging;
using Fdp.Interfaces; // Use Fdp Interface
using ModuleHost.Core.Abstractions;
using ModuleHost.Core.Network;
using ModuleHost.Core.Network.Interfaces;
using FDP.Toolkit.Lifecycle;
using FDP.Toolkit.Lifecycle.Events;
using ModuleHost.Network.Cyclone.Services;
using ModuleHost.Network.Cyclone.Translators;
using ModuleHost.Network.Cyclone.Topics;
using ModuleHost.Network.Cyclone.Systems;
using ModuleHost.Network.Cyclone.Providers;
using FDP.Toolkit.Replication.Components;
using FDP.Toolkit.Replication.Services; // For NetworkEntityMap

using NetworkEntityMap = FDP.Toolkit.Replication.Services.NetworkEntityMap; // Alias to force Toolkit Map
using IDescriptorTranslator = Fdp.Interfaces.IDescriptorTranslator; // Alias to force Fdp Interface
// using IDataReader removed
// using IDataWriter removed
using INetworkTopology = Fdp.Interfaces.INetworkTopology;

namespace ModuleHost.Network.Cyclone.Modules
{
    /// <summary>
    /// Master module for CycloneDDS networking.
    /// Wires up all services, translators, and systems required for distributed simulation.
    /// </summary>
    public class CycloneNetworkModule : IModule
    {
        public string Name => "CycloneNetwork";
        
        public ExecutionPolicy Policy => ExecutionPolicy.Synchronous();

        private readonly DdsParticipant _participant;
        private readonly NodeIdMapper _nodeMapper;
        private readonly INetworkIdAllocator _idAllocator;
        private readonly INetworkTopology _topology;
        private readonly EntityLifecycleModule _elm;
        
        // Translators and Services
        private NetworkEntityMap _entityMap;
        private TypeIdMapper _typeMapper;
        private EntityMasterTranslator _masterTranslator;
        private EntityStateTranslator _stateTranslator;
        
        // Dynamic / Custom Translators
        private readonly List<IDescriptorTranslator> _customTranslators = new();
        
        private NetworkGatewayModule _gatewayModule;

        public CycloneNetworkModule(
            DdsParticipant participant,
            NodeIdMapper nodeMapper,
            INetworkIdAllocator idAllocator,
            INetworkTopology topology,
            EntityLifecycleModule elm,
            Fdp.Interfaces.ISerializationRegistry? serializationRegistry = null,
            IEnumerable<IDescriptorTranslator>? customTranslators = null,
            NetworkEntityMap? sharedEntityMap = null)
        {
            _participant = participant ?? throw new ArgumentNullException(nameof(participant));
            _nodeMapper = nodeMapper ?? throw new ArgumentNullException(nameof(nodeMapper));
            _idAllocator = idAllocator ?? throw new ArgumentNullException(nameof(idAllocator));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _elm = elm ?? throw new ArgumentNullException(nameof(elm));
            
            // Initialize Services
            _entityMap = sharedEntityMap ?? new NetworkEntityMap();
            _typeMapper = new TypeIdMapper();

            if (serializationRegistry != null)
            {
                // Register Serialization Providers
                serializationRegistry.Register(1001, new CycloneSerializationProvider<NetworkPosition>());
                serializationRegistry.Register(1002, new CycloneSerializationProvider<NetworkVelocity>());
                serializationRegistry.Register(1003, new CycloneSerializationProvider<NetworkIdentity>());
                serializationRegistry.Register(1004, new CycloneSerializationProvider<NetworkSpawnRequest>());
            }

            // Initialize Translators
            _masterTranslator = new EntityMasterTranslator(_entityMap, _nodeMapper, _typeMapper, _participant);
            _stateTranslator = new EntityStateTranslator(_entityMap, _participant);
            
            if (customTranslators != null)
            {
                _customTranslators.AddRange(customTranslators);
            }
            
            _gatewayModule = new NetworkGatewayModule(101, _nodeMapper.LocalNodeId, _topology, _elm);
        }

        public void RegisterSystems(ISystemRegistry registry)
        {
            // Combine Default + Custom
            var allTranslators = new List<IDescriptorTranslator> { _masterTranslator, _stateTranslator };
            allTranslators.AddRange(_customTranslators);

            // Register Ingress
            registry.RegisterSystem(new CycloneNetworkIngressSystem(
                allTranslators.ToArray()
            ));
            
            // Register Egress
            registry.RegisterSystem(new CycloneEgressSystem(
                allTranslators.ToArray()
            ));

            // Register Cleanup System (Lifecycle)
            // Cleanup system usually uses writer to send kill msg.
            // Wait, CycloneNetworkCleanupSystem might need writer or simplified.
            // I'll check its definition next. Assuming it takes translator or writer.
            // Assuming it needs update, so I'll comment out or check what it needs.
            // If it takes a writer, I can't pass it easily as I don't expose it from translator?
            // "Update CycloneNetworkCleanupSystem: Call Dispose(netId) instead of _masterWriter.Dispose()."
            // So it probably takes "IDescriptorTranslator masterTranslator" now?
            // This replace block covers RegisterSystems, so I need to know what to put there.
            
            // Let's assume for now I pass _masterTranslator and will fix CleanupSystem if needed.
             registry.RegisterSystem(new CycloneNetworkCleanupSystem(_masterTranslator));
            
            // Register Gateway
             if (_gatewayModule is IModuleSystem ms)
                registry.RegisterSystem(ms);
        }

        public void Tick(ISimulationView view, float deltaTime)
        {
             _gatewayModule.Tick(view, deltaTime);
        }
    }

    // Local implementation of Ingress System since it appears missing from Core
    [UpdateInPhase(SystemPhase.Input)]
    public class CycloneNetworkIngressSystem : IModuleSystem
    {
        private readonly Fdp.Interfaces.IDescriptorTranslator[] _translators;
        
        public CycloneNetworkIngressSystem(Fdp.Interfaces.IDescriptorTranslator[] translators)
        {
             _translators = translators;
        }
        
        public void Execute(ISimulationView view, float deltaTime)
        {
            var cmd = view.GetCommandBuffer();
            for(int i=0; i<_translators.Length; i++)
            {
                    _translators[i].PollIngress(cmd, view);
            }
        }
    }
}
