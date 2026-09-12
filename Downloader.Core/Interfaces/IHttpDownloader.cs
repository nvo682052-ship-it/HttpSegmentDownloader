using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Downloader.Core.Interfaces
{
    public interface IHttpDownloader
    {
        Task<long> GetFileLengthAsync(string url, CancellationToken cancellationToken = default);
        Task DownloadSegmentAsync(string url, long startByte, long endByte, Stream destinationStream, IProgress<long> progress, CancellationToken cancellationToken = default);
    }
}