using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Downloader.Core.Interfaces;

namespace Downloader.Network
{
    public class HttpRangeDownloader : IHttpDownloader
    {
        private readonly HttpClient _httpClient;

        public HttpRangeDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<long> GetFileLengthAsync(string url, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.Content.Headers.ContentLength ?? -1;
        }

        public async Task DownloadSegmentAsync(string url, long startByte, long endByte, Stream destinationStream, IProgress<long> progress, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(startByte, endByte);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new byte[81920]; // Buffer 80KB
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                progress?.Report(bytesRead);
            }
        }
    }
}