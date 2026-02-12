# MAP-TASK-TRACKER.md
## FDP UI Reusable Toolkits - Task Tracker

**Reference**: See [MAP-TASK-DETAIL.md](./MAP-TASK-DETAIL.md) for detailed task descriptions  
**Design Document**: [MAP-DESIGN.md](./MAP-DESIGN.md)  
**Version**: 1.0  
**Date**: 2026-02-11

---

## Phase 1: FDP.Toolkit.ImGui (The Inspectors)

**Goal**: Create renderer-agnostic debugging and inspection tools for FDP entities, events, and systems.

- [x] **MAP-P1-001** Project Setup and Core Abstractions [details](./MAP-TASK-DETAIL.md#map-p1-001-project-setup-and-core-abstractions)
- [x] **MAP-P1-002** ComponentReflector Implementation (BATCH-01) [details](./MAP-TASK-DETAIL.md#map-p1-002-componentreflector-implementation)
- [x] **MAP-P1-003** EntityInspectorPanel Implementation (BATCH-01) [details](./MAP-TASK-DETAIL.md#map-p1-003-entityinspectorpanel-implementation)
- [x] **MAP-P1-004** SystemProfilerPanel Implementation (BATCH-01) [details](./MAP-TASK-DETAIL.md#map-p1-004-systemprofilerpanel-implementation)
- [x] **MAP-P1-005** EventBrowserPanel Implementation (BATCH-01) [details](./MAP-TASK-DETAIL.md#map-p1-005-eventbrowserpanel-implementation)

---

## Phase 2: FDP.Framework.Raylib (The App Host)

**Goal**: Eliminate boilerplate code for windowing, rendering loop, and ImGui setup.

- [x] **MAP-P2-001** Project Setup and ApplicationConfig (BATCH-02) [details](./MAP-TASK-DETAIL.md#map-p2-001-project-setup-and-applicationconfig)
- [x] **MAP-P2-002** FdpApplication Base Class (BATCH-02) [details](./MAP-TASK-DETAIL.md#map-p2-002-fdpapplication-base-class)
- [x] **MAP-P2-003** InputFilter Utility (BATCH-02) [details](./MAP-TASK-DETAIL.md#map-p2-003-inputfilter-utility)

---

## Phase 3: FDP.Toolkit.Vis2D (The Map)

**Goal**: Create abstract 2D visualization system with layers, tools, and adapters.

- [x] **MAP-P3-001** Project Setup and Core Abstractions (SKELETON) [details](./MAP-TASK-DETAIL.md#map-p3-001-project-setup-and-core-abstractions)
- [x] **MAP-P3-002** MapCamera Implementation (BATCH-03) [details](./MAP-TASK-DETAIL.md#map-p3-002-mapcamera-implementation)
- [x] **MAP-P3-003** MapDisplayComponent and Layer Infrastructure (BATCH-03) [details](./MAP-TASK-DETAIL.md#map-p3-003-mapdisplaycomponent-and-layer-infrastructure)
- [x] **MAP-P3-004** MapCanvas Implementation (BATCH-03) [details](./MAP-TASK-DETAIL.md#map-p3-004-mapcanvas-implementation)
- [x] **MAP-P3-005** EntityRenderLayer Implementation (BATCH-03) [details](./MAP-TASK-DETAIL.md#map-p3-005-entityrenderlayer-implementation)
- [x] **MAP-P3-006** DelegateAdapter Implementation (BATCH-03) [details](./MAP-TASK-DETAIL.md#map-p3-006-delegateadapter-implementation)
- [x] **MAP-P3-007** DebugGizmos and DebugGizmoLayer (BATCH-03) [details](./MAP-TASK-DETAIL.md#map-p3-007-debuggizmos-and-debuggizmolayer)

---

## Phase 4: Integration (CarKinem Refactoring)

**Goal**: Refactor Fdp.Examples.CarKinem to use the new framework toolkits.

- [ ] **MAP-P4-001** Add Framework References to CarKinem [details](./MAP-TASK-DETAIL.md#map-p4-001-add-framework-references-to-carkinem)
- [ ] **MAP-P4-002** Implement VehicleVisualizer Adapter [details](./MAP-TASK-DETAIL.md#map-p4-002-implement-vehiclevisualizer-adapter)
- [ ] **MAP-P4-003** Create CarKinemApp Class [details](./MAP-TASK-DETAIL.md#map-p4-003-create-carkinem-app-class)
- [ ] **MAP-P4-004** Simplify Program.cs [details](./MAP-TASK-DETAIL.md#map-p4-004-simplify-programcs)
- [ ] **MAP-P4-005** Delete Obsolete Files [details](./MAP-TASK-DETAIL.md#map-p4-005-delete-obsolete-files)

---

## Phase 5: Map Interaction Tools

**Goal**: Implement tool system for different interaction modes (selection, drag, path editing).

- [ ] **MAP-P5-001** IMapTool Interface [details](./MAP-TASK-DETAIL.md#map-p5-001-imaptool-interface)
- [ ] **MAP-P5-002** DefaultSelectionTool Implementation [details](./MAP-TASK-DETAIL.md#map-p5-002-defaultselectiontool-implementation)
- [ ] **MAP-P5-003** EntityDragTool Implementation [details](./MAP-TASK-DETAIL.md#map-p5-003-entitydragtool-implementation)
- [ ] **MAP-P5-004** PointSequenceTool Implementation [details](./MAP-TASK-DETAIL.md#map-p5-004-pointsequencetool-implementation)
- [ ] **MAP-P5-005** Integrate Tools in CarKinem [details](./MAP-TASK-DETAIL.md#map-p5-005-integrate-tools-in-carkinem)
- [ ] **MAP-P5-006** TrajectoryEditTool Implementation [details](./MAP-TASK-DETAIL.md#map-p5-006-trajectoryedittool-implementation)

---

## Phase 6: Aggregation & Decluttering

**Goal**: Support hierarchical organization (ORBAT) with semantic zoom and automatic decluttering.

- [ ] **MAP-P6-001** Hierarchy Data Components [details](./MAP-TASK-DETAIL.md#map-p6-001-hierarchy-data-components)
- [ ] **MAP-P6-002** IHierarchyAdapter Interface [details](./MAP-TASK-DETAIL.md#map-p6-002-ihierarchyadapter-interface)
- [ ] **MAP-P6-003** HierarchyOrderSystem Implementation [details](./MAP-TASK-DETAIL.md#map-p6-003-hierarchyordersystem-implementation)
- [ ] **MAP-P6-004** AggregateUpdateSystem Implementation [details](./MAP-TASK-DETAIL.md#map-p6-004-aggregateupdatesystem-implementation)
- [ ] **MAP-P6-005** HierarchicalRenderLayer Implementation [details](./MAP-TASK-DETAIL.md#map-p6-005-hierarchicalrenderlayer-implementation)
- [ ] **MAP-P6-006** Integrate Hierarchy in CarKinem [details](./MAP-TASK-DETAIL.md#map-p6-006-integrate-hierarchy-in-carkinem)

---

## Performance Validation

**Goal**: Verify performance targets are met across all systems.

- [ ] **MAP-PERF-001** Aggregate Update Performance Test [details](./MAP-TASK-DETAIL.md#map-perf-001-aggregate-update-performance-test)
- [ ] **MAP-PERF-002** Rendering Performance Test [details](./MAP-TASK-DETAIL.md#map-perf-002-rendering-performance-test)

---

## Documentation

**Goal**: Complete API documentation and usage examples.

- [ ] **MAP-DOC-001** API Documentation [details](./MAP-TASK-DETAIL.md#map-doc-001-api-documentation)
- [ ] **MAP-DOC-002** Usage Examples [details](./MAP-TASK-DETAIL.md#map-doc-002-usage-examples)

---

## Progress Summary

**Total Tasks**: 36  
**Completed**: 11  
**In Progress**: 0  
**Not Started**: 22  

**Phase Status**:
- Phase 1 (ImGui): 5/5 tasks completed ✅
- Phase 2 (Raylib): 3/3 tasks completed ✅
- Phase 3 (Vis2D): 7/7 tasks completed ✅
- Phase 4 (Integration): 0/5 tasks completed
- Phase 5 (Tools): 0/6 tasks completed
- Phase 6 (Aggregation): 0/6 tasks completed
- Performance: 0/2 tasks completed
- Documentation: 0/2 tasks completed

---

## Current Sprint

**Focus**: Skeleton Implementation Complete ✅

**Completed**:
- ✅ All 3 framework project structures created
- ✅ Core abstractions implemented (IInspectorContext, IVisualizerAdapter, IMapLayer, IMapTool, IHierarchyAdapter)
- ✅ Critical safety features implemented (cycle detection in HierarchyOrderSystem)
- ✅ Editable component inspector implemented (ComponentReflector with write-back)
- ✅ Zero-allocation hierarchy iterator (ChildEnumerator ref struct)

**Active Tasks**:
**Active Tasks**:
- MAP-P4-001 through MAP-P4-005 assigned to **BATCH-04**

**Blockers**: None

**Next Up**: MAP-P1-002 (Complete ComponentReflector.RepoReflector cache implementation)

---

## Skeleton Status

**Framework Structure**: ✅ Complete
- `/Framework/FDP.Toolkit.ImGui/` - Project created, core abstractions done
- `/Framework/FDP.Framework.Raylib/` - Project created, application host complete
- `/Framework/FDP.Toolkit.Vis2D/` - Project created, hierarchy system with cycle detection complete

**Critical Fixes Applied**: ✅ All 3 Gaps Addressed
1. ✅ **MAP-P5-006 added** - TrajectoryEditTool task specification (Gap #1)
2. ✅ **ComponentReflector editable** - Supports in-place component editing with ECS versioning (Gap #2)
3. ✅ **Cycle detection** - HierarchyOrderSystem includes safety checks to prevent infinite loops (Gap #3)

See [Framework/README-SKELETON.md](../Framework/README-SKELETON.md) for implementation details.

---

## Notes

- Each phase should be completed in order as later phases depend on earlier ones
- Phase 4 requires Phases 1, 2, and 3 to be complete
- Phase 5 can proceed in parallel with Phase 6 if needed
- Performance validation should occur after Phase 6
- Documentation should be ongoing but finalized after all implementation tasks

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-02-11 | 1.1 | Skeleton implementation complete. Added MAP-P5-006 (TrajectoryEditTool). Fixed 3 critical gaps: editable inspector, cycle detection, missing edit tool task. Created framework project structure. |
| 2026-02-11 | 1.0 | Initial task tracker created |

---

## End of Task Tracker
