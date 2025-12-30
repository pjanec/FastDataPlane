using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Fdp.Kernel;
using Fdp.Kernel.FlightRecorder;

namespace Fdp.Tests
{
    /// <summary>
    /// Integration tests for Flight Recorder components working together.
    /// Tests end-to-end scenarios combining RecorderSystem, AsyncRecorder, PlaybackSystem, and RecordingReader.
    /// </summary>
    public class FlightRecorderIntegrationTests : IDisposable
    {
        private readonly string _testFilePath;
        
        public FlightRecorderIntegrationTests()
        {
            _testFilePath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.fdp");
        }
        
        public void Dispose()
        {
            try { File.Delete(_testFilePath); } catch {}
        }

        #region Simple Integration Tests (Minimal Components)
        
        [Fact]
        public void SimpleRecordPlayback_SingleFrame_PreservesData()
        {
            // Test the most basic record → playback cycle
            using var sourceRepo = new EntityRepository();
            sourceRepo.RegisterUnmanagedComponent<int>();
            
            // Create initial state
            var e1 = sourceRepo.CreateEntity();
            sourceRepo.AddUnmanagedComponent(e1, 42);
            var e2 = sourceRepo.CreateEntity(); 
            sourceRepo.AddUnmanagedComponent(e2, 100);
            sourceRepo.Tick(); // V=2
            
            // Record keyframe
            using (var recorder = new AsyncRecorder(_testFilePath))
            {
                recorder.CaptureKeyframe(sourceRepo, blocking: true);
            }
            
            // Playback to fresh repo
            using var targetRepo = new EntityRepository();
            targetRepo.RegisterUnmanagedComponent<int>();
            
            using var reader = new RecordingReader(_testFilePath);
            bool frameLoaded = reader.ReadNextFrame(targetRepo);
            
            // Verify
            Assert.True(frameLoaded);
            Assert.Equal(2, targetRepo.GetEntityIndex().ActiveCount);
            Assert.Equal(42, targetRepo.GetUnmanagedComponentRO<int>(e1));
            Assert.Equal(100, targetRepo.GetUnmanagedComponentRO<int>(e2));
        }
        
        [Fact]
        public void SimpleRecordPlayback_DeltaFrame_PreservesChanges()
        {
            // Test keyframe + delta record → playback cycle
            using var sourceRepo = new EntityRepository();
            sourceRepo.RegisterUnmanagedComponent<int>();
            
            var e1 = sourceRepo.CreateEntity();
            sourceRepo.AddUnmanagedComponent(e1, 42);
            sourceRepo.Tick();
            
            // Record keyframe + delta
            using (var recorder = new AsyncRecorder(_testFilePath))
            {
                recorder.CaptureKeyframe(sourceRepo, blocking: true);
                
                // Make changes
                sourceRepo.Tick();
                sourceRepo.SetUnmanagedComponent(e1, 200);
                
                recorder.CaptureFrame(sourceRepo, sourceRepo.GlobalVersion - 1, blocking: true);
            }
            
            // Playback sequence
            using var targetRepo = new EntityRepository();
            targetRepo.RegisterUnmanagedComponent<int>();
            
            using var reader = new RecordingReader(_testFilePath);
            
            // Frame 1 (keyframe)
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(42, targetRepo.GetUnmanagedComponentRO<int>(e1));
            
            // Frame 2 (delta)
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(200, targetRepo.GetUnmanagedComponentRO<int>(e1));
        }
        
        [Fact]
        public void SimpleRecordPlayback_EntityDestruction_ReflectedInPlayback()
        {
            // Test entity destruction recording and playback
            using var sourceRepo = new EntityRepository();
            sourceRepo.RegisterUnmanagedComponent<int>();
            
            var e1 = sourceRepo.CreateEntity();
            var e2 = sourceRepo.CreateEntity();
            sourceRepo.AddUnmanagedComponent(e1, 10);
            sourceRepo.AddUnmanagedComponent(e2, 20);
            sourceRepo.Tick();
            
            using (var recorder = new AsyncRecorder(_testFilePath))
            {
                // Keyframe with 2 entities
                recorder.CaptureKeyframe(sourceRepo, blocking: true);
                
                // Destroy one entity
                sourceRepo.Tick();
                sourceRepo.DestroyEntity(e2);
                
                // Record delta
                recorder.CaptureFrame(sourceRepo, sourceRepo.GlobalVersion - 1, blocking: true);
            }
            
            // Playback
            using var targetRepo = new EntityRepository();
            targetRepo.RegisterUnmanagedComponent<int>();
            
            using var reader = new RecordingReader(_testFilePath);
            
            // Frame 1: Both entities exist
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(2, targetRepo.GetEntityIndex().ActiveCount);
            Assert.True(targetRepo.IsAlive(e1));
            Assert.True(targetRepo.IsAlive(e2));
            
            // Frame 2: e2 destroyed
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(1, targetRepo.GetEntityIndex().ActiveCount);
            Assert.True(targetRepo.IsAlive(e1));
            Assert.False(targetRepo.IsAlive(e2));
        }

        #endregion

        #region Medium Complexity Integration Tests
        
        [Fact]
        public void MultiFrameSequence_MixedOperations_MaintainsConsistency()
        {
            // Test a longer sequence with create, modify, destroy operations
            using var sourceRepo = new EntityRepository();
            sourceRepo.RegisterUnmanagedComponent<int>();
            sourceRepo.RegisterUnmanagedComponent<float>();
            
            // Declare entities at method scope for use in both recording and playback
            Entity e1, e2, e3;
            
            using (var recorder = new AsyncRecorder(_testFilePath))
            {
                // Frame 1: Initial entities
                e1 = sourceRepo.CreateEntity();
                e2 = sourceRepo.CreateEntity();
                sourceRepo.AddUnmanagedComponent(e1, 100);
                sourceRepo.AddUnmanagedComponent(e2, 200);
                sourceRepo.Tick();
                recorder.CaptureKeyframe(sourceRepo, blocking: true);
                
                // Frame 2: Add component to e1
                sourceRepo.Tick();
                sourceRepo.AddUnmanagedComponent(e1, 1.5f);
                recorder.CaptureFrame(sourceRepo, sourceRepo.GlobalVersion - 1, blocking: true);
                
                // Frame 3: Create new entity
                sourceRepo.Tick(); 
                e3 = sourceRepo.CreateEntity();
                sourceRepo.AddUnmanagedComponent(e3, 300);
                sourceRepo.AddUnmanagedComponent(e3, 3.14f);
                recorder.CaptureFrame(sourceRepo, sourceRepo.GlobalVersion - 1, blocking: true);
                
                // Frame 4: Modify and destroy
                sourceRepo.Tick();
                sourceRepo.SetUnmanagedComponent(e1, 150);
                sourceRepo.DestroyEntity(e2);
                recorder.CaptureFrame(sourceRepo, sourceRepo.GlobalVersion - 1, blocking: true);
            }
            
            // Playback and verify each frame
            using var targetRepo = new EntityRepository();
            targetRepo.RegisterUnmanagedComponent<int>();
            targetRepo.RegisterUnmanagedComponent<float>();
            
            using var reader = new RecordingReader(_testFilePath);
            
            // Frame 1: Initial state
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(2, targetRepo.GetEntityIndex().ActiveCount);
            Assert.Equal(100, targetRepo.GetUnmanagedComponentRO<int>(e1));
            Assert.Equal(200, targetRepo.GetUnmanagedComponentRO<int>(e2));
            Assert.False(targetRepo.HasUnmanagedComponent<float>(e1));
            
            // Frame 2: e1 gets float component
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(2, targetRepo.GetEntityIndex().ActiveCount);
            Assert.True(targetRepo.HasUnmanagedComponent<float>(e1));
            Assert.Equal(1.5f, targetRepo.GetUnmanagedComponentRO<float>(e1));
            
            // Frame 3: e3 created
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(3, targetRepo.GetEntityIndex().ActiveCount);
            Assert.True(targetRepo.IsAlive(e3));
            Assert.Equal(300, targetRepo.GetUnmanagedComponentRO<int>(e3));
            Assert.Equal(3.14f, targetRepo.GetUnmanagedComponentRO<float>(e3));
            
            // Frame 4: e1 modified, e2 destroyed
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(2, targetRepo.GetEntityIndex().ActiveCount);
            Assert.Equal(150, targetRepo.GetUnmanagedComponentRO<int>(e1));
            Assert.False(targetRepo.IsAlive(e2));
            Assert.True(targetRepo.IsAlive(e1));
            Assert.True(targetRepo.IsAlive(e3));
        }
        
        [Fact]
        public void IndexRepairIntegration_ImplicitEntityCreation_WorksEndToEnd()
        {
            // Test that PlaybackSystem properly handles implicit entity creation
            // when component data is received without explicit entity headers
            using var sourceRepo = new EntityRepository();
            sourceRepo.RegisterUnmanagedComponent<int>();
            
            // Create entities with gaps (to test sparse scenarios)
            var entities = new Entity[10];
            for (int i = 0; i < 10; i += 2) // 0, 2, 4, 6, 8
            {
                entities[i] = sourceRepo.CreateEntity();
                sourceRepo.AddUnmanagedComponent(entities[i], i * 10);
            }
            sourceRepo.Tick();
            
            // Record
            using (var recorder = new AsyncRecorder(_testFilePath))
            {
                recorder.CaptureKeyframe(sourceRepo, blocking: true);
            }
            
            // Playback to empty repo
            using var targetRepo = new EntityRepository();
            targetRepo.RegisterUnmanagedComponent<int>();
            
            using var reader = new RecordingReader(_testFilePath);
            Assert.True(reader.ReadNextFrame(targetRepo));
            
            // Verify sparse entities are correctly restored
            Assert.Equal(5, targetRepo.GetEntityIndex().ActiveCount);
            for (int i = 0; i < 10; i += 2)
            {
                Assert.True(targetRepo.IsAlive(entities[i]));
                Assert.Equal(i * 10, targetRepo.GetUnmanagedComponentRO<int>(entities[i]));
            }
            
            // Verify free list works (odd indices should be available)
            var newEntity = targetRepo.CreateEntity();
            Assert.True(newEntity.Index % 2 == 1, "New entity should use free slot from odd indices");
        }

        #endregion

        #region Error Handling Integration Tests
        
        [Fact]
        public void RecorderError_PlaybackAttempt_HandlesGracefully()
        {
            // Test what happens when playback tries to read corrupted/incomplete data
            using var sourceRepo = new EntityRepository();
            sourceRepo.RegisterUnmanagedComponent<int>();
            
            var e1 = sourceRepo.CreateEntity();
            sourceRepo.AddUnmanagedComponent(e1, 42);
            sourceRepo.Tick();
            
            // Record partial frame then corrupt file
            using (var recorder = new AsyncRecorder(_testFilePath))
            {
                recorder.CaptureKeyframe(sourceRepo, blocking: true);
            }
            
            // Corrupt the file by truncating it
            var originalBytes = File.ReadAllBytes(_testFilePath);
            File.WriteAllBytes(_testFilePath, originalBytes.AsSpan(0, originalBytes.Length / 2).ToArray());
            
            // Attempt playback
            using var targetRepo = new EntityRepository();
            targetRepo.RegisterUnmanagedComponent<int>();
            
            using var reader = new RecordingReader(_testFilePath);
            
            // Should handle corruption gracefully by returning false (not throwing exception)
            bool frameLoaded = reader.ReadNextFrame(targetRepo);
            
            Assert.False(frameLoaded, "Should return false when file is corrupted/truncated");
        }

        #endregion

        #region Performance Integration Tests
        
        [Fact]
        public void HighVolumeRecordPlayback_MaintainsPerformance()
        {
            // Test recording and playback with many entities and frames
            const int entityCount = 1000;
            const int frameCount = 10;
            
            using var sourceRepo = new EntityRepository();
            sourceRepo.RegisterUnmanagedComponent<int>();
            
            // Create many entities
            var entities = new Entity[entityCount];
            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = sourceRepo.CreateEntity();
                sourceRepo.AddUnmanagedComponent(entities[i], i);
            }
            sourceRepo.Tick();
            
            var startTime = DateTime.UtcNow;
            
            // Record many frames
            using (var recorder = new AsyncRecorder(_testFilePath))
            {
                recorder.CaptureKeyframe(sourceRepo, blocking: true);
                
                for (int frame = 1; frame < frameCount; frame++)
                {
                    sourceRepo.Tick();
                    
                    // Modify some entities
                    for (int i = 0; i < entityCount; i += 10)
                    {
                        sourceRepo.SetUnmanagedComponent(entities[i], entities[i].Index * frame);
                    }
                    
                    recorder.CaptureFrame(sourceRepo, sourceRepo.GlobalVersion - 1, blocking: true);
                }
            }
            
            var recordTime = DateTime.UtcNow - startTime;
            Assert.True(recordTime.TotalSeconds < 5.0, $"Recording took too long: {recordTime.TotalSeconds}s");
            
            // Verify playback performance
            startTime = DateTime.UtcNow;
            
            using var targetRepo = new EntityRepository();
            targetRepo.RegisterUnmanagedComponent<int>();
            
            using var reader = new RecordingReader(_testFilePath);
            int framesRead = 0;
            while (reader.ReadNextFrame(targetRepo))
            {
                framesRead++;
                if (framesRead >= frameCount) break; // Safety limit
            }
            
            var playbackTime = DateTime.UtcNow - startTime;
            Assert.True(playbackTime.TotalSeconds < 2.0, $"Playback took too long: {playbackTime.TotalSeconds}s");
            Assert.Equal(frameCount, framesRead);
            Assert.Equal(entityCount, targetRepo.GetEntityIndex().ActiveCount);
        }

        #endregion

        #region Cross-Component Integration Tests
        
        [Fact]
        public void ManagedComponentIntegration_RecordPlayback_PreservesComplexData()
        {
            // Test with both unmanaged and managed components
            using var sourceRepo = new EntityRepository();
            sourceRepo.RegisterUnmanagedComponent<int>();
            sourceRepo.RegisterManagedComponent<TestManagedComponent>();
            
            var e1 = sourceRepo.CreateEntity();
            sourceRepo.AddUnmanagedComponent(e1, 42);
            sourceRepo.AddManagedComponent(e1, new TestManagedComponent { Value = "Hello", Count = 123 });
            sourceRepo.Tick();
            
            // Record
            using (var recorder = new AsyncRecorder(_testFilePath))
            {
                recorder.CaptureKeyframe(sourceRepo, blocking: true);
                
                // Modify managed component
                sourceRepo.Tick();
                var managed = sourceRepo.GetManagedComponentRW<TestManagedComponent>(e1);
                managed.Value = "Modified";
                managed.Count = 456;
                
                recorder.CaptureFrame(sourceRepo, sourceRepo.GlobalVersion - 1, blocking: true);
            }
            
            // Playback
            using var targetRepo = new EntityRepository();
            targetRepo.RegisterUnmanagedComponent<int>();
            targetRepo.RegisterManagedComponent<TestManagedComponent>();
            
            using var reader = new RecordingReader(_testFilePath);
            
            // Frame 1: Initial values
            Assert.True(reader.ReadNextFrame(targetRepo));
            Assert.Equal(42, targetRepo.GetUnmanagedComponentRO<int>(e1));
            var managedComp1 = targetRepo.GetManagedComponentRO<TestManagedComponent>(e1);
            Assert.Equal("Hello", managedComp1.Value);
            Assert.Equal(123, managedComp1.Count);
            
            // Frame 2: Modified values  
            Assert.True(reader.ReadNextFrame(targetRepo));
            var managedComp2 = targetRepo.GetManagedComponentRO<TestManagedComponent>(e1);
            Assert.Equal("Modified", managedComp2.Value);
            Assert.Equal(456, managedComp2.Count);
        }

        #endregion
    }
    
    // Test component for managed component integration tests
    [MessagePack.MessagePackObject]
    public class TestManagedComponent
    {
        [MessagePack.Key(0)]
        public string Value { get; set; } = string.Empty;
        
        [MessagePack.Key(1)] 
        public int Count { get; set; }
    }
}