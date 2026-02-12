# MAP-TASK-TRACKER.md
## FDP UI Reusable Toolkits - Task Tracker

**Reference**: See [MAP-TASK-DETAIL.md](./MAP-TASK-DETAIL.md) for detailed task descriptions  
**Design Document**: [MAP-DESIGN.md](./MAP-DESIGN.md)  
**Version**: 2.0  
**Date**: 2026-02-12

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

- [x] **MAP-P4-001** Add Framework References to CarKinem [details](./MAP-TASK-DETAIL.md#map-p4-001-add-framework-references-to-carkinem)
- [x] **MAP-P4-002** Implement VehicleVisualizer Adapter [details](./MAP-TASK-DETAIL.md#map-p4-002-implement-vehiclevisualizer-adapter)
- [x] **MAP-P4-003** Create CarKinemApp Class [details](./MAP-TASK-DETAIL.md#map-p4-003-create-carkinem-app-class)
- [x] **MAP-P4-004** Simplify Program.cs [details](./MAP-TASK-DETAIL.md#map-p4-004-simplify-programcs)
- [x] **MAP-P4-005** Delete Obsolete Files [details](./MAP-TASK-DETAIL.md#map-p4-005-delete-obsolete-files)

---

## Phase 5: Map Interaction Tools (POSTPONED)

**Goal**: Implement tool system for different interaction modes (selection, drag, path editing).

- [~] **MAP-P5-001** IMapTool Interface (Partial) [details](./MAP-TASK-DETAIL.md#map-p5-001-imaptool-interface)
- [~] **MAP-P5-002** DefaultSelectionTool Implementation (Partial) [details](./MAP-TASK-DETAIL.md#map-p5-002-defaultselectiontool-implementation)
- [~] **MAP-P5-003** EntityDragTool Implementation (Partial) [details](./MAP-TASK-DETAIL.md#map-p5-003-entitydragtool-implementation)
- [~] **MAP-P5-004** PointSequenceTool Implementation (Partial) [details](./MAP-TASK-DETAIL.md#map-p5-004-pointsequencetool-implementation)
- [~] **MAP-P5-005** Integrate Tools in CarKinem (Partial) [details](./MAP-TASK-DETAIL.md#map-p5-005-integrate-tools-in-carkinem)
- [ ] **MAP-P5-006** TrajectoryEditTool Implementation [details](./MAP-TASK-DETAIL.md#map-p5-006-trajectoryedittool-implementation)

*Note: Phase 5 was partially implemented in Batch 06 but stopped to focus on Phase 7 architecture.*

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

## Phase 7: Architectural Refinements (ACTIVE)

**Goal**: Decouple toolkits, improve testability, and formalize input/resource patterns.

- [ ] **MAP-P7-001** ISelectionState Abstraction [details](./MAP-TASK-DETAIL.md#map-p7-001-iselectionstate-abstraction)
- [ ] **MAP-P7-002** IResourceProvider Pattern [details](./MAP-TASK-DETAIL.md#map-p7-002-iresourceprovider-pattern)
- [ ] **MAP-P7-003** IInputProvider Abstraction [details](./MAP-TASK-DETAIL.md#map-p7-003-iinputprovider-abstraction)
- [ ] **MAP-P7-004** IInspectableSession Adapter [details](./MAP-TASK-DETAIL.md#map-p7-004-iinspectablesession-adapter)
- [ ] **MAP-P7-005** MapCamera Logic Split [details](./MAP-TASK-DETAIL.md#map-p7-005-mapcamera-logic-split)
- [ ] **MAP-P7-006** MapCanvas Input Pipeline Formalization [details](./MAP-TASK-DETAIL.md#map-p7-006-mapcanvas-input-pipeline-formalization)
- [ ] **MAP-P7-007** Tool Event Pattern [details](./MAP-TASK-DETAIL.md#map-p7-007-tool-event-pattern)
- [ ] **MAP-P7-008** Multi-Selection Support [details](./MAP-TASK-DETAIL.md#map-p7-008-multi-selection-support)
- [ ] **MAP-P7-009** Input Action Mapping [details](./MAP-TASK-DETAIL.md#map-p7-009-input-action-mapping)
- [ ] **MAP-P7-010** Visual Picking for Hierarchical Layers [details](./MAP-TASK-DETAIL.md#map-p7-010-visual-picking-for-hierarchical-layers)

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

**Total Tasks**: 46
**Completed**: 16
**Partial/Postponed**: 6
**Not Started**: 24

**Phase Status**:
- Phase 1 (ImGui): 5/5 tasks completed ✅
- Phase 2 (Raylib): 3/3 tasks completed ✅
- Phase 3 (Vis2D): 7/7 tasks completed ✅
- Phase 4 (Integration): 5/5 tasks completed ✅
- Phase 5 (Tools): 0/6 tasks completed (Postponed)
- Phase 6 (Aggregation): 0/6 tasks completed
- Phase 7 (Architecture): 0/10 tasks completed (Active)
- Performance: 0/2 tasks completed
- Documentation: 0/2 tasks completed

---

## Current Sprint

**Focus**: Architectural Refinements (Phase 7)

**Completed**:
- ✅ All 3 framework project structures created
- ✅ Core abstractions implemented (IInspectorContext, IVisualizerAdapter, IMapLayer, IMapTool, IHierarchyAdapter)

**Active Tasks**:
- MAP-P7-001 through MAP-P7-004 assigned to **BATCH-07**

**Blockers**: None

**Next Up**: MAP-P7-001 (Decouple Selection State)

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-02-12 | 2.0 | Added Phase 7 (Refinements). Postponed Phase 5. Updated status for Phase 4 completion. |
| 2026-02-11 | 1.1 | Skeleton implementation complete. Added MAP-P5-006 (TrajectoryEditTool). Fixed 3 critical gaps. |
| 2026-02-11 | 1.0 | Initial task tracker created |

---

## End of Task Tracker
