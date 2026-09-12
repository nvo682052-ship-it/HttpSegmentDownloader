using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Downloader.Benchmarks.Benchmarks
{
    /// <summary>
    /// Benchmark for multi-task parallel downloads using Task.Run.
    /// Demonstrates standard .NET async/await parallelism without native I/O optimization.
    /// </summary>
    public class MultiTaskBenchmark
    {
        private readonly string _testFile;
        private readonly long _fileSize;
        private readonly int _segments;

        public MultiTaskBenchmark(string testFile, long fileSize, int segments = 4)
        {
            _testFile = testFile;
            _fileSize = fileSize;
            _segments = segments;
        }

        /// <summary>
        /// Simulates multi-threaded download by writing segments in parallel.
        /// </summary>
        public async Task<BenchmarkResult> RunParallelSegmentWriteAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Pre-allocate file
                using (var fs = new FileStream(_testFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.SetLength(_fileSize);
                }

                long segmentSize = _fileSize / _segments;
                var tasks = new Task[_segments];

                // Create parallel write tasks for each segment
                for (int i = 0; i < _segments; i++)
                {
                    long start = i * segmentSize;
                    long end = (i == _segments - 1) ? _fileSize : start + segmentSize;
                    long segmentLength = end - start;

                    tasks[i] = WriteSegmentAsync(_testFile, start, segmentLength);
                }

                await Task.WhenAll(tasks);

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = $"Multi-Task Parallel Write ({_segments} segments)",
                    BytesProcessed = _fileSize,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(_fileSize, stopwatch.Elapsed),
                    Description = $"Standard async/await parallel download using Task.Run with {_segments} segments"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = $"Multi-Task Parallel Write ({_segments} segments)",
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
        /// Simulates multi-threaded download with smaller buffer segments.
        /// </summary>
        public async Task<BenchmarkResult> RunParallelStreamDownloadAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Pre-allocate file
                using (var fs = new FileStream(_testFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.SetLength(_fileSize);
                }

                long segmentSize = _fileSize / _segments;
                var buffer = new byte[65536];
                Random.Shared.NextBytes(buffer);

                var tasks = new Task[_segments];

                for (int i = 0; i < _segments; i++)
                {
                    long start = i * segmentSize;
                    long end = (i == _segments - 1) ? _fileSize : start + segmentSize;

                    tasks[i] = WriteStreamSegmentAsync(_testFile, start, end, buffer);
                }

                await Task.WhenAll(tasks);

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = $"Multi-Task Parallel Stream Download ({_segments} segments)",
                    BytesProcessed = _fileSize,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(_fileSize, stopwatch.Elapsed),
                    Description = $"Parallel stream download with {_segments} concurrent tasks using standard FileStream"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = $"Multi-Task Parallel Stream Download ({_segments} segments)",
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
        /// Benchmarks with varying segment counts to find optimal parallelism level.
        /// </summary>
        public async Task<BenchmarkResult> RunAdaptiveSegmentCountAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Test with different segment counts
                int optimalSegments = _segments;
                TimeSpan bestDuration = TimeSpan.MaxValue;

                for (int testSegments = 1; testSegments <= Math.Min(_segments * 2, Environment.ProcessorCount * 2); testSegments++)
                {
                    var benchmark = new MultiTaskBenchmark(_testFile, _fileSize, testSegments);
                    var result = await benchmark.RunParallelSegmentWriteAsync();

                    if (!result.IsError && result.Duration < bestDuration)
                    {
                        bestDuration = result.Duration;
                        optimalSegments = testSegments;
                    }
                }

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = "Adaptive Segment Count Analysis",
                    Duration = stopwatch.Elapsed,
                    Throughput = optimalSegments.ToString(),
                    Description = $"Found optimal segment count: {optimalSegments} for {_fileSize / (1024 * 1024)}MB file"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = "Adaptive Segment Count Analysis",
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

        private async Task WriteSegmentAsync(string filePath, long offset, long length)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 65536, useAsync: true))
            {
                fs.Seek(offset, SeekOrigin.Begin);

                var buffer = new byte[65536];
                Random.Shared.NextBytes(buffer);

                long written = 0;
                while (written < length)
                {
                    long remaining = length - written;
                    int toWrite = (int)Math.Min(buffer.Length, remaining);

                    await fs.WriteAsync(buffer, 0, toWrite);
                    written += toWrite;
                }
            }
        }

        private async Task WriteStreamSegmentAsync(string filePath, long start, long end, byte[] buffer)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 65536, useAsync: true))
            {
                fs.Seek(start, SeekOrigin.Begin);

                long pos = start;
                while (pos < end)
                {
                    long remaining = end - pos;
                    int toWrite = (int)Math.Min(buffer.Length, remaining);

                    await fs.WriteAsync(buffer, 0, toWrite);
                    pos += toWrite;
                }
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
