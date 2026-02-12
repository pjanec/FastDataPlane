using System.Numerics;
using Fdp.Kernel;
using Fdp.Kernel.Collections;
using FDP.Toolkit.ImGui.Abstractions;
using FDP.Toolkit.Vis2D.Abstractions;
using ModuleHost.Core.Abstractions;
using Raylib_cs;

namespace FDP.Toolkit.Vis2D.Tools
{
    public class SelectionTool : IMapTool
    {
        public string Name => "Selection";

        private readonly ISimulationView _view;
        private readonly EntityQuery _query;
        private readonly IInspectorContext _inspector;
        private readonly IVisualizerAdapter _adapter;

        private bool _isDragging;
        private Vector2 _startPos;
        private Vector2 _currentPos;
        // Threshold to distinguish click from drag
        private const float DRAG_THRESHOLD = 5.0f; 

        public SelectionTool(ISimulationView view, EntityQuery query, IInspectorContext inspector, IVisualizerAdapter adapter)
        {
            _view = view;
            _query = query;
            _inspector = inspector;
            _adapter = adapter;
        }

        public void OnEnter(MapCanvas canvas) { }
        public void OnExit() { _isDragging = false; }

        public virtual void Update(float dt)
        {
            // Check for drag release
            if (_isDragging && IsMouseButtonReleased(MouseButton.Left))
            {
                FinishSelection();
                _isDragging = false;
            }
        }

        public virtual void Draw(RenderContext ctx)
        {
            if (_isDragging)
            {
                float width = _currentPos.X - _startPos.X;
                float height = _currentPos.Y - _startPos.Y;
                DrawSelectionBox(_startPos, width, height);
            }
        }

        protected virtual bool IsMouseButtonReleased(MouseButton button) => Raylib.IsMouseButtonReleased(button);
        protected virtual bool IsMouseButtonDown(MouseButton button) => Raylib.IsMouseButtonDown(button);
        protected virtual void DrawSelectionBox(Vector2 start, float width, float height) => Raylib.DrawRectangleLines((int)start.X, (int)start.Y, (int)width, (int)height, Color.Green);

        public bool HandleClick(Vector2 worldPos, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _startPos = worldPos;
                _currentPos = worldPos;
                // Do NOT start dragging immediately. Wait for movement.
                // But we must NOT consume the click yet, so Single Click Selection (Layer) works.
                // If we return false, Layers get the click.
                
                // If we want Box Selection to override Layer Selection when dragging starts...
                // Only if Drag happens.
                
                return false; 
            }
            return false;
        }

        public bool HandleDrag(Vector2 worldPos, Vector2 delta)
        {
            if (IsMouseButtonDown(MouseButton.Left))
            {
                _currentPos = worldPos;
                
                if (!_isDragging)
                {
                    if (Vector2.Distance(_currentPos, _startPos) > DRAG_THRESHOLD)
                    {
                        _isDragging = true;
                    }
                }
                
                if (_isDragging) return true; // Consume drag input
            }
            return false;
        }

        public bool HandleHover(Vector2 worldPos) => false;
        
        public void FinishSelection() // Public for testing? No, keep private and test via Update or public Finish
        {
            // Normalize box
            float xMin = System.Math.Min(_startPos.X, _currentPos.X);
            float xMax = System.Math.Max(_startPos.X, _currentPos.X);
            float yMin = System.Math.Min(_startPos.Y, _currentPos.Y);
            float yMax = System.Math.Max(_startPos.Y, _currentPos.Y);

            // Find first entity in box? Or all?
            // Inspector usually selects one.
            // Let's select the first one found or closest to center?
            // "Box Selection" often implies multi-selection, but Inspector is single-entity.
            // Requirement says "handles box selection". 
            // If I just select the first one, it's fine for now.
            
            foreach (var entity in _query)
            {
                var pos = _adapter.GetPosition(_view, entity);
                if (pos.HasValue)
                {
                    if (pos.Value.X >= xMin && pos.Value.X <= xMax &&
                        pos.Value.Y >= yMin && pos.Value.Y <= yMax)
                    {
                        _inspector.SelectedEntity = entity;
                        return; // Select first and exit
                    }
                }
            }
        }
    }
}
