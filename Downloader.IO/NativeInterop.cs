using System;
using System.Runtime.InteropServices;

namespace Downloader.IO
{
    /// <summary>
    /// Windows native API P/Invoke declarations for IOCP, overlapped I/O, and thread pool operations.
    /// This class provides low-level access to Windows optimized I/O and threading mechanisms.
    /// </summary>
    public static class NativeInterop
    {
        // Windows API Constants
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint OPEN_EXISTING = 3;
        public const uint OPEN_ALWAYS = 4;
        public const uint CREATE_NEW = 1;
        public const uint FILE_SHARE_READ = 1;
        public const uint FILE_SHARE_WRITE = 2;
        public const uint FILE_ATTRIBUTE_NORMAL = 128;
        public const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;

        // IOCP Constants
        public const uint INFINITE = 0xFFFFFFFF;

        // Thread Pool Constants
        public const uint WT_EXECUTEDEFAULT = 0;
        public const uint WT_EXECUTEINIOTHREAD = 1;
        public const uint WT_EXECUTEINPERSISTENTTHREAD = 80;
        public const uint WT_EXECUTELONGFUNCTION = 16;
        public const uint WT_TRANSFER_IMPERSONATION = 256;

        // File Handle (SafeHandle is used instead of IntPtr)
        [StructLayout(LayoutKind.Sequential)]
        public struct OVERLAPPED
        {
            public UIntPtr Internal;
            public UIntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr hEvent;
        }

        // I/O Completion Packet
        [StructLayout(LayoutKind.Sequential)]
        public struct IOCP_PACKET
        {
            public uint CompletionKey;
            public IntPtr Overlapped;
            public uint BytesTransferred;
        }

        // Thread Pool Work Structure
        public delegate void PTP_WORK_CALLBACK(IntPtr Instance, IntPtr Context, IntPtr Work);

        /// <summary>
        /// Creates or opens a file using Windows API with overlapped I/O support.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeHandle CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            SafeHandle hTemplateFile);

        /// <summary>
        /// Creates a completion port for I/O completion.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeHandle CreateIoCompletionPort(
            SafeHandle FileHandle,
            SafeHandle ExistingCompletionPort,
            ulong CompletionKey,
            uint NumberOfConcurrentThreads);

        /// <summary>
        /// Associates a file handle with an I/O completion port.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetFileCompletionNotificationModes(
            SafeHandle FileHandle,
            byte Flags);

        /// <summary>
        /// Waits for an I/O completion packet from a completion port.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetQueuedCompletionStatus(
            SafeHandle CompletionPort,
            out uint lpNumberOfBytesTransferred,
            out ulong lpCompletionKey,
            out IntPtr lpOverlapped,
            uint dwMilliseconds);

        /// <summary>
        /// Posts a user-defined I/O completion packet to a completion port.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool PostQueuedCompletionStatus(
            SafeHandle CompletionPort,
            uint dwNumberOfBytesToTransfer,
            ulong dwCompletionKey,
            IntPtr lpOverlapped);

        /// <summary>
        /// Performs asynchronous reading from a file handle with overlapped I/O.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            SafeHandle hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            ref OVERLAPPED lpOverlapped);

        /// <summary>
        /// Performs asynchronous writing to a file handle with overlapped I/O.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteFile(
            SafeHandle hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            ref OVERLAPPED lpOverlapped);

        /// <summary>
        /// Closes a file handle.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Gets the current thread count in the system thread pool.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetThreadpoolInfo(
            IntPtr ptpp,
            out uint pcbInfo);

        /// <summary>
        /// Creates a thread pool.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateThreadpool(
            IntPtr pEnviron);

        /// <summary>
        /// Creates a thread pool work object.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateThreadpoolWork(
            PTP_WORK_CALLBACK pfnwk,
            IntPtr pv,
            IntPtr pcbe);

        /// <summary>
        /// Submits a thread pool work object to the thread pool.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void SubmitThreadpoolWork(IntPtr pwk);

        /// <summary>
        /// Waits for all thread pool work objects to complete.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void WaitForThreadpoolWorkCallbacks(
            IntPtr pwk,
            bool fCancelPendingCallbacks);

        /// <summary>
        /// Closes a thread pool work object.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void CloseThreadpoolWork(IntPtr pwk);

        /// <summary>
        /// Closes a thread pool.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void CloseThreadpool(IntPtr ptpp);

        /// <summary>
        /// Gets the number of processors in the system.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern int GetActiveProcessorCount(uint GroupNumber);

        /// <summary>
        /// Gets system information about processor count and thread pool sizing.
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public uint dwOemId;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public UIntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }

        /// <summary>
        /// Sets file pointer position for random access I/O operations.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetFilePointerEx(
            SafeHandle hFile,
            long liDistanceToMove,
            out long lpNewFilePointer,
            uint dwMoveMethod);

        /// <summary>
        /// Allocates aligned memory for I/O operations.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flAllocationType,
            uint flProtect);

        /// <summary>
        /// Frees aligned memory previously allocated with VirtualAlloc.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFree(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType);

        // VirtualAlloc flags
        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_RELEASE = 0x8000;
        public const uint PAGE_READWRITE = 0x04;

        /// <summary>
        /// Creates an event object.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateEventW(
            IntPtr lpEventAttributes,
            bool bManualReset,
            bool bInitialState,
            string lpName);

        /// <summary>
        /// Sets an event object to the signaled state.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetEvent(IntPtr hEvent);

        /// <summary>
        /// Waits for an event to be signaled.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        public const uint WAIT_OBJECT_0 = 0;
        public const uint WAIT_TIMEOUT = 258;
    }
}
