using System;
using System.Collections.Generic;
using System.Numerics;
using CarKinem.Commands;
using CarKinem.Core;
using CarKinem.Formation;
using CarKinem.Road;
using CarKinem.Trajectory;
using Fdp.Examples.CarKinem.Components;
using Fdp.Kernel;

namespace Fdp.Examples.CarKinem.Core
{
    public class ScenarioManager
    {
        private readonly EntityRepository _repository;
        private readonly RoadNetworkBlob _roadNetwork;
        private readonly TrajectoryPoolManager _trajectoryPool;
        private readonly FormationTemplateManager _formationTemplates;
        private readonly Random _rng = new Random();

        // Roaming Logic
        private HashSet<int> _roamingEntities = new HashSet<int>();
        
        // Waypoint Logic
        private Dictionary<int, List<Vector2>> _waypointQueues = new Dictionary<int, List<Vector2>>();

        public ScenarioManager(
            EntityRepository repository, 
            RoadNetworkBlob roadNetwork, 
            TrajectoryPoolManager trajectoryPool,
            FormationTemplateManager formationTemplates)
        {
            _repository = repository;
            _roadNetwork = roadNetwork;
            _trajectoryPool = trajectoryPool;
            _formationTemplates = formationTemplates;
        }

        public void ClearAll()
        {
             // Query all vehicles
             var query = _repository.Query().With<VehicleState>().Build();
             var toDestroy = new System.Collections.Generic.List<Fdp.Kernel.Entity>();
             
             foreach(var e in query)
             {
                 toDestroy.Add(e);
             }
             
             foreach(var e in toDestroy)
             {
                 _repository.DestroyEntity(e);
             }
             
             // Clear local state
             _roamingEntities.Clear();
             _waypointQueues.Clear();
             
             // Clear Trajectories
             _trajectoryPool.Clear();
        }

        public void Update()
        {
            UpdateWaypointQueues();
            UpdateRoamers();
        }

        private void UpdateRoamers()
        {
            foreach (var id in new List<int>(_roamingEntities))
            {
                var entity = new Entity(id, 1);
                if (!_repository.IsAlive(entity)) { _roamingEntities.Remove(id); continue; }

                if (!_repository.HasComponent<NavState>(entity)) continue;

                var nav = _repository.GetComponentRO<NavState>(entity);
                if (nav.HasArrived == 1)
                {
                    // Pick new random destination
                    SetDestination(id, new Vector2(_rng.Next(0, 500), _rng.Next(0, 500)));
                }
            }
        }

        private void UpdateWaypointQueues()
        {
            foreach (var entityIndex in new List<int>(_waypointQueues.Keys))
            {
                var queue = _waypointQueues[entityIndex];
                if (queue.Count == 0) continue;

                var entity = new Entity(entityIndex, 1);
                if (!_repository.IsAlive(entity))
                {
                    _waypointQueues.Remove(entityIndex);
                    continue;
                }

                var state = _repository.GetComponentRO<VehicleState>(entity);

                // Check distance to next target
                if (Vector2.Distance(state.Position, queue[0]) < 8.0f)
                {
                    queue.RemoveAt(0);
                    // Trajectory continues to next point automatically if it was built with multiple points?
                    // Actually, AddWaypoint builds a single trajectory with ALL points.
                    // So we don't need to re-issue command.
                    // We just track progress here to remove from queue.
                    // Wait, if we just remove from queue, do we need to do anything?
                    // The vehicle follows the *generated* trajectory.
                    // This queue seems to be local tracking only.
                }
            }
        }

        public int SpawnVehicle(Vector2 position, Vector2 heading, VehicleClass vehicleClass = VehicleClass.PersonalCar)
        {
            var e = _repository.CreateEntity();
            
            _repository.AddComponent(e, new VehicleState { 
                Position = position, 
                Forward = heading,
                Speed = 0,
                SteerAngle = 0
            });
            
            var preset = global::CarKinem.Core.VehiclePresets.GetPreset(vehicleClass);
            preset.Class = vehicleClass; // Ensure class is set
            _repository.AddComponent(e, preset);
            
            // Use component-based color defaults if needed, or rely on visualizer
            // DemoSimulation defaulted to GreenYellow via component
            _repository.AddComponent(e, VehicleColor.GreenYellow);
            
            _repository.AddComponent(e, new NavState {
                Mode = NavigationMode.None
            });
            
            return e.Index;
        }

        public void AddWaypoint(int entityIndex, Vector2 destination, TrajectoryInterpolation interpolation = TrajectoryInterpolation.Linear)
        {
             // 1. Get/Create Queue
             if (!_waypointQueues.ContainsKey(entityIndex))
             {
                 _waypointQueues[entityIndex] = new List<Vector2>();
             }
             
             // 2. Add to Queue
             _waypointQueues[entityIndex].Add(destination);
             
             // 3. Construct Trajectory from Current Position
             var entity = new Entity(entityIndex, 1);
             if (!_repository.IsAlive(entity)) return;

             var state = _repository.GetComponentRO<VehicleState>(entity);
             
             var path = new List<Vector2>();
             path.Add(state.Position);
             path.AddRange(_waypointQueues[entityIndex]);
             
             // 4. Create Speeds (Cruise=15, Stop=0 at end)
             var speeds = new float[path.Count];
             for(int i=0; i<speeds.Length; i++) speeds[i] = 15.0f;
             speeds[speeds.Length - 1] = 0.0f; // Stop at end
             
             // 5. Register new Trajectory
             int trajId = _trajectoryPool.RegisterTrajectory(path.ToArray(), speeds, false, interpolation);
             
             // Cleanup old trajectory
             var oldNav = _repository.GetComponentRO<NavState>(entity);
             if (oldNav.Mode == NavigationMode.CustomTrajectory && oldNav.TrajectoryId > 0)
             {
                 _trajectoryPool.RemoveTrajectory(oldNav.TrajectoryId);
             }
             
             // 6. Issue Command
             _repository.Bus.Publish(new CmdFollowTrajectory {
                Entity = entity,
                TrajectoryId = trajId
            });
        }
        
        public void SetDestination(int entityIndex, Vector2 destination, TrajectoryInterpolation interpolation = TrajectoryInterpolation.Linear)
        {
             if (_waypointQueues.ContainsKey(entityIndex))
             {
                 _waypointQueues[entityIndex].Clear();
             }
             AddWaypoint(entityIndex, destination, interpolation);
        }

        // --- Scenarios ---

        public void SpawnCollisionTest(VehicleClass vClass)
        {
            for(int i=0; i<5; i++)
            {
                 Vector2 center = new Vector2(250 + i * 20, 250 + i * 20); 
                 Vector2 offset = new Vector2(40, 0);
                 
                 int idA = SpawnVehicle(center - offset, new Vector2(1, 0), vClass);
                 SetDestination(idA, center + offset);
                 
                 int idB = SpawnVehicle(center + offset, new Vector2(-1, 0), vClass);
                 SetDestination(idB, center - offset);
            }
        }

        public void SpawnFastOne()
        {
            if (!_roadNetwork.Nodes.IsCreated || _roadNetwork.Nodes.Length < 2) return;
            
            // Pick two random nodes
            int startIdx = _rng.Next(0, _roadNetwork.Nodes.Length);
            int endIdx = _rng.Next(0, _roadNetwork.Nodes.Length);
            while (startIdx == endIdx) endIdx = _rng.Next(0, _roadNetwork.Nodes.Length);
            
            var startNode = _roadNetwork.Nodes[startIdx];
            var endNode = _roadNetwork.Nodes[endIdx];
            
            int id = SpawnVehicle(startNode.Position, new Vector2(1,0), VehicleClass.PersonalCar);
            
            Entity entity = new Entity(id, 1);
            
            // Boost speed
            var vParams = _repository.GetComponentRO<VehicleParams>(entity); // Struct copy
            vParams.MaxSpeedFwd = 50.0f; 
            vParams.MaxAccel = 10.0f;     
            vParams.MaxLatAccel = 15.0f;  
            _repository.SetComponent(entity, vParams);
            
            _repository.Bus.Publish(new CmdNavigateViaRoad {
                 Entity = entity,
                 Destination = endNode.Position,
                 ArrivalRadius = 5.0f
            });
        }

        public void SpawnRoadUsers(int count, VehicleClass vClass)
        {
            if (!_roadNetwork.Nodes.IsCreated || _roadNetwork.Nodes.Length < 2) return;
            
            for(int i=0; i<count; i++)
            {
                int startNodeIdx = _rng.Next(0, _roadNetwork.Nodes.Length);
                var startNode = _roadNetwork.Nodes[startNodeIdx];
                int endNodeIdx = _rng.Next(0, _roadNetwork.Nodes.Length);
                var endNode = _roadNetwork.Nodes[endNodeIdx];
                
                int id = SpawnVehicle(startNode.Position, new Vector2(1,0), vClass);
                
                var entity = new Entity(id, 1);
                _repository.SetComponent(entity, VehicleColor.Blue);
                
                _repository.Bus.Publish(new CmdNavigateViaRoad {
                     Entity = entity,
                     Destination = endNode.Position,
                     ArrivalRadius = 5.0f
                });
            }
        }

        public void SpawnRoamers(int count, VehicleClass vClass, TrajectoryInterpolation interpolation = TrajectoryInterpolation.Linear)
        {
            for(int i=0; i<count; i++)
            {
                 Vector2 pos = new Vector2(_rng.Next(0,500), _rng.Next(0,500));
                 Vector2 heading = new Vector2((float)_rng.NextDouble() - 0.5f, (float)_rng.NextDouble() - 0.5f);
                 if (heading == Vector2.Zero) heading = new Vector2(1, 0);
                 else heading = Vector2.Normalize(heading);

                 int id = SpawnVehicle(pos, heading, vClass);
                 _repository.SetComponent(new Entity(id, 1), VehicleColor.Orange);
                 
                 _roamingEntities.Add(id);
                 SetDestination(id, new Vector2(_rng.Next(0,500), _rng.Next(0,500)), interpolation);
            }
        }

        public void SpawnFormation(VehicleClass vClass, FormationType type, int count, TrajectoryInterpolation interpolation)
        {
             Vector2 startPos = new Vector2(_rng.Next(100, 400), _rng.Next(100, 400));
             Vector2 heading = new Vector2(1, 0); 
             
             int leaderId = SpawnVehicle(startPos, heading, vClass);
             var leaderEntity = new Entity(leaderId, 1);
             _repository.SetComponent(leaderEntity, VehicleColor.Magenta);
             
             _repository.Bus.Publish(new CmdCreateFormation
             {
                 LeaderEntity = leaderEntity,
                 Type = type,
                 Params = new FormationParams 
                 {
                     Spacing = 12.0f,
                     WedgeAngleRad = 0.5f,
                     MaxCatchUpFactor = 1.25f,
                     BreakDistance = 50.0f,
                     ArrivalThreshold = 2.0f,
                     SpeedFilterTau = 1.0f
                 }
             });
             
             var template = _formationTemplates.GetTemplate(type);
             
             for (int i = 0; i < count - 1; i++) 
             {
                 Vector2 followerPos = template.GetSlotPosition(i, startPos, heading);
                 int followerId = SpawnVehicle(followerPos, heading, vClass);
                 var followerEntity = new Entity(followerId, 1);
                 _repository.SetComponent(followerEntity, VehicleColor.Cyan);
                 
                 _repository.Bus.Publish(new CmdJoinFormation
                 {
                     Entity = followerEntity,
                     LeaderEntity = leaderEntity,
                     SlotIndex = i
                 });
             }
             
             Vector2 dest = startPos + new Vector2(200, 0);
             SetDestination(leaderId, dest, interpolation);
         }
    }
}
