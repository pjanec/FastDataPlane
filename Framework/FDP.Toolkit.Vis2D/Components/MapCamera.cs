using System.Numerics;
using Raylib_cs;
using FDP.Framework.Raylib.Input;

namespace FDP.Toolkit.Vis2D.Components
{
    /// <summary>
    /// Wrapper around Raylib Camera2D with mouse control.
    /// Handles pan, zoom-to-cursor, and coordinate conversion.
    /// </summary>
    public class MapCamera
    {
        public Camera2D InnerCamera; // Public field for direct access if needed, or property
        
        public float Zoom 
        { 
            get => InnerCamera.Zoom; 
            set => InnerCamera.Zoom = value; 
        }

        public Vector2 Target
        {
            get => InnerCamera.Target;
            set => InnerCamera.Target = value;
        }
        
        public Vector2 Offset
        {
            get => InnerCamera.Offset;
            set => InnerCamera.Offset = value;
        }

        // Configuration
        public float ZoomSpeed { get; set; } = 0.1f;
        public float MinZoom { get; set; } = 0.1f;
        public float MaxZoom { get; set; } = 10.0f;
        public MouseButton PanButton { get; set; } = MouseButton.Right;

        // State for dragging
        private Vector2 _lastMousePos;
        private bool _isDragging;

        public MapCamera()
        {
            InnerCamera = new Camera2D();
            InnerCamera.Zoom = 1.0f;
            InnerCamera.Rotation = 0.0f;
            InnerCamera.Offset = Vector2.Zero;
            InnerCamera.Target = Vector2.Zero;
        }

        public virtual void Update(float dt)
        {
            // Gather inputs
            float wheel = Raylib.GetMouseWheelMove();
            Vector2 mousePos = Raylib.GetMousePosition();
            bool isPanDown = Raylib.IsMouseButtonDown(PanButton);
            bool isCaptured = InputFilter.IsMouseCaptured;

            ProcessInput(wheel, mousePos, isPanDown, isCaptured);
        }

        /// <summary>
        /// Update camera state based on inputs. Public for testing.
        /// </summary>
        public void ProcessInput(float wheelMove, Vector2 mousePos, bool isPanDown, bool isInputCaptured)
        {
            if (isInputCaptured)
            {
                _isDragging = false;
                return;
            }

            // Zoom
            if (wheelMove != 0)
            {
                // Zoom to cursor logic:
                // 1. Get world point under cursor
                Vector2 mouseWorldBefore = ScreenToWorld(mousePos);

                // 2. Adjust zoom
                float zoomFactor = 1.0f + (wheelMove * ZoomSpeed);
                // Try to keep zoom changes proportional? Or just add?
                // Standard is usually multiplicative or exponential but linear is fine for now.
                // Let's use simple addition/subtraction or multiplication.
                // Task says "ZoomSpeed". Usually implies linear or multiplicative factor.
                
                // Let's implement multiplicative zoom for smoother feel
                // If wheel > 0, zoom in (multiply by 1.1)
                // If wheel < 0, zoom out (multiply by 0.9)
                // Or use ZoomSpeed as a t value.
                
                float newZoom = InnerCamera.Zoom + (wheelMove * ZoomSpeed * InnerCamera.Zoom); 
                // Using linear addition based on current zoom level makes it exponential-ish
                
                newZoom = System.Math.Clamp(newZoom, MinZoom, MaxZoom);

                InnerCamera.Zoom = newZoom;

                // 3. Adjust target so that world point is still under cursor
                // For this we need ScreenToWorld with new zoom.
                // Actually easier: 
                // target = mouseWorldBefore - (mousePos - offset) / newZoom
                // ScreenToWorld(screen) = target + (screen - offset) / zoom
                // We want ScreenToWorld(mousePos) == mouseWorldBefore
                // mouseWorldBefore = newTarget + (mousePos - offset) / newZoom
                // newTarget = mouseWorldBefore - (mousePos - offset) / newZoom
                
                InnerCamera.Target = mouseWorldBefore - (mousePos - InnerCamera.Offset) / InnerCamera.Zoom;
            }

            // Pan
            if (isPanDown)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    _lastMousePos = mousePos;
                }
                else
                {
                    Vector2 deltaWrapper = mousePos - _lastMousePos;
                    // Pan moves the world, so we move target in opposite direction of mouse drag, scaled by zoom
                    // deltaScreen = deltaWorld * Zoom
                    // deltaWorld = deltaScreen / Zoom
                    // We move Target by -deltaWorld
                    
                    Vector2 deltaWorld = deltaWrapper / InnerCamera.Zoom;
                    InnerCamera.Target -= deltaWorld;
                    
                    _lastMousePos = mousePos;
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        public virtual void BeginMode()
        {
            Raylib.BeginMode2D(InnerCamera);
        }

        public virtual void EndMode()
        {
            Raylib.EndMode2D();
        }

        public virtual Vector2 ScreenToWorld(Vector2 screenPos)
        {
            // Calculate manually to support unit testing (Raylib context might not be available) and ensure consistency
            // Formula matches Raylib's GetScreenToWorld2D for Rotation=0
            // World = (Screen - Offset) / Zoom + Target
            return (screenPos - InnerCamera.Offset) / InnerCamera.Zoom + InnerCamera.Target;
        }

        public virtual Vector2 WorldToScreen(Vector2 worldPos)
        {
            // Calculate manually to support unit testing
            // Screen = (World - Target) * Zoom + Offset
            return (worldPos - InnerCamera.Target) * InnerCamera.Zoom + InnerCamera.Offset;
        }
    }
}
