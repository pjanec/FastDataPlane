# MAP-DESIGN.md
## FDP UI Reusable Toolkits - Design Document

**Project**: FDP Framework Enhancement  
**Purpose**: Extract reusable UI and visualization toolkits from Fdp.Examples.CarKinem  
**Version**: 1.0  
**Date**: 2026-02-11

---

## 1. Overview

### 1.1 Objective
Transform the monolithic UI code in `Fdp.Examples.CarKinem` into a set of reusable, framework-level toolkits that can be used across any FDP-based application. This includes:
- Generic debugging and inspection tools (ImGui-based)
- Abstract 2D visualization and interaction framework
- Application host framework eliminating boilerplate

### 1.2 Design Principles
- **Separation of Concerns**: Data inspection separate from rendering
- **Abstraction**: Generic interfaces using adapters to avoid coupling
- **Flexibility**: "Pick from defaults and override/customize if you need more"
- **Performance**: Zero-allocation where possible, cache-friendly data structures
- **Composability**: Reusable components that work together but can be used independently

### 1.3 Target Architecture

```
/Framework
  /FDP.Toolkit.ImGui          ← Generic Debug Panels (Renderer-agnostic)
  /FDP.Toolkit.Vis2D          ← 2D Map, Camera, Interaction (Abstract)
  /FDP.Framework.Raylib       ← Windowing, Loop, Input wrapping
/Examples
  /Fdp.Examples.CarKinem      ← Refactored to use above
```

---

## 2. Phase 1: FDP.Toolkit.ImGui (The Inspectors)

### 2.1 Purpose
Provide renderer-agnostic debugging and inspection tools for FDP entities, events, and systems.

### 2.2 Dependencies
- `Fdp.Kernel`
- `ModuleHost.Core`
- `ImGui.NET`

### 2.3 Components

#### 2.3.1 IInspectorContext
**Purpose**: Decouple UI selection state from game logic.

```csharp
public interface IInspectorContext
{
    Entity? SelectedEntity { get; set; }
    Entity? HoveredEntity { get; set; }
}
```

**Default Implementation**: `InspectorState` class

#### 2.3.2 EntityInspectorPanel
**Features**:
- Entity list with search/filter
- Component details with reflection-based rendering
- Two-column layout (list | details)
- Scrollable regions using `ImGui.BeginChild`

**Key Methods**:
- `Draw(EntityRepository repo, IInspectorContext context)` - Main rendering
- `DrawEntityList()` - Left panel with entity selection
- `DrawEntityDetails()` - Right panel showing components

#### 2.3.3 ComponentReflector
**Purpose**: Generic component rendering using cached reflection.

**Features**:
- Field cache to avoid repeated reflection
- Support for Vector2, Vector3, and custom types
- Table-based property display
- **Editable fields** using ImGui input widgets (InputFloat, InputInt, InputFloat2, etc.)
- **Write-back support** to ECS with proper versioning via `repo.SetComponent<T>()`

**Methods**:
- `DrawComponents(EntityRepository repo, Entity e)` - Iterate all components
- `DrawObjectProperties(object obj, Entity entity, EntityRepository repo)` - Render fields as editable ImGui widgets
- `WriteComponentBack<T>(EntityRepository repo, Entity entity, T component)` - Helper to trigger ECS versioning

#### 2.3.4 SystemProfilerPanel
**Purpose**: Display ModuleHost execution statistics.

**Features**:
- Table view of all registered modules
- Execution count, circuit breaker status, failure count
- Color-coded status indicators
- Sortable columns

#### 2.3.5 EventBrowserPanel
**Purpose**: Capture and display event history.

**Features**:
- Persistent event log with configurable capacity
- Pause/Resume capture
- Frame-stamped events
- Reverse chronological display

**Methods**:
- `Update(FdpEventBus bus, uint currentFrame)` - Capture events
- `Draw()` - Render event log

---

## 3. Phase 2: FDP.Framework.Raylib (The App Host)

### 3.1 Purpose
Eliminate boilerplate code for windowing, rendering loop, and ImGui setup.

### 3.2 Dependencies
- `Fdp.Kernel`
- `ModuleHost.Core`
- `Raylib-cs`
- `rlImGui-cs`

### 3.3 Components

#### 3.3.1 ApplicationConfig
**Purpose**: Configuration struct for window setup.

**Properties**:
- `WindowTitle`, `Width`, `Height`
- `TargetFPS`
- `ConfigFlags` (Raylib window flags)
- `PersistenceEnabled` (save/restore window state)

#### 3.3.2 FdpApplication (Abstract Base Class)
**Purpose**: Standardized application lifecycle.

**Lifecycle Methods** (Override in derived classes):
- `OnLoad()` - Initialize World, Kernel, Modules
- `OnUpdate(float dt)` - Logic update (default: calls Kernel.Update())
- `OnDrawWorld()` - Render game world (Raylib context)
- `OnDrawUI()` - Render ImGui panels (ImGui context)
- `OnUnload()` - Cleanup resources

**Main Loop Structure**:
```
InitializeWindow()
OnLoad()
while (!WindowShouldClose):
    OnUpdate(dt)
    BeginDrawing()
        OnDrawWorld()
        rlImGui.Begin()
        OnDrawUI()
        rlImGui.End()
    EndDrawing()
OnUnload()
ShutdownWindow()
```

#### 3.3.3 InputFilter
**Purpose**: Prevent input bleed-through between UI and game world.

**Methods**:
- `IsMouseCaptured` - Returns true if ImGui wants mouse
- `IsKeyboardCaptured` - Returns true if ImGui wants keyboard

---

## 4. Phase 3: FDP.Toolkit.Vis2D (The Map)

### 4.1 Purpose
Abstract 2D visualization system that is data-agnostic and highly flexible.

### 4.2 Dependencies
- `Fdp.Kernel`
- `ModuleHost.Core`
- `FDP.Framework.Raylib`
- `FDP.Toolkit.ImGui` (for IInspectorContext)

### 4.3 Core Abstractions

#### 4.3.1 IVisualizerAdapter (The Bridge Pattern)
**Purpose**: Decouple map rendering from specific component types.

**Methods**:
```csharp
Vector2? GetPosition(ISimulationView view, Entity entity)
void Render(ISimulationView view, Entity entity, Vector2 position, RenderContext ctx)
float GetHitRadius(ISimulationView view, Entity entity)
string? GetHoverLabel(ISimulationView view, Entity entity)
```

**RenderContext Structure**:
- `Camera2D Camera` - Current camera state
- `float ZoomLevel` - Current zoom
- `Vector2 MouseWorldPos` - Mouse position in world space
- `uint VisibleLayersMask` - Active layer flags
- `bool IsSelected`, `bool IsHovered` - Entity state

#### 4.3.2 MapCamera
**Purpose**: Abstract camera control (pan, zoom).

**Features**:
- Zoom to cursor (wheel input)
- Pan with configurable mouse button
- Screen ↔ World coordinate conversion
- Respects InputFilter (no pan during UI interaction)

**Methods**:
- `Update(float dt)` - Handle input
- `BeginMode()`, `EndMode()` - Raylib camera scope
- `ScreenToWorld(Vector2)`, `WorldToScreen(Vector2)` - Conversions

#### 4.3.3 MapCanvas
**Purpose**: Main rendering container orchestrating layers and tools.

**Properties**:
- `MapCamera Camera` - Camera instance
- `uint ActiveLayerMask` - Visibility bitmask (32 layers)
- `IMapTool ActiveTool` - Current interaction mode

**Methods**:
- `AddLayer(IMapLayer layer)` - Register rendering layer
- `Update(float dt)` - Update camera and layers
- `Draw()` - Render all visible layers
- `HandleInput()` - Route input to tools and layers
- `SwitchTool(IMapTool)`, `ResetTool()` - Tool management

### 4.4 Layer System

#### 4.4.1 IMapLayer Interface
**Purpose**: Self-contained rendering unit with input handling.

**Methods**:
```csharp
string Name { get; }
int LayerBitIndex { get; } // -1 for "always on" layers
void Update(float dt)
void Draw(RenderContext ctx)
bool HandleInput(Vector2 worldPos, MouseButton button, bool isPressed)
```

**Rendering Order**: Layer 0 → Layer N (background to foreground)  
**Input Order**: Layer N → Layer 0 (top layer gets first priority)

#### 4.4.2 EntityRenderLayer
**Purpose**: Generic layer for rendering entities matching a query.

**Features**:
- Filters entities by `MapDisplayComponent.LayerMask`
- Supports entity overlap with multiple layers
- Handles selection via IInspectorContext
- Hit testing for mouse interaction

**Constructor Parameters**:
- `string name` - Display name
- `int layerBitIndex` - Which bit in mask
- `EntityQuery query` - What entities to render
- `IVisualizerAdapter adapter` - How to render them

#### 4.4.3 MapDisplayComponent
**Purpose**: Per-entity layer membership (ECS component).

```csharp
public struct MapDisplayComponent
{
    public uint LayerMask; // Bitmask of which layers to show on
}
```

**Example**: `LayerMask = 0b0011` means entity appears on Layers 0 and 1.

#### 4.4.4 DebugGizmoLayer
**Purpose**: Immediate-mode debug drawing (lines, circles, etc.).

**Features**:
- Thread-safe concurrent queues for debug primitives
- Cleared each frame
- Overlay rendering (drawn on top)

**Usage in Systems**:
```csharp
var gizmos = World.GetSingletonManaged<DebugGizmos>();
gizmos.DrawLine(start, end, Color.Red);
```

### 4.5 Default Implementations

#### 4.5.1 DelegateAdapter
**Purpose**: Quick adapter using lambda functions.

**Use Case**: Rapid prototyping without creating custom adapter classes.

**Constructor**:
```csharp
DelegateAdapter(
    Func<ISimulationView, Entity, Vector2?> positionExtractor,
    Action<...>? customDraw = null,
    float hitRadius = 2.0f)
```

---

## 5. Phase 4: Integration (Refactoring CarKinem)

### 5.1 Files to Delete
- `Rendering/VehicleRenderer.cs` → Replaced by `VehicleVisualizer` adapter
- `Rendering/RoadRenderer.cs` → Replaced by `RoadLayer`
- `Input/InputManager.cs` → Replaced by `MapCamera`
- `Input/SelectionManager.cs` → Replaced by `InspectorState`
- `UI/EntityInspector.cs` → Replaced by `EntityInspectorPanel`
- `Simulation/DemoSimulation.cs` → Logic moved to `CarKinemApp`

### 5.2 New Structure

#### 5.2.1 CarKinemApp.cs
**Inherits**: `FdpApplication`

**Responsibilities**:
- Setup World, Kernel, Modules in `OnLoad()`
- Create `MapCanvas` with `VehicleVisualizer`
- Configure layers (Roads, Vehicles, Debug)
- Render map in `OnDrawWorld()`
- Render inspectors in `OnDrawUI()`

#### 5.2.2 VehicleVisualizer.cs
**Implements**: `IVisualizerAdapter`

**Responsibilities**:
- Extract position from `VehicleState`
- Draw rotated rectangles for vehicles
- Color-code by vehicle class
- Draw selection rings

#### 5.2.3 Program.cs
**Simplified**:
```csharp
static void Main()
{
    using var app = new CarKinemApp();
    app.Run();
}
```

---

## 6. Phase 5: Map Interaction Tools

### 6.1 Purpose
Support different interaction modes (selection, drag, path editing) using the State Pattern.

### 6.2 Core Abstraction

#### 6.2.1 IMapTool Interface
**Purpose**: Define a specific input mode.

**Methods**:
```csharp
string Name { get; }
void OnEnter(MapCanvas canvas)
void OnExit()
void Update(float dt)
void Draw(RenderContext ctx)
bool HandleClick(Vector2 worldPos, MouseButton button)
bool HandleDrag(Vector2 worldPos, Vector2 delta)
bool HandleHover(Vector2 worldPos)
```

**Input Priority**: Tool → Layers (tool can consume input)

### 6.3 Tool Implementations

#### 6.3.1 DefaultSelectionTool
**Purpose**: Standard click-to-select mode.

**Features**:
- Click entity to select
- Click empty space to deselect
- Hover highlighting
- Optional drag-to-move detection

#### 6.3.2 EntityDragTool
**Purpose**: Move an entity by dragging.

**Constructor Parameters**:
- `Entity target` - Entity being moved
- `Action<Entity, Vector2> onMove` - Callback to update position
- `Action onComplete` - Callback when released

**Behavior**:
- Updates entity position via callback during drag
- Draws target reticle at cursor
- Auto-reverts to default tool on mouse up

#### 6.3.3 PointSequenceTool
**Purpose**: Define a path by clicking points.

**Constructor Parameters**:
- `Action<Vector2[]> onFinish` - Callback with completed path

**Behavior**:
- Left click adds point
- Right click finishes and invokes callback
- Draws preview line from last point to cursor
- Draws circles at existing points

#### 6.3.4 TrajectoryEditTool (App-Specific Example)
**Purpose**: Edit trajectory waypoints with drag handles.

**Features**:
- Visualizes waypoints as circles
- Click-and-drag to reposition waypoints
- Direct modification of `NativeArray<TrajectoryWaypoint>` (zero-copy)
- Recomputes arc lengths on completion
- Renders spline preview in real-time

**Implementation Details**:
- Stored in CarKinem project (app-specific tool)
- Uses hit testing to select waypoint handles
- Modifies trajectory data in-place (zero allocations)
- Triggers arc-length recalculation on mouse release
- Provides visual feedback with handle highlighting

**Usage**: User selects vehicle with trajectory, clicks "Edit Path" button, enters edit mode with draggable waypoint handles.

---

## 7. Phase 6: Aggregation & Decluttering

### 7.1 Purpose
Support hierarchical organization (ORBAT) with semantic zoom and automatic decluttering.

### 7.2 Core Concepts

#### 7.2.1 Hierarchical Rendering
**Goal**: Render either detail (individual units) or aggregate (platoon symbols) based on zoom level.

**Decision Logic**:
- If screen-space size < threshold → Show aggregate symbol
- If screen-space size > threshold → Expand and show children

#### 7.2.2 Aggregate Positioning
**Rule**: Aggregate position = centroid of subordinates.

**Example**: Platoon position = average of its 4 tanks.

### 7.3 Data Structures

#### 7.3.1 AggregateState Component
```csharp
public struct AggregateState
{
    public Vector2 Centroid;       // Average position
    public Vector2 BoundsMin;      // AABB min
    public Vector2 BoundsMax;      // AABB max
    public int ActiveChildCount;   // Number of children
}
```

**Attached To**: Logical parent entities (Platoons, Companies).

#### 7.3.2 HierarchyNode Component
```csharp
public struct HierarchyNode
{
    public Entity Parent;
    public Entity FirstChild;
    public Entity NextSibling;
}
```

**Purpose**: Linked-list structure for parent-child relationships.

#### 7.3.3 IHierarchyAdapter Interface
**Purpose**: Abstract hierarchy traversal.

**Methods**:
```csharp
Entity GetParent(ISimulationView view, Entity entity)
ChildEnumerator GetChildren(ISimulationView view, Entity entity)
bool IsSpatialLeaf(ISimulationView view, Entity entity)
```

**Zero-Alloc**: `ChildEnumerator` is a `ref struct` for allocation-free iteration.

### 7.4 System Architecture

#### 7.4.1 HierarchyOrderSystem
**Purpose**: Maintain topologically sorted entity list (bottom-up).

**Output**: `SortedHierarchyData` singleton containing `NativeArray<Entity>`.

**Performance**:
- **Dirty Flag**: Only re-sorts when hierarchy structure changes
- **Cost When Clean**: O(0) (early return)
- **Cost When Dirty**: O(N log N) sorting, but rare

**Algorithm**: Post-order tree traversal (children before parents).

**Safety**: Cycle detection using `HashSet<Entity>` during traversal. If a cycle is detected, logs error and aborts traversal to prevent infinite loops or stack overflow.

#### 7.4.2 AggregateUpdateSystem
**Purpose**: Calculate `AggregateState` for all aggregate entities.

**Input**: `SortedHierarchyData` from `HierarchyOrderSystem`.

**Algorithm**:
```
for entity in bottomUpList:
    if IsSpatialLeaf(entity):
        AggregateState = { Centroid = position, Bounds = point }
    else:
        sumPos = 0, boundsMin/Max = inf
        for child in GetChildren(entity):
            childState = GetComponent<AggregateState>(child)
            sumPos += childState.Centroid
            boundsMin = Min(boundsMin, childState.BoundsMin)
            boundsMax = Max(boundsMax, childState.BoundsMax)
        AggregateState = { Centroid = sumPos / count, Bounds = [min, max] }
```

**Performance**: O(N) linear iteration over flat array.

### 7.5 Rendering Layer

#### 7.5.1 HierarchicalRenderLayer
**Purpose**: Render hierarchy with automatic semantic zoom.

**Constructor Parameters**:
- `IHierarchyAdapter hierarchy` - How to traverse
- `IVisualizerAdapter visualizer` - How to draw
- `EntityQuery rootQuery` - Top-level entities (Brigades)

**Configuration**:
- `float SymbolPixelSize` - Expected screen size of icon
- `float ClutterThreshold` - Multiplier for expansion decision

**Rendering Logic** (Recursive per root):
```
function DrawRecursive(entity):
    position = GetPosition(entity)  // Either direct or from AggregateState
    
    if ShouldExpand(entity, position, bounds, context):
        // EXPANDED VIEW
        if not IsLeaf(entity):
            DrawAggregateBounds(bounds)  // Optional bounding box
        for child in GetChildren(entity):
            DrawRecursive(child)
    else:
        // COLLAPSED VIEW
        Render(entity, position)  // Draw aggregate symbol
        if not IsLeaf(entity):
            DrawSubordinatePreview(entity)  // Optional dots/lines
```

**ShouldExpand Logic**:
1. Leaves never expand (return false)
2. Calculate screen-space bounds width
3. If width < (SymbolPixelSize × ClutterThreshold) → Collapse
4. Otherwise → Expand

**Visual Features**:
- **Bounding Box**: Semi-transparent rectangle around expanded aggregate
- **Subordinate Preview**: When collapsed, draw lines/dots to show subordinate positions

### 7.6 Performance Optimizations

#### 7.6.1 Topology Dirty Flagging
**Trigger Points**:
- Entity parented/unparented
- Hierarchy component added/removed

**Implementation**: Call `HierarchyOrderSystem.MarkDirty()` from command processors.

#### 7.6.2 Zero-Allocation Iteration
**Techniques**:
- `ref struct ChildEnumerator` (no IEnumerable boxing)
- `NativeArray<Entity>` storage (no GC)
- Struct components (no object overhead)

**Result**: Zero GC allocations during hierarchy update and rendering.

#### 7.6.3 Cache Coherency
**Data Layout**: Flat array of entities in bottom-up order.

**Benefit**: Sequential memory access → CPU cache prefetching → Fast.

---

## 8. CarKinem Integration Examples

### 8.1 Layer Configuration
```csharp
// In CarKinemApp.OnLoad():

_map = new MapCanvas(Vector2.Zero);

// Layer 0: Background (Grid/Roads)
_map.AddLayer(new RoadLayer(_roadNetwork) { Name = "Roads", LayerBitIndex = 0 });

// Layer 1: Ground Units
var vehicleQuery = World.Query().With<VehicleState>().Build();
_map.AddLayer(new EntityRenderLayer("Vehicles", 1, World, vehicleQuery, 
    new VehicleVisualizer(), _selectionContext));

// Layer 2: Debug Gizmos
_map.AddLayer(new DebugGizmoLayer(World) { Name = "Debug", LayerBitIndex = 2 });
```

### 8.2 Interaction Tool Usage
```csharp
// In UI panel
if (ImGui.Button("Draw Path"))
{
    _map.SwitchTool(new PointSequenceTool(points => 
    {
        int trajId = _trajPool.RegisterTrajectory(points);
        // Assign to selected entity...
        _map.ResetTool();
    }));
}
```

### 8.3 ORBAT Adapter Implementation
```csharp
public class OrbatAdapter : IHierarchyAdapter
{
    public bool IsSpatialLeaf(ISimulationView view, Entity entity)
    {
        return view.HasComponent<VehicleState>(entity);
    }

    public ChildEnumerator GetChildren(ISimulationView view, Entity entity)
    {
        Entity first = Entity.Null;
        if (view.HasComponent<HierarchyNode>(entity))
        {
            first = view.GetComponentRO<HierarchyNode>(entity).FirstChild;
        }
        return new ChildEnumerator(view, first);
    }

    public Entity GetParent(ISimulationView view, Entity entity)
    {
        if (view.HasComponent<HierarchyNode>(entity))
        {
            return view.GetComponentRO<HierarchyNode>(entity).Parent;
        }
        return Entity.Null;
    }
}
```

---

## 9. Testing Strategy

### 9.1 Unit Tests
- **EntityInspectorPanel**: Verify selection sync with IInspectorContext
- **MapCamera**: Test zoom/pan math, coordinate conversions
- **HierarchyOrderSystem**: Validate sorting correctness for various tree shapes
- **AggregateUpdateSystem**: Check centroid and bounds calculation

### 9.2 Integration Tests
- **Layer Visibility**: Toggle layers, verify correct entities rendered
- **Tool Switching**: Switch tools, verify input routing
- **Hierarchy Rendering**: Create multi-level ORBAT, verify expansion logic

### 9.3 Performance Tests
- **Aggregate Update**: Benchmark 1000+ entity hierarchy update time
- **Rendering**: Measure FPS with 10,000 entities across multiple layers
- **Allocation**: Verify zero GC during steady-state rendering

---

## 10. Future Extensions

### 10.1 3D Visualization Toolkit
- `FDP.Toolkit.Vis3D` with similar layer/adapter architecture
- Use same `IMapTool` interface for consistency

### 10.2 Network-Aware Inspectors
- Show replicated vs. local component status
- Visualize authority and ownership in distributed simulations

### 10.3 Recording/Playback UI
- Unified timeline scrubber
- Frame-by-frame stepping with state inspection

### 10.4 Dynamic Query Builder UI
- Visual query construction (drag-and-drop components)
- Live preview of matching entities

---

## 11. Migration Guide

### 11.1 For Existing Projects Using CarKinem Code

**Step 1**: Add framework references
```xml
<ProjectReference Include="..\..\Framework\FDP.Framework.Raylib" />
<ProjectReference Include="..\..\Framework\FDP.Toolkit.ImGui" />
<ProjectReference Include="..\..\Framework\FDP.Toolkit.Vis2D" />
```

**Step 2**: Create application class
```csharp
public class MyApp : FdpApplication
{
    protected override void OnLoad() { /* Setup */ }
    protected override void OnDrawWorld() { /* Render */ }
    protected override void OnDrawUI() { /* Panels */ }
}
```

**Step 3**: Implement visualizer adapter
```csharp
public class MyEntityVisualizer : IVisualizerAdapter
{
    // Implement GetPosition, Render, GetHitRadius, GetHoverLabel
}
```

**Step 4**: Update Program.cs
```csharp
static void Main()
{
    using var app = new MyApp();
    app.Run();
}
```

### 11.2 Incremental Adoption
- Frameworks can be adopted individually
- Start with `FDP.Framework.Raylib` to simplify windowing
- Add `FDP.Toolkit.ImGui` for inspectors when needed
- Add `FDP.Toolkit.Vis2D` when ready to refactor rendering

---

## 12. Design Rationale

### 12.1 Why Adapters?
**Problem**: Different projects have different component types.  
**Solution**: Adapter pattern allows map to work with any data via callbacks/interfaces.  
**Benefit**: Zero coupling between toolkit and specific game logic.

### 12.2 Why Layers?
**Problem**: Complex scenes need multiple rendering passes (background, entities, UI).  
**Solution**: Layer system with explicit ordering and visibility control.  
**Benefit**: Composable rendering, easy to add/remove visual elements.

### 12.3 Why Tools?
**Problem**: Different interaction modes need different input handling.  
**Solution**: State pattern with tool switching.  
**Benefit**: Clean separation of interaction logic, easy to add custom modes.

### 12.4 Why Hierarchy Linearization?
**Problem**: Tree traversal every frame is slow (pointer chasing, cache misses).  
**Solution**: Pre-compute bottom-up sorted list, update only when structure changes.  
**Benefit**: O(N) linear performance instead of O(N × depth) recursive cost.

---

## 13. Glossary

- **ORBAT**: Order of Battle - hierarchical organization structure
- **Adapter**: Pattern to decouple abstraction from implementation
- **Layer**: Self-contained rendering unit with priority ordering
- **Tool**: Stateful input handler defining interaction mode
- **Semantic Zoom**: Automatic switch between detail/aggregate based on view scale
- **Decluttering**: Hiding overlapping symbols to reduce visual complexity
- **Aggregate**: Logical entity representing a group (Squad, Platoon, Company)
- **Leaf**: Spatial entity (individual unit like Tank or Soldier)
- **Centroid**: Average position of a group of entities

---

## End of Design Document
