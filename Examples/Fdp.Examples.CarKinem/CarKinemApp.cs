using System.Numerics;
using Raylib_cs;
using Fdp.Kernel;
using FDP.Framework.Raylib;
using FDP.Toolkit.Vis2D;
using FDP.Toolkit.Vis2D.Layers;
using Fdp.Examples.CarKinem.Simulation;
using Fdp.Examples.CarKinem.UI;
using Fdp.Examples.CarKinem.Input;
using Fdp.Examples.CarKinem.Visualization;
using CarKinem.Core;
using CarKinem.Road;

namespace Fdp.Examples.CarKinem;

public class CarKinemApp : FdpApplication
{
    private DemoSimulation _simulation = null!;
    private MainUI _legacyUI = null!;
    private SelectionManager _selectionManager = null!; 
    private InputManager _inputManager = null!;
    private PathEditingMode _pathEditor = null!;
    private MapCanvas _map = null!;
    private VehicleVisualizer _vehicleVisualizer = null!;
    private CarKinemInspectorAdapter _inspectorAdapter = null!;

    public CarKinemApp() : base(new ApplicationConfig 
    { 
        Width = 1280, 
        Height = 720, 
        WindowTitle = "FDP CarKinem (Semantically Ported)",
        TargetFPS = 60,
        Flags = ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint
    }) 
    { 
    }

    protected override void OnLoad()
    {
        // 1. Initialize Simulation
        _simulation = new DemoSimulation(); 
        
        // 2. Initialize Visuals (MapCanvas)
        _map = new MapCanvas();
        
        // Add Road Layer
        var roadLayer = new RoadMapLayer(_simulation.RoadNetwork);
        _map.AddLayer(roadLayer);

        // Add Vehicle Layer
        _vehicleVisualizer = new VehicleVisualizer();
        _selectionManager = new SelectionManager();
        _inspectorAdapter = new CarKinemInspectorAdapter(_selectionManager, _simulation.Repository);
        
        // Note: Repository is inside Simulation
        var vehicleQuery = _simulation.Repository.Query()
            .With<VehicleState>()
            .With<VehicleParams>()
            .Build();
            
        var vehicleLayer = new EntityRenderLayer(
            "Vehicles",     
            0,              // Layer Bit (Entities default to 0 if no MapDisplayComponent) 
            _simulation.Repository, 
            vehicleQuery, 
            _vehicleVisualizer, 
            _inspectorAdapter
        );
        _map.AddLayer(vehicleLayer);

        // Add Trajectory Layer (Overlay on top of roads/vehicles)
        var trajectoryLayer = new TrajectoryMapLayer(_simulation.TrajectoryPool, _simulation.View, _inspectorAdapter);
        _map.AddLayer(trajectoryLayer);

        // 3. Initialize Legacy UI & Input
        _legacyUI = new MainUI();
        _inputManager = new InputManager();
        _pathEditor = new PathEditingMode();
        
        // IMPORTANT: Disable InputManager's internal camera control so MapCanvas can handle it.
        _inputManager.EnableCameraControl = false;
    }

    protected override void OnUpdate(float dt)
    {
        // Update Simulation
        _simulation.Tick(dt, _legacyUI.TimeScale);
        
        // Update Map Logic (Camera, Tools)
        _map.Update(dt);

        // Handle Legacy Input (Selection, Spawning)
        // Get Raylib camera struct (copy) from MapCamera wrapper
        Camera2D cam = _map.Camera.InnerCamera; 
        
        _inputManager.HandleInput(
            _selectionManager, 
            _pathEditor, 
            ref cam, 
            _simulation, 
            _legacyUI.UIState
        );
        
        // _map.Camera.InnerCamera = cam; // NOT WRITING BACK. MapCanvas owns movement.
    }

    protected override void OnDrawWorld()
    {
        // Draw the Map (handles BeginMode2D/EndMode2D internally)
        _map.Draw();
    }

    protected override void OnDrawUI()
    {
        // Draw the Legacy UI (ImGui)
        _legacyUI.Render(_simulation, _selectionManager, _inspectorAdapter);
    }
    
    protected override void OnUnload()
    {
        _simulation?.Dispose();
        base.OnUnload();
    }
}
