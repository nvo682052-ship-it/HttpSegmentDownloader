namespace Downloader.Core.Models
{
    public class DownloadProgress
    {
        public long TotalBytes { get; set; }
        public long BytesDownloaded { get; set; }
        public double ProgressPercentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
        public double SpeedBytesPerSecond { get; set; }
        public bool IsCompleted { get; set; }
    }
}