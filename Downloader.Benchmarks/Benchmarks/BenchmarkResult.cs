using System;

namespace Downloader.Benchmarks.Benchmarks
{
    /// <summary>
    /// Represents the result of a benchmark run.
    /// </summary>
    public class BenchmarkResult
    {
        public string BenchmarkName { get; set; }
        public long BytesProcessed { get; set; }
        public TimeSpan Duration { get; set; }
        public string Throughput { get; set; }
        public string Description { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            if (IsError)
                return $"[ERROR] {BenchmarkName}: {ErrorMessage}";

            return $"{BenchmarkName}\n" +
                   $"  Duration: {Duration.TotalMilliseconds:F2} ms\n" +
                   $"  Bytes: {BytesProcessed:N0}\n" +
                   $"  Throughput: {Throughput}\n" +
                   $"  Description: {Description}";
        }
    }
}
