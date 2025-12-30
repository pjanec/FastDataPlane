using System;

namespace Fdp.Kernel
{
    /// <summary>
    /// Untyped event stream for replay - stores raw bytes without generic type knowledge.
    /// Used when replaying events where we only know the element size, not the type.
    /// </summary>
    public unsafe class UntypedNativeEventStream : INativeEventStream, IDisposable
    {
        private byte* _currentBuffer;
        private long _currentSize;
        private int _currentCount;
        private readonly int _elementSize;
        private readonly int _typeId;

        public int EventTypeId => _typeId;
        public int ElementSize => _elementSize;

        public UntypedNativeEventStream(int typeId, int elementSize)
        {
            _typeId = typeId;
            _elementSize = elementSize;
            _currentSize = (long)elementSize * 1024; // Start with 1024 events
            _currentBuffer = (byte*)NativeMemoryAllocator.Reserve(_currentSize);
            NativeMemoryAllocator.Commit(_currentBuffer, _currentSize);
            _currentCount = 0;
        }

        public ReadOnlySpan<byte> GetRawBytes()
        {
            return new ReadOnlySpan<byte>(_currentBuffer, _currentCount * _elementSize);
        }

        public ReadOnlySpan<byte> GetPendingBytes()
        {
            // Untyped streams are read-only for replay, no pending data
            return ReadOnlySpan<byte>.Empty;
        }

        public void InjectIntoCurrent(ReadOnlySpan<byte> data)
        {
            int eventCount = data.Length / _elementSize;
            
            // Ensure capacity
            long requiredSize = (long)eventCount * _elementSize;
            if (requiredSize > _currentSize)
            {
                long newSize = requiredSize * 2;
                byte* newBuffer = (byte*)NativeMemoryAllocator.Reserve(newSize);
                NativeMemoryAllocator.Commit(newBuffer, newSize);
                
                // Free old
                NativeMemoryAllocator.Free(_currentBuffer, _currentSize);
                
                _currentBuffer = newBuffer;
                _currentSize = newSize;
            }
            
            // Copy data
            fixed (byte* src = data)
            {
                System.Buffer.MemoryCopy(src, _currentBuffer, _currentSize, data.Length);
            }
            
            _currentCount = eventCount;
        }

        public void ClearCurrent()
        {
            _currentCount = 0;
        }

        public void Swap()
        {
            // Untyped streams don't swap - they're replay-only
        }

        public void Clear()
        {
            _currentCount = 0;
        }

        public void Dispose()
        {
            if (_currentBuffer != null)
            {
                NativeMemoryAllocator.Free(_currentBuffer, _currentSize);
                _currentBuffer = null;
            }
            GC.SuppressFinalize(this);
        }

        ~UntypedNativeEventStream()
        {
            Dispose();
        }
    }
}
