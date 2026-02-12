# MAP-TASK-DETAIL.md
## FDP UI Reusable Toolkits - Task Details

**Reference Design**: [MAP-DESIGN.md](./MAP-DESIGN.md)  
**Task Tracker**: [MAP-TASK-TRACKER.md](./MAP-TASK-TRACKER.md)  
**Version**: 1.0  
**Date**: 2026-02-11

---

## Phase 1: FDP.Toolkit.ImGui

### MAP-P1-001: Project Setup and Core Abstractions

**Description**:
Create the `FDP.Toolkit.ImGui` class library project with proper dependencies and implement core abstraction interfaces.

**Dependencies**: None

**Implementation Steps**:
1. Create project: `Framework/FDP.Toolkit.ImGui/FDP.Toolkit.ImGui.csproj`
2. Add NuGet package: `ImGui.NET` version 1.91.0.1
3. Add project references: `Fdp.Kernel`, `ModuleHost.Core`
4. Create folder structure: `Abstractions/`, `Panels/`, `Utils/`
5. Implement `IInspectorContext` interface in `Abstractions/IInspectorContext.cs`
6. Implement `InspectorState` default implementation

**Success Conditions**:
- Project builds without errors
- `IInspectorContext` interface defined with `SelectedEntity` and `HoveredEntity` properties
- `InspectorState` class implements interface with auto-properties
- All dependencies resolve correctly

**Unit Tests**:
- `InspectorState_DefaultConstructor_InitializesNull()` - Verify default nulls
- `InspectorState_SetSelectedEntity_Persists()` - Verify state storage

---

### MAP-P1-002: ComponentReflector Implementation

**Description**:
Implement generic component rendering system using cached reflection to display arbitrary FDP components.

**Dependencies**: MAP-P1-001

**Implementation Steps**:
1. Create `Utils/ComponentReflector.cs`
2. Implement field reflection cache using `Dictionary<Type, List<FieldInfo>>`
3. Create `DrawComponents(EntityRepository repo, Entity e)` method
4. Implement `DrawObjectProperties(object obj, Entity entity, EntityRepository repo)` with ImGui table layout
5. Use `ImGui.InputFloat`, `ImGui.InputInt`, `ImGui.InputText` for editable fields (NOT just Text display)
6. Detect changes using `ImGui.IsItemDeactivatedAfterEdit()`
7. Write back modified values using `PropertyInfo.SetValue` + `repo.SetComponent<T>(entity, modifiedComponent)` to trigger ECS versioning
8. Add special formatters for Vector2, Vector3 (use `ImGui.InputFloat2`, `InputFloat3`)
9. Create `Utils/RepoReflector.cs` for cached generic method invocation (GetComponent, SetComponent)

**Success Conditions**:
- Can iterate all component types on an entity
- Renders component fields in two-column table (Name | Editable Widget)
- User can modify float/int/Vector2 fields in-place
- Changes written back to ECS immediately with proper versioning
- Caches reflection data (subsequent calls don't re-reflect)
- Handles null/default values gracefully
- Supports both managed and unmanaged component types

**Unit Tests**:
- `ComponentReflector_DrawComponents_IteratesAllTypes()` - Verify iteration
- `ComponentReflector_CachesFieldInfo_NoReflectionOnSecondCall()` - Verify caching
- `ComponentReflector_FormatVector2_ShowsTwoDecimals()` - Verify formatting

---

### MAP-P1-003: EntityInspectorPanel Implementation

**Description**:
Create the main entity inspection UI panel with entity list, search, and detail view.

**Dependencies**: MAP-P1-002

**Implementation Steps**:
1. Create `Panels/EntityInspectorPanel.cs`
2. Implement search filter field using `ImGui.InputTextWithHint`
3. Create two-column layout using `ImGui.BeginTable` with `ImGuiTableFlags.Resizable`
4. Implement `DrawEntityList()` - left panel with entity selection
5. Implement `DrawEntityDetails()` - right panel using ComponentReflector
6. Add hover detection to set `HoveredEntity`
7. Optimize to limit displayed entities to 1000 max

**Success Conditions**:
- Window displays with "Entity Inspector" title
- Left panel shows scrollable entity list
- Right panel shows selected entity components
- Search filter by entity ID works
- Selection syncs with `IInspectorContext.SelectedEntity`
- Hover updates `IInspectorContext.HoveredEntity`
- List truncates at 1000 entities with "... (List truncated) ..." message

**Unit Tests**:
- `EntityInspectorPanel_SelectEntity_UpdatesContext()` - Verify selection
- `EntityInspectorPanel_SearchFilter_FiltersById()` - Verify filtering
- `EntityInspectorPanel_HoverEntity_UpdatesHoveredState()` - Verify hover

**Integration Tests**:
- Create EntityRepository with 10 entities
- Verify panel renders all entities
- Click entity, verify selection changes

---

### MAP-P1-004: SystemProfilerPanel Implementation

**Description**:
Implement system performance monitoring panel displaying ModuleHost statistics.

**Dependencies**: MAP-P1-001

**Implementation Steps**:
1. Create `Panels/SystemProfilerPanel.cs`
2. Implement `Draw(List<ModuleStats> stats)` method
3. Create 4-column table: Module | Executions | Status | Failures
4. Add color coding: Green for Closed circuit, Red for Open
5. Implement sortable columns using `ImGuiTableFlags.Sortable`
6. Add warning color (orange) for non-zero failure counts

**Success Conditions**:
- Window displays with "System Profiler" title
- Table shows all registered modules
- Circuit state color-coded correctly
- Execution counts displayed
- Failure counts highlighted in orange/red
- Table columns are sortable

**Unit Tests**:
- `SystemProfilerPanel_Draw_ShowsAllModules()` - Verify all modules displayed
- `SystemProfilerPanel_ClosedCircuit_ShowsGreen()` - Verify color coding

---

### MAP-P1-005: EventBrowserPanel Implementation

**Description**:
Create event history capture and display panel with pause/resume functionality.

**Dependencies**: MAP-P1-001

**Implementation Steps**:
1. Create `Panels/EventBrowserPanel.cs`
2. Define `CapturedEvent` struct with Frame, Type, Data fields
3. Implement history buffer with configurable capacity (default 100)
4. Create `Update(FdpEventBus bus, uint currentFrame)` method
5. Implement `Draw()` with pause checkbox and clear button
6. Display events in reverse chronological order
7. Color-code frame numbers in green

**Success Conditions**:
- Window displays with "Event Browser" title
- Captures events from event bus each frame (when not paused)
- Pause checkbox stops capture
- Clear button empties history
- Events displayed newest-first
- Frame numbers color-coded
- History limited to 100 events (configurable)

**Unit Tests**:
- `EventBrowserPanel_Update_CapturesEvents()` - Verify capture
- `EventBrowserPanel_Pause_StopsCapture()` - Verify pause
- `EventBrowserPanel_Clear_EmptiesHistory()` - Verify clear
- `EventBrowserPanel_CapacityLimit_TrimsOldest()` - Verify capacity

---

## Phase 2: FDP.Framework.Raylib

### MAP-P2-001: Project Setup and ApplicationConfig

**Description**:
Create the `FDP.Framework.Raylib` class library with Raylib dependencies and configuration structures.

**Dependencies**: None

**Implementation Steps**:
1. Create project: `Framework/FDP.Framework.Raylib/FDP.Framework.Raylib.csproj`
2. Set `AllowUnsafeBlocks` to true
3. Add NuGet packages: `Raylib-cs` 7.0.2, `rlImgui-cs` 3.2.0
4. Add project references: `Fdp.Kernel`, `ModuleHost.Core`
5. Create `ApplicationConfig.cs` struct with window properties
6. Add defaults: 1280×720, 60 FPS, ResizableWindow | Msaa4xHint

**Success Conditions**:
- Project builds without errors
- `ApplicationConfig` struct has all required properties
- Default constructor provides sensible defaults
- TargetFramework is net8.0

**Unit Tests**:
- `ApplicationConfig_DefaultConstructor_HasDefaults()` - Verify defaults

---

### MAP-P2-002: FdpApplication Base Class

**Description**:
Implement the abstract application host base class with standardized lifecycle.

**Dependencies**: MAP-P2-001

**Implementation Steps**:
1. Create `FdpApplication.cs` abstract class
2. Add protected properties: `World`, `Kernel`, `Config`
3. Implement `Run()` method with main loop
4. Create lifecycle hooks: `OnLoad()`, `OnUpdate(float)`, `OnDrawWorld()`, `OnDrawUI()`, `OnUnload()`
5. Implement window initialization/shutdown with rlImGui setup
6. Add IDisposable support
7. Structure loop: Update → BeginDrawing → DrawWorld → rlImGui(DrawUI) → EndDrawing

**Success Conditions**:
- `Run()` initializes window with config settings
- rlImGui initialized with docking enabled
- Lifecycle methods called in correct order
- Window closes cleanly on exit
- Default `OnUpdate()` calls `Kernel?.Update()` if not overridden
- Dispose pattern implemented correctly

**Unit Tests**:
- `FdpApplication_Run_CallsLifecycleMethods()` - Verify order (mock test)

**Integration Tests**:
- Create minimal derived app class
- Call `Run()`, verify window opens
- Close window, verify cleanup

---

### MAP-P2-003: InputFilter Utility

**Description**:
Implement mouse/keyboard capture detection to prevent UI bleed-through.

**Dependencies**: MAP-P2-001

**Implementation Steps**:
1. Create `Input/InputFilter.cs` static class
2. Implement `IsMouseCaptured` property using `ImGui.GetIO().WantCaptureMouse`
3. Implement `IsKeyboardCaptured` property using `ImGui.GetIO().WantCaptureKeyboard`
4. Add XML documentation explaining usage

**Success Conditions**:
- `IsMouseCaptured` returns true when ImGui window hovered
- `IsKeyboardCaptured` returns true when ImGui text input focused
- Static properties accessible from anywhere
- No allocations

**Unit Tests**:
- `InputFilter_ImGuiHovered_MouseCaptured()` - Verify mouse capture

---

## Phase 3: FDP.Toolkit.Vis2D

### MAP-P3-001: Project Setup and Core Abstractions

**Description**:
Create the `FDP.Toolkit.Vis2D` class library with core adapter interfaces.

**Dependencies**: MAP-P1-001, MAP-P2-001

**Implementation Steps**:
1. Create project: `Framework/FDP.Toolkit.Vis2D/FDP.Toolkit.Vis2D.csproj`
2. Add project references: `Fdp.Kernel`, `ModuleHost.Core`, `FDP.Framework.Raylib`, `FDP.Toolkit.ImGui`
3. Create folder structure: `Abstractions/`, `Components/`, `Layers/`, `Tools/`, `Systems/`, `Defaults/`
4. Define `RenderContext` struct with Camera, Zoom, MouseWorldPos, VisibleLayersMask
5. Define `IVisualizerAdapter` interface with GetPosition, Render, GetHitRadius, GetHoverLabel
6. Define `IMapLayer` interface with Name, LayerBitIndex, Update, Draw, HandleInput
7. Define `IMapTool` interface with tool lifecycle and input methods

**Success Conditions**:
- Project builds without errors
- All core interfaces defined with XML documentation
- RenderContext is a struct (value type)
- Interfaces use ISimulationView (not EntityRepository) for abstraction

**Unit Tests**:
- `RenderContext_IsValueType()` - Verify struct

---

### MAP-P3-002: MapCamera Implementation

**Description**:
Implement camera control with pan, zoom-to-cursor, and coordinate conversion.

**Dependencies**: MAP-P3-001

**Implementation Steps**:
1. Create `Components/MapCamera.cs` class
2. Wrap `Raylib_cs.Camera2D` as `InnerCamera` property
3. Implement zoom with mouse wheel using cursor-centered logic
4. Implement pan on configurable mouse button (default Right)
5. Add `ZoomSpeed`, `MinZoom`, `MaxZoom` properties
6. Check `InputFilter.IsMouseCaptured` before handling input
7. Implement `ScreenToWorld` and `WorldToScreen` using Raylib utilities
8. Implement `BeginMode()` and `EndMode()` for rendering scope

**Success Conditions**:
- Zoom centers on mouse cursor position
- Pan uses configured mouse button
- No pan/zoom when ImGui captures mouse
- Min/max zoom limits enforced
- Coordinate conversions accurate
- Camera state (target, zoom) persists across frames

**Unit Tests**:
- `MapCamera_ZoomIn_IncreasesZoom()` - Verify zoom
- `MapCamera_ZoomClamp_EnforcesLimits()` - Verify clamping
- `MapCamera_ScreenToWorld_RoundTrip()` - Verify conversion accuracy
- `MapCamera_ImGuiCapture_IgnoresInput()` - Verify input filter

---

### MAP-P3-003: MapDisplayComponent and Layer Infrastructure

**Description**:
Implement per-entity layer membership component and base layer structures.

**Dependencies**: MAP-P3-001

**Implementation Steps**:
1. Create `Components/MapDisplayComponent.cs` struct with LayerMask field
2. Add `Default` static property returning LayerMask = 1
3. Document bitmask usage with examples
4. Create `Layers/` folder for layer implementations

**Success Conditions**:
- `MapDisplayComponent` is unmanaged struct
- LayerMask is uint (32 bits)
- Default value places entity on Layer 0

**Unit Tests**:
- `MapDisplayComponent_Default_IsLayerZero()` - Verify default

---

### MAP-P3-004: MapCanvas Implementation

**Description**:
Implement the main map container orchestrating layers and tools.

**Dependencies**: MAP-P3-002, MAP-P3-003

**Implementation Steps**:
1. Create `MapCanvas.cs` class
2. Add `Camera` property of type `MapCamera`
3. Add `ActiveLayerMask` property (default 0xFFFFFFFF)
4. Add list of `IMapLayer` layers
5. Add `ActiveTool` property and tool management methods
6. Implement `AddLayer(IMapLayer)` method
7. Implement `Update(float)` calling camera and layer updates
8. Implement `Draw()` method:
   - Begin camera mode
   - Iterate layers, check visibility via bitmask
   - Call layer.Draw() for visible layers
   - Draw active tool overlay
   - End camera mode
9. Implement `HandleInput()` routing to tool first, then layers (reverse order)
10. Implement `SwitchTool()` and `ResetTool()` methods

**Success Conditions**:
- Layers rendered in order (0 → N)
- Input handled in reverse order (N → 0)
- Layer visibility controlled by ActiveLayerMask
- Tool gets input priority over layers
- Camera updates before rendering
- BeginMode/EndMode properly scoped

**Unit Tests**:
- `MapCanvas_AddLayer_IncreasesLayerCount()` - Verify layer addition
- `MapCanvas_LayerMask_FiltersVisibility()` - Verify masking
- `MapCanvas_SwitchTool_CallsOnEnterExit()` - Verify tool lifecycle

**Integration Tests**:
- Add 3 layers with different bit indices
- Set mask to 0b101, verify only layers 0 and 2 render

---

### MAP-P3-005: EntityRenderLayer Implementation

**Description**:
Create generic layer for rendering entities with layer mask filtering.

**Dependencies**: MAP-P3-004

**Implementation Steps**:
1. Create `Layers/EntityRenderLayer.cs` class implementing `IMapLayer`
2. Store: name, layerBitIndex, query, adapter, selectionContext
3. Implement `Update(float)` - empty for now
4. Implement `Draw(RenderContext)`:
   - Iterate query results
   - Check entity MapDisplayComponent.LayerMask overlap with ctx.VisibleLayersMask
   - Get position via adapter
   - Check if selected/hovered via selectionContext
   - Call adapter.Render() with context
5. Implement `HandleInput()`:
   - Iterate query
   - Hit test using adapter.GetHitRadius()
   - Find closest entity
   - Update selectionContext
   - Return true if entity hit

**Success Conditions**:
- Only renders entities whose LayerMask overlaps with VisibleLayersMask
- Selection state passed to adapter
- Hit testing finds closest entity within radius
- Input returns true only if entity hit (consumes input)

**Unit Tests**:
- `EntityRenderLayer_LayerMaskFilter_HidesNonMatching()` - Verify filtering
- `EntityRenderLayer_HitTest_FindsClosest()` - Verify hit testing

---

### MAP-P3-006: DelegateAdapter Implementation

**Description**:
Create quick-start adapter using lambda functions for position and rendering.

**Dependencies**: MAP-P3-001

**Implementation Steps**:
1. Create `Defaults/DelegateAdapter.cs` implementing `IVisualizerAdapter`
2. Store delegates: positionExtractor, drawFunc, hitRadius
3. If drawFunc is null, use `DefaultDraw` (simple circle)
4. Implement interface methods by invoking stored delegates

**Success Conditions**:
- Can construct with just position extractor lambda
- Optional custom draw function
- Default draw renders blue/green circle
- No allocations during rendering

**Unit Tests**:
- `DelegateAdapter_NullDrawFunc_UsesDefault()` - Verify default
- `DelegateAdapter_CustomDrawFunc_Invoked()` - Verify custom

---

### MAP-P3-007: DebugGizmos and DebugGizmoLayer

**Description**:
Implement immediate-mode debug drawing system with thread-safe queues.

**Dependencies**: MAP-P3-004

**Implementation Steps**:
1. Create `Debug/DebugGizmos.cs` class with ConcurrentQueue<DebugLine>, ConcurrentQueue<DebugCircle>
2. Add `DrawLine()`, `DrawCircle()` methods enqueueing primitives
3. Add `Clear()` method
4. Create `Layers/DebugGizmoLayer.cs` implementing `IMapLayer`
5. Store reference to World (to get DebugGizmos singleton)
6. Implement `Draw()`:
   - Get DebugGizmos singleton
   - Dequeue and render all lines
   - Dequeue and render all circles
   - Call Clear()
7. Set LayerBitIndex as constructor parameter

**Success Conditions**:
- Thread-safe enqueue from systems
- Gizmos rendered on top of everything
- Cleared each frame after rendering
- No exceptions with empty queues
- Lines respect world-space coordinates

**Unit Tests**:
- `DebugGizmos_DrawLine_Enqueues()` - Verify queueing
- `DebugGizmoLayer_Draw_ClearsAfterRender()` - Verify clear

---

## Phase 4: Integration (CarKinem Refactoring)

### MAP-P4-001: Add Framework References to CarKinem

**Description**:
Update CarKinem project file with new framework dependencies.

**Dependencies**: MAP-P1-005, MAP-P2-003, MAP-P3-007

**Implementation Steps**:
1. Open `Examples/Fdp.Examples.CarKinem/Fdp.Examples.CarKinem.csproj`
2. Add project reference to `FDP.Framework.Raylib`
3. Add project reference to `FDP.Toolkit.ImGui`
4. Add project reference to `FDP.Toolkit.Vis2D`
5. Verify project builds

**Success Conditions**:
- All framework assemblies referenced
- No circular dependencies
- Project compiles

**Unit Tests**: N/A (build verification)

---

### MAP-P4-002: Implement VehicleVisualizer Adapter

**Description**:
Create CarKinem-specific adapter for rendering vehicles.

**Dependencies**: MAP-P4-001

**Implementation Steps**:
1. Create `Visualization/VehicleVisualizer.cs` implementing `IVisualizerAdapter`
2. Implement `GetPosition()` reading `VehicleState.Position`
3. Implement `GetHitRadius()` returning `VehicleParams.Length / 2`
4. Implement `GetHoverLabel()` returning entity ID
5. Implement `Render()`:
   - Get VehicleState and VehicleParams
   - Calculate rotation from Forward vector
   - Draw rotated rectangle using vertex math
   - Color by vehicle class
   - Highlight if selected/hovered
   - Draw selection ring if selected
6. Create `GetColorForClass()` helper method

**Success Conditions**:
- Vehicles render as rotated rectangles
- Size based on VehicleParams
- Color coded by class (Red=PersonalCar, Blue=Truck, DarkGray=Tank)
- Selection ring drawn when selected
- Front edge thicker (visual indicator)

**Unit Tests**:
- `VehicleVisualizer_GetPosition_ReturnsVehicleStatePosition()` - Verify position
- `VehicleVisualizer_GetColorForClass_ReturnsCorrectColor()` - Verify coloring

---

### MAP-P4-003: Create CarKinemApp Class

**Description**:
Implement main application class inheriting from FdpApplication.

**Dependencies**: MAP-P4-002

**Implementation Steps**:
1. Create `CarKinemApp.cs` inheriting `FdpApplication`
2. Add constructor passing `ApplicationConfig` with title "Car Kinematics Demo"
3. Declare fields: _map (MapCanvas), _inspector (EntityInspectorPanel), _selectionContext (InspectorState)
4. Implement `OnLoad()`:
   - Create EntityRepository
   - Create ModuleHostKernel
   - Register components (VehicleState, VehicleParams, NavState)
   - Register systems (SpatialHashSystem, CarKinematicsSystem)
   - Initialize kernel
   - Create MapCanvas
   - Add vehicle layer to map
   - Spawn test vehicle
5. Implement `OnUpdate(float)`:
   - Call _map.Update(dt)
   - Check InputFilter before handling hotkeys
   - Call base.OnUpdate(dt)
6. Implement `OnDrawWorld()`:
   - Call _map.Render()
7. Implement `OnDrawUI()`:
   - Call _inspector.Draw()
   - Draw spawn panel
8. Implement `OnUnload()`:
   - Dispose systems
   - Call base.OnUnload()

**Success Conditions**:
- Application starts without errors
- Window displays at 1600×900
- Map renders with test vehicle
- Inspector shows entity list
- Selection works between map and inspector
- No memory leaks on shutdown

**Integration Tests**:
- Launch app, verify window opens
- Click entity on map, verify inspector shows selection
- Click entity in inspector, verify map highlights it

---

### MAP-P4-004: Simplify Program.cs

**Description**:
Replace boilerplate Program.cs with minimal entry point.

**Dependencies**: MAP-P4-003

**Implementation Steps**:
1. Open `Examples/Fdp.Examples.CarKinem/Program.cs`
2. Delete all existing code
3. Add minimal Main method:
   ```csharp
   static void Main()
   {
       using var app = new CarKinemApp();
       app.Run();
   }
   ```

**Success Conditions**:
- Program.cs is 5-10 lines total
- Application launches correctly
- No functionality lost compared to original

**Unit Tests**: N/A (integration test in MAP-P4-003)

---

### MAP-P4-005: Delete Obsolete Files

**Description**:
Remove old custom UI code replaced by frameworks.

**Dependencies**: MAP-P4-003

**Implementation Steps**:
1. Delete `Rendering/VehicleRenderer.cs`
2. Delete `Rendering/RoadRenderer.cs`
3. Delete `Rendering/DebugLabelRenderer.cs`
4. Delete `Input/InputManager.cs`
5. Delete `Input/SelectionManager.cs`
6. Delete `UI/EntityInspector.cs`
7. Delete `UI/EventInspector.cs` (if exists)
8. Delete `Simulation/DemoSimulation.cs`
9. Verify project still builds
10. Verify application runs correctly

**Success Conditions**:
- All listed files deleted
- No broken references
- Application functionality unchanged
- Code size reduced significantly

**Unit Tests**: N/A (integration verification)

---

## Phase 5: Map Interaction Tools

### MAP-P5-001: IMapTool Interface

**Description**:
Define core abstraction for interaction modes.

**Dependencies**: MAP-P3-001

**Implementation Steps**:
1. Create `Abstractions/IMapTool.cs` interface in Vis2D project
2. Define lifecycle methods: OnEnter, OnExit
3. Define execution methods: Update, Draw
4. Define input methods: HandleClick, HandleDrag, HandleHover
5. Add XML documentation explaining state pattern usage

**Success Conditions**:
- Interface defined with all required methods
- Draw receives RenderContext
- Input methods return bool (consume flag)
- Documented with examples

**Unit Tests**: N/A (interface only)

---

### MAP-P5-002: DefaultSelectionTool Implementation

**Description**:
Implement standard click-to-select tool.

**Dependencies**: MAP-P5-001

**Implementation Steps**:
1. Create `Tools/DefaultSelectionTool.cs` implementing `IMapTool`
2. Store reference to IInspectorContext
3. Implement `HandleClick()`:
   - Propagate to layers via MapCanvas (tool returns false to allow layer handling)
4. Implement `HandleDrag()`:
   - Detect drag start (distance threshold)
   - Fire `OnEntityDragStart` event
5. Add `event Action<Entity> OnEntityDragStart`
6. Implement stub Update and Draw methods

**Success Conditions**:
- Click selects entity via layer hit testing
- Click empty space deselects
- Drag detection triggers event
- No drawing (pure input handler)

**Unit Tests**:
- `DefaultSelectionTool_ClickEntity_Selects()` - Verify selection
- `DefaultSelectionTool_DragThreshold_FiresEvent()` - Verify drag detection

---

### MAP-P5-003: EntityDragTool Implementation

**Description**:
Implement tool for dragging entities to new positions.

**Dependencies**: MAP-P5-001

**Implementation Steps**:
1. Create `Tools/EntityDragTool.cs` implementing `IMapTool`
2. Store: target entity, onMove callback, onComplete callback
3. Implement `HandleDrag()`:
   - Invoke onMove with entity and current world position
   - Return true (consume input)
4. Implement `HandleClick()`:
   - Check for mouse release
   - Invoke onComplete callback
   - Return true
5. Implement `Draw()`:
   - Draw target reticle at cursor (circle)
   - Scale by zoom for consistent screen size

**Success Conditions**:
- Entity position updates during drag via callback
- Tool returns to default on mouse release
- Visual feedback (reticle) drawn
- Smooth movement (no jitter)

**Unit Tests**:
- `EntityDragTool_Drag_InvokesCallback()` - Verify callback
- `EntityDragTool_Release_CompletesAndResets()` - Verify completion

---

### MAP-P5-004: PointSequenceTool Implementation

**Description**:
Implement tool for defining paths by clicking points.

**Dependencies**: MAP-P5-001

**Implementation Steps**:
1. Create `Tools/PointSequenceTool.cs` implementing `IMapTool`
2. Store: list of Vector2 points, onFinish callback
3. Implement `OnEnter()` clearing point list
4. Implement `HandleClick()`:
   - Left click: add point to list
   - Right click: invoke onFinish with array, return true
5. Implement `Draw()`:
   - Draw lines between existing points
   - Draw circles at each point
   - Draw elastic line from last point to cursor
   - Use preview color (Yellow)

**Success Conditions**:
- Left click adds points
- Right click finishes and invokes callback
- Preview line shows next segment
- Points drawn as circles
- Elastic line follows cursor

**Unit Tests**:
- `PointSequenceTool_LeftClick_AddsPoint()` - Verify point addition
- `PointSequenceTool_RightClick_FinishesPath()` - Verify completion

---

### MAP-P5-005: Integrate Tools in CarKinem

**Description**:
Add tool usage to CarKinem demo with UI controls.

**Dependencies**: MAP-P4-003, MAP-P5-004

**Implementation Steps**:
1. In `CarKinemApp.OnLoad()`, create DefaultSelectionTool
2. Hook OnEntityDragStart event to switch to EntityDragTool
3. In EntityDragTool onMove callback, update VehicleState.Position
4. In EntityDragTool onComplete callback, reset to default tool
5. In UI panel, add "Draw Path" button
6. Button click switches to PointSequenceTool
7. PointSequenceTool onFinish creates trajectory and assigns to selected entity
8. Test drag and path creation

**Success Conditions**:
- Can drag vehicles by clicking and dragging
- "Draw Path" button appears in UI
- Clicking button enters path draw mode
- Left click adds waypoints
- Right click finishes path
- Path assigned to selected vehicle

**Integration Tests**:
- Select vehicle, click "Draw Path", draw path, verify vehicle follows it

---

### MAP-P5-006: TrajectoryEditTool Implementation

**Description**:
Implement tool for editing existing trajectories by dragging control points (waypoints).

**Dependencies**: MAP-P5-001, MAP-P4-003

**Implementation Steps**:
1. In `CarKinemApp.OnLoad()`, create DefaultSelectionTool
2. Hook OnEntityDragStart event to switch to EntityDragTool
3. In EntityDragTool onMove callback, update VehicleState.Position
4. In EntityDragTool onComplete callback, reset to default tool
5. In UI panel, add "Draw Path" button
6. Button click switches to PointSequenceTool
7. PointSequenceTool onFinish creates trajectory and assigns to selected entity
8. Test drag and path creation

**Success Conditions**:
- Can drag vehicles by clicking and dragging
- "Draw Path" button appears in UI
- Clicking button enters path draw mode
- Left click adds waypoints
- Right click finishes path
- Path assigned to selected vehicle

**Integration Tests**:
- Select vehicle, click "Draw Path", draw path, verify vehicle follows it

---

### MAP-P5-006: TrajectoryEditTool Implementation

**Description**:
Implement tool for editing existing trajectories by dragging control points (waypoints).

**Dependencies**: MAP-P5-001, MAP-P4-003

**Implementation Steps**:
1. Create `Tools/TrajectoryEditTool.cs` (app-specific, in CarKinem project) implementing `IMapTool`
2. Store reference to `CustomTrajectory` (from TrajectoryPoolManager)
3. Implement `Draw(RenderContext)`:
   - Iterate `trajectory.Waypoints` NativeArray
   - Draw circle handles at each waypoint position
   - Highlight hovered handle (color change when mouse near)
   - Draw spline preview connecting waypoints (Hermite or line segments)
4. Implement `HandleClick(Vector2 worldPos, MouseButton button)`:
   - Test distance to each waypoint
   - If within hit radius, set `_draggedWaypointIndex`
   - Return true if handle clicked
5. Implement `HandleDrag(Vector2 worldPos, Vector2 delta)`:
   - If `_draggedWaypointIndex != -1`:
     - Directly modify `trajectory.Waypoints[_draggedWaypointIndex].Position = worldPos`
     - Return true (consume drag)
6. Implement `HandleClick` on mouse release:
   - Set `_draggedWaypointIndex = -1`
   - Call `trajectory.RecalculateArcLengths()` (recompute spline metadata)
7. Add `OnEnter()`: Store original trajectory state for potential undo
8. Add `OnExit()`: Commit changes to trajectory pool

**Success Conditions**:
- Waypoints render as visible circles
- Can click and drag individual waypoints
- Waypoint position updates in real-time during drag
- Spline preview updates immediately
- Arc-length metadata recalculated on mouse release
- Zero allocations during drag (modifies NativeArray in-place)
- Tool exits cleanly, returning to default selection tool

**Unit Tests**:
- `TrajectoryEditTool_DragWaypoint_UpdatesPosition()` - Verify position update
- `TrajectoryEditTool_MouseRelease_RecalculatesArcLength()` - Verify recalc trigger

**Integration Tests**:
- Create trajectory with 5 waypoints
- Enter edit mode, drag middle waypoint 10 meters
- Verify vehicle following trajectory adjusts path
- Exit edit mode, verify changes persist

---

## Phase 6: Aggregation & Decluttering

### MAP-P6-001: Hierarchy Data Components

**Description**:
Define ECS components for hierarchy and aggregate state.

**Dependencies**: MAP-P3-001

**Implementation Steps**:
1. Create `Components/HierarchyNode.cs` struct
2. Add fields: Parent, FirstChild, NextSibling (all Entity)
3. Create `Components/AggregateState.cs` struct
4. Add fields: Centroid (Vector2), BoundsMin/Max (Vector2), ActiveChildCount (int)
5. Add computed properties: IsValid, Size
6. Create `Components/AggregateRoot.cs` tag struct

**Success Conditions**:
- All components are unmanaged structs
- HierarchyNode forms linked-list structure
- AggregateState fully describes spatial bounds
- Components registered with FDP

**Unit Tests**:
- `AggregateState_IsValid_ReturnsTrueWhenChildrenExist()` - Verify validation

---

### MAP-P6-002: IHierarchyAdapter Interface

**Description**:
Define abstraction for hierarchy traversal with zero-alloc iterator.

**Dependencies**: MAP-P6-001

**Implementation Steps**:
1. Create `Abstractions/IHierarchyAdapter.cs` interface
2. Define GetParent method returning Entity
3. Define IsSpatialLeaf method returning bool
4. Define GetChildren method returning ChildEnumerator
5. Create `ChildEnumerator` ref struct
6. Implement MoveNext, Current, GetEnumerator methods
7. Use HierarchyNode.NextSibling for iteration

**Success Conditions**:
- IHierarchyAdapter interface defined
- ChildEnumerator is ref struct (no allocations)
- ChildEnumerator supports foreach syntax
- Traverses sibling linked list

**Unit Tests**:
- `ChildEnumerator_Foreach_IteratesChildren()` - Verify iteration
- `ChildEnumerator_IsRefStruct()` - Verify type

---

### MAP-P6-003: HierarchyOrderSystem Implementation

**Description**:
Implement system to maintain bottom-up sorted entity list.

**Dependencies**: MAP-P6-002

**Implementation Steps**:
1. Create `Systems/HierarchyOrderSystem.cs` extending ComponentSystem
2. Define `SortedHierarchyData` singleton struct with NativeArray<Entity>
3. Add _isDirty flag and _buffer NativeArray
4. Implement `MarkDirty()` method
5. Implement `OnUpdate()`:
   - Early return if !_isDirty
   - Find all root entities
   - Call `ProcessNode()` recursively (post-order)
   - Publish SortedHierarchyData singleton
   - Set _isDirty = false
6. Implement `ProcessNode(Entity entity, HashSet<Entity> visited)`:
   - **SAFETY**: Check if `visited.Contains(entity)` - if true, log error and return (cycle detected)
   - Add entity to visited set
   - Recurse to children first
   - Append self to buffer
   - Remove entity from visited set (for correct sibling handling)
7. Implement OnCreate and OnDestroy for buffer management
8. Add cycle detection error logging: "Cycle detected in hierarchy at entity {entity.Index}"

**Success Conditions**:
- Outputs flat array in bottom-up order
- Only runs when _isDirty is true
- Buffer resizes if needed
- Post-order traversal correct
- **Does not crash or hang on cyclic hierarchies**
- Logs clear error when cycle detected

**Unit Tests**:
- `HierarchyOrderSystem_Sorting_ChildrenBeforeParents()` - Verify order
- `HierarchyOrderSystem_NotDirty_SkipsUpdate()` - Verify dirty flag

---

### MAP-P6-004: AggregateUpdateSystem Implementation

**Description**:
Implement system to calculate aggregate positions and bounds.

**Dependencies**: MAP-P6-003

**Implementation Steps**:
1. Create `Systems/AggregateUpdateSystem.cs` extending ComponentSystem
2. Store IHierarchyAdapter and IVisualizerAdapter
3. Implement `OnUpdate()`:
   - Get SortedHierarchyData singleton
   - Iterate entity array linearly
   - For each entity, check if leaf or aggregate
   - If leaf: set AggregateState from position
   - If aggregate: call `CalculateFromChildren()`
4. Implement `CalculateFromChildren()`:
   - Iterate children using adapter.GetChildren()
   - Read child AggregateState components
   - Accumulate: sum positions, min/max bounds, count
   - Calculate centroid = sum / count
   - Set parent AggregateState component

**Success Conditions**:
- Runs in O(N) time
- No recursion
- Zero allocations (ref struct iterators)
- Correct centroid calculation
- Correct AABB calculation

**Unit Tests**:
- `AggregateUpdateSystem_Centroid_IsAverage()` - Verify centroid math
- `AggregateUpdateSystem_Bounds_EncompassesChildren()` - Verify AABB

---

### MAP-P6-005: HierarchicalRenderLayer Implementation

**Description**:
Implement layer with automatic semantic zoom and decluttering.

**Dependencies**: MAP-P6-004

**Implementation Steps**:
1. Create `Layers/HierarchicalRenderLayer.cs` implementing `IMapLayer`
2. Store: view, hierarchy adapter, visualizer adapter, root query
3. Add config: SymbolPixelSize (32), ClutterThreshold (1.2)
4. Implement `Draw()`:
   - Iterate root query
   - Call `DrawRecursive()` for each root
5. Implement `DrawRecursive(Entity, RenderContext)`:
   - Get position (leaf: from adapter, aggregate: from AggregateState)
   - Call `ShouldExpand()`
   - If expand: draw bounds, recurse to children
   - If collapse: render entity symbol, draw subordinate preview
6. Implement `ShouldExpand()`:
   - Return false if leaf
   - Project bounds to screen space
   - Compare width to SymbolPixelSize × ClutterThreshold
7. Implement `DrawAggregateBounds()` - rectangle outline
8. Implement `DrawSubordinatePreview()` - lines and dots

**Success Conditions**:
- Automatically expands when zoomed in
- Automatically collapses when zoomed out
- Bounding boxes drawn for aggregates
- Subordinate preview visible when collapsed
- No flickering during zoom transitions

**Unit Tests**:
- `HierarchicalRenderLayer_ShouldExpandLogic_WorksCorrectly()` - Verify expansion logic

**Integration Tests**:
- Create 3-level hierarchy (Company → Platoon → Tanks)
- Zoom out, verify company symbol shown
- Zoom in, verify individual tanks shown

---

### MAP-P6-006: Integrate Hierarchy in CarKinem

**Description**:
Add ORBAT support to CarKinem demo with formation hierarchy.

**Dependencies**: MAP-P4-003, MAP-P6-005

**Implementation Steps**:
1. Create `OrbatAdapter.cs` in CarKinem implementing `IHierarchyAdapter`
2. Implement IsSpatialLeaf: return true if has VehicleState
3. Implement GetChildren: use HierarchyNode or FormationRoster
4. Implement GetParent: use HierarchyNode
5. In CarKinemApp.OnLoad():
   - Register HierarchyNode, AggregateState, AggregateRoot components
   - Create and register HierarchyOrderSystem
   - Create and register AggregateUpdateSystem with OrbatAdapter
6. Replace EntityRenderLayer with HierarchicalRenderLayer
7. Update VehicleVisualizer to handle FormationRoster entities (draw aggregate symbol)
8. Add UI to create test formations

**Success Conditions**:
- Can create multi-level formations
- Formations render as symbols when zoomed out
- Individual vehicles visible when zoomed in
- Bounding boxes show formation extent
- Selection works at any hierarchy level

**Integration Tests**:
- Create formation with 12 tanks (3 platoons × 4 tanks)
- Verify platoon symbols at medium zoom
- Verify individual tanks at close zoom
- Verify company symbol at far zoom

---

## Performance Validation Tasks

### MAP-PERF-001: Aggregate Update Performance Test

**Description**:
Verify AggregateUpdateSystem meets performance targets.

**Dependencies**: MAP-P6-004

**Implementation Steps**:
1. Create benchmark project
2. Create hierarchy with 1000 entities (10 levels deep)
3. Measure AggregateUpdateSystem execution time
4. Run with dirty flag false (should be ~0ms)
5. Run with dirty flag true first time (measure sort time)
6. Run subsequent frames (should be <1ms for 1000 entities)

**Success Conditions**:
- Non-dirty update: < 0.1ms
- Update with 1000 entities: < 1ms
- No GC allocations during update

---

### MAP-PERF-002: Rendering Performance Test

**Description**:
Verify rendering performance with many entities across layers.

**Dependencies**: MAP-P3-005

**Implementation Steps**:
1. Create test scene with 10,000 entities
2. Distribute across 4 layers
3. Test with all layers visible
4. Measure frame time and FPS
5. Verify no GC allocations per frame

**Success Conditions**:
- 10,000 entities: > 60 FPS
- No per-frame allocations
- Layer masking has negligible cost

---

## Documentation Tasks

### MAP-DOC-001: API Documentation

**Description**:
Add XML documentation to all public APIs.

**Dependencies**: All implementation tasks

**Implementation Steps**:
1. Review all public classes, interfaces, methods
2. Add `<summary>` tags
3. Add `<param>` tags for all parameters
4. Add `<returns>` tags for return values
5. Add `<example>` tags for key classes

**Success Conditions**:
- Every public member documented
- Examples provided for main entry points
- Documentation builds without warnings

---

### MAP-DOC-002: Usage Examples

**Description**:
Create standalone example snippets for common scenarios.

**Dependencies**: MAP-P4-005, MAP-P5-005, MAP-P6-006

**Implementation Steps**:
1. Create Examples folder in each toolkit
2. Write minimal example for EntityInspectorPanel
3. Write minimal example for MapCanvas with custom adapter
4. Write minimal example for custom tool implementation
5. Write minimal example for hierarchy setup

**Success Conditions**:
- Each example is self-contained
- Examples compile and run
- Cover 80% of common use cases

---

## End of Task Details Document
