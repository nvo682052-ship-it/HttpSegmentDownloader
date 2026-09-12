using System.Diagnostics;
using System.Runtime.InteropServices;
using Downloader.Core.Models;
using Downloader.Core.Services;
using Downloader.Network;

namespace Downloader.App
{
    public partial class MainForm : Form
    {
        private DownloadManager? _downloadManager;
        private HttpClient? _httpClient;
        private bool _isDownloading;
        private CancellationTokenSource? _cancellationSource;

        private TimeSpan _lastProcessorTime;
        private DateTime _lastCpuSampleUtc;
        private ulong _lastWriteTransferCount;
        private int _peakThreadCount;
        private double _peakCpuPercent;
        private double _peakDiskWriteMBps;

        public MainForm()
        {
            InitializeComponent();
            InitializeDownloadManager();
            strategyCombo.SelectedIndex = 0;
            ResetMetricBaselines();
            UpdateLiveMetrics();
        }

        private void InitializeDownloadManager()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(30)
            };

            _downloadManager = new DownloadManager(
                new HttpRangeDownloader(_httpClient),
                DownloadStrategy.Adaptive);
        }

        private void browseButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file",
                Filter = "Tất cả file (*.*)|*.*",
                InitialDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads")
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                pathTextBox.Text = dialog.FileName;
            }
        }

        private async void downloadButton_Click(object? sender, EventArgs e)
        {
            await StartDownloadAsync();
        }

        private void cancelButton_Click(object? sender, EventArgs e)
        {
            CancelDownload();
        }

        private void metricsTimer_Tick(object? sender, EventArgs e)
        {
            UpdateLiveMetrics();
        }

        private async Task StartDownloadAsync()
        {
            if (_isDownloading)
            {
                LogMessage("Đang có một tác vụ tải xuống đang chạy!", Color.Yellow);
                return;
            }

            string url = urlTextBox.Text.Trim();
            string path = pathTextBox.Text.Trim();
            int segments = (int)segmentsUpDown.Value;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(path))
            {
                LogMessage("Vui lòng nhập URL và đường dẫn lưu file!", Color.Red);
                return;
            }

            DownloadStrategy strategy = strategyCombo.SelectedIndex switch
            {
                1 => DownloadStrategy.Standard,
                2 => DownloadStrategy.CustomThreadPool,
                3 => DownloadStrategy.OverlappedIO,
                _ => DownloadStrategy.Adaptive
            };

            _downloadManager = new DownloadManager(
                new HttpRangeDownloader(_httpClient!),
                strategy);

            _cancellationSource?.Dispose();
            _cancellationSource = new CancellationTokenSource();

            _isDownloading = true;
            downloadButton.Enabled = false;
            cancelButton.Enabled = true;
            progressBar.Value = 0;
            statusLabel.Text = "Đang tải...";

            _peakThreadCount = 0;
            _peakCpuPercent = 0;
            _peakDiskWriteMBps = 0;
            ResetMetricBaselines();
            metricsTimer.Start();
            UpdateLiveMetrics();

            LogMessage($"Bắt đầu tải từ: {url}", Color.Cyan);
            LogMessage($"Chiến lược: {strategy}", Color.Cyan);
            LogMessage($"Số phân đoạn: {segments}", Color.Cyan);
            LogMessage("Bắt đầu đo luồng OS / CPU / ghi đĩa mỗi 500ms.", Color.Gray);
            LogMessage("---", Color.Gray);

            var stopwatch = Stopwatch.StartNew();
            long totalBytes = 0;
            long downloadedBytes = 0;

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    totalBytes = p.TotalBytes;
                    downloadedBytes = p.BytesDownloaded;

                    int percent = p.TotalBytes > 0
                        ? (int)Math.Min(100, (p.BytesDownloaded * 100) / p.TotalBytes)
                        : 0;
                    progressBar.Value = percent;

                    string downloadedMb = (p.BytesDownloaded / (1024d * 1024d)).ToString("F2");
                    string totalMb = (p.TotalBytes / (1024d * 1024d)).ToString("F2");
                    statusLabel.Text = $"Tiến trình: {percent}% ({downloadedMb}/{totalMb} MB)";
                });

                await _downloadManager.DownloadFileAsync(
                    url,
                    path,
                    segments,
                    progress,
                    _cancellationSource.Token);

                stopwatch.Stop();

                if (totalBytes <= 0 && File.Exists(path))
                {
                    totalBytes = new FileInfo(path).Length;
                    downloadedBytes = totalBytes;
                }

                double elapsedSeconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
                double downloadedMb = downloadedBytes / (1024d * 1024d);
                double averageSpeedMBps = downloadedMb / elapsedSeconds;

                progressBar.Value = 100;
                statusLabel.Text = "Hoàn thành";
                LogMessage("Tải xuống hoàn thành!", Color.Lime);
                LogMessage($"Tổng thời gian tải: {elapsedSeconds:F2} giây", Color.Lime);
                LogMessage($"Tốc độ trung bình: {averageSpeedMBps:F2} MB/s", Color.Lime);
                LogMessage($"Dung lượng: {downloadedMb:F2} MB", Color.Lime);
                LogMessage($"Đỉnh luồng OS: {_peakThreadCount}", Color.Aqua);
                LogMessage($"Đỉnh CPU process: {_peakCpuPercent:F1} %", Color.Aqua);
                LogMessage($"Đỉnh ghi đĩa: {_peakDiskWriteMBps:F2} MB/s", Color.Aqua);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                statusLabel.Text = "Đã hủy";
                LogMessage($"Tác vụ đã bị hủy sau {stopwatch.Elapsed.TotalSeconds:F2} giây.", Color.Yellow);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                statusLabel.Text = "Lỗi";
                LogMessage($"Lỗi: {ex.Message}", Color.Red);
            }
            finally
            {
                metricsTimer.Stop();
                UpdateLiveMetrics();
                _isDownloading = false;
                downloadButton.Enabled = true;
                cancelButton.Enabled = false;
            }
        }

        private void CancelDownload()
        {
            if (!_isDownloading)
            {
                return;
            }

            LogMessage("Đang hủy tác vụ...", Color.Yellow);
            _cancellationSource?.Cancel();
        }

        private void ResetMetricBaselines()
        {
            using Process process = Process.GetCurrentProcess();
            process.Refresh();
            _lastProcessorTime = process.TotalProcessorTime;
            _lastCpuSampleUtc = DateTime.UtcNow;
            _lastWriteTransferCount = TryGetWriteTransferCount(process.Handle, out ulong writes) ? writes : 0UL;
        }

        private void UpdateLiveMetrics()
        {
            using Process process = Process.GetCurrentProcess();
            process.Refresh();

            int threadCount = process.Threads.Count;
            lblActiveThreads.Text = threadCount.ToString();
            _peakThreadCount = Math.Max(_peakThreadCount, threadCount);

            DateTime nowUtc = DateTime.UtcNow;
            TimeSpan processorTime = process.TotalProcessorTime;
            double elapsedSeconds = (nowUtc - _lastCpuSampleUtc).TotalSeconds;
            if (elapsedSeconds < 0.1)
            {
                return;
            }

            double cpuDeltaSeconds = (processorTime - _lastProcessorTime).TotalSeconds;
            double cpuPercent = Math.Clamp(
                cpuDeltaSeconds / (elapsedSeconds * Environment.ProcessorCount) * 100.0,
                0,
                100);

            lblCpuUsage.Text = $"{cpuPercent:F1} %";
            _peakCpuPercent = Math.Max(_peakCpuPercent, cpuPercent);
            _lastProcessorTime = processorTime;
            _lastCpuSampleUtc = nowUtc;

            double diskWriteMBps = 0;
            if (TryGetWriteTransferCount(process.Handle, out ulong writeBytes))
            {
                if (elapsedSeconds > 0 && writeBytes >= _lastWriteTransferCount)
                {
                    diskWriteMBps = (writeBytes - _lastWriteTransferCount) / (1024d * 1024d) / elapsedSeconds;
                }

                _lastWriteTransferCount = writeBytes;
            }

            lblDiskWriteSpeed.Text = $"{diskWriteMBps:F2} MB/s";
            _peakDiskWriteMBps = Math.Max(_peakDiskWriteMBps, diskWriteMBps);
        }

        private static bool TryGetWriteTransferCount(IntPtr processHandle, out ulong writeTransferCount)
        {
            writeTransferCount = 0;
            if (!GetProcessIoCounters(processHandle, out IO_COUNTERS counters))
            {
                return false;
            }

            writeTransferCount = counters.WriteTransferCount;
            return true;
        }

        private void LogMessage(string message, Color color)
        {
            if (logRichTextBox.InvokeRequired)
            {
                logRichTextBox.Invoke(() => LogMessage(message, color));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logRichTextBox.SelectionStart = logRichTextBox.TextLength;
            logRichTextBox.SelectionLength = 0;
            logRichTextBox.SelectionColor = color;
            logRichTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            logRichTextBox.SelectionColor = logRichTextBox.ForeColor;
            logRichTextBox.ScrollToCaret();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS lpIoCounters);
    }
}
