# MAP-DESIGN.md
## FDP UI Reusable Toolkits - Design Document

**Project**: FDP Framework Enhancement  
**Purpose**: Extract reusable UI and visualization toolkits from Fdp.Examples.CarKinem  
**Version**: 2.0  
**Date**: 2026-02-12  
**Status**: Phase 1-5 Implemented, Phase 6-7 Pending

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

## 7. Phase 7: Architectural Refinements (Decoupling & Testability)

### 7.1 Purpose
Address architectural issues identified during initial implementation to improve:
- Decoupling between toolkits (Vis2D ↔ ImGui)
- Testability (input abstraction, mock-friendly)
- Input routing clarity (strict Chain of Responsibility)
- Multi-selection support for RTS-style interaction
- Flexibility (resource injection, adapters for different data sources)

### 7.2 Motivation: Identified Coupling Violations

**Issue 1**: `EntityRenderLayer` depends on `IInspectorContext` from `FDP.Toolkit.ImGui`.  
**Problem**: Visualization toolkit has hard dependency on UI toolkit. Cannot use Vis2D without ImGui.  
**Solution**: Introduce `ISelectionState` in Vis2D. Application creates adapter that implements both interfaces.

**Issue 2**: `VehicleVisualizer` constructor requires `TrajectoryPoolManager`.  
**Problem**: Visualizers permanently coupled to simulation managers. Cannot reuse in different contexts (replay, network client).  
**Solution**: Inject resources via `RenderContext` using `IResourceProvider` pattern.

**Issue 3**: `MapCanvas` calls static `Raylib.*` methods directly.  
**Problem**: Impossible to unit test input routing without real window. Cannot implement replay/synthetic input.  
**Solution**: Abstract input via `IInputProvider` interface with default `RaylibInputProvider` implementation.

**Issue 4**: `EntityInspectorPanel` requires concrete `EntityRepository`.  
**Problem**: Cannot inspect snapshots, network proxies, or read-only views.  
**Solution**: Introduce `IInspectableSession` adapter for flexible data access.

### 7.3 Decoupling Pattern 1: Selection State

#### 7.3.1 ISelectionState (Vis2D)
**Purpose**: Define what the map needs without knowing about UI specifics.

```csharp
namespace FDP.Toolkit.Vis2D.Abstractions
{
    public interface ISelectionState
    {
        /// <summary>Hot path: O(1) check for rendering</summary>
        bool IsSelected(Entity entity);
        
        /// <summary>Data path: Full selection set</summary>
        IReadOnlyCollection<Entity> SelectedEntities { get; }
        
        /// <summary>Focus path: Primary entity for inspector details</summary>
        Entity? PrimarySelected { get; set; }
        
        /// <summary>Transient hover state</summary>
        Entity? HoveredEntity { get; set; }
    }
}
```

#### 7.3.2 Application Bridge Pattern
**In CarKinemApp**:
```csharp
public class SelectionAdapter : ISelectionState, IInspectorContext
{
    private readonly SelectionManager _manager; // App's actual state
    
    // ISelectionState methods (for Map)
    public bool IsSelected(Entity e) => _manager.Contains(e);
    public IReadOnlyCollection<Entity> SelectedEntities => _manager.GetAll();
    
    // IInspectorContext methods (for Inspector) - same backing field
    public Entity? SelectedEntity 
    { 
        get => _manager.PrimaryId.HasValue ? new Entity(_manager.PrimaryId.Value, ...) : null;
        set => _manager.SetPrimary(value?.Index ?? -1);
    }
    
    public Entity? HoveredEntity { get; set; } // Shared transient state
}
```

**Wiring**:
```csharp
var adapter = new SelectionAdapter(_selectionManager);
_map.AddLayer(new EntityRenderLayer(..., adapter)); // ISelectionState interface
_inspector.Draw(World, adapter); // IInspectorContext interface
```

**Result**: Vis2D and ImGui no longer reference each other. Both depend on abstractions owned by their own toolkit.

### 7.4 Decoupling Pattern 2: Resource Injection

#### 7.4.1 IResourceProvider
**Purpose**: Pass global rendering resources without constructor coupling.

```csharp
namespace FDP.Toolkit.Vis2D.Abstractions
{
    public interface IResourceProvider
    {
        T? Get<T>() where T : class;
        bool Has<T>() where T : class;
    }
}
```

#### 7.4.2 Updated RenderContext
```csharp
public struct RenderContext
{
    public Camera2D Camera;
    public Vector2 MouseWorldPos;
    public float DeltaTime;
    public uint VisibleLayersMask;
    
    // NEW: Access to shared resources
    public IResourceProvider Resources;
}
```

#### 7.4.3 MapCanvas as Resource Container
**File**: `MapCanvas.cs` additions

```csharp
public class MapCanvas : IResourceProvider
{
    private readonly Dictionary<Type, object> _resources = new();
    
    public void AddResource<T>(T resource) where T : class
    {
        _resources[typeof(T)] = resource;
    }
    
    public T? Get<T>() where T : class
    {
        return _resources.TryGetValue(typeof(T), out var obj) ? (T)obj : null;
    }
    
    public bool Has<T>() where T : class => _resources.ContainsKey(typeof(T));
    
    public void Draw()
    {
        // ...
        var ctx = new RenderContext
        {
            // ...
            Resources = this // Inject canvas itself as provider
        };
        // ...
    }
}
```

#### 7.4.4 Usage in Visualizer
**Before** (Coupled):
```csharp
public VehicleVisualizer(TrajectoryPoolManager pool) { _pool = pool; }
```

**After** (Decoupled):
```csharp
public VehicleVisualizer() { } // No constructor dependency

public void Render(..., RenderContext ctx, ...)
{
    var pool = ctx.Resources.Get<TrajectoryPoolManager>();
    if (pool != null && trajId > 0 && pool.TryGetTrajectory(trajId, out var traj))
    {
        // Draw trajectory
    }
    // Graceful degradation: if pool missing, just skip trajectory rendering
}
```

**App Registration**:
```csharp
_map.AddResource(_trajectoryPoolManager); // Register once
_map.AddResource(_formationManager);
_map.AddResource(_terrainData);
```

**Benefit**: Visualizer can be reused in contexts without trajectory pools. Unit tests don't need to mock managers they don't care about.

### 7.5 Decoupling Pattern 3: Input Abstraction

#### 7.5.1 IInputProvider
**Purpose**: Abstract input polling for testability and replay.

```csharp
namespace FDP.Toolkit.Vis2D.Abstractions
{
    public interface IInputProvider
    {
        Vector2 MousePosition { get; }
        Vector2 MouseDelta { get; }
        float MouseWheelMove { get; }
        
        bool IsMouseButtonPressed(MouseButton button);
        bool IsMouseButtonDown(MouseButton button);
        bool IsMouseButtonReleased(MouseButton button);
        
        bool IsKeyDown(KeyboardKey key);
        bool IsKeyPressed(KeyboardKey key);
        
        int ScreenWidth { get; }
        int ScreenHeight { get; }
    }
}
```

#### 7.5.2 Default Implementation
```csharp
namespace FDP.Toolkit.Vis2D.Defaults
{
    public class RaylibInputProvider : IInputProvider
    {
        public Vector2 MousePosition => Raylib.GetMousePosition();
        public float MouseWheelMove => Raylib.GetMouseWheelMove();
        public bool IsMouseButtonPressed(MouseButton b) => Raylib.IsMouseButtonPressed(b);
        // ... (wrap all Raylib static calls)
    }
}
```

#### 7.5.3 Updated MapCamera
**Before**:
```csharp
public void Update(float dt)
{
    float wheel = Raylib.GetMouseWheelMove(); // Direct coupling
    // ...
}
```

**After**:
```csharp
public bool HandleInput(IInputProvider input) // Now takes provider
{
    float wheel = input.MouseWheelMove;
    if (wheel != 0)
    {
        // ... zoom logic ...
        return true; // Consumed
    }
    return false; // Not consumed
}

public void Update(float dt)
{
    // Separate: smooth interpolation, constraints
    // No input polling here
}
```

#### 7.5.4 Updated MapCanvas
```csharp
public class MapCanvas
{
    private readonly IInputProvider _input;
    
    public MapCanvas(IInputProvider? input = null)
    {
        _input = input ?? new RaylibInputProvider(); // Default to live input
    }
    
    public void Update(float dt)
    {
        Camera.Update(dt); // Physics/smoothing always runs
        
        ProcessInputPipeline(); // Separate method for input
    }
    
    private void ProcessInputPipeline()
    {
        if (InputFilter.IsMouseCaptured) return;
        
        bool consumed = false;
        Vector2 mouseWorld = Camera.ScreenToWorld(_input.MousePosition);
        
        // 1. Tool gets first priority
        if (ActiveTool != null && _input.IsMouseButtonPressed(MouseButton.Left))
            consumed = ActiveTool.HandleClick(mouseWorld, MouseButton.Left);
        
        // 2. Camera gets second priority
        if (!consumed)
            consumed = Camera.HandleInput(_input);
        
        // 3. Layers get last priority
        if (!consumed)
        {
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                if (_layers[i].HandleInput(mouseWorld, ...))
                    break;
            }
        }
    }
}
```

**Benefits**:
- **Testability**: Write unit tests with `MockInputProvider`
- **Replay**: Implement `RecordedInputProvider` that reads from file
- **Determinism**: Synthetic input for debugging
- **Portability**: Easily adapt to different rendering backends (GLFW, SDL, Silk.NET)

### 7.6 Decoupling Pattern 4: Inspector Session Abstraction

#### 7.6.1 IInspectableSession
**Purpose**: Abstract data access for inspector to support different data sources.

```csharp
namespace FDP.Toolkit.ImGui.Abstractions
{
    public interface IInspectableSession
    {
        int EntityCount { get; }
        bool IsReadOnly { get; }
        
        IEnumerable<Entity> GetEntities(string filter, int maxCount);
        bool IsAlive(Entity entity);
        
        bool HasComponent(Entity entity, Type type);
        object? GetComponent(Entity entity, Type type);
        void SetComponent(Entity entity, Type type, object value);
    }
}
```

#### 7.6.2 Repository Adapter
**File**: `FDP.Toolkit.ImGui/Adapters/RepositoryAdapter.cs`

```csharp
public class RepositoryAdapter : IInspectableSession
{
    private readonly EntityRepository _repo;
    
    public int EntityCount => _repo.EntityCount;
    public bool IsReadOnly => false; // Live world is mutable
    
    public object? GetComponent(Entity e, Type t) => RepoReflector.GetComponent(_repo, e, t);
    public void SetComponent(Entity e, Type t, object val) => RepoReflector.SetComponent(_repo, e, t, val);
    // ... other methods ...
}
```

#### 7.6.3 Snapshot Adapter (Example)
```csharp
public class SnapshotAdapter : IInspectableSession
{
    private readonly ISimulationView _snapshot;
    
    public bool IsReadOnly => true; // Snapshots are immutable
    
    public void SetComponent(...) => throw new InvalidOperationException("Snapshot is read-only");
    // ... other methods using ISimulationView instead of EntityRepository ...
}
```

#### 7.6.4 Updated Inspector Usage
**Before**:
```csharp
_inspector.Draw(World, context); // Hardcoded EntityRepository
```

**After**:
```csharp
var session = new RepositoryAdapter(World);
_inspector.Draw(session, context); // Works with any data source
```

**Read-Only Behavior**:
- When `session.IsReadOnly == true`, inspector automatically disables all input widgets
- Shows red "[READ-ONLY]" indicator
- Prevents accidental modification of historical/snapshot data

### 7.7 Input Routing: Strict Chain of Responsibility

#### 7.7.1 The Problem
**Before**: Tools, Camera, and Layers all handle input, but priority is unclear. Camera updates unconditionally, causing conflicts.

**Example Issue**: Dragging a unit with Right Mouse Button while Camera also tries to pan with RMB → Jittery fight.

#### 7.7.2 The Solution: Explicit Priority Pipeline
**Order**: `UI (ImGui) → Active Tool → Camera → Layers`

**Rule**: If a higher-priority handler returns `true` (consumed), lower handlers are skipped.

#### 7.7.3 Updated MapCamera Logic Split
**Before**: Single `Update(dt)` method handles both input and physics.

**After**: Two methods with clear responsibilities:
- `HandleInput(IInputProvider)` - **Command**: Interpret raw input into "intent" (target zoom, target position). Returns `true` if user is actively controlling camera.
- `Update(float dt)` - **Animation**: Smooth interpolation toward targets. Always runs.

**Code**:
```csharp
public class MapCamera
{
    // Target state (the "intent")
    private Vector2 _targetOffset;
    private Vector2 _targetTarget;
    private float _targetZoom;
    
    // Actual state (current rendering values)
    public Camera2D InnerCamera;
    
    public bool HandleInput(IInputProvider input)
    {
        bool consumed = false;
        
        // Zoom
        float wheel = input.MouseWheelMove;
        if (wheel != 0)
        {
            Vector2 mouseWorld = /* calculate using _targetZoom */;
            _targetZoom = Math.Clamp(_targetZoom * scaleFactor, MinZoom, MaxZoom);
            _targetTarget = /* adjust to keep mouse fixed */;
            consumed = true;
        }
        
        // Pan
        if (input.IsMouseButtonDown(PanButton))
        {
            Vector2 delta = /* calculate */;
            _targetTarget += delta / _targetZoom;
            consumed = true;
        }
        
        return consumed;
    }
    
    public void Update(float dt)
    {
        // Lerp current state toward target
        float t = Math.Clamp(dt * SmoothTime, 0f, 1f);
        InnerCamera.Offset = Vector2.Lerp(InnerCamera.Offset, _targetOffset, t);
        InnerCamera.Target = Vector2.Lerp(InnerCamera.Target, _targetTarget, t);
        InnerCamera.Zoom = Lerp(InnerCamera.Zoom, _targetZoom, t);
    }
}
```

**Benefit**: Programmatic camera control (`FocusOn(Vector2)` sets `_targetTarget`) and user input both use same smoothing pipeline automatically.

#### 7.7.4 MapCanvas Pipeline Implementation
```csharp
private void ProcessInputPipeline()
{
    if (InputFilter.IsMouseCaptured) return; // UI has priority
    
    bool consumed = false;
    Vector2 mouseWorld = Camera.ScreenToWorld(_input.MousePosition);
    
    // Priority 1: Tool
    if (ActiveTool != null)
    {
        if (_input.IsMouseButtonPressed(MouseButton.Left))
            consumed = ActiveTool.HandleClick(mouseWorld, MouseButton.Left);
        
        if (!consumed && _input.IsMouseButtonDown(MouseButton.Left))
            consumed = ActiveTool.HandleDrag(mouseWorld, _input.MouseDelta / Camera.Zoom);
        
        ActiveTool.HandleHover(mouseWorld); // Hover doesn't consume (visual only)
    }
    
    // Priority 2: Camera
    if (!consumed)
        consumed = Camera.HandleInput(_input);
    
    // Priority 3: Layers (Reverse order = topmost first)
    if (!consumed)
    {
        for (int i = _layers.Count - 1; i >= 0; i--)
        {
            if (!IsLayerVisible(_layers[i])) continue;
            if (_layers[i].HandleInput(mouseWorld, ...))
                break; // Consumed
        }
    }
}
```

### 7.8 Multi-Selection Support

#### 7.8.1 Updated ISelectionState
**Before**: `Entity? SelectedEntity` (singular)

**After**: `IReadOnlyCollection<Entity> SelectedEntities` (plural) + `Entity? PrimarySelected` for inspector focus.

#### 7.8.2 Updated EntityRenderLayer
**Before**:
```csharp
bool isSelected = _inspector.SelectedEntity == entity;
```

**After**:
```csharp
bool isSelected = _selectionState.IsSelected(entity); // O(1) HashSet check
```

#### 7.8.3 Tool Event Pattern
**StandardInteractionTool** fires events instead of setting state directly:

```csharp
public event Action<Entity, bool>? OnEntitySelectRequest; // entity, isAdditive

public bool HandleClick(Vector2 worldPos, MouseButton button)
{
    Entity hit = FindEntityAt(worldPos);
    bool isShift = _input.IsKeyDown(KeyboardKey.LeftShift);
    
    OnEntitySelectRequest?.Invoke(hit, isShift);
    return true;
}
```

**App handles logic**:
```csharp
_interactionTool.OnEntitySelectRequest += (entity, additive) =>
{
    if (additive)
        _selectionManager.Add(entity);
    else
        _selectionManager.Set(entity); // Clear others and select this one
};
```

#### 7.8.4 BoxSelectionTool Integration
**Tool fires event with list**:
```csharp
public event Action<List<Entity>>? OnRegionSelected;

// On mouse release:
_results.Clear();
foreach (var entity in _query)
{
    if (IsInRect(pos, _boxMin, _boxMax))
        _results.Add(entity);
}
OnRegionSelected?.Invoke(_results);
```

**App updates selection manager**:
```csharp
_boxSelectTool.OnRegionSelected += (entities) =>
{
    _selectionManager.SetMultiple(entities);
};
```

### 7.9 Input Action Mapping (Rebindable Controls)

#### 7.9.1 Configuration Object
```csharp
namespace FDP.Toolkit.Vis2D.Input
{
    public class Vis2DInputMap
    {
        public MouseButton SelectButton { get; set; } = MouseButton.Left;
        public MouseButton PanButton { get; set; } = MouseButton.Right;
        public MouseButton ContextButton { get; set; } = MouseButton.Right;
        
        public KeyboardKey MultiSelectModifier { get; set; } = KeyboardKey.LeftShift;
        public KeyboardKey RangeSelectModifier { get; set; } = KeyboardKey.LeftControl;
        
        public static Vis2DInputMap Default => new();
    }
}
```

#### 7.9.2 Tool and Camera Injection
**MapCamera Constructor**:
```csharp
public MapCamera(Vis2DInputMap? inputMap = null)
{
    _inputMap = inputMap ?? Vis2DInputMap.Default;
}
```

**Usage in HandleInput**:
```csharp
if (input.IsMouseButtonDown(_inputMap.PanButton)) // Configurable
{
    // ... pan logic ...
}
```

**StandardInteractionTool**:
```csharp
if (input.IsMouseButtonPressed(_inputMap.SelectButton))
{
    // ... selection logic ...
}
```

**Benefit**: Users can configure "RTS Mode" (Left=Select, Right=Context/Pan) vs "Editor Mode" (Right=Select, Middle=Pan) without code changes.

### 7.10 Visual Picking for Hierarchical Rendering

#### 7.10.1 The Problem
With Aggregation/Decluttering, the "visible" entity changes based on zoom.  
**Issue**: If `StandardInteractionTool` iterates a flat `VehicleQuery`, it might select a Tank that's visually hidden inside a Platoon symbol.

#### 7.10.2 The Solution: PickEntity Method
**Updated IMapLayer**:
```csharp
public interface IMapLayer
{
    // ... existing methods ...
    
    /// <summary>
    /// Ask layer what entity (if any) exists at this world position.
    /// Used by tools to select what is currently visible.
    /// </summary>
    Entity? PickEntity(Vector2 worldPos);
}
```

**MapCanvas Method**:
```csharp
public Entity PickTopmostEntity(Vector2 worldPos)
{
    for (int i = _layers.Count - 1; i >= 0; i--) // Top to bottom
    {
        if (!IsLayerVisible(_layers[i])) continue;
        
        Entity? hit = _layers[i].PickEntity(worldPos);
        if (hit.HasValue) return hit.Value;
    }
    return Entity.Null;
}
```

**StandardInteractionTool Updated**:
```csharp
public bool HandleClick(Vector2 worldPos, MouseButton button)
{
    // BEFORE: var hit = FindEntityAt(worldPos); // Flat iteration
    // AFTER:
    Entity hit = _canvas.PickTopmostEntity(worldPos); // Delegates to layers
    
    OnEntitySelectRequest?.Invoke(hit, _isShiftHeld);
    return true;
}
```

**HierarchicalRenderLayer Implementation**:
```csharp
public Entity? PickEntity(Vector2 worldPos)
{
    foreach (var root in _rootQuery)
    {
        Entity? result = PickRecursive(root, worldPos, _lastFrameContext.Zoom);
        if (result.HasValue) return result;
    }
    return null;
}

private Entity? PickRecursive(Entity entity, Vector2 mousePos, float zoom)
{
    Vector2 pos = /* get position */;
    Vector2 bMin = /* get bounds */;
    Vector2 bMax = /* get bounds */;
    
    if (ShouldExpand(entity, bMin, bMax, zoom)) // Same logic as Draw!
    {
        // Currently expanded -> check children
        foreach (var child in _hierarchy.GetChildren(_view, entity))
        {
            Entity? hit = PickRecursive(child, mousePos, zoom);
            if (hit.HasValue) return hit;
        }
    }
    else
    {
        // Currently collapsed -> hit test this aggregate
        float radius = _visualizer.GetHitRadius(_view, entity);
        if (Vector2.DistanceSquared(pos, mousePos) < radius * radius)
            return entity; // Return the Platoon, not the Tank
    }
    return null;
}
```

**Result**: Selection perfectly matches what user sees. If zoomed out showing Platoon, clicking selects Platoon. If zoomed in showing Tanks, clicking selects Tank.

### 7.11 Migration from Phase 1-6 to Phase 7

#### 7.11.1 EntityRenderLayer Changes
**Before**:
```csharp
public EntityRenderLayer(..., IInspectorContext inspector)
{
    _inspector = inspector;
}

// In Draw():
bool isSelected = _inspector.SelectedEntity == entity;
```

**After**:
```csharp
public EntityRenderLayer(..., ISelectionState selectionState)
{
    _selectionState = selectionState;
}

// In Draw():
bool isSelected = _selectionState.IsSelected(entity);
```

**In App**:
```csharp
var adapter = new SelectionAdapter(_selectionManager);
var layer = new EntityRenderLayer(..., adapter); // Pass ISelectionState interface
_inspector.Draw(_worldSession, adapter); // Pass IInspectorContext interface
```

#### 7.11.2 VehicleVisualizer Changes
**Before**:
```csharp
public VehicleVisualizer(TrajectoryPoolManager pool) { _pool = pool; }
```

**After**:
```csharp
public VehicleVisualizer() { }

public void Render(..., RenderContext ctx, ...)
{
    var pool = ctx.Resources.Get<TrajectoryPoolManager>();
    if (pool != null) { /* use pool */ }
}
```

**In App**:
```csharp
_map.AddResource(_trajectoryPoolManager); // Register resource
```

#### 7.11.3 MapCanvas Changes
**Before**:
```csharp
_map = new MapCanvas();
_map.Camera.Update(dt); // Inside app update loop
```

**After**:
```csharp
_map = new MapCanvas(); // Or: new MapCanvas(new MockInputProvider()) for tests
// Camera Update is now internal to MapCanvas.Update()
_map.Update(dt); // Handles input pipeline automatically
```

#### 7.11.4 EntityInspectorPanel Changes
**Before**:
```csharp
_inspector.Draw(World, _inspectorContext);
```

**After**:
```csharp
var session = new RepositoryAdapter(World);
_inspector.Draw(session, _inspectorContext);
```

### 7.12 Testing Benefits

#### 7.12.1 Input Routing Unit Test (Example)
```csharp
[Test]
public void Camera_DoesNotPan_WhenToolConsumesInput()
{
    var input = new MockInputProvider
    {
        MousePosition = new Vector2(100, 100),
        IsButtonDown = { [MouseButton.Right] = true }
    };
    
    var canvas = new MapCanvas(input);
    var tool = new MockTool { ShouldConsumeClick = true };
    canvas.SwitchTool(tool);
    
    Vector2 initialTarget = canvas.Camera.InnerCamera.Target;
    
    canvas.Update(0.016f); // One frame
    
    Assert.AreEqual(initialTarget, canvas.Camera.InnerCamera.Target); // Camera did NOT move
    Assert.IsTrue(tool.WasHandleClickCalled); // Tool got the input
}
```

#### 7.12.2 Replay Input Test
```csharp
[Test]
public void Map_CanBeDrivenFromRecordedInput()
{
    var recording = InputRecording.Load("test_session.input");
    var playback = new PlaybackInputProvider(recording);
    
    var canvas = new MapCanvas(playback);
    
    for (int frame = 0; frame < recording.FrameCount; frame++)
    {
        playback.AdvanceFrame();
        canvas.Update(0.016f);
        
        // Assert expected state for this frame
    }
}
```

### 7.13 Summary of Phase 7 Changes

**Abstractions Introduced**:
1. `ISelectionState` - Decouple Vis2D from ImGui
2. `IResourceProvider` - Inject rendering resources dynamically
3. `IInputProvider` - Abstract input for testing/replay
4. `IInspectableSession` - Support multiple data sources for inspector
5. `Vis2DInputMap` - Rebindable input controls

**Architecture Patterns**:
1. **Adapter Bridges** - Single class implements multiple toolkit interfaces
2. **Resource Injection** - Via RenderContext instead of constructors
3. **Chain of Responsibility** - Explicit input priority pipeline
4. **Command/Event Pattern** - Tools fire events, app handles logic
5. **Visual Picking** - Layers delegate hit testing for semantic correctness

**Performance Impact**: Zero. All abstractions compile to virtual calls or interface lookups (single indirection). Resource lookup is `Dictionary<Type, object>` which is O(1).

**Testing Impact**: Massive improvement. Input routing, selection logic, camera math all testable in isolation without GPU/window.

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
