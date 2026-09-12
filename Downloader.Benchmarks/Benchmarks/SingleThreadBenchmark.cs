using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Downloader.Benchmarks.Benchmarks
{
    /// <summary>
    /// Benchmark for single-threaded sequential download.
    /// This serves as the baseline for comparison with optimized approaches.
    /// </summary>
    public class SingleThreadBenchmark
    {
        private readonly string _testFile;
        private readonly long _fileSize;

        public SingleThreadBenchmark(string testFile, long fileSize)
        {
            _testFile = testFile;
            _fileSize = fileSize;
        }

        /// <summary>
        /// Simulates a single-threaded sequential write operation.
        /// </summary>
        public async Task<BenchmarkResult> RunSequentialWriteAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            long bytesWritten = 0;

            try
            {
                // Create test file
                using (var fs = new FileStream(_testFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
                {
                    fs.SetLength(_fileSize);

                    var buffer = new byte[65536]; // 64KB chunks
                    Random.Shared.NextBytes(buffer);

                    while (bytesWritten < _fileSize)
                    {
                        long remaining = _fileSize - bytesWritten;
                        int toWrite = (int)Math.Min(buffer.Length, remaining);

                        await fs.WriteAsync(buffer, 0, toWrite);
                        bytesWritten += toWrite;
                    }

                    await fs.FlushAsync();
                }

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = "Single-Threaded Sequential Write",
                    BytesProcessed = bytesWritten,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(bytesWritten, stopwatch.Elapsed),
                    Description = "Baseline single-threaded sequential write using async file operations"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = "Single-Threaded Sequential Write",
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
        /// Simulates a single-threaded sequential read operation.
        /// </summary>
        public async Task<BenchmarkResult> RunSequentialReadAsync()
        {
            // First create a test file
            await CreateTestFileAsync();

            var stopwatch = Stopwatch.StartNew();
            long bytesRead = 0;

            try
            {
                using (var fs = new FileStream(_testFile, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true))
                {
                    var buffer = new byte[65536];
                    int bytesReadThisTime;

                    while ((bytesReadThisTime = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        bytesRead += bytesReadThisTime;
                    }
                }

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = "Single-Threaded Sequential Read",
                    BytesProcessed = bytesRead,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(bytesRead, stopwatch.Elapsed),
                    Description = "Baseline single-threaded sequential read using async file operations"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = "Single-Threaded Sequential Read",
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
        /// Simulates single-threaded random access write pattern.
        /// </summary>
        public async Task<BenchmarkResult> RunRandomAccessWriteAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            long bytesWritten = 0;

            try
            {
                using (var fs = new FileStream(_testFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true))
                {
                    fs.SetLength(_fileSize);

                    var buffer = new byte[65536];
                    Random.Shared.NextBytes(buffer);

                    long position = 0;
                    while (position < _fileSize)
                    {
                        long remaining = _fileSize - position;
                        int toWrite = (int)Math.Min(buffer.Length, remaining);

                        fs.Seek(position, SeekOrigin.Begin);
                        await fs.WriteAsync(buffer, 0, toWrite);

                        bytesWritten += toWrite;
                        // Simulate non-sequential access pattern
                        position += toWrite;
                        if (position + 1048576 < _fileSize)
                            position += 1048576; // Skip 1MB
                    }

                    await fs.FlushAsync();
                }

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = "Single-Threaded Random Access Write",
                    BytesProcessed = bytesWritten,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(bytesWritten, stopwatch.Elapsed),
                    Description = "Single-threaded random access write pattern to test seek performance"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = "Single-Threaded Random Access Write",
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
