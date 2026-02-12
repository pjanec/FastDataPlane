using System.Numerics;
using FluentAssertions;
using FDP.Toolkit.Vis2D.Components;
using Raylib_cs;
using Xunit;

namespace FDP.Toolkit.Vis2D.Tests;

public class MapCameraTests
{
    private class TestableMapCamera : MapCamera
    {
        // Publicly expose ProcessInput for testing
        public new void ProcessInput(float wheelMove, Vector2 mousePos, bool isPanDown, bool isInputCaptured)
        {
            base.ProcessInput(wheelMove, mousePos, isPanDown, isInputCaptured);
        }
    }

    [Fact]
    public void MapCamera_Defaults_AreSane()
    {
        var camera = new MapCamera();
        camera.Zoom.Should().Be(1.0f);
        camera.Target.Should().Be(Vector2.Zero);
        camera.Offset.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void MapCamera_ZoomIn_IncreasesZoom()
    {
        var camera = new TestableMapCamera();
        float initialZoom = camera.Zoom;
        
        // Zoom in (positive wheel)
        camera.ProcessInput(1.0f, Vector2.Zero, false, false);
        
        camera.Zoom.Should().BeGreaterThan(initialZoom);
    }
    
    [Fact]
    public void MapCamera_ZoomOut_DecreasesZoom()
    {
        var camera = new TestableMapCamera();
        float initialZoom = camera.Zoom;
        
        // Zoom out (negative wheel)
        camera.ProcessInput(-1.0f, Vector2.Zero, false, false);
        
        camera.Zoom.Should().BeLessThan(initialZoom);
    }

    [Fact]
    public void MapCamera_ScreenToWorld_RoundTrip()
    {
        var camera = new MapCamera();
        camera.Zoom = 2.0f;
        camera.InnerCamera.Target = new Vector2(100, 100);
        camera.InnerCamera.Offset = new Vector2(400, 300); // Screen center usually

        Vector2 worldPoint = new Vector2(50, 50);
        Vector2 screenPoint = camera.WorldToScreen(worldPoint);
        Vector2 roundTrip = camera.ScreenToWorld(screenPoint);

        roundTrip.X.Should().BeApproximately(worldPoint.X, 0.001f);
        roundTrip.Y.Should().BeApproximately(worldPoint.Y, 0.001f);
    }

    [Fact]
    public void MapCamera_Zoom_CentersOnCursor()
    {
        var camera = new TestableMapCamera();
        // Setup: Camera looking at 0,0. Screen center 400,300.
        // Assume mouse is at 500,300 (Right of center).
        // World point under mouse: 
        // Screen(500,300) -> (500-400)/1 + 0 = 100. World X=100.
        camera.InnerCamera.Offset = new Vector2(400, 300);
        Vector2 mousePos = new Vector2(500, 300);
        
        // Check pre-condition
        Vector2 worldUnderMouseBefore = camera.ScreenToWorld(mousePos);
        worldUnderMouseBefore.X.Should().Be(100);
        
        // Act: Zoom in
        camera.ProcessInput(1.0f, mousePos, false, false);
        
        // Assert: World point under mouse should STILL be ~100
        Vector2 worldUnderMouseAfter = camera.ScreenToWorld(mousePos);
        worldUnderMouseAfter.X.Should().BeApproximately(100f, 0.1f);
        
        // Also verify zoom increased
        camera.Zoom.Should().BeGreaterThan(1.0f);
        
        // And Target should have moved to compensate
        // If we zoomed in, the world got bigger on screen, so target (center) must shift right to keep the point at 500 fixed?
        // Let's rely on the ScreenToWorld check.
    }
    
    [Fact]
    public void MapCamera_Pan_MovesTarget()
    {
        var camera = new TestableMapCamera();
        Vector2 startMouse = new Vector2(100, 100);
        Vector2 endMouse = new Vector2(50, 100); // Drag left by 50 pixels
        
        // 1. Start drag
        camera.ProcessInput(0, startMouse, true, false);
        
        // 2. Continue drag
        camera.ProcessInput(0, endMouse, true, false);
        
        // Dragging mouse LEFT should move camera RIGHT (Target X increases) ???
        // "Pan moves the world". If I drag map left (mouse goes left), the world moves left. 
        // If world moves left, the "Target" (point at center of screen) moves right.
        camera.Target.X.Should().BeGreaterThan(0); 
    }
    
    [Fact]
    public void MapCamera_InputCaptured_IgnoresInput()
    {
        var camera = new TestableMapCamera();
        float initialZoom = camera.Zoom;
        
        // Try to zoom while captured
        camera.ProcessInput(1.0f, Vector2.Zero, false, true);
        
        camera.Zoom.Should().Be(initialZoom);
    }
}
