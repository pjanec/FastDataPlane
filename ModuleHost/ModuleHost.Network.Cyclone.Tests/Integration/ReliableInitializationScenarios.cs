using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;
using Fdp.Kernel;
using Fdp.Interfaces;
using NetworkEntityMap = FDP.Toolkit.Replication.Services.NetworkEntityMap;
using ModuleHost.Core.Abstractions;
using ModuleHost.Core.Network;
using ModuleHost.Network.Cyclone.Components;
using FDP.Toolkit.Replication.Components;
using ModuleHost.Network.Cyclone.Services;
using ModuleHost.Network.Cyclone.Topics;
using ModuleHost.Network.Cyclone.Translators;

namespace ModuleHost.Network.Cyclone.Tests.Integration
{
    public class ReliableInitializationScenarios : IDisposable
    {
        private CycloneDDS.Runtime.DdsParticipant _participant;

        public ReliableInitializationScenarios()
        {
            try {
                // Use default domain or specific test domain
                _participant = new CycloneDDS.Runtime.DdsParticipant(0);
            } catch {
                // Ignore if library missing, but tests will likely fail later if they rely on it
            }
        }

        public void Dispose()
        {
            _participant?.Dispose();
        }

        [Fact]
        public void Translator_Restoration_SmokeTest()
        {
            if (_participant == null) return; // Skip if no DDS

            // 1. Setup Environment
            var repo = new EntityRepository();
            var view = (ISimulationView)repo;
            var cmd = view.GetCommandBuffer();
            
            // Register Components
            repo.RegisterComponent<NetworkIdentity>();
            repo.RegisterComponent<NetworkSpawnRequest>();
            repo.RegisterComponent<NetworkOwnership>();
            repo.RegisterComponent<NetworkPosition>();
            repo.RegisterComponent<NetworkVelocity>();
            repo.RegisterComponent<NetworkOrientation>();
            repo.RegisterComponent<NetworkAuthority>(); 

            var entityMap = new NetworkEntityMap();
            var nodeMapper = new NodeIdMapper(0, 1); // Domain 0, Instance 1
            var typeMapper = new TypeIdMapper();

            // Pass participant
            var masterTranslator = new EntityMasterTranslator(entityMap, nodeMapper, typeMapper, _participant);
            var stateTranslator = new EntityStateTranslator(entityMap, _participant);

            // 2. Simulate Ingress: Entity Master (Spawn)
            long netEntityId = 999;
            ulong disType = 55;
            
            var remoteAppId = new ModuleHost.Network.Cyclone.Topics.NetworkAppId { AppDomainId = 0, AppInstanceId = 99 };
            int ownerNodeId = nodeMapper.GetOrRegisterInternalId(remoteAppId);
            
            var masterTopic = new EntityMasterTopic 
            { 
                EntityId = netEntityId,
                DisTypeValue = disType,
                OwnerId = nodeMapper.GetExternalId(ownerNodeId)
            };

            // Serialize to bytes for InjectReplayData
            byte[] masterBytes = new byte[Marshal.SizeOf<EntityMasterTopic>()];
            MemoryMarshal.Write(masterBytes, ref masterTopic);

            // Use InjectReplayData instead of PollIngress with MockDataReader
            masterTranslator.InjectReplayData(masterBytes, cmd, repo);
            
            // Execution Phase
            ((EntityCommandBuffer)cmd).Playback(repo); 

            // Verify
            Assert.True(entityMap.TryGetEntity(netEntityId, out var restoredEntity));
            Assert.True(repo.HasComponent<NetworkIdentity>(restoredEntity));
            Assert.Equal(netEntityId, repo.GetComponentRO<NetworkIdentity>(restoredEntity).Value);
            
            Entity realEntity = restoredEntity;

            // 3. Verify Spawn
            Assert.True(repo.HasComponent<NetworkSpawnRequest>(realEntity));
            Assert.Equal(disType, repo.GetComponentRO<NetworkSpawnRequest>(realEntity).DisType);

            // 4. Simulate Ingress: Entity State
            var stateTopic = new EntityStateTopic
            {
                EntityId = netEntityId,
                PositionX = 100, PositionY = 200, PositionZ = 300,
                VelocityX = 1, VelocityY = 0, VelocityZ = 0,
                OrientationX = 0, OrientationY = 0, OrientationZ = 0, OrientationW = 1
            };

            byte[] stateBytes = new byte[Marshal.SizeOf<EntityStateTopic>()];
            MemoryMarshal.Write(stateBytes, ref stateTopic);

            stateTranslator.InjectReplayData(stateBytes, cmd, repo);
            ((EntityCommandBuffer)cmd).Playback(repo);

            // 5. Verify State
            Assert.True(repo.HasComponent<NetworkPosition>(realEntity));
            var pos = repo.GetComponentRO<NetworkPosition>(realEntity);
            Assert.Equal(100f, pos.Value.X);
            Assert.Equal(200f, pos.Value.Y);
            Assert.Equal(300f, pos.Value.Z);
        }

        [Fact]
        public void Egress_ScanAndPublish_SmokeTest()
        {
            if (_participant == null) return;

             // 1. Setup Environment
            var repo = new EntityRepository();
            var view = (ISimulationView)repo;
            var cmd = view.GetCommandBuffer();
            
            repo.RegisterComponent<NetworkIdentity>();
            repo.RegisterComponent<NetworkSpawnRequest>();
            repo.RegisterComponent<NetworkOwnership>();
            repo.RegisterComponent<NetworkPosition>();
            repo.RegisterComponent<NetworkVelocity>();
            repo.RegisterComponent<NetworkOrientation>();

            var entityMap = new NetworkEntityMap();
            var nodeMapper = new NodeIdMapper(0, 1); 
            var typeMapper = new TypeIdMapper();

            var masterTranslator = new EntityMasterTranslator(entityMap, nodeMapper, typeMapper, _participant);
            var stateTranslator = new EntityStateTranslator(entityMap, _participant);

            // 2. Create Local Entity
            var entity = repo.CreateEntity();
            repo.AddComponent(entity, new NetworkIdentity { Value = 888 });
            repo.AddComponent(entity, new NetworkSpawnRequest { DisType = 12, OwnerId = 1 });
            repo.AddComponent(entity, new NetworkOwnership { PrimaryOwnerId = 1, LocalNodeId = 1 });
            repo.AddComponent(entity, new NetworkPosition { Value = new Vector3(10, 20, 30) });
            repo.AddComponent(entity, new NetworkVelocity { Value = new Vector3(0,1,0) });
            repo.AddComponent(entity, new NetworkOrientation { Value = Quaternion.Identity });

            // 3. Scan Egress
            masterTranslator.ScanAndPublish(repo);
            stateTranslator.ScanAndPublish(repo);
        }
    }
}

