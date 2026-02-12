using System;
using System.Numerics;
using Fdp.Kernel;
using Fdp.Kernel.Collections;
using FDP.Toolkit.Vis2D.Abstractions;
using FDP.Toolkit.Vis2D.Tools;
using ModuleHost.Core.Abstractions;
using Raylib_cs;
using Xunit;
using Moq;
using System.Linq;

namespace FDP.Toolkit.Vis2D.Tests.Tools
{
    public class DefaultSelectionToolTests
    {
        private Mock<ISimulationView> _viewMock;
        private Mock<IVisualizerAdapter> _adapterMock;
        private EntityQuery _query;
        private DefaultSelectionTool _tool;
        private bool _clearedSelection;
        private Entity _dragStartEntity;
        private EntityRepository _repo;

        public DefaultSelectionToolTests()
        {
            _viewMock = new Mock<ISimulationView>();
            _adapterMock = new Mock<IVisualizerAdapter>();
            
            _repo = new EntityRepository();
            // We use the repo query mechanism
            _query = _repo.Query().Build();
            
            // For DefaultSelectionTool constructor we can pass the repo as ISimulationView ? 
            // The tool uses ISimulationView.IsAlive, IsValid, etc.
            // EntityRepository implements ISimulationView ? No, IEntityDatabase usually does or wraps it.
            // Let's assume we can mock ISimulationView behavior completely.
            
            // Wait, I need a query that DefaultSelectionTool accepts.
            // And ISimulationView.
            
            // Let's use the mocked view.
            _tool = new DefaultSelectionTool(_viewMock.Object, _query, _adapterMock.Object);
            _clearedSelection = false;
            _dragStartEntity = Entity.Null;

            _tool.OnClearSelection += () => _clearedSelection = true;
            _tool.OnEntityDragStart += (e) => _dragStartEntity = e;
        }

        [Fact]
        public void HandleClick_Background_FiresClearSelection()
        {
            // Arrange
            // Ensure FindEntityAt returns Null.
            // _query is empty.
            _viewMock.Setup(v => v.IsAlive(It.IsAny<Entity>())).Returns(false);
            
            // Act
            bool consumed = _tool.HandleClick(new Vector2(100, 100), MouseButton.Left);

            // Assert
            Assert.True(consumed);
            Assert.True(_clearedSelection);
        }

        [Fact]
        public void HandleClick_EntityHit_ReturnsFalse()
        {
            // Arrange
            var entity = _repo.CreateEntity();
            // We need query to contain this entity.
            // Since we can't easily inject entities into a query without components matching,
            // we will add a component to the entity and recreate the query/tool.
            
            _repo.RegisterComponent<VisualComponent>();
            _repo.AddComponent(entity, new VisualComponent());
            _query = _repo.Query().With<VisualComponent>().Build();
            
            // Re-create tool with populated query
            _tool = new DefaultSelectionTool(_viewMock.Object, _query, _adapterMock.Object);
            _clearedSelection = false; // Reset listener
             _tool.OnClearSelection += () => _clearedSelection = true;

            // Setup View to recognize entity
            _viewMock.Setup(v => v.IsAlive(entity)).Returns(true);
            
            // Setup Adapter to return hit
            _adapterMock.Setup(a => a.GetPosition(It.IsAny<ISimulationView>(), entity)).Returns(new Vector2(100, 100));
            _adapterMock.Setup(a => a.GetHitRadius(It.IsAny<ISimulationView>(), entity)).Returns(10.0f);

            // Act
            bool consumed = _tool.HandleClick(new Vector2(100, 100), MouseButton.Left);

            // Assert
            Assert.False(consumed, "Should return false to let layer handle selection");
            Assert.False(_clearedSelection, "Should not clear selection on hit");
        }
        
        private struct VisualComponent {}
    }
}
