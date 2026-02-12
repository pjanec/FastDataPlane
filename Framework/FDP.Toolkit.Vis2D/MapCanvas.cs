using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using FDP.Toolkit.Vis2D.Abstractions;
using FDP.Toolkit.Vis2D.Components;
using FDP.Framework.Raylib.Input;

namespace FDP.Toolkit.Vis2D
{
    public class MapCanvas
    {
        public MapCamera Camera { get; set; } = new MapCamera();
        public uint ActiveLayerMask { get; set; } = 0xFFFFFFFF;
        
        // Backing field for ActiveTool to allow private set
        public IMapTool ActiveTool { get; private set; }

        private readonly List<IMapLayer> _layers = new();

        public void AddLayer(IMapLayer layer)
        {
            if (!_layers.Contains(layer))
                _layers.Add(layer);
        }

        public void RemoveLayer(IMapLayer layer)
        {
            _layers.Remove(layer);
        }

        public void SwitchTool(IMapTool tool)
        {
            if (ActiveTool != null)
                ActiveTool.OnExit();
            
            ActiveTool = tool;
            
            if (ActiveTool != null)
                ActiveTool.OnEnter(this);
        }

        public void ResetTool()
        {
            SwitchTool(null);
        }

        public void Update(float dt)
        {
            // Update Camera
            Camera.Update(dt);

            // Update Layers (0 -> N usually, order doesn't matter much for update but consistency helps)
            foreach (var layer in _layers)
            {
                layer.Update(dt);
            }

            // Update Tool
            if (ActiveTool != null)
                ActiveTool.Update(dt);
            
            // Handle Input Routing
            HandleInput();
        }

        public void Draw()
        {
            Camera.BeginMode();

            var ctx = new RenderContext
            {
                Camera = Camera.InnerCamera,
                MouseWorldPos = Camera.ScreenToWorld(GetMousePosition()),
                DeltaTime = GetFrameTime(),
                VisibleLayersMask = ActiveLayerMask
            };

            // Draw Layers (0 -> N) - Bottom to Top
            foreach (var layer in _layers)
            {
                // Verify visibility
                if (IsLayerVisible(layer))
                {
                    layer.Draw(ctx);
                }
            }
            
            // Draw Tool Overlay (Topmost)
            if (ActiveTool != null)
            {
                ActiveTool.Draw(ctx);
            }

            Camera.EndMode();
        }

        private bool IsLayerVisible(IMapLayer layer)
        {
            if (layer.LayerBitIndex < 0) return true; // Always visible
            if (layer.LayerBitIndex >= 32) return false; // Out of range

            uint mask = 1u << layer.LayerBitIndex;
            return (ActiveLayerMask & mask) != 0;
        }

        // Internal virtual to allow testing input routing logic without Raylib
        // Or just implement it assuming mocked Raylib usage for now.
        // Actually since MapCanvas calls Raylib static methods, unit testing input routing is hard unless wrapped.
        // But for Task 3, integration with input is required.
        protected virtual void HandleInput()
        {
            if (IsMouseCaptured()) return;

            Vector2 mouseScreen = GetMousePosition();
            Vector2 mouseWorld = Camera.ScreenToWorld(mouseScreen);
            
            bool leftPressed = IsMouseButtonPressed(MouseButton.Left);
            bool rightPressed = IsMouseButtonPressed(MouseButton.Right);
            
            // Tool Priority
            if (ActiveTool != null)
            {
                bool consumed = false;

                // Clicks
                if (leftPressed) consumed = ActiveTool.HandleClick(mouseWorld, MouseButton.Left);
                if (!consumed && rightPressed) consumed = ActiveTool.HandleClick(mouseWorld, MouseButton.Right);
                
                // Hover
                ActiveTool.HandleHover(mouseWorld);

                // Drag? (Simple implementation: always call if button down?)
                // Interface: bool HandleDrag(Vector2 worldPos, Vector2 delta);
                // We calculate delta.
                if (IsMouseButtonDown(MouseButton.Left) || IsMouseButtonDown(MouseButton.Right))
                {
                    Vector2 deltaScreen = GetMouseDelta();
                    // Convert screen delta to world delta approximate?
                    // Accurate: ScreenToWorld(pos) - ScreenToWorld(pos - delta)
                    // Simple: deltaScreen / Zoom
                    Vector2 deltaWorld = deltaScreen * (1.0f / Camera.Zoom);
                    
                    if (deltaWorld != Vector2.Zero)
                        ActiveTool.HandleDrag(mouseWorld, deltaWorld); 
                }

                if (consumed) return;
            }

            // Layer Priority (Reverse N -> 0)
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                var layer = _layers[i];
                if (!IsLayerVisible(layer)) continue;

                if (leftPressed)
                {
                    if (layer.HandleInput(mouseWorld, MouseButton.Left, true)) return;
                }
                if (rightPressed)
                {
                    if (layer.HandleInput(mouseWorld, MouseButton.Right, true)) return;
                }
            }
        }

        // Virtual for testing
        protected virtual Vector2 GetMousePosition() => Raylib.GetMousePosition();
        protected virtual float GetFrameTime() => Raylib.GetFrameTime();
        protected virtual bool IsMouseButtonPressed(MouseButton button) => Raylib.IsMouseButtonPressed(button);
        protected virtual bool IsMouseButtonDown(MouseButton button) => Raylib.IsMouseButtonDown(button);
        protected virtual Vector2 GetMouseDelta() => Raylib.GetMouseDelta();
        protected virtual bool IsMouseCaptured() => InputFilter.IsMouseCaptured;
    }
}
