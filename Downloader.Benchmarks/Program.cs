using System;
using System.IO;
using System.Threading.Tasks;
using Downloader.Benchmarks.Benchmarks;

namespace Downloader.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  HTTP Segmented Downloader - Performance Benchmark Suite       ║");
            Console.WriteLine("║  Windows Thread Pool & Overlapped I/O Optimization Research   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Configuration
            string testDirectory = Path.Combine(Path.GetTempPath(), "DownloaderBenchmarks");
            string testFile = Path.Combine(testDirectory, "test_file.bin");
            long fileSize = 100 * 1024 * 1024; // 100MB test file
            int segmentCount = 4;

            // Create test directory if it doesn't exist
            if (!Directory.Exists(testDirectory))
                Directory.CreateDirectory(testDirectory);

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Test File: {testFile}");
            Console.WriteLine($"  File Size: {fileSize / (1024 * 1024)}MB");
            Console.WriteLine($"  Segments: {segmentCount}");
            Console.WriteLine($"  Processors: {Environment.ProcessorCount}");
            Console.WriteLine();

            try
            {
                // Display menu
                while (true)
                {
                    Console.WriteLine("Select Benchmark Suite to Run:");
                    Console.WriteLine("  1. Single-Thread Benchmarks (Baseline)");
                    Console.WriteLine("  2. Multi-Task Benchmarks (Standard Async)");
                    Console.WriteLine("  3. Thread Pool Benchmarks (Custom vs Default)");
                    Console.WriteLine("  4. Overlapped I/O Benchmarks (IOCP)");
                    Console.WriteLine("  5. Run All Benchmarks");
                    Console.WriteLine("  6. Exit");
                    Console.Write("\nSelect option (1-6): ");

                    string choice = Console.ReadLine();
                    Console.WriteLine();

                    switch (choice)
                    {
                        case "1":
                            await RunSingleThreadBenchmarks(testFile, fileSize);
                            break;

                        case "2":
                            await RunMultiTaskBenchmarks(testFile, fileSize, segmentCount);
                            break;

                        case "3":
                            await RunThreadPoolBenchmarks(testFile, fileSize, segmentCount);
                            break;

                        case "4":
                            await RunOverlappedIOBenchmarks(testFile, fileSize, segmentCount);
                            break;

                        case "5":
                            await RunAllBenchmarks(testFile, fileSize, segmentCount);
                            break;

                        case "6":
                            Console.WriteLine("Exiting benchmark suite...");
                            return;

                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }

                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
            finally
            {
                // Cleanup
                try
                {
                    if (Directory.Exists(testDirectory))
                        Directory.Delete(testDirectory, recursive: true);
                }
                catch { }
            }
        }

        private static async Task RunSingleThreadBenchmarks(string testFile, long fileSize)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Single-Thread Benchmarks (Baseline)                           ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            var benchmark = new SingleThreadBenchmark(testFile, fileSize);

            Console.WriteLine("Running sequential write benchmark...");
            var result1 = await benchmark.RunSequentialWriteAsync();
            PrintResult(result1);

            Console.WriteLine("\nRunning sequential read benchmark...");
            var result2 = await benchmark.RunSequentialReadAsync();
            PrintResult(result2);

            Console.WriteLine("\nRunning random access write benchmark...");
            var result3 = await benchmark.RunRandomAccessWriteAsync();
            PrintResult(result3);
        }

        private static async Task RunMultiTaskBenchmarks(string testFile, long fileSize, int segments)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Multi-Task Benchmarks (Standard Async)                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            var benchmark = new MultiTaskBenchmark(testFile, fileSize, segments);

            Console.WriteLine("Running parallel segment write benchmark...");
            var result1 = await benchmark.RunParallelSegmentWriteAsync();
            PrintResult(result1);

            Console.WriteLine("\nRunning parallel stream download benchmark...");
            var result2 = await benchmark.RunParallelStreamDownloadAsync();
            PrintResult(result2);

            Console.WriteLine("\nRunning adaptive segment count analysis...");
            var result3 = await benchmark.RunAdaptiveSegmentCountAsync();
            PrintResult(result3);
        }

        private static async Task RunThreadPoolBenchmarks(string testFile, long fileSize, int segments)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Thread Pool Benchmarks (Custom vs Default)                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            var benchmark = new ThreadPoolBenchmark(testFile, fileSize, segments);

            Console.WriteLine("Running custom thread pool benchmark...");
            var result1 = await benchmark.RunCustomThreadPoolAsync();
            PrintResult(result1);

            Console.WriteLine("\nRunning default thread pool benchmark...");
            var result2 = await benchmark.RunDefaultThreadPoolAsync();
            PrintResult(result2);

            Console.WriteLine("\nRunning thread pool comparison...");
            var result3 = await benchmark.RunComparisonAsync();
            PrintResult(result3);

            Console.WriteLine("\nRunning thread pool scalability test...");
            var result4 = await benchmark.RunScalabilityTestAsync();
            PrintResult(result4);
        }

        private static async Task RunOverlappedIOBenchmarks(string testFile, long fileSize, int segments)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Overlapped I/O Benchmarks (Windows IOCP)                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Note: OverlappedIOManager uses native Windows APIs and is Windows-only
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("⚠ Overlapped I/O benchmarks are only available on Windows.");
                return;
            }

            var benchmark = new OverlappedIOBenchmark(testFile, fileSize, segments);

            Console.WriteLine("Running overlapped I/O write benchmark...");
            var result1 = await benchmark.RunOverlappedWriteAsync();
            PrintResult(result1);

            Console.WriteLine("\nRunning overlapped I/O read benchmark...");
            var result2 = await benchmark.RunOverlappedReadAsync();
            PrintResult(result2);

            Console.WriteLine("\nRunning parallel overlapped I/O benchmark...");
            var result3 = await benchmark.RunParallelOverlappedIOAsync();
            PrintResult(result3);

            Console.WriteLine("\nRunning overlapped vs standard I/O comparison...");
            var result4 = await benchmark.RunComparisonAsync();
            PrintResult(result4);

            Console.WriteLine("\nRunning buffer size optimization analysis...");
            var result5 = await benchmark.RunBufferSizeOptimizationAsync();
            PrintResult(result5);
        }

        private static async Task RunAllBenchmarks(string testFile, long fileSize, int segments)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Running Complete Benchmark Suite                             ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            await RunSingleThreadBenchmarks(testFile, fileSize);
            Console.WriteLine("\nPress any key to continue to Multi-Task benchmarks...");
            Console.ReadKey();

            await RunMultiTaskBenchmarks(testFile, fileSize, segments);
            Console.WriteLine("\nPress any key to continue to Thread Pool benchmarks...");
            Console.ReadKey();

            await RunThreadPoolBenchmarks(testFile, fileSize, segments);

            if (OperatingSystem.IsWindows())
            {
                Console.WriteLine("\nPress any key to continue to Overlapped I/O benchmarks...");
                Console.ReadKey();
                await RunOverlappedIOBenchmarks(testFile, fileSize, segments);
            }

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  All Benchmarks Completed Successfully!                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        }

        private static void PrintResult(BenchmarkResult result)
        {
            Console.WriteLine();
            if (result.IsError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ {result.BenchmarkName}");
                Console.WriteLine($"   Error: {result.ErrorMessage}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ {result.BenchmarkName}");
                if (result.BytesProcessed > 0)
                {
                    Console.WriteLine($"   Duration: {result.Duration.TotalMilliseconds:F2} ms");
                    Console.WriteLine($"   Bytes Processed: {result.BytesProcessed:N0}");
                    Console.WriteLine($"   Throughput: {result.Throughput}");
                }
                else if (!string.IsNullOrEmpty(result.Throughput))
                {
                    Console.WriteLine($"   Throughput: {result.Throughput}");
                }

                if (!string.IsNullOrEmpty(result.Description))
                    Console.WriteLine($"   {result.Description}");

                Console.ResetColor();
            }
            Console.WriteLine();
        }
    }
}

