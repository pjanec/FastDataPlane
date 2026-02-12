using ImGuiNET;
using System.Numerics;
using Fdp.Examples.CarKinem.Simulation;
using Fdp.Examples.CarKinem.Input;
using FDP.Toolkit.ImGui.Panels; // Framework
using FDP.Toolkit.ImGui.Abstractions;

namespace Fdp.Examples.CarKinem.UI
{
    public class MainUI
    {
        private SpawnControlsPanel _spawnControls = new();
        private SimulationControlsPanel _simControls = new();
        private EntityInspectorPanel _entityInspector = new(); // Framework Panel
        private EventBrowserPanel _eventInspector = new();     // Framework Panel
        private PerformancePanel _perfPanel = new();
        private SystemPerformanceWindow _sysPerfWindow = new();
        
        public UIState UIState { get; } = new();
        public bool IsPaused => _simControls.IsPaused;
        public float TimeScale => _simControls.TimeScale;

        public void Render(DemoSimulation simulation, SelectionManager selection, IInspectorContext inspectorCtx)
        {
            ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(300, 500), ImGuiCond.FirstUseEver);
            
            if (ImGui.Begin("Simulation Control"))
            {
                ImGui.Text($"FPS: {Raylib_cs.Raylib.GetFPS()}");
                ImGui.Separator();
                
                if (ImGui.CollapsingHeader("Simulation", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    _simControls.Render(simulation);
                }
                
                if (ImGui.CollapsingHeader("Spawning", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    _spawnControls.Render(simulation, UIState);
                }
                
                if (ImGui.CollapsingHeader("Performance", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    _perfPanel.Render(simulation);
                    ImGui.Checkbox("Show System Profiler", ref _sysPerfWindow.IsOpen);
                }
                
                ImGui.End();
            }
            
            // Entity Inspector - use Framework panel
            _entityInspector.Draw(simulation.Repository, inspectorCtx);
            
            // Event Inspector - use Framework panel
            // Need frame count? Use arbitrary incrementing counter or try to get from simulation
            uint frame = (uint)Raylib_cs.Raylib.GetTime() * 60; // Approximate
            // Better: _eventInspector.Update(simulation.Repository.Bus, frame);
            // Actually, EventBrowserPanel needs Update() called to capture.
            // Let's do it here for simplicity, assuming Bus has data available.
            
            // Note: EventBrowser uses internal history, Update adds to it.
            // If we call Update here every frame, we capture events.
            _eventInspector.Update(simulation.Repository.Bus, frame);
            _eventInspector.Draw();
            
            // System Performance Profiler - separate window
            _sysPerfWindow.Render(simulation.Systems);
        }
    }
}
