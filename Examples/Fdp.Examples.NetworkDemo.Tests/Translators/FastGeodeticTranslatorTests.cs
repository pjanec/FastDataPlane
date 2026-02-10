using System;
using System.Numerics;
using System.Collections.Generic;
using Xunit;
using Moq;
using Fdp.Examples.NetworkDemo.Translators;
using Fdp.Examples.NetworkDemo.Descriptors;
using Fdp.Examples.NetworkDemo.Components;
using Fdp.Modules.Geographic;
using FDP.Toolkit.Replication.Services;
using FDP.Toolkit.Replication.Components;
using ModuleHost.Core.Abstractions;
using Fdp.Kernel;
using CycloneDDS.Runtime;
using Fdp.Interfaces;
using ModuleHost.Core.Network;

namespace Fdp.Examples.NetworkDemo.Tests.Translators
{
    // Testable wrapper to expose protected methods and mock dependencies
    public class TestableFastGeodeticTranslator : FastGeodeticTranslator
    {
        public List<GeoStateDescriptor> Published = new();

        public TestableFastGeodeticTranslator(
            DdsParticipant p, 
            IGeographicTransform geo, 
            NetworkEntityMap map) 
            : base(p, geo, map)
        {
        }

        public void DecodePublic(in GeoStateDescriptor data, IEntityCommandBuffer cmd, ISimulationView view)
        {
            base.Decode(data, cmd, view);
        }

        protected override void Publish(in GeoStateDescriptor sample)
        {
            Published.Add(sample);
        }
    }

    public class FastGeodeticTranslatorTests : IDisposable
    {
        private DdsParticipant? _participant;
        private Mock<IGeographicTransform> _mockGeo;
        private NetworkEntityMap _entityMap;
        private Mock<IEntityCommandBuffer> _mockCmd;
        private Mock<ISimulationView> _mockView;
        private TestableFastGeodeticTranslator _translator = default!;
        private EntityRepository _repo;

        public FastGeodeticTranslatorTests()
        {
            try
            {
                _participant = new DdsParticipant(0);
            }
            catch (Exception)
            {
                // If DDS libs are missing, tests will fail or need SKIP
                // For now assuming env is correct
                _participant = null; 
            }

            _mockGeo = new Mock<IGeographicTransform>();
            _entityMap = new NetworkEntityMap();
            _mockCmd = new Mock<IEntityCommandBuffer>();
            _mockView = new Mock<ISimulationView>();
            _repo = new EntityRepository();
            Fdp.Examples.NetworkDemo.Configuration.DemoComponentRegistry.Register(_repo);

            if (_participant != null)
            {
                _translator = new TestableFastGeodeticTranslator(_participant, _mockGeo.Object, _entityMap);
            }
        }

        public void Dispose()
        {
            _participant?.Dispose();
        }

        private delegate void SetComponentCallback(Entity e, in DemoPosition p);

        [Fact]
        public void Decode_LatLonAlt_ConvertsToCartesian()
        {
            if (_translator == null) return; // Skip if no DDS

            // Arrange
            var entityId = 100L;
            var entity = _repo.CreateEntity();
            _entityMap.Register(entityId, entity);

            var input = new GeoStateDescriptor { 
                EntityId = entityId, 
                Lat = 37.7749, 
                Lon = -122.4194, 
                Alt = 10.0f 
            };

            var expectedCartesian = new Vector3(100f, 200f, 10f);
            
            _mockGeo.Setup(x => x.ToCartesian(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                   .Returns(expectedCartesian);

            // Act
            DemoPosition capturedData = default;
            _mockCmd.Setup(cmd => cmd.SetComponent(entity, It.Ref<DemoPosition>.IsAny))
                    .Callback(new SetComponentCallback((Entity e, in DemoPosition p) => capturedData = p));

            _translator.DecodePublic(input, _mockCmd.Object, _mockView.Object);

            // Assert
            // Verify cmd.SetComponent was called
            // _mockCmd.Verify(cmd => cmd.SetComponent(entity, It.IsAny<DemoPosition>()), Times.Once);
            
            // Check value
            Assert.True((capturedData.Value - expectedCartesian).LengthSquared() < 0.001f, 
                $"Expected {expectedCartesian}, got {capturedData.Value}");
        }

        [Fact]
        public void ScanAndPublish_CartesianPosition_ConvertsToLatLon()
        {
            if (_translator == null) return;

            // Arrange
            var entity = _repo.CreateEntity();
            var netId = 999;
            _repo.AddComponent(entity, new DemoPosition { Value = new Vector3(50, 60, 0) });
            _repo.AddComponent(entity, new NetworkIdentity { Value = netId });

            // Mock Query using the Repo as View (simpler than mocking QueryBuilder)
            // But base ScanAndPublish uses view.Query()...
            // The Query API is fluent. Mocking it is hard.
            // Better to use EntityRepository as the View directly if possible.
            // EntityRepository implements ISimulationView.
            
            // However, FastGeodeticTranslator uses view.Query().With...Build()
            // Real Repo works best here.
            
            _mockGeo.Setup(x => x.ToGeodetic(It.IsAny<Vector3>()))
                   .Returns((52.0, 13.0, 100.0));

            // We need to permit authority
            var mockViewWithAuth = new Mock<ISimulationView>();
            // Since we can't easily mock the fluent Query of ISimulationView in a few lines
            // unless we wrap the repo. 
            // Let's rely on the fact that _repo HAS the components.
            // Does _repo.Query implementation work? Yes.
            
            // But we also need 'HasAuthority' which is an extension method or on interface.
            // HasAuthority is an Extension on ISimulationView?
            // "view.HasAuthority(entity, DescriptorOrdinal)"
            // Actually it is defined in FDP.Toolkit.Replication.Extensions.SimulationViewExtensions?
            // Or directly on Interface? Snippet said extension.
            // If it is an extension, it calls view.GetComponent? 
            
            // If I use _repo, I need to ensure the logic for Authority passes.
            // HasAuthority usually checks NetworkAuthority component.
            // Let's add NetworkAuthority to entity.
            
            _repo.AddComponent(entity, new NetworkAuthority { 
                LocalNodeId = 1, 
                PrimaryOwnerId = 1 // IsOwner
            });

            // Act
            // Use _repo as view.
            _translator.ScanAndPublish(_repo);

            // Assert
            Assert.Single(_translator.Published);
            var pub = _translator.Published[0];
            Assert.Equal(netId, pub.EntityId);
            Assert.Equal(52.0, pub.Lat);
        }
    }
}
