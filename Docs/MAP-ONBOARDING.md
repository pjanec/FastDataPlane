# MAP-ONBOARDING.md
## FDP UI Reusable Toolkits - Onboarding Guide

**Welcome to the FDP UI Reusable Toolkits Project!**

This document will help you get started as a developer contributing to this enhancement project.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [What We're Building](#what-were-building)
3. [Repository Structure](#repository-structure)
4. [Getting Started](#getting-started)
5. [Build Instructions](#build-instructions)
6. [Development Workflow](#development-workflow)
7. [Testing Strategy](#testing-strategy)
8. [Key Concepts](#key-concepts)
9. [Where to Find Things](#where-to-find-things)
10. [Development Guidelines](#development-guidelines)
11. [Common Tasks](#common-tasks)
12. [Troubleshooting](#troubleshooting)
13. [Resources](#resources)

---

## Project Overview

### What Is This Project?

We are **extracting and refactoring** the UI and visualization code from `Fdp.Examples.CarKinem` into **reusable framework toolkits** that can be used across any FDP-based application.

### Goals

1. **Eliminate Duplication**: Stop copying UI code between FDP projects
2. **Reduce Boilerplate**: Make it trivial to create new visual FDP applications
3. **Maintain Flexibility**: Generic enough for any use case, yet easy to customize
4. **Achieve Performance**: Zero-allocation rendering, cache-friendly data structures

### Current Status

This is a **greenfield development** project. We're building from the design documents:
- **Design Document**: [MAP-DESIGN.md](./MAP-DESIGN.md) - Full technical design
- **Task Details**: [MAP-TASK-DETAIL.md](./MAP-TASK-DETAIL.md) - Detailed implementation tasks
- **Task Tracker**: [MAP-TASK-TRACKER.md](./MAP-TASK-TRACKER.md) - Progress tracking

---

## What We're Building

We are creating **three new framework libraries**:

### 1. FDP.Toolkit.ImGui
**Location**: `Framework/FDP.Toolkit.ImGui/`  
**Purpose**: Generic debugging and inspection tools  
**Dependencies**: `Fdp.Kernel`, `ModuleHost.Core`, `ImGui.NET`

**Components**:
- `EntityInspectorPanel` - Browse and inspect entities
- `EventBrowserPanel` - View event history
- `SystemProfilerPanel` - Monitor system performance
- `ComponentReflector` - Generic component display

**Key Feature**: Renderer-agnostic (no Raylib dependency)

---

### 2. FDP.Framework.Raylib
**Location**: `Framework/FDP.Framework.Raylib/`  
**Purpose**: Application host eliminating boilerplate  
**Dependencies**: `Fdp.Kernel`, `ModuleHost.Core`, `Raylib-cs`, `rlImGui-cs`

**Components**:
- `FdpApplication` (abstract base class) - Main application lifecycle
- `ApplicationConfig` - Window configuration
- `InputFilter` - Prevent UI/game input bleed-through

**Key Feature**: Inherit from `FdpApplication`, override 4 methods, done!

---

### 3. FDP.Toolkit.Vis2D
**Location**: `Framework/FDP.Toolkit.Vis2D/`  
**Purpose**: Abstract 2D visualization with layers and tools  
**Dependencies**: `Fdp.Kernel`, `ModuleHost.Core`, `FDP.Framework.Raylib`, `FDP.Toolkit.ImGui`

**Components**:
- `MapCanvas` - Main map container
- `MapCamera` - Pan/zoom camera
- `IVisualizerAdapter` - Abstraction for rendering entities
- `IMapLayer` - Layer system for composable rendering
- `IMapTool` - Tool system for interaction modes
- `HierarchicalRenderLayer` - ORBAT with semantic zoom

**Key Feature**: Data-agnostic via adapter pattern

---

### 4. Refactored Example (Integration Proof)
**Location**: `Examples/Fdp.Examples.CarKinem/`  
**Purpose**: Demonstrate framework usage  
**What Changes**: Deletes custom UI code, uses frameworks

**Before**: ~3000 lines of custom rendering/UI code  
**After**: ~300 lines using frameworks

---

## Repository Structure

```
D:\Work\FDP\
├── Framework\                      ← NEW: Framework libraries
│   ├── FDP.Toolkit.ImGui\         ← Phase 1
│   ├── FDP.Framework.Raylib\      ← Phase 2
│   └── FDP.Toolkit.Vis2D\         ← Phase 3
│
├── Examples\                       
│   └── Fdp.Examples.CarKinem\     ← Phase 4: Refactored to use frameworks
│
├── Kernel\                         ← Core FDP ECS
│   └── Fdp.Kernel\
│
├── ModuleHost\                     ← System execution framework
│   └── ModuleHost.Core\
│
├── Toolkits\                       ← Domain-specific toolkits
│   ├── FDP.Toolkit.CarKinem\
│   ├── FDP.Toolkit.Geographic\
│   └── ...
│
└── Docs\                           ← THIS IS WHERE YOU ARE
    ├── MAP-DESIGN.md              ← Read this first!
    ├── MAP-TASK-DETAIL.md         ← Implementation guide
    ├── MAP-TASK-TRACKER.md        ← Progress tracking
    ├── MAP-ONBOARDING.md          ← This document
    └── DEV-LEAD-GUIDE.md          ← Development standards
```

---

## Getting Started

### Prerequisites

**Required**:
- Visual Studio 2022 (v17.8+) or Rider 2024.1+
- .NET 8.0 SDK
- Git

**Recommended**:
- Windows 10/11 (primary development platform)
- 16GB+ RAM (for large solutions)

### Initial Setup

1. **Clone the repository** (if not already done):
   ```powershell
   cd D:\Work
   git clone <repository-url> FDP
   cd FDP
   ```

2. **Verify existing projects build**:
   ```powershell
   cd D:\Work\FDP
   dotnet build FDP.sln
   ```
   This ensures your environment is correct before starting new work.

3. **Review the design**:
   - Read [MAP-DESIGN.md](./MAP-DESIGN.md) sections 1-3 (40 minutes)
   - Understand the adapter pattern (critical!)
   - Review Phase 1 details (your likely starting point)

4. **Check the task tracker**:
   - Open [MAP-TASK-TRACKER.md](./MAP-TASK-TRACKER.md)
   - Find the next available task
   - Assign yourself in your team's tracking system

---

## Build Instructions

### Building the Entire Solution

```powershell
# From repository root
dotnet build FDP.sln --configuration Debug
```

### Building Individual Projects

```powershell
# Example: Build just the Kernel
dotnet build Kernel\Fdp.Kernel\Fdp.Kernel.csproj

# Example: Build framework toolkit
dotnet build Framework\FDP.Toolkit.ImGui\FDP.Toolkit.ImGui.csproj
```

### Running the CarKinem Example

```powershell
cd Examples\Fdp.Examples.CarKinem
dotnet run
```

**Note**: After Phase 4, this will use the new frameworks!

### Running Tests

```powershell
# Run all tests
dotnet test

# Run specific test project
dotnet test Kernel\Fdp.Kernel.Tests\Fdp.Kernel.Tests.csproj
```

---

## Development Workflow

### 1. Pick a Task

- Open [MAP-TASK-TRACKER.md](./MAP-TASK-TRACKER.md)
- Select an uncompleted task from the current phase
- Ensure dependencies are complete (check "Dependencies" in task details)

### 2. Read Task Details

- Open [MAP-TASK-DETAIL.md](./MAP-TASK-DETAIL.md)
- Find your task ID (e.g., `MAP-P1-003`)
- Read: Description, Implementation Steps, Success Conditions

### 3. Implementation

1. **Create branch**: `git checkout -b feature/MAP-P1-003-entity-inspector`
2. **Follow the steps** in the task detail document
3. **Write unit tests** as you go (don't save testing for the end)
4. **Commit frequently**: Small, logical commits with clear messages

### 4. Testing

- Run unit tests: `dotnet test`
- If integration test required, follow the test scenario in task details
- Verify no regressions in existing projects

### 5. Review & Merge

- Create pull request
- Reference task ID in PR description
- Update task tracker (mark task as completed)
- Wait for code review
- Address feedback, merge

---

## Testing Strategy

### Unit Tests

**Location**: Create `<ProjectName>.Tests` projects  
**Example**: `Framework/FDP.Toolkit.ImGui.Tests/`

**Naming Convention**:
- Test class: `<ClassUnderTest>Tests.cs`
- Test method: `<Method>_<Scenario>_<ExpectedResult>()`

**Example**:
```csharp
public class InspectorStateTests
{
    [Fact]
    public void SetSelectedEntity_ValidEntity_Persists()
    {
        var state = new InspectorState();
        var entity = new Entity(42, 1);
        
        state.SelectedEntity = entity;
        
        Assert.Equal(entity, state.SelectedEntity);
    }
}
```

### Integration Tests

**Location**: Within test projects, `Integration/` folder

**Purpose**: Test interaction between components (e.g., MapCanvas + Layer + Adapter)

### Manual Testing

**For UI Components**:
1. Run CarKinem example
2. Verify visual correctness
3. Test interaction (click, drag, zoom)
4. Check for visual artifacts or performance issues

---

## Key Concepts

### The Adapter Pattern

**Problem**: Map needs to render entities, but doesn't know about `VehicleState` or `TankState`.

**Solution**: Define `IVisualizerAdapter` interface:
```csharp
public interface IVisualizerAdapter
{
    Vector2? GetPosition(ISimulationView view, Entity entity);
    void Render(ISimulationView view, Entity entity, Vector2 pos, RenderContext ctx);
    float GetHitRadius(ISimulationView view, Entity entity);
}
```

**Your Job**: Implement this for your specific component types.

**Benefit**: MapCanvas is 100% generic, works with ANY game.

### Layer System

**Concept**: Instead of one giant render function, break rendering into composable layers.

**Example Layers**:
- GridLayer (background)
- RoadLayer (infrastructure)
- EntityLayer (units)
- DebugGizmoLayer (overlay)

**Rendering Order**: Layer 0 → N (bottom to top)  
**Input Order**: Layer N → 0 (top layer can consume input first)

### Tool System (State Pattern)

**Concept**: Different interaction modes need different input handling.

**Examples**:
- `DefaultSelectionTool` - Click to select
- `EntityDragTool` - Drag entity to move
- `PointSequenceTool` - Click points to define path

**Switching**: `mapCanvas.SwitchTool(new MyTool())`

### Zero-Allocation Design

**Why**: GC pressure kills frame rates in large simulations.

**Techniques**:
- Use `ref struct` for iterators (e.g., `ChildEnumerator`)
- Use `NativeArray<T>` for collections
- Pass structs by ref: `ref readonly ComponentType`
- Avoid LINQ, prefer manual loops

**Testing**: Run performance test after Phase 6, verify 0 GC allocs per frame.

---

## Where to Find Things

### Core FDP Engine
- **Entity/Component**: `Kernel/Fdp.Kernel/`
- **Systems**: `ModuleHost/ModuleHost.Core/`
- **Component Types**: Domain-specific (e.g., `Toolkits/FDP.Toolkit.CarKinem/Components/`)

### Existing UI Code (To Be Refactored)
- **Entity Inspector**: `Examples/Fdp.Examples.CarKinem/UI/EntityInspector.cs`
- **Rendering**: `Examples/Fdp.Examples.CarKinem/Rendering/`
- **Input**: `Examples/Fdp.Examples.CarKinem/Input/`

**Note**: This code will be deleted in Phase 4 after frameworks are complete.

### New Framework Code (You'll Create This)
- **Inspectors**: `Framework/FDP.Toolkit.ImGui/Panels/`
- **Application Host**: `Framework/FDP.Framework.Raylib/`
- **Map/Visualization**: `Framework/FDP.Toolkit.Vis2D/`

### Tests
- Create alongside implementation: `<Project>.Tests/`

### Documentation
- **Design**: `Docs/MAP-DESIGN.md`
- **Tasks**: `Docs/MAP-TASK-DETAIL.md`, `Docs/MAP-TASK-TRACKER.md`
- **Standards**: `Docs/DEV-LEAD-GUIDE.md`

---

## Development Guidelines

**IMPORTANT**: Read [DEV-LEAD-GUIDE.md](./DEV-LEAD-GUIDE.md) for comprehensive development standards.

### Quick Rules

1. **Follow the task details exactly**
   - Don't skip steps
   - Implement all success conditions
   - Write the specified tests

2. **Code style**
   - Use C# 12 features where appropriate
   - Nullable reference types enabled
   - XML documentation on all public APIs

3. **Performance**
   - No allocations in hot paths (rendering, updates)
   - Use structs for data
   - Profile before optimizing, but design for performance

4. **Testing**
   - Unit tests required for all logic
   - Integration tests required where specified
   - Manual testing for UI components

5. **Commit messages**
   ```
   [MAP-P1-003] Implement EntityInspectorPanel
   
   - Add two-column layout
   - Implement search filter
   - Add hover detection
   
   Refs: #123
   ```

6. **Dependencies**
   - Don't add dependencies without discussion
   - Prefer project references over NuGet where possible
   - Keep framework toolkits minimal

---

## Common Tasks

### Task: Create a New Project

```powershell
# Navigate to appropriate folder
cd Framework

# Create project (example for Toolkit.ImGui)
dotnet new classlib -n FDP.Toolkit.ImGui -f net8.0

# Add to solution
dotnet sln ../../FDP.sln add FDP.Toolkit.ImGui/FDP.Toolkit.ImGui.csproj

# Add dependencies
cd FDP.Toolkit.ImGui
dotnet add package ImGui.NET --version 1.91.0.1
dotnet add reference ../../Kernel/Fdp.Kernel/Fdp.Kernel.csproj
```

### Task: Add a New Component

```csharp
// In appropriate Components/ folder
using Fdp.Kernel;

namespace FDP.Toolkit.Vis2D.Components
{
    /// <summary>
    /// Controls which map layers an entity appears on.
    /// </summary>
    public struct MapDisplayComponent
    {
        /// <summary>
        /// Bitmask of layer membership (bit 0-31).
        /// </summary>
        public uint LayerMask;
        
        public static MapDisplayComponent Default => new() { LayerMask = 1 };
    }
}
```

**Don't forget**: Register component in World setup!

### Task: Implement an Adapter

```csharp
// In your application project (e.g., CarKinem)
public class VehicleVisualizer : IVisualizerAdapter
{
    public Vector2? GetPosition(ISimulationView view, Entity entity)
    {
        if (view.HasComponent<VehicleState>(entity))
            return view.GetComponentRO<VehicleState>(entity).Position;
        return null;
    }
    
    public void Render(ISimulationView view, Entity entity, Vector2 pos, RenderContext ctx)
    {
        // Your custom rendering code
        var state = view.GetComponentRO<VehicleState>(entity);
        Color color = ctx.IsSelected ? Color.Green : Color.Blue;
        Raylib.DrawCircleV(pos, 2.0f, color);
    }
    
    public float GetHitRadius(ISimulationView view, Entity entity) => 2.0f;
    
    public string? GetHoverLabel(ISimulationView view, Entity entity) 
        => $"Entity {entity.Index}";
}
```

---

## Troubleshooting

### Build Errors

**Problem**: "Project reference not found"  
**Solution**: Ensure you're building from repository root, or add explicit project reference.

**Problem**: "Type or namespace not found"  
**Solution**: Check .csproj for correct `<ProjectReference>` or `<PackageReference>`.

### Runtime Errors

**Problem**: `NullReferenceException` in adapter  
**Solution**: Check `entity.IsAlive()` and `HasComponent<T>()` before accessing.

**Problem**: ImGui not rendering  
**Solution**: Ensure `rlImGui.Setup()` called in FdpApplication initialization.

**Problem**: Map not responding to input  
**Solution**: Check `InputFilter.IsMouseCaptured` - UI might be capturing mouse.

### Performance Issues

**Problem**: Low FPS with many entities  
**Solution**: 
1. Check for accidental LINQ in render loop
2. Verify layer masking is working (not rendering hidden layers)
3. Profile with dotTrace or PerfView

### Test Failures

**Problem**: Unit test fails unpredictably  
**Solution**: Check for uninitialized state, static singletons, or timing issues.

**Problem**: Integration test can't load assets  
**Solution**: Set working directory or use embedded resources.

---

## Resources

### Internal Documentation
- [MAP-DESIGN.md](./MAP-DESIGN.md) - Complete technical design
- [MAP-TASK-DETAIL.md](./MAP-TASK-DETAIL.md) - Implementation tasks
- [MAP-TASK-TRACKER.md](./MAP-TASK-TRACKER.md) - Progress tracker
- [DEV-LEAD-GUIDE.md](./DEV-LEAD-GUIDE.md) - Development standards
- [00-FDP-SOLUTION-OVERVIEW.md](./projects/00-FDP-SOLUTION-OVERVIEW.md) - Solution architecture

### External References
- **ECS Concepts**: https://github.com/SanderMertens/ecs-faq
- **ImGui.NET**: https://github.com/ImGuiNET/ImGui.NET
- **Raylib-cs**: https://github.com/ChrisDill/Raylib-cs
- **C# Performance**: https://learn.microsoft.com/en-us/dotnet/standard/collections/threadsafe/

### Getting Help

1. **Check the design doc first** - Most questions are answered there
2. **Review existing code** - Look at CarKinem examples
3. **Ask the team** - Use your team communication channel
4. **Update documentation** - If something is unclear, improve the docs!

---

## Phase-Specific Guidance

### For Phase 1 Developers (ImGui Toolkit)

**Focus**: Pure data inspection, no rendering logic.

**Key Point**: Your panels should work equally well in a console app as in a visual app.

**Testing**: Create mock EntityRepository, verify panels display data correctly.

**No Dependencies On**: Raylib, any rendering library

### For Phase 2 Developers (Raylib Framework)

**Focus**: Window lifecycle, not game logic.

**Key Point**: Make inheriting from `FdpApplication` feel like using Unity's MonoBehaviour.

**Testing**: Create minimal derived app, verify lifecycle methods called in order.

### For Phase 3 Developers (Vis2D Toolkit)

**Focus**: Generic abstractions, extreme flexibility.

**Key Point**: Should work for RTS, C2 system, arcade game, or data visualization.

**Testing**: Create multiple dummy adapters, verify map works with all of them.

**Hardest Part**: Getting the adapter interface right (iterate if needed).

### For Phase 4 Developers (Integration)

**Focus**: Prove the frameworks work by refactoring CarKinem.

**Key Point**: If you struggle to use the frameworks, the framework APIs are wrong.

**Testing**: CarKinem functionality must be 100% preserved.

**Success Metric**: Delete 2500+ lines of code.

### For Phase 5 Developers (Tools)

**Focus**: Input handling abstraction.

**Key Point**: Tools consume input, layers receive input. Priority matters.

**Testing**: Switch between tools, verify input routes correctly.

### For Phase 6 Developers (Hierarchy)

**Focus**: Performance and correctness.

**Key Point**: This is the most complex phase (topology sorting, zero-alloc iteration).

**Testing**: Create deep hierarchies (1000+ entities), verify:
- Correct centroid calculation
- Dirty flag optimization works
- Zero allocations

**Debug Tip**: Use DebugGizmos to visualize bounds and centroids.

---

## Your First Task Checklist

Before you start coding, ensure you've completed:

- [ ] Read this onboarding document fully
- [ ] Read [MAP-DESIGN.md](./MAP-DESIGN.md) sections 1-4
- [ ] Read [DEV-LEAD-GUIDE.md](./DEV-LEAD-GUIDE.md)
- [ ] Built existing solution successfully
- [ ] Run CarKinem example and understand what it does
- [ ] Selected a task from [MAP-TASK-TRACKER.md](./MAP-TASK-TRACKER.md)
- [ ] Read your task details in [MAP-TASK-DETAIL.md](./MAP-TASK-DETAIL.md)
- [ ] Created feature branch
- [ ] Set up test project (if your task includes unit tests)

**Now you're ready to code! 🚀**

---

## Welcome!

We're excited to have you on this project. These frameworks will make every future FDP project easier to build.

If you have questions, ask early and often. If you spot ways to improve the design, speak up!

**Happy coding!**

---

## End of Onboarding Document
