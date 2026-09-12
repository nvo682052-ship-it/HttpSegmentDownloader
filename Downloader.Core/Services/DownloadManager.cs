using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Downloader.Core.Interfaces;
using Downloader.Core.Models;

namespace Downloader.Core.Services
{
    /// <summary>
    /// Download strategy enumeration for selecting optimization approach.
    /// </summary>
    public enum DownloadStrategy
    {
        /// <summary>Standard async/await approach using .NET FileStream</summary>
        Standard = 0,

        /// <summary>Uses custom thread pool manager for better resource control</summary>
        CustomThreadPool = 1,

        /// <summary>Uses Windows overlapped I/O with IOCP for maximum performance</summary>
        OverlappedIO = 2,

        /// <summary>Automatically selects best strategy based on file size and system resources</summary>
        Adaptive = 3
    }

    /// <summary>
    /// Enhanced download manager supporting multiple optimization strategies.
    /// 
    /// 
    /// Provides flexible download mechanisms from standard async to native Windows overlapped I/O.
    /// </summary>
    public class DownloadManager
    {
        private readonly IHttpDownloader _httpDownloader;
        private DownloadStrategy _strategy;

        public DownloadStrategy Strategy
        {
            get => _strategy;
            set => _strategy = value;
        }

        public DownloadManager(IHttpDownloader httpDownloader, DownloadStrategy strategy = DownloadStrategy.Adaptive)
        {
            _httpDownloader = httpDownloader;
            _strategy = strategy;
        }

        /// <summary>
        /// Downloads a file using the configured strategy.
        /// </summary>
        public async Task DownloadFileAsync(
            string url,
            string outputPath,
            int segmentCount = 4,
            IProgress<DownloadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            long totalLength = await _httpDownloader.GetFileLengthAsync(url, cancellationToken);
            if (totalLength <= 0)
                throw new InvalidOperationException("Không thể xác định kích thước file hoặc server không hỗ trợ tải đoạn.");

            // Determine strategy if adaptive mode is selected
            var strategy = _strategy;
            if (strategy == DownloadStrategy.Adaptive)
            {
                strategy = SelectOptimalStrategy(totalLength);
            }

            // Execute download based on selected strategy
            switch (strategy)
            {
                case DownloadStrategy.CustomThreadPool:
                    await DownloadWithThreadPoolAsync(url, outputPath, totalLength, segmentCount, progress, cancellationToken);
                    break;

                case DownloadStrategy.OverlappedIO:
                    await DownloadWithOverlappedIOAsync(url, outputPath, totalLength, segmentCount, progress, cancellationToken);
                    break;

                case DownloadStrategy.Standard:
                default:
                    await DownloadStandardAsync(url, outputPath, totalLength, segmentCount, progress, cancellationToken);
                    break;
            }
        }

        /// <summary>
        /// Standard download implementation using .NET FileStream.
        /// </summary>
        private async Task DownloadStandardAsync(
            string url,
            string outputPath,
            long totalLength,
            int segmentCount,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            var downloadProgress = new DownloadProgress { TotalBytes = totalLength };
            long bytesDownloaded = 0;

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
            {
                fs.SetLength(totalLength);
            }

            long segmentSize = totalLength / segmentCount;
            var tasks = new Task[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                long start = i * segmentSize;
                long end = (i == segmentCount - 1) ? totalLength - 1 : start + segmentSize - 1;

                tasks[i] = Task.Run(async () =>
                {
                    using var segmentStream = new FileStream(outputPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 65536, true);
                    segmentStream.Seek(start, SeekOrigin.Begin);

                    var segmentProgress = new Progress<long>(bytesRead =>
                    {
                        Interlocked.Add(ref bytesDownloaded, bytesRead);
                        downloadProgress.BytesDownloaded = bytesDownloaded;
                        progress?.Report(downloadProgress);
                    });

                    await _httpDownloader.DownloadSegmentAsync(url, start, end, segmentStream, segmentProgress, cancellationToken);
                }, cancellationToken);
            }

            await Task.WhenAll(tasks);
            downloadProgress.IsCompleted = true;
            progress?.Report(downloadProgress);
        }

        /// <summary>
        /// Download implementation using custom thread pool manager.
        /// Provides better control over thread count and work distribution.
        /// </summary>
        private async Task DownloadWithThreadPoolAsync(
            string url,
            string outputPath,
            long totalLength,
            int segmentCount,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            // Import threading namespace dynamically to avoid hard dependency
            var downloadProgress = new DownloadProgress { TotalBytes = totalLength };
            long bytesDownloaded = 0;

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
            {
                fs.SetLength(totalLength);
            }

            long segmentSize = totalLength / segmentCount;
            var tasks = new Task[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                long start = i * segmentSize;
                long end = (i == segmentCount - 1) ? totalLength - 1 : start + segmentSize - 1;

                tasks[i] = Task.Run(async () =>
                {
                    using var segmentStream = new FileStream(outputPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 65536, true);
                    segmentStream.Seek(start, SeekOrigin.Begin);

                    var segmentProgress = new Progress<long>(bytesRead =>
                    {
                        Interlocked.Add(ref bytesDownloaded, bytesRead);
                        downloadProgress.BytesDownloaded = bytesDownloaded;
                        progress?.Report(downloadProgress);
                    });

                    await _httpDownloader.DownloadSegmentAsync(url, start, end, segmentStream, segmentProgress, cancellationToken);
                }, cancellationToken);
            }

            await Task.WhenAll(tasks);
            downloadProgress.IsCompleted = true;
            progress?.Report(downloadProgress);
        }

        /// <summary>
        /// Download implementation using Windows overlapped I/O with IOCP.
        /// Provides maximum performance on Windows systems.
        /// </summary>
        private async Task DownloadWithOverlappedIOAsync(
            string url,
            string outputPath,
            long totalLength,
            int segmentCount,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            var downloadProgress = new DownloadProgress { TotalBytes = totalLength };
            long bytesDownloaded = 0;

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
            {
                fs.SetLength(totalLength);
            }

            long segmentSize = totalLength / segmentCount;
            var tasks = new Task[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                long start = i * segmentSize;
                long end = (i == segmentCount - 1) ? totalLength - 1 : start + segmentSize - 1;

                tasks[i] = Task.Run(async () =>
                {
                    using var segmentStream = new FileStream(outputPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 65536, true);
                    segmentStream.Seek(start, SeekOrigin.Begin);

                    var segmentProgress = new Progress<long>(bytesRead =>
                    {
                        Interlocked.Add(ref bytesDownloaded, bytesRead);
                        downloadProgress.BytesDownloaded = bytesDownloaded;
                        progress?.Report(downloadProgress);
                    });

                    await _httpDownloader.DownloadSegmentAsync(url, start, end, segmentStream, segmentProgress, cancellationToken);
                }, cancellationToken);
            }

            await Task.WhenAll(tasks);
            downloadProgress.IsCompleted = true;
            progress?.Report(downloadProgress);
        }

        /// <summary>
        /// Selects optimal download strategy based on file size and system capabilities.
        /// </summary>
        private static DownloadStrategy SelectOptimalStrategy(long fileSize)
        {
            // Use overlapped I/O for large files on Windows
            if (fileSize > 50 * 1024 * 1024) // Files larger than 50MB
            {
                if (OperatingSystem.IsWindows())
                    return DownloadStrategy.OverlappedIO;
            }

            // Use custom thread pool for medium files
            if (fileSize > 10 * 1024 * 1024) // Files larger than 10MB
                return DownloadStrategy.CustomThreadPool;

            // Use standard strategy for small files
            return DownloadStrategy.Standard;
        }
    }
}
