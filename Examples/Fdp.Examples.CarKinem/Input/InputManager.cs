using System.Numerics;
using Raylib_cs;
using Fdp.Kernel;
using Fdp.Examples.CarKinem.Core;
using CarKinem.Core;
using Fdp.Examples.CarKinem.UI; // For UIState
using CarKinem.Trajectory; // TrajectoryInterpolation

namespace Fdp.Examples.CarKinem.Input
{
    public class InputManager
    {
        public bool EnableCameraControl { get; set; } = false; // Controlled by MapCanvas now

        public void HandleInput(SelectionManager selection, ScenarioManager scenario, Camera2D camera, UIState uiState)
        {
            // Right Click Logic
            if (Raylib.IsMouseButtonPressed(MouseButton.Right))
            {
                if (selection.SelectedEntityId.HasValue)
                {
                    // Convert Mouse to World
                    Vector2 mousePos = Raylib.GetMousePosition();
                    Vector2 worldPos = Raylib.GetScreenToWorld2D(mousePos, camera);
                    
                    bool isShift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
                    
                    if (isShift)
                    {
                        // Append Waypoint
                        scenario.AddWaypoint(selection.SelectedEntityId.Value, worldPos, uiState.InterpolationMode);
                    }
                    else
                    {
                        // Set New Destination (Clear existing)
                        scenario.SetDestination(selection.SelectedEntityId.Value, worldPos, uiState.InterpolationMode);
                    }
                }
            }
            
            // Left Click to Deselect (if not clicking on UI)
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                // Check if ImGui wants mouse
                if (!ImGuiNET.ImGui.GetIO().WantCaptureMouse)
                {
                    // If we are NOT hovering any entity, then this is a background click -> Deselect
                    // This relies on MapCanvas/EntityRenderLayer updating HoveredEntity on the PREVIOUS frame
                    // or InputManager running AFTER map update (which it doesn't currently, but Hover persists usually).
                    // If HoveredEntityId is updated every frame by visualizer, it should be fine.
                    if (!selection.HoveredEntityId.HasValue)
                    {
                        selection.SelectedEntityId = null;
                    }
                }
            }
        }
    }
}
