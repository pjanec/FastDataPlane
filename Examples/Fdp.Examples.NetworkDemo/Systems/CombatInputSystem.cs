using System;
using System.Numerics;
using Fdp.Kernel;
using Fdp.Examples.NetworkDemo.Components;
using Fdp.Examples.NetworkDemo.Events;
using FDP.Toolkit.Replication.Components;
using ModuleHost.Core.Abstractions;
using ModuleHost.Core.Network;
using FDP.Kernel.Logging;

namespace Fdp.Examples.NetworkDemo.Systems
{
    [UpdateInPhase(SystemPhase.Input)]
    public class CombatInputSystem : IModuleSystem
    {
        private readonly int _localNodeId;
        private readonly IEventBus _bus;

        public CombatInputSystem(int localNodeId, IEventBus bus)
        {
            _localNodeId = localNodeId;
            _bus = bus;
        }

        public void Execute(ISimulationView view, float dt)
        {
            // Check for SPACE key
            bool spacePressed = false;
            try
            {
                if (!Console.IsInputRedirected && Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Spacebar) spacePressed = true;
                }
            }
            catch { /* Ignore console errors in tests */ }

            if (!spacePressed) return;

            // 1. Find my tank (locally owned)
            var myTanks = view.Query()
                .With<NetworkOwnership>()
                .With<DemoPosition>()
                .Build();

            Entity myTank = Entity.Null;
            Vector3 myPos = Vector3.Zero;

            foreach (var e in myTanks)
            {
                ref readonly var own = ref view.GetComponentRO<NetworkOwnership>(e);
                if (own.PrimaryOwnerId == _localNodeId)
                {
                    myTank = e;
                    myPos = view.GetComponentRO<DemoPosition>(e).Value;
                    break;
                }
            }

            if (myTank == Entity.Null)
            {
                FdpLog<CombatInputSystem>.Warn("No local tank found to fire from");
                return;
            }

            // 2. Find nearest enemy
            Entity target = Entity.Null;
            float minDist = float.MaxValue;

            var enemies = view.Query()
                .With<NetworkOwnership>()
                .With<DemoPosition>()
                .Build();

            foreach (var e in enemies)
            {
                ref readonly var own = ref view.GetComponentRO<NetworkOwnership>(e);
                if (own.PrimaryOwnerId == _localNodeId) continue; // Skip self

                var pos = view.GetComponentRO<DemoPosition>(e).Value;
                float dist = Vector3.Distance(myPos, pos);

                if (dist < minDist && dist < 1000.0f) // Max range
                {
                    minDist = dist;
                    target = e;
                }
            }

            // 3. Fire event
            if (target != Entity.Null)
            {
                FdpLog<CombatInputSystem>.Info($"[Combat] Firing at target (Dist: {minDist:F1})");
                
                _bus.Publish(new FireInteractionEvent
                {
                    AttackerRoot = myTank,
                    TargetRoot = target,
                    WeaponInstanceId = 1, // Main gun
                    Damage = 10.0f
                });
            }
            else
            {
                FdpLog<CombatInputSystem>.Warn("No valid target in range");
            }
        }
    }
}
