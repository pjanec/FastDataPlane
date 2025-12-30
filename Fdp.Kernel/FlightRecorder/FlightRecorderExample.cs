using System;
using System.IO;
using Fdp.Kernel;
using Fdp.Kernel.FlightRecorder;

namespace Fdp.Examples
{
    /// <summary>
    /// Example demonstrating Flight Recorder usage.
    /// Shows how to record and replay simulation state at 60Hz.
    /// </summary>
    public class FlightRecorderExample
    {
        public static void RecordSimulation()
        {
            // Create repository and register components
            using var repo = new EntityRepository();
            repo.RegisterComponent<Position>();
            repo.RegisterComponent<Velocity>();
            
            // Create some entities
            var entity1 = repo.CreateEntity();
            repo.AddComponent(entity1, new Position { X = 10, Y = 20, Z = 30 });
            repo.AddComponent(entity1, new Velocity { X = 1, Y = 0, Z = 0 });
            
            var entity2 = repo.CreateEntity();
            repo.AddComponent(entity2, new Position { X = 0, Y = 0, Z = 0 });
            
            // Start recording
            using var recorder = new AsyncRecorder("simulation.fdp");
            
            // Simulate 300 frames (5 seconds at 60Hz)
            for (int frame = 0; frame < 300; frame++)
            {
                repo.Tick(); // Increment global version
                
                // Simulate physics (every entity with velocity moves)
                var query = repo.Query()
                    .With<Position>()
                    .With<Velocity>()
                    .Build();
                
                query.ForEach((Entity e) =>
                {
                    ref var pos = ref repo.GetComponentRW<Position>(e);
                    ref readonly var vel = ref repo.GetComponentRO<Velocity>(e);
                    
                    pos.X += vel.X;
                    pos.Y += vel.Y;
                    pos.Z += vel.Z;
                });
                
                // Record keyframe every 60 frames (1 second)
                if (frame % 60 == 0)
                {
                    Console.WriteLine($"Recording keyframe at frame {frame}");
                    recorder.CaptureKeyframe(repo);
                }
                else
                {
                    // Record delta frame
                    uint prevTick = repo.GlobalVersion - 1;
                    recorder.CaptureFrame(repo, prevTick);
                }
                
                // Randomly destroy an entity to test destruction logging
                if (frame == 150 && repo.IsAlive(entity2))
                {
                    Console.WriteLine($"Destroying entity at frame {frame}");
                    repo.DestroyEntity(entity2);
                }
            }
            
            Console.WriteLine($"Recording complete. Frames recorded: {recorder.RecordedFrames}, Dropped: {recorder.DroppedFrames}");
        }
        
        public static void ReplaySimulation()
        {
            // Create fresh repository
            using var repo = new EntityRepository();
            repo.RegisterComponent<Position>();
            repo.RegisterComponent<Velocity>();
            
            // Open recording
            using var reader = new RecordingReader("simulation.fdp");
            
            Console.WriteLine($"Replay started. Recording version: {reader.FormatVersion}, Timestamp: {reader.RecordingTimestamp}");
            
            int frameCount = 0;
            while (reader.ReadNextFrame(repo))
            {
                frameCount++;
                
                // Print state every 60 frames
                if (frameCount % 60 == 0)
                {
                    Console.WriteLine($"Frame {frameCount}: Active entities: {repo.EntityCount}");
                    
                    // Print positions
                    var query = repo.Query().With<Position>().Build();
                    query.ForEach((Entity e) =>
                    {
                        ref readonly var pos = ref repo.GetComponentRO<Position>(e);
                        Console.WriteLine($"  Entity {e.Index}: Position ({pos.X}, {pos.Y}, {pos.Z})");
                    });
                }
            }
            
            Console.WriteLine($"Replay complete. Total frames: {frameCount}");
        }
    }
    
    // Example component types
    public struct Position
    {
        public float X, Y, Z;
    }
    
    public struct Velocity
    {
        public float X, Y, Z;
    }
}
