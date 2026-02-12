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
        public IMapTool? ActiveTool
        {
             get => _toolStack.Count > 0 ? _toolStack.Peek() : null;
             // Set is removed, use SwitchTool/PushTool
        }

        private readonly Stack<IMapTool> _toolStack = new();
        private bool _isSwitching = false;
        
        // Input state tracking to separate Click from Drag
        private bool _isDraggingInteraction = false;

        public IReadOnlyList<IMapLayer> Layers => _layers;
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

        /// <summary>
        /// Clears the tool stack and sets the new tool as the base.
        /// Use this for major mode switches.
        /// </summary>
        public void SwitchTool(IMapTool? tool)
        {
            if (_isSwitching) return; // Prevent recursion loops
            
            // Check if we are already effective
            // if (ActiveTool == tool) return; // Hard to check with stack clearing semantics

            _isSwitching = true;
            try
            {
                // Exit all tools in stack from top to bottom
                while (_toolStack.Count > 0)
                {
                    var t = _toolStack.Pop();
                    t.OnExit();
                }
                
                if (tool != null)
                {
                    _toolStack.Push(tool);
                    tool.OnEnter(this);
                }
            }
            finally
            {
                _isSwitching = false;
            }
        }
        
        /// <summary>
        /// Pushes a new tool onto the stack (e.g. starting a sub-task).
        /// The previous tool is suspended (OnExit *is* called?).
        /// Convention: OnExit is usually called when losing focus.
        /// </summary>
        public void PushTool(IMapTool tool)
        {
             if (_isSwitching) return;
             _isSwitching = true;
             try 
             {
                 var current = ActiveTool;
                 // We choose NOT to call OnExit on the suspended tool? 
                 // Or we DO call OnExit, and OnEnter when it returns?
                 // Standard state machine: Exit old, Enter new.
                 if (current != null) current.OnExit();
                 
                 _toolStack.Push(tool);
                 tool.OnEnter(this);
             }
             finally { _isSwitching = false; }
        }
        
        /// <summary>
        /// Pops the current tool and returns to the previous one.
        /// </summary>
        public void PopTool()
        {
            if (_isSwitching) return;
            if (_toolStack.Count == 0) return;
            
            _isSwitching = true;
            try
            {
                var current = _toolStack.Pop();
                current.OnExit();
                
                var prev = ActiveTool;
                if (prev != null) prev.OnEnter(this);
            }
            finally { _isSwitching = false; }
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
            bool leftReleased = Raylib.IsMouseButtonReleased(MouseButton.Left);
            bool rightReleased = Raylib.IsMouseButtonReleased(MouseButton.Right);

            // Reset drag state on new press
            if (leftPressed || rightPressed)
            {
                _isDraggingInteraction = false;
            }
            
            // Tool Priority
            if (ActiveTool != null)
            {
                bool consumed = false;
                
                // 1. Hover (Always update hover state)
                try { ActiveTool.HandleHover(mouseWorld); } catch { /* Ignore */ }

                // 2. Drag (Priority over Click)
                // If we are holding buttons, check for drag
                if (IsMouseButtonDown(MouseButton.Left) || IsMouseButtonDown(MouseButton.Right))
                {
                    Vector2 deltaScreen = GetMouseDelta();
                    Vector2 deltaWorld = deltaScreen * (1.0f / Camera.Zoom);
                    
                    // Allow tools to decide if they want to handle drag even with zero delta (e.g. initial grab)
                    // But typically we send delta.
                    if (deltaWorld != Vector2.Zero || _isDraggingInteraction)
                    {
                         bool dragged = ActiveTool.HandleDrag(mouseWorld, deltaWorld);
                         if (dragged) _isDraggingInteraction = true;
                    }
                }

                // 3. Click (Only on Release, and only if we didn't Drag)
                // This prevents "Selection" triggering at the end of a Box Select or Entity Drag
                // when the user releases the mouse.
                if (!_isDraggingInteraction)
                {
                    if (leftReleased) consumed = ActiveTool.HandleClick(mouseWorld, MouseButton.Left);
                    if (!consumed && rightReleased) consumed = ActiveTool.HandleClick(mouseWorld, MouseButton.Right);
                }
                else
                {
                    // effectively consumed by drag
                    if (leftReleased || rightReleased) consumed = true;
                }

                // Note: If Tool consumes Release, we assume it blocked lower layers from acting on Release implies they shouldn't act on Press?
                // But Layers usually act on Press. 
                // If Tool ignores 'Press', Layers might act on it.
                // Standard Map Interaction usually blocks Layers if the Mouse is over the Map area.
                // For now, we preserve existing Layer-Press logic if Tool didn't consume Click (but Layer-Press happens on Press).
                // Wait: checks below are for 'leftPressed'.
                // If ActiveTool moved to Release, 'leftPressed' is unhandled by Tool here.
                // So Layers get all Presses. This is likely Correct for UI overlaid on Map.
                
                // If dragged, we consume everything.
                if (_isDraggingInteraction) return; 
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
