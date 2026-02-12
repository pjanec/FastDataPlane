using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using Fdp.Kernel;
using FDP.Toolkit.Vis2D.Abstractions;
using ModuleHost.Core.Abstractions;

namespace FDP.Toolkit.Vis2D.Tools
{
    /// <summary>
    /// The standard "Default" tool that handles:
    /// 1. Clicking to select.
    /// 2. Dragging entities (delegates to EntityDragTool).
    /// 3. Box selection (delegates to BoxSelectionTool).
    /// 4. Global hotkeys (Delete).
    /// 5. Global Actions (Shift+Right Click Waypoints).
    /// </summary>
    public class StandardInteractionTool : IMapTool
    {
        public string Name => "Interaction";

        // Configuration
        private const float DRAG_THRESHOLD = 5.0f;

        // Dependencies
        private readonly ISimulationView _view;
        private readonly EntityQuery _query;
        private readonly IVisualizerAdapter _adapter;
        
        // Callbacks / Action Handlers
        // Generic Events (Decoupled)
        public event Action<Vector2, MouseButton, bool, bool, Entity>? OnWorldClick; // Pos, Button, Shift, Ctrl, HitEntity
        public event Action<List<Entity>>? OnRegionSelected;     // Result of Box Select
        public event Action<Entity, Vector2>? OnEntityMoved;     // Result of Drag Operation

        // State
        private bool _isLeftMouseDown;
        private Vector2 _mouseDownPos;
        private Entity _potentialTarget = Entity.Null;
        private bool _shiftHeld => Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        private bool _ctrlHeld => Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        private MapCanvas _canvas;

        public StandardInteractionTool(
            ISimulationView view,
            EntityQuery query,
            IVisualizerAdapter adapter)
        {
            _view = view;
            _query = query;
            _adapter = adapter;
        }

        public void OnEnter(MapCanvas canvas)
        {
            _canvas = canvas;
            ResetState();
        }

        public void OnExit()
        {
            ResetState();
        }

        private void ResetState()
        {
            _isLeftMouseDown = false;
            _potentialTarget = Entity.Null;
        }

        public void Update(float dt)
        {
            if (_isLeftMouseDown && !Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                ResetState();
            }
            // No global key handling in generic tool
        }

        public void Draw(RenderContext ctx)
        {
            // Nothing to draw
        }

        public bool HandleClick(Vector2 worldPos, MouseButton button)
        {
            // Detect hit
            var hit = FindEntityAt(worldPos);
            
            // Notify Generic Click
            OnWorldClick?.Invoke(worldPos, button, _shiftHeld, _ctrlHeld, hit);
            
            return true; // Consumed
        }

        public bool HandleDrag(Vector2 worldPos, Vector2 delta)
        {
            if (_isLeftMouseDown)
            {
                float dist = Vector2.Distance(worldPos, _mouseDownPos);
                if (dist > DRAG_THRESHOLD)
                {
                    // Threshold passed -> Decide Action
                    if (_view.IsAlive(_potentialTarget))
                    {
                        // Dragging on an entity -> Entity Drag
                        var startPos = _adapter.GetPosition(_view, _potentialTarget) ?? _mouseDownPos;
                        var tool = new EntityDragTool(
                            _potentialTarget, 
                            startPos, 
                            (e, p) => OnEntityMoved?.Invoke(e, p),
                            () => _canvas.PopTool()
                        );
                        _canvas.PushTool(tool);

                        ResetState();
                        return true;
                    }
                    else
                    {
                        // Dragging on empty space -> Box Select
                        var tool = new BoxSelectionTool(
                            _mouseDownPos,
                            _view,
                            _query,
                            _adapter,
                            (list) => { OnRegionSelected?.Invoke(list); _canvas.PopTool(); },
                            () => _canvas.PopTool()
                        );
                        _canvas.PushTool(tool);

                        ResetState();
                        return true;
                    }
                }
            }
            
            return false;
        }

        public bool HandleHover(Vector2 worldPos)
        {
            // Track Mouse Down for Drag Initiation
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _isLeftMouseDown = true;
                _mouseDownPos = worldPos;
                _potentialTarget = FindEntityAt(worldPos);
            }
            return false;
        }

        private Entity FindEntityAt(Vector2 pos)
        {
            float bestDistSq = float.MaxValue;
            Entity bestEntity = Entity.Null;

            foreach (var entity in _query)
            {
                var p = _adapter.GetPosition(_view, entity);
                if (!p.HasValue) continue;

                float r = _adapter.GetHitRadius(_view, entity);
                // Hit Area logic could be improved (screen space vs world space), 
                // but using Radius for now.
                
                float dSq = Vector2.DistanceSquared(p.Value, pos);

                if (dSq <= r * r && dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    bestEntity = entity;
                }
            }
            return bestEntity;
        }
    }
}
