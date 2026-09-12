using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Downloader.IO
{
    /// <summary>
    /// Provides asynchronous file I/O operations using Windows overlapped I/O mechanism.
    /// This enables more efficient I/O handling compared to standard async file operations.
    /// </summary>
    public class OverlappedIOManager : IDisposable
    {
        private SafeHandle _fileHandle;
        private SafeHandle _completionPort;
        private IntPtr _alignedBuffer;
        private uint _bufferSize;
        private bool _disposed;

        public string FilePath { get; }
        public uint BufferSize => _bufferSize;

        /// <summary>
        /// Initializes a new instance of OverlappedIOManager for async file operations.
        /// </summary>
        public OverlappedIOManager(string filePath, uint bufferSize = 65536)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// Opens a file for writing with overlapped I/O support.
        /// </summary>
        public void OpenForWrite()
        {
            ThrowIfDisposed();

            // Create or truncate the file
            _fileHandle = NativeInterop.CreateFileW(
                FilePath,
                NativeInterop.GENERIC_WRITE,
                NativeInterop.FILE_SHARE_READ,
                IntPtr.Zero,
                NativeInterop.CREATE_NEW,
                NativeInterop.FILE_FLAG_OVERLAPPED | NativeInterop.FILE_FLAG_SEQUENTIAL_SCAN,
                null);

            if (_fileHandle == null || _fileHandle.IsInvalid)
                throw new IOException($"Failed to open file '{FilePath}' for writing. Error: {Marshal.GetLastWin32Error()}");

            // Create completion port for this file
            _completionPort = NativeInterop.CreateIoCompletionPort(
                _fileHandle,
                null,
                (ulong)_fileHandle.GetHashCode(),
                (uint)Environment.ProcessorCount);

            if (_completionPort == null || _completionPort.IsInvalid)
                throw new IOException($"Failed to create I/O completion port. Error: {Marshal.GetLastWin32Error()}");

            // Allocate aligned buffer for optimal I/O performance
            AllocateAlignedBuffer();
        }

        /// <summary>
        /// Opens an existing file for writing at specific positions with overlapped I/O.
        /// </summary>
        public void OpenForRandomWrite()
        {
            ThrowIfDisposed();

            _fileHandle = NativeInterop.CreateFileW(
                FilePath,
                NativeInterop.GENERIC_WRITE,
                NativeInterop.FILE_SHARE_WRITE | NativeInterop.FILE_SHARE_READ,
                IntPtr.Zero,
                NativeInterop.OPEN_EXISTING,
                NativeInterop.FILE_FLAG_OVERLAPPED,
                null);

            if (_fileHandle == null || _fileHandle.IsInvalid)
                throw new IOException($"Failed to open file '{FilePath}' for random write. Error: {Marshal.GetLastWin32Error()}");

            _completionPort = NativeInterop.CreateIoCompletionPort(
                _fileHandle,
                null,
                (ulong)_fileHandle.GetHashCode(),
                (uint)Environment.ProcessorCount);

            if (_completionPort == null || _completionPort.IsInvalid)
                throw new IOException($"Failed to create I/O completion port. Error: {Marshal.GetLastWin32Error()}");

            AllocateAlignedBuffer();
        }

        /// <summary>
        /// Opens an existing file for reading with overlapped I/O support.
        /// </summary>
        public void OpenForRead()
        {
            ThrowIfDisposed();

            _fileHandle = NativeInterop.CreateFileW(
                FilePath,
                NativeInterop.GENERIC_READ,
                NativeInterop.FILE_SHARE_READ,
                IntPtr.Zero,
                NativeInterop.OPEN_EXISTING,
                NativeInterop.FILE_FLAG_OVERLAPPED | NativeInterop.FILE_FLAG_SEQUENTIAL_SCAN,
                null);

            if (_fileHandle == null || _fileHandle.IsInvalid)
                throw new IOException($"Failed to open file '{FilePath}' for reading. Error: {Marshal.GetLastWin32Error()}");

            _completionPort = NativeInterop.CreateIoCompletionPort(
                _fileHandle,
                null,
                (ulong)_fileHandle.GetHashCode(),
                (uint)Environment.ProcessorCount);

            if (_completionPort == null || _completionPort.IsInvalid)
                throw new IOException($"Failed to create I/O completion port. Error: {Marshal.GetLastWin32Error()}");

            AllocateAlignedBuffer();
        }

        /// <summary>
        /// Asynchronously writes data to the file at the specified position.
        /// </summary>
        public async Task WriteAsync(long fileOffset, byte[] buffer, int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (count > _bufferSize)
                throw new ArgumentException($"Count ({count}) exceeds buffer size ({_bufferSize})", nameof(count));

            // Copy data to aligned buffer
            Marshal.Copy(buffer, 0, _alignedBuffer, count);

            var overlapped = new NativeInterop.OVERLAPPED
            {
                Offset = (uint)(fileOffset & 0xFFFFFFFF),
                OffsetHigh = (uint)((fileOffset >> 32) & 0xFFFFFFFF),
                hEvent = IntPtr.Zero
            };

            // Initiate async write
            bool result = NativeInterop.WriteFile(
                _fileHandle,
                _alignedBuffer,
                (uint)count,
                out uint bytesWritten,
                ref overlapped);

            if (!result && Marshal.GetLastWin32Error() != 997) // ERROR_IO_PENDING
                throw new IOException($"WriteFile failed. Error: {Marshal.GetLastWin32Error()}");

            // Wait for completion
            await WaitForCompletionAsync(cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads data from the file at the specified position.
        /// </summary>
        public async Task<int> ReadAsync(long fileOffset, byte[] buffer, int count, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (count > _bufferSize)
                throw new ArgumentException($"Count ({count}) exceeds buffer size ({_bufferSize})", nameof(count));

            var overlapped = new NativeInterop.OVERLAPPED
            {
                Offset = (uint)(fileOffset & 0xFFFFFFFF),
                OffsetHigh = (uint)((fileOffset >> 32) & 0xFFFFFFFF),
                hEvent = IntPtr.Zero
            };

            // Initiate async read
            bool result = NativeInterop.ReadFile(
                _fileHandle,
                _alignedBuffer,
                (uint)count,
                out uint bytesRead,
                ref overlapped);

            if (!result && Marshal.GetLastWin32Error() != 997) // ERROR_IO_PENDING
                throw new IOException($"ReadFile failed. Error: {Marshal.GetLastWin32Error()}");

            // Wait for completion
            int completedBytes = await WaitForCompletionAsync(cancellationToken);

            // Copy data from aligned buffer to caller's buffer
            Marshal.Copy(_alignedBuffer, buffer, 0, completedBytes);

            return completedBytes;
        }

        /// <summary>
        /// Waits for I/O completion using the completion port.
        /// </summary>
        private async Task<int> WaitForCompletionAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                bool success = NativeInterop.GetQueuedCompletionStatus(
                    _completionPort,
                    out uint bytesTransferred,
                    out ulong completionKey,
                    out IntPtr overlapped,
                    NativeInterop.INFINITE);

                if (!success)
                    throw new IOException($"GetQueuedCompletionStatus failed. Error: {Marshal.GetLastWin32Error()}");

                return (int)bytesTransferred;
            }, cancellationToken);
        }

        /// <summary>
        /// Sets the file size for pre-allocation.
        /// </summary>
        public void SetFileSize(long sizeInBytes)
        {
            ThrowIfDisposed();

            if (!NativeInterop.SetFilePointerEx(_fileHandle, sizeInBytes, out long newPointer, 0))
                throw new IOException($"SetFilePointerEx failed. Error: {Marshal.GetLastWin32Error()}");
        }

        /// <summary>
        /// Allocates a buffer aligned to page boundary for optimal I/O performance.
        /// </summary>
        private void AllocateAlignedBuffer()
        {
            _alignedBuffer = NativeInterop.VirtualAlloc(
                IntPtr.Zero,
                (UIntPtr)_bufferSize,
                NativeInterop.MEM_COMMIT | NativeInterop.MEM_RESERVE,
                NativeInterop.PAGE_READWRITE);

            if (_alignedBuffer == IntPtr.Zero)
                throw new OutOfMemoryException($"Failed to allocate aligned buffer of size {_bufferSize}");
        }

        /// <summary>
        /// Frees the aligned buffer.
        /// </summary>
        private void FreeAlignedBuffer()
        {
            if (_alignedBuffer != IntPtr.Zero)
            {
                NativeInterop.VirtualFree(_alignedBuffer, (UIntPtr)_bufferSize, NativeInterop.MEM_RELEASE);
                _alignedBuffer = IntPtr.Zero;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            FreeAlignedBuffer();
            _fileHandle?.Dispose();
            _completionPort?.Dispose();
            _disposed = true;
        }
    }
}
