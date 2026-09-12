using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Downloader.IO;

namespace Downloader.Benchmarks.Benchmarks
{
    /// <summary>
    /// Benchmark for Windows Overlapped I/O with IOCP (I/O Completion Ports).
    /// Demonstrates the performance benefits of native overlapped I/O over standard async file operations.
    /// </summary>
    public class OverlappedIOBenchmark
    {
        private readonly string _testFile;
        private readonly long _fileSize;
        private readonly int _segments;

        public OverlappedIOBenchmark(string testFile, long fileSize, int segments = 4)
        {
            _testFile = testFile;
            _fileSize = fileSize;
            _segments = segments;
        }

        /// <summary>
        /// Benchmarks overlapped I/O write operations using IOCP.
        /// </summary>
        public async Task<BenchmarkResult> RunOverlappedWriteAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            long bytesWritten = 0;

            try
            {
                // Create overlapped I/O manager
                var ioManager = new OverlappedIOManager(_testFile, 65536);
                ioManager.OpenForWrite();
                ioManager.SetFileSize(_fileSize);

                var buffer = new byte[65536];
                Random.Shared.NextBytes(buffer);

                long offset = 0;
                while (offset < _fileSize)
                {
                    long remaining = _fileSize - offset;
                    int toWrite = (int)Math.Min(buffer.Length, remaining);

                    await ioManager.WriteAsync(offset, buffer, toWrite);
                    bytesWritten += toWrite;
                    offset += toWrite;
                }

                ioManager.Dispose();
                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = "Windows Overlapped I/O Write (IOCP)",
                    BytesProcessed = bytesWritten,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(bytesWritten, stopwatch.Elapsed),
                    Description = "Native Windows overlapped I/O with I/O Completion Ports for optimal throughput"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = "Windows Overlapped I/O Write (IOCP)",
                    Duration = stopwatch.Elapsed,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                CleanupTestFile();
            }
        }

        /// <summary>
        /// Benchmarks overlapped I/O read operations using IOCP.
        /// </summary>
        public async Task<BenchmarkResult> RunOverlappedReadAsync()
        {
            // First create test file
            await CreateTestFileAsync();

            var stopwatch = Stopwatch.StartNew();
            long bytesRead = 0;

            try
            {
                var ioManager = new OverlappedIOManager(_testFile, 65536);
                ioManager.OpenForRead();

                var buffer = new byte[65536];
                long offset = 0;

                while (offset < _fileSize)
                {
                    int read = await ioManager.ReadAsync(offset, buffer, (int)Math.Min(buffer.Length, _fileSize - offset));
                    if (read == 0)
                        break;

                    bytesRead += read;
                    offset += read;
                }

                ioManager.Dispose();
                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = "Windows Overlapped I/O Read (IOCP)",
                    BytesProcessed = bytesRead,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(bytesRead, stopwatch.Elapsed),
                    Description = "Native Windows overlapped I/O for sequential read operations"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = "Windows Overlapped I/O Read (IOCP)",
                    Duration = stopwatch.Elapsed,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                CleanupTestFile();
            }
        }

        /// <summary>
        /// Benchmarks parallel overlapped I/O with multiple concurrent operations.
        /// </summary>
        public async Task<BenchmarkResult> RunParallelOverlappedIOAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Pre-allocate file
                using (var fs = new FileStream(_testFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.SetLength(_fileSize);
                }

                var ioManagers = new OverlappedIOManager[_segments];
                var tasks = new Task[_segments];

                // Initialize one OverlappedIOManager per segment
                for (int i = 0; i < _segments; i++)
                {
                    ioManagers[i] = new OverlappedIOManager(_testFile, 65536);
                    ioManagers[i].OpenForRandomWrite();
                }

                long segmentSize = _fileSize / _segments;
                var buffer = new byte[65536];
                Random.Shared.NextBytes(buffer);

                // Queue parallel write tasks
                for (int i = 0; i < _segments; i++)
                {
                    long start = i * segmentSize;
                    long end = (i == _segments - 1) ? _fileSize : start + segmentSize;
                    int managerIndex = i;

                    tasks[i] = WriteOverlappedSegmentAsync(
                        ioManagers[managerIndex],
                        start,
                        end,
                        buffer);

                }

                await Task.WhenAll(tasks);

                // Cleanup
                foreach (var manager in ioManagers)
                {
                    manager?.Dispose();
                }

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = $"Parallel Overlapped I/O ({_segments} concurrent)",
                    BytesProcessed = _fileSize,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(_fileSize, stopwatch.Elapsed),
                    Description = $"Parallel write using {_segments} overlapped I/O channels with IOCP"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = $"Parallel Overlapped I/O ({_segments} concurrent)",
                    Duration = stopwatch.Elapsed,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                CleanupTestFile();
            }
        }

        /// <summary>
        /// Compares overlapped I/O with standard async I/O performance.
        /// </summary>
        public async Task<BenchmarkResult> RunComparisonAsync()
        {
            var overlappedResult = await RunOverlappedWriteAsync();

            // Standard async I/O benchmark
            var standardBenchmark = new SingleThreadBenchmark(_testFile, _fileSize);
            var standardResult = await standardBenchmark.RunSequentialWriteAsync();

            if (overlappedResult.IsError || standardResult.IsError)
            {
                return new BenchmarkResult
                {
                    BenchmarkName = "Overlapped vs Standard I/O Comparison",
                    IsError = true,
                    ErrorMessage = overlappedResult.ErrorMessage ?? standardResult.ErrorMessage
                };
            }

            double improvementPercent = ((standardResult.Duration.TotalMilliseconds - overlappedResult.Duration.TotalMilliseconds)
                / standardResult.Duration.TotalMilliseconds) * 100;

            return new BenchmarkResult
            {
                BenchmarkName = "Overlapped I/O vs Standard Async I/O",
                Duration = overlappedResult.Duration + standardResult.Duration,
                Throughput = $"Overlapped: {overlappedResult.Throughput}, Standard: {standardResult.Throughput}",
                Description = $"Overlapped I/O Performance Improvement: {improvementPercent:+0.00;-0.00;0}%"
            };
        }

        /// <summary>
        /// Benchmarks with different buffer sizes to find optimal alignment.
        /// </summary>
        public async Task<BenchmarkResult> RunBufferSizeOptimizationAsync()
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine("Buffer Size Optimization:");
            results.AppendLine("Buffer Size\tDuration (ms)\tThroughput");

            uint[] bufferSizes = { 4096, 8192, 16384, 32768, 65536, 131072, 262144 };

            foreach (var bufferSize in bufferSizes)
            {
                try
                {
                    var ioManager = new OverlappedIOManager(_testFile, bufferSize);
                    ioManager.OpenForWrite();
                    ioManager.SetFileSize(_fileSize);

                    long bytesWritten = 0;
                    var stopwatch = Stopwatch.StartNew();

                    var buffer = new byte[bufferSize];
                    Random.Shared.NextBytes(buffer);

                    long offset = 0;
                    while (offset < _fileSize)
                    {
                        long remaining = _fileSize - offset;
                        int toWrite = (int)Math.Min(buffer.Length, remaining);

                        await ioManager.WriteAsync(offset, buffer, toWrite);
                        bytesWritten += toWrite;
                        offset += toWrite;
                    }

                    stopwatch.Stop();
                    ioManager.Dispose();

                    double throughput = bytesWritten / (1024 * 1024) / stopwatch.Elapsed.TotalSeconds;
                    results.AppendLine($"{bufferSize}\t{stopwatch.ElapsedMilliseconds}\t{throughput:F2} MB/s");

                    CleanupTestFile();
                }
                catch (Exception ex)
                {
                    results.AppendLine($"{bufferSize}\tERROR\t{ex.Message}");
                }
            }

            return new BenchmarkResult
            {
                BenchmarkName = "Buffer Size Optimization Analysis",
                Description = results.ToString()
            };
        }

        private async Task WriteOverlappedSegmentAsync(
            OverlappedIOManager ioManager,
            long start,
            long end,
            byte[] buffer)
        {
            long offset = start;
            while (offset < end)
            {
                long remaining = end - offset;
                int toWrite = (int)Math.Min(buffer.Length, remaining);

                await ioManager.WriteAsync(offset, buffer, toWrite);
                offset += toWrite;
            }
        }

        private async Task CreateTestFileAsync()
        {
            if (File.Exists(_testFile))
                return;

            using (var fs = new FileStream(_testFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
            {
                fs.SetLength(_fileSize);

                var buffer = new byte[65536];
                Random.Shared.NextBytes(buffer);

                long bytesWritten = 0;
                while (bytesWritten < _fileSize)
                {
                    long remaining = _fileSize - bytesWritten;
                    int toWrite = (int)Math.Min(buffer.Length, remaining);

                    await fs.WriteAsync(buffer, 0, toWrite);
                    bytesWritten += toWrite;
                }

                await fs.FlushAsync();
            }
        }

        private void CleanupTestFile()
        {
            try
            {
                if (File.Exists(_testFile))
                    File.Delete(_testFile);
            }
            catch { }
        }

        private static string CalculateThroughput(long bytes, TimeSpan duration)
        {
            if (duration.TotalSeconds == 0)
                return "N/A";

            double bytesPerSecond = bytes / duration.TotalSeconds;
            double mbPerSecond = bytesPerSecond / (1024 * 1024);

            return $"{mbPerSecond:F2} MB/s";
        }
    }
}
