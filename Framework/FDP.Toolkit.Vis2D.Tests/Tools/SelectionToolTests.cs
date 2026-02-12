using System.Numerics;
using Xunit;
using Moq;
using Fdp.Kernel;
using Fdp.Kernel.Collections;
using FDP.Toolkit.Vis2D.Tools;
using FDP.Toolkit.Vis2D.Abstractions;
using FDP.Toolkit.ImGui.Abstractions;
using ModuleHost.Core.Abstractions;
using Raylib_cs;

namespace FDP.Toolkit.Vis2D.Tests.Tools
{
    public class TestableSelectionTool : SelectionTool
    {
        public bool IsLeftReleased { get; set; }
        public bool IsLeftDown { get; set; }
        
        public TestableSelectionTool(ISimulationView view, EntityQuery query, IInspectorContext inspector, IVisualizerAdapter adapter) 
             : base(view, query, inspector, adapter) { }

        protected override bool IsMouseButtonReleased(MouseButton button) => IsLeftReleased;
        protected override bool IsMouseButtonDown(MouseButton button) => IsLeftDown;
        protected override void DrawSelectionBox(Vector2 start, float width, float height) { }
    }

    public class SelectionToolTests
    {
        [Fact]
        public void SelectionTool_BoxSelect_SelectsEntityInBox()
        {
            // Setup
            var world = new EntityRepository();
            var adapter = new Mock<IVisualizerAdapter>();
            var inspector = new Mock<IInspectorContext>();
            var query = world.Query().Build();

            // Create Entity at 10,10
            var entity = world.CreateEntity();
            adapter.Setup(a => a.GetPosition(It.IsAny<ISimulationView>(), entity)).Returns(new Vector2(10, 10));

            var tool = new TestableSelectionTool(world, query, inspector.Object, adapter.Object);

            // 1. Click at 0,0
            tool.HandleClick(new Vector2(0, 0), MouseButton.Left);
            
            // 2. Drag to 20,20 (Left Button Down)
            tool.IsLeftDown = true;
            tool.HandleDrag(new Vector2(20, 20), new Vector2(20, 20)); // Delta is large
            
            // 3. Update (Release Left Button)
            tool.IsLeftReleased = true;
            tool.Update(0.016f);
            
            // Verify
            inspector.VerifySet(i => i.SelectedEntity = entity, Times.Once);
        }

        [Fact]
        public void SelectionTool_BoxSelect_IgnoresEntityOutsideBox()
        {
            // Setup
            var world = new EntityRepository();
            var adapter = new Mock<IVisualizerAdapter>();
            var inspector = new Mock<IInspectorContext>();
            var query = world.Query().Build();

            var entity = world.CreateEntity();
            adapter.Setup(a => a.GetPosition(It.IsAny<ISimulationView>(), entity)).Returns(new Vector2(100, 100));

            var tool = new TestableSelectionTool(world, query, inspector.Object, adapter.Object);

            // Box 0,0 to 20,20
            tool.HandleClick(new Vector2(0, 0), MouseButton.Left);
            tool.IsLeftDown = true;
            tool.HandleDrag(new Vector2(20, 20), new Vector2(20, 20));
            
            tool.IsLeftReleased = true;
            tool.Update(0.016f);
            
            inspector.VerifySet(i => i.SelectedEntity = It.IsAny<Entity>(), Times.Never);
        }
    }
}
