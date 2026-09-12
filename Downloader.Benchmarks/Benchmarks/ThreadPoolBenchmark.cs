using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Downloader.Threading;

namespace Downloader.Benchmarks.Benchmarks
{
    /// <summary>
    /// Benchmark for custom thread pool manager performance.
    /// Compares custom thread pool with default .NET thread pool for download scenarios.
    /// </summary>
    public class ThreadPoolBenchmark
    {
        private readonly string _testFile;
        private readonly long _fileSize;
        private readonly int _segments;

        public ThreadPoolBenchmark(string testFile, long fileSize, int segments = 4)
        {
            _testFile = testFile;
            _fileSize = fileSize;
            _segments = segments;
        }

        /// <summary>
        /// Benchmarks custom thread pool with optimized segment downloads.
        /// </summary>
        public async Task<BenchmarkResult> RunCustomThreadPoolAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            var threadPool = ThreadPoolManager.CreateForDownloads();

            try
            {
                // Pre-allocate file
                using (var fs = new FileStream(_testFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.SetLength(_fileSize);
                }

                long segmentSize = _fileSize / _segments;

                // Queue all segments to custom thread pool
                var tasks = new Task[_segments];
                for (int i = 0; i < _segments; i++)
                {
                    long start = i * segmentSize;
                    long end = (i == _segments - 1) ? _fileSize : start + segmentSize;
                    long segmentLength = end - start;

                    int segmentIndex = i;
                    tasks[i] = threadPool.QueueWorkAsync(async () =>
                    {
                        await WriteSegmentAsync(_testFile, start, segmentLength);
                    });
                }

                await Task.WhenAll(tasks);
                await threadPool.WaitForCompletionAsync();

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = $"Custom ThreadPool ({_segments} segments)",
                    BytesProcessed = _fileSize,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(_fileSize, stopwatch.Elapsed),
                    Description = $"Download using custom thread pool manager with {_segments} parallel segments"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = $"Custom ThreadPool ({_segments} segments)",
                    Duration = stopwatch.Elapsed,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                threadPool?.Dispose();
                CleanupTestFile();
            }
        }

        /// <summary>
        /// Benchmarks default .NET thread pool using ThreadPool.QueueUserWorkItem.
        /// </summary>
        public async Task<BenchmarkResult> RunDefaultThreadPoolAsync()
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
                var completionSources = new TaskCompletionSource<bool>[_segments];

                for (int i = 0; i < _segments; i++)
                {
                    long start = i * segmentSize;
                    long end = (i == _segments - 1) ? _fileSize : start + segmentSize;
                    long segmentLength = end - start;

                    completionSources[i] = new TaskCompletionSource<bool>();
                    int taskIndex = i;

                    ThreadPool.QueueUserWorkItem(async state =>
                    {
                        try
                        {
                            await WriteSegmentAsync(_testFile, start, segmentLength);
                            completionSources[taskIndex].SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            completionSources[taskIndex].SetException(ex);
                        }
                    });
                }

                await Task.WhenAll(Array.ConvertAll(completionSources, tcs => tcs.Task));

                stopwatch.Stop();

                return new BenchmarkResult
                {
                    BenchmarkName = $"Default ThreadPool ({_segments} segments)",
                    BytesProcessed = _fileSize,
                    Duration = stopwatch.Elapsed,
                    Throughput = CalculateThroughput(_fileSize, stopwatch.Elapsed),
                    Description = $"Download using default .NET thread pool with {_segments} parallel segments"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new BenchmarkResult
                {
                    BenchmarkName = $"Default ThreadPool ({_segments} segments)",
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
        /// Compares performance of custom vs default thread pool side by side.
        /// </summary>
        public async Task<BenchmarkResult> RunComparisonAsync()
        {
            var customResult = await RunCustomThreadPoolAsync();
            var defaultResult = await RunDefaultThreadPoolAsync();

            if (customResult.IsError || defaultResult.IsError)
            {
                return new BenchmarkResult
                {
                    BenchmarkName = "ThreadPool Comparison",
                    IsError = true,
                    ErrorMessage = customResult.ErrorMessage ?? defaultResult.ErrorMessage
                };
            }

            double improvementPercent = ((defaultResult.Duration.TotalMilliseconds - customResult.Duration.TotalMilliseconds) 
                / defaultResult.Duration.TotalMilliseconds) * 100;

            return new BenchmarkResult
            {
                BenchmarkName = "ThreadPool Comparison (Custom vs Default)",
                Duration = customResult.Duration + defaultResult.Duration,
                Throughput = $"Custom: {customResult.Throughput}, Default: {defaultResult.Throughput}",
                Description = $"Improvement: {improvementPercent:+0.00;-0.00;0}% (Negative = custom pool is slower)"
            };
        }

        /// <summary>
        /// Tests thread pool scalability with increasing thread counts.
        /// </summary>
        public async Task<BenchmarkResult> RunScalabilityTestAsync()
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine("ThreadPool Scalability Test:");
            results.AppendLine("Threads\tDuration (ms)\tThroughput");

            for (int threads = 1; threads <= Math.Min(16, Environment.ProcessorCount * 2); threads *= 2)
            {
                var threadPool = new ThreadPoolManager(threads, threads);

                try
                {
                    var stopwatch = Stopwatch.StartNew();

                    long segmentSize = _fileSize / _segments;
                    var tasks = new Task[_segments];

                    for (int i = 0; i < _segments; i++)
                    {
                        long start = i * segmentSize;
                        long end = (i == _segments - 1) ? _fileSize : start + segmentSize;
                        long segmentLength = end - start;

                        tasks[i] = threadPool.QueueWorkAsync(async () =>
                        {
                            await WriteSegmentAsync(_testFile, start, segmentLength);
                        });
                    }

                    await Task.WhenAll(tasks);
                    await threadPool.WaitForCompletionAsync();

                    stopwatch.Stop();

                    double throughput = _fileSize / (1024 * 1024) / stopwatch.Elapsed.TotalSeconds;
                    results.AppendLine($"{threads}\t{stopwatch.ElapsedMilliseconds}\t{throughput:F2} MB/s");

                    CleanupTestFile();
                }
                finally
                {
                    threadPool?.Dispose();
                }
            }

            return new BenchmarkResult
            {
                BenchmarkName = "ThreadPool Scalability Analysis",
                Description = results.ToString()
            };
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
