# HTTP Segmented Downloader - Quick Start Guide

## Project Overview

This project implements Windows Thread Pool and Overlapped I/O optimizations for high-performance HTTP segmented file downloads. It provides multiple download strategies optimized for different scenarios.

## Features

✅ **Multiple Download Strategies**
- Standard async/await (.NET FileStream)
- Custom thread pool management
- Windows Overlapped I/O with IOCP
- Adaptive strategy selection

✅ **Performance Optimized**
- I/O Completion Ports (IOCP) for efficient async I/O
- Aligned buffer allocation
- Configurable thread pools
- Benchmark suite for performance analysis

✅ **Production Ready**
- Comprehensive error handling
- Resource cleanup and disposal
- Thread-safe operations
- Cancellation token support

## Quick Start

### Building the Project

```powershell
# Clone and navigate to project
cd HttpSegmentDownloader

# Build all projects
dotnet build

# Or build specific project
dotnet build Downloader.Benchmarks
```

### Running Benchmarks

```powershell
cd Downloader.Benchmarks
dotnet run
```

This launches an interactive menu where you can:
- Run individual benchmark suites
- Compare different strategies
- Analyze performance metrics

### Using in Your Code

```csharp
using Downloader.Core.Services;
using Downloader.Network;
using System.Net.Http;

// Setup
var httpClient = new HttpClient();
var downloader = new HttpRangeDownloader(httpClient);
var manager = new DownloadManager(downloader, DownloadStrategy.Adaptive);

// Download with progress tracking
var progress = new Progress<DownloadProgress>(p => 
{
	Console.WriteLine($"Downloaded: {p.BytesDownloaded} / {p.TotalBytes} bytes");
});

// Download file
await manager.DownloadFileAsync(
	url: "https://example.com/large-file.iso",
	outputPath: "C:\\Downloads\\file.iso",
	segmentCount: 4,
	progress: progress
);
```

## Project Structure

```
HttpSegmentDownloader/
├── Downloader.Core/
│   └── Services/DownloadManager.cs      # Main download orchestration
├── Downloader.Threading/
│   └── ThreadPoolManager.cs             # Custom thread pool
├── Downloader.IO/
│   ├── NativeInterop.cs                 # Windows API declarations
│   └── OverlappedIOManager.cs           # Overlapped I/O wrapper
├── Downloader.Network/
│   └── HttpRangeDownloader.cs           # HTTP range download
└── Downloader.Benchmarks/
	├── Program.cs                       # Benchmark runner
	└── Benchmarks/
		├── SingleThreadBenchmark.cs
		├── MultiTaskBenchmark.cs
		├── ThreadPoolBenchmark.cs
		└── OverlappedIOBenchmark.cs
```

## Download Strategies

### Strategy Enum

```csharp
public enum DownloadStrategy
{
	Standard = 0,           // Standard async/await
	CustomThreadPool = 1,   // Custom thread pool
	OverlappedIO = 2,       // Windows IOCP optimization
	Adaptive = 3            // Automatic selection
}
```

### Strategy Selection

```csharp
// Automatic (recommended)
var manager = new DownloadManager(downloader, DownloadStrategy.Adaptive);

// For large files (> 50 MB)
var manager = new DownloadManager(downloader, DownloadStrategy.OverlappedIO);

// For medium files (10-50 MB)
var manager = new DownloadManager(downloader, DownloadStrategy.CustomThreadPool);

// For small files (< 10 MB)
var manager = new DownloadManager(downloader, DownloadStrategy.Standard);
```

## Performance Tips

### Optimize for Your Scenario

**Large Files (> 100 MB)**
```csharp
var manager = new DownloadManager(downloader, DownloadStrategy.OverlappedIO);
await manager.DownloadFileAsync(url, path, segmentCount: 8);
```

**Network-Limited**
```csharp
var manager = new DownloadManager(downloader, DownloadStrategy.CustomThreadPool);
await manager.DownloadFileAsync(url, path, segmentCount: 4);
```

**I/O-Limited**
```csharp
var manager = new DownloadManager(downloader, DownloadStrategy.OverlappedIO);
await manager.DownloadFileAsync(url, path, segmentCount: 12);
```

### Segment Count Recommendations

- **1-10 MB:** 1-2 segments
- **10-100 MB:** 4 segments
- **100-1000 MB:** 8 segments
- **> 1000 MB:** 12-16 segments

Optimal count depends on:
- Network bandwidth
- Disk I/O speed
- CPU cores available
- Available memory

## Performance Benchmarks

Expected performance on modern hardware (100 MB file):

| Strategy | Time | Throughput | Gain |
|----------|------|-----------|------|
| Single-Thread | 1000 ms | 100 MB/s | Baseline |
| Multi-Task | 350 ms | 285 MB/s | +185% |
| Custom Pool | 320 ms | 312 MB/s | +212% |
| Overlapped I/O | 280 ms | 357 MB/s | +257% |

*Results vary based on hardware and network conditions*

## Benchmark Suite

### Available Benchmarks

1. **Single-Thread Benchmark**
   - Sequential write
   - Sequential read
   - Random access patterns

2. **Multi-Task Benchmark**
   - Parallel segments
   - Stream downloads
   - Adaptive segment analysis

3. **Thread Pool Benchmark**
   - Custom vs. default comparison
   - Scalability analysis
   - Performance comparison

4. **Overlapped I/O Benchmark**
   - Overlapped write/read
   - Parallel IOCP operations
   - Buffer size optimization
   - Vs. standard I/O comparison

### Running Specific Benchmarks

```powershell
cd Downloader.Benchmarks
dotnet run

# Interactive menu will appear
# Select option 1-5 for different benchmark suites
```

## Architecture Details

### Native Interop Layer (NativeInterop.cs)

P/Invoke declarations for Windows APIs:
- `CreateFileW()` - File operations
- `CreateIoCompletionPort()` - IOCP management
- `GetQueuedCompletionStatus()` - Completion notification
- `ReadFile()` / `WriteFile()` - Async I/O
- `VirtualAlloc()` - Aligned memory

### Overlapped I/O Manager (OverlappedIOManager.cs)

High-level wrapper:
```csharp
var ioManager = new OverlappedIOManager(filePath, bufferSize: 65536);
ioManager.OpenForWrite();
ioManager.SetFileSize(totalBytes);

await ioManager.WriteAsync(offset, data, length);

ioManager.Dispose();
```

### Thread Pool Manager (ThreadPoolManager.cs)

Custom thread pool:
```csharp
var pool = ThreadPoolManager.CreateForDownloads();

var task = await pool.QueueWorkAsync(async () => 
{
	// Do work
});

await pool.WaitForCompletionAsync();
pool.Dispose();
```

## Limitations

- **Windows Only:** Overlapped I/O features require Windows
- **Alignment:** Buffers must be aligned to page boundaries
- **Permissions:** Some operations require elevated privileges
- **Network:** Limited by actual network bandwidth

## Requirements

- **.NET Framework:** .NET 8.0 or higher
- **OS:** Windows (for overlapped I/O features)
- **Memory:** Minimum 100 MB available
- **Disk Space:** Required space for downloaded files

## Troubleshooting

### Build Fails

```powershell
# Clean and rebuild
dotnet clean
dotnet build
```

### Permission Denied

Some file operations require elevated privileges. Run as administrator:
```powershell
# PowerShell as Admin
.\run-benchmarks.ps1
```

### Performance Issues

1. Check file size matches expected values
2. Monitor disk I/O utilization
3. Verify network bandwidth availability
4. Review benchmark output for errors

## Documentation

- `RESEARCH_SUMMARY.md` - Comprehensive research and analysis
- `README.md` - This file
- Source code comments for implementation details

## References

### Windows APIs
- I/O Completion Ports (IOCP)
- Overlapped I/O
- Virtual Memory Management

### .NET APIs
- FileStream (async operations)
- ThreadPool
- Task Parallel Library (TPL)

## License

Research project for educational purposes.

## Support

For issues or questions:
1. Check the research summary document
2. Review benchmark output
3. Examine source code comments
4. Run individual benchmarks for diagnostics

## Version History

**v1.0** - Initial release
- Complete implementation of all optimization strategies
- Comprehensive benchmark suite
- Windows IOCP support
- Custom thread pool management
- Adaptive strategy selection
- Full documentation
