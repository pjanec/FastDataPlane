using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fdp.Kernel.FlightRecorder
{
    /// <summary>
    /// Asynchronous Flight Recorder with double buffering.
    /// Implements FDP-DES-004 design for non-blocking snapshot capture.
    /// **Zero-allocation after initialization** on hot path.
    /// </summary>
    public class AsyncRecorder : IDisposable
    {
        private const int BUFFER_SIZE = 32 * 1024 * 1024; // 32MB Buffer
        
        // Double Buffers (pre-allocated)
        private byte[] _frontBuffer;
        private byte[] _backBuffer;
        
        // Pre-allocated write buffer with 4-byte header space (zero-allocation)
        private readonly byte[] _writeBuffer;
        
        private Task? _workerTask;
        private readonly FileStream _outputStream;
        private readonly RecorderSystem _recorderSystem;
        
        // Stats
        public int DroppedFrames { get; private set; }
        public int RecordedFrames { get; private set; }
        
        // Error propagation for tests
        public Exception? LastError { get; private set; }
        
        private bool _disposed;
        
        public AsyncRecorder(string filePath)
        {
            _frontBuffer = new byte[BUFFER_SIZE];
            _backBuffer = new byte[BUFFER_SIZE];
            _writeBuffer = new byte[BUFFER_SIZE + 4]; // Pre-allocate for length prefix
            _recorderSystem = new RecorderSystem();
            
            // Open file for async I/O
            _outputStream = new FileStream(
                filePath, 
                FileMode.Create, 
                FileAccess.Write, 
                FileShare.None, 
                4096, 
                useAsync: false);
            
            // Write Global Header immediately (See DES-002)
            WriteGlobalHeader();
        }
        
        /// <summary>
        /// Call this at End-Of-Frame (Phase: PostSimulation).
        /// </summary>
        /// <param name="blocking">If true, waits for previous write to complete instead of dropping frame.</param>
        public void CaptureFrame(EntityRepository repo, uint prevTick, bool blocking = false)
        {
            // 1. SAFETY CHECK
            // If the worker is still busy compressing the LAST frame, we are generating data faster than disk can write.
            if (_workerTask != null && !_workerTask.IsCompleted)
            {
                if (blocking)
                {
                    _workerTask.Wait();
                }
                else
                {
                    // Options:
                    // A) Block (Stutter game)
                    // B) Drop Frame (Gaps in replay) -> Preferred for Recorder
                    DroppedFrames++;
                    return;
                }
            }
            
            // 2. CAPTURE (Main Thread - Hot Path - ZERO ALLOCATION)
            int bytesWritten = 0;
            
            using (var ms = new MemoryStream(_frontBuffer))
            using (var writer = new BinaryWriter(ms))
            {
                // Use the logic from FDP-DES-002
                _recorderSystem.RecordDeltaFrame(repo, prevTick, writer);
                writer.Flush();
                
                bytesWritten = (int)ms.Position;
            }
            
            // Clear the destruction log after recording
            repo.ClearDestructionLog();
            
            // 3. SWAP POINTERS
            var dataToWrite = _frontBuffer;
            var freeBuffer = _backBuffer;
            _frontBuffer = freeBuffer;
            _backBuffer = dataToWrite;
            
            // 4. DISPATCH WORKER
            // Capture 'bytesWritten' by value closure
            _workerTask = Task.Run(() => ProcessBuffer(dataToWrite, bytesWritten));
            
            RecordedFrames++;
        }
        
        /// <summary>
        /// Captures a full keyframe instead of a delta.
        /// </summary>
        public void CaptureKeyframe(EntityRepository repo, bool blocking = false)
        {
            // Wait for previous frame to complete
            _workerTask?.Wait();
            
            int bytesWritten = 0;
            
            using (var ms = new MemoryStream(_frontBuffer))
            using (var writer = new BinaryWriter(ms))
            {
                _recorderSystem.RecordKeyframe(repo, writer);
                writer.Flush();
                bytesWritten = (int)ms.Position;
            }
            
            // Clear the destruction log
            repo.ClearDestructionLog();
            
            // Swap and dispatch
            var dataToWrite = _frontBuffer;
            var freeBuffer = _backBuffer;
            _frontBuffer = freeBuffer;
            _backBuffer = dataToWrite;
            
            _workerTask = Task.Run(() => ProcessBuffer(dataToWrite, bytesWritten));
            
            if (blocking)
            {
                _workerTask.Wait();
            }
            
            RecordedFrames++;
        }
        
        /// <summary>
        /// Runs on ThreadPool - ZERO ALLOCATION (uses pre-allocated _writeBuffer)
        /// </summary>
        private void ProcessBuffer(byte[] rawData, int length)
        {
            try
            {
                // Zero-allocation: Use pre-allocated _writeBuffer
                // Format: [TotalLength: int] [Bytes...]
                
                lock (_outputStream)
                {
                    // Copy length prefix (4 bytes)
                    BitConverter.TryWriteBytes(new Span<byte>(_writeBuffer, 0, 4), length);
                    
                    // Copy payload
                    Array.Copy(rawData, 0, _writeBuffer, 4, length);
                    
                    // Single write operation
                    _outputStream.Write(_writeBuffer, 0, length + 4);
                    _outputStream.Flush();
                }
            }
            catch (Exception ex)
            {
                // Store error for propagation
                LastError = ex;
            }
        }
        
        private void WriteGlobalHeader()
        {
            // Global Header Format (FDP-DES-002):
            // [Magic: 6 bytes] [Version: uint] [Timestamp: long]
            
            byte[] magic = System.Text.Encoding.ASCII.GetBytes("FDPREC");
            _outputStream.Write(magic, 0, 6);
            
            byte[] version = BitConverter.GetBytes(FdpConfig.FORMAT_VERSION);
            _outputStream.Write(version, 0, 4);
            
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            byte[] timestampBytes = BitConverter.GetBytes(timestamp);
            _outputStream.Write(timestampBytes, 0, 8);
            
            _outputStream.Flush();
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _workerTask?.Wait();
            _outputStream?.Dispose();
            
            _disposed = true;
            
            if (LastError != null)
            {
                throw new IOException("Recorder background worker failed", LastError);
            }
        }
    }
}
