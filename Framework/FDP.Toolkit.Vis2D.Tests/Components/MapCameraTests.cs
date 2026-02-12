using System.Numerics;
using Xunit;
using FDP.Toolkit.Vis2D.Components;
using Raylib_cs;

namespace FDP.Toolkit.Vis2D.Tests.Components
{
    // Mock class to avoid native Raylib calls
    public class TestableMapCamera : MapCamera
    {
        public override Vector2 ScreenToWorld(Vector2 screenPos)
        {
            // Simple logic: World = Target + (Screen - Offset) / Zoom
            return InnerCamera.Target + (screenPos - InnerCamera.Offset) * (1.0f / InnerCamera.Zoom);
        }

        public override Vector2 WorldToScreen(Vector2 worldPos)
        {
            // Simple logic: Screen = Offset + (World - Target) * Zoom
            return InnerCamera.Offset + (worldPos - InnerCamera.Target) * InnerCamera.Zoom;
        }
    }

    public class MapCameraTests
    {
        [Fact]
        public void MapCamera_ZoomIn_IncreasesZoom()
        {
            // Arrange
            var camera = new TestableMapCamera();
            camera.Zoom = 1.0f;
            float initialZoom = camera.Zoom;

            // Act - simulate wheel move up (positive)
            // Zoom speed is 0.1f by default. 1.0 + (1.0 * 0.1 * 1.0) = 1.1
            // Or if logic is zoom += wheel * speed * zoom
            camera.ProcessInput(1.0f, Vector2.Zero, false, false);

            // Assert
            Assert.True(camera.Zoom > initialZoom);
        }

        [Fact]
        public void MapCamera_ZoomClamp_EnforcesLimits()
        {
            var camera = new TestableMapCamera();
            camera.MinZoom = 0.5f;
            camera.MaxZoom = 2.0f;
            camera.Zoom = 2.0f;

            // Act - try to zoom in more
            camera.ProcessInput(1.0f, Vector2.Zero, false, false);

            // Assert
            Assert.Equal(2.0f, camera.Zoom);

            // Act - try to zoom out below min
            camera.Zoom = 0.5f;
            camera.ProcessInput(-1.0f, Vector2.Zero, false, false);
            
            Assert.Equal(0.5f, camera.Zoom);
        }

        [Fact]
        public void MapCamera_ScreenToWorld_RoundTrip()
        {
            var camera = new TestableMapCamera();
            camera.Zoom = 2.0f;
            camera.Target = new Vector2(50, 50);
            camera.Offset = new Vector2(400, 300); // Center of screen usually

            Vector2 screenPoint = new Vector2(400, 300); // Should map to target directly if offset is center
            Vector2 worldPoint = camera.ScreenToWorld(screenPoint);

            Assert.Equal(new Vector2(50, 50), worldPoint);

            Vector2 screenPointBack = camera.WorldToScreen(worldPoint);
            Assert.Equal(screenPoint, screenPointBack);
        }

        [Fact]
        public void MapCamera_ImGuiCapture_IgnoresInput()
        {
            var camera = new TestableMapCamera();
            float initialZoom = camera.Zoom;

            // Act - input captured
            camera.ProcessInput(1.0f, Vector2.Zero, false, true);

            // Assert
            Assert.Equal(initialZoom, camera.Zoom);
        }

        [Fact]
        public void MapCamera_Pan_MovesTarget()
        {
            var camera = new TestableMapCamera();
            camera.Zoom = 1.0f;
            camera.Target = Vector2.Zero;

            // Start drag
            camera.ProcessInput(0, new Vector2(100, 100), true, false);
            
            // Move mouse by (10, 10)
            camera.ProcessInput(0, new Vector2(110, 110), true, false);

            // We moved mouse right/down by 10.
            // World should move right/down, meaning Target should move left/up (decrease).
            // Delta = (10, 10). Target -= Delta.
            Assert.Equal(new Vector2(-10, -10), camera.Target);
        }
    }
}
