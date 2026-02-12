using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;
using Fdp.Examples.CarKinem.Simulation;
using Fdp.Examples.CarKinem.Rendering;
using Fdp.Examples.CarKinem.Input;
using Fdp.Examples.CarKinem.UI;
using CarKinem.Core;
using CarKinem.Formation;
using CarKinem.Spatial;
using CarKinem.Systems;

namespace Fdp.Examples.CarKinem
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("--headless"))
            {
                RunHeadless();
                return;
            }

            using var app = new CarKinemApp();
            app.Run();
        }

        static void RunHeadless()
        {
            Console.WriteLine("--- HEADLESS MODE START ---");
            using var sim = new DemoSimulation();
            
            // Spawn
            var eid = sim.SpawnVehicle(new System.Numerics.Vector2(0, 0), new System.Numerics.Vector2(1, 0));
            Console.WriteLine($"Spawned Entity {eid} at (0,0)");
            
            // Issue Command
            sim.IssueMoveToPointCommand(eid, new System.Numerics.Vector2(100, 0));
            Console.WriteLine("Issued Move To (100,0)");
            
            // Tick loop
            for (int i = 0; i < 60; i++)
            {
                sim.Tick(0.1f, 1.0f);
                var nav = sim.GetNavState(eid);
                
                if (i % 10 == 0)
                {
                    // Access detailed state for debugging
                    Console.WriteLine($"Tick {i}: NavMode={nav.Mode} TargetSpeed={nav.TargetSpeed:F2} Arrived={nav.HasArrived}");
                }
            }
            Console.WriteLine("--- HEADLESS MODE END ---");
        }
    }
}
