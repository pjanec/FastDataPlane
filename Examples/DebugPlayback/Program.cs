using System;
using System.IO;
using Fdp.Kernel;
using Fdp.Kernel.FlightRecorder;

class Program
{
    static void Main()
    {
        string testFile = Path.Combine(Path.GetTempPath(), $"debug_playback_{Guid.NewGuid()}.fdp");
        
        try
        {
            // Create test recording
            CreateTestRecording(testFile, frameCount: 20, keyframeInterval: 5);
            
            // Open and analyze
            using var controller = new PlaybackController(testFile);
            
            Console.WriteLine($"Total frames: {controller.TotalFrames}");
            Console.WriteLine("Frame -> Tick mapping:");
            
            for (int i = 0; i < Math.Min(controller.TotalFrames, 20); i++)
            {
                var metadata = controller.GetFrameMetadata(i);
                Console.WriteLine($"  Frame {i}: Tick {metadata.Tick}, Type: {metadata.FrameType}");
            }
            
            // Test SeekToTick step by step
            Console.WriteLine("\nTesting SeekToTick(10) step by step:");
            using var repo = new EntityRepository();
            repo.RegisterUnmanagedComponent<Position>();
            
            // Manually recreate the entity structure first
            var entity = repo.CreateEntity();
            repo.AddUnmanagedComponent(entity, new Position { X = -999, Y = -999, Z = -999 });
            
            Console.WriteLine("Initial state:");
            DebugHelpers.PrintEntityPosition(repo);
            
            // Try to apply individual frames
            Console.WriteLine("\nApplying Frame 5 (keyframe at tick 7):");
            // Reset repo to clean state for frame application
            DebugHelpers.ApplySingleFrame(controller, repo, 5);
            DebugHelpers.PrintEntityPosition(repo);
            
            Console.WriteLine("\nApplying Frame 6 (delta at tick 8):");
            DebugHelpers.ApplySingleFrame(controller, repo, 6);
            DebugHelpers.PrintEntityPosition(repo);
            
            Console.WriteLine("\nApplying Frame 8 (delta at tick 10):");
            DebugHelpers.ApplySingleFrame(controller, repo, 8);
            DebugHelpers.PrintEntityPosition(repo);
            
            // Now test the full SeekToTick
            Console.WriteLine("\nFull SeekToTick(10) test:");
            using var repo2 = new EntityRepository();
            repo2.RegisterUnmanagedComponent<Position>();
            
            controller.SeekToTick(repo2, 10);
            Console.WriteLine($"  Current frame after SeekToTick(10): {controller.CurrentFrame}");
            DebugHelpers.PrintEntityPosition(repo2);
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }
    
    static void CreateTestRecording(string filePath, int frameCount, int keyframeInterval)
    {
        using var repo = new EntityRepository();
        repo.RegisterUnmanagedComponent<Position>();
        
        var entity = repo.CreateEntity();
        repo.AddUnmanagedComponent(entity, new Position { X = 0, Y = 0, Z = 0 });
        
        using var recorder = new AsyncRecorder(filePath);
        uint prevTick = 0;
        
        Console.WriteLine("Recording frames:");
        
        for (int frame = 0; frame < frameCount; frame++)
        {
            // CRITICAL: Tick FIRST to advance to the new frame version
            repo.Tick();
            uint currentTick = repo.GlobalVersion;
            
            // NOW modify components - they will be tagged with the current version
            ref var pos = ref repo.GetUnmanagedComponentRW<Position>(entity);
            pos.X = frame;
            pos.Y = frame * 2;
            pos.Z = frame * 3;
            
            Console.WriteLine($"  Frame {frame}: GlobalVersion={currentTick}, Position.X={pos.X}");
            
            if (frame % keyframeInterval == 0)
            {
                recorder.CaptureKeyframe(repo, blocking: true);
                Console.WriteLine($"    -> Keyframe recorded");
            }
            else
            {
                // prevTick < currentTick, and modifications happened at currentTick
                // So the version check (currentTick > prevTick) will succeed
                recorder.CaptureFrame(repo, prevTick, blocking: true);
                Console.WriteLine($"    -> Delta recorded (prevTick={prevTick}, currentTick={currentTick})");
            }
            
            prevTick = currentTick;
        }
    }
}

public struct Position
{
    public float X, Y, Z;
}

static class DebugHelpers
{
    public static void ApplySingleFrame(PlaybackController controller, EntityRepository repo, int frameIndex)
    {
        var metadata = controller.GetFrameMetadata(frameIndex);
        Console.WriteLine($"Frame {frameIndex}: Tick {metadata.Tick}, Size {metadata.FrameSize}, Type {metadata.FrameType}");
        
        // This is tricky - we need access to the file stream that's inside the controller
        // For now, let's just print what we know and skip this part
        Console.WriteLine("    (Frame application skipped - need to implement proper access)");
    }
    
    public static void PrintEntityPosition(EntityRepository repo)
    {
        var query = repo.Query().With<Position>().Build();
        bool found = false;
        query.ForEach((Entity e) =>
        {
            ref readonly var pos = ref repo.GetUnmanagedComponentRO<Position>(e);
            Console.WriteLine($"    Entity {e.Index}: X={pos.X}, Y={pos.Y}, Z={pos.Z}");
            found = true;
        });
        
        if (!found)
        {
            Console.WriteLine("    No entities found");
        }
    }
}