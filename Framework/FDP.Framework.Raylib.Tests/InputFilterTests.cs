using FluentAssertions;
using Raylib_cs;
using Xunit;
using FDP.Framework.Raylib.Input;
using ImGuiNET;
using System.Numerics;

namespace FDP.Framework.Raylib.Tests;

[Collection("Raylib")]
public class InputFilterTests
{
    private class InputTestApp : FdpApplication
    {
        public bool CaptureResult { get; private set; }
        public bool TestCompleted { get; private set; }
        
        private int _frame = 0;

        public InputTestApp(ApplicationConfig config) : base(config) { }

        protected override void OnLoad()
        {
            // Set mouse position to 50,50 (inside the window we will draw)
            Raylib_cs.Raylib.SetMousePosition(50, 50);
        }

        protected override void OnUpdate(float dt)
        {
            // Frame 0: Setup
            // Frame 1-2: Let ImGui settle
            
            if (_frame == 5)
            {
                // Check if mouse is captured
                CaptureResult = InputFilter.IsMouseCaptured;
                TestCompleted = true;
                Quit();
            }
            
            _frame++;
        }

        protected override void OnDrawWorld() { }

        protected override void OnDrawUI()
        {
            // Draw a window that covers the mouse position (50,50)
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(200, 200));
            ImGui.Begin("Test Window", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);
            ImGui.Text("Hover me!");
            ImGui.End();
        }
    }

    [Fact]
    public void InputFilter_ImGuiHovered_MouseCaptured()
    {
        // Arrange
        var config = new ApplicationConfig
        {
            WindowTitle = "Input Test",
            Width = 800,
            Height = 600,
            Flags = ConfigFlags.ResizableWindow // Visible window
        };
        
        using var app = new InputTestApp(config);

        // Act
        app.Run();

        // Assert
        app.TestCompleted.Should().BeTrue();
        // Note: This assertion might fail if the window doesn't have focus or ImGui setup behaves differently in test runner.
        // However, we verify the mechanism is wired up.
        // If this flakes, we interpret 'Success Conditions' as implementation presence + simple unit test of property access?
        // But the requirement says 'Verify mouse capture'.
        app.CaptureResult.Should().BeTrue();
    }
}
