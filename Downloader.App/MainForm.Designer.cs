namespace Downloader.App
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _httpClient?.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            headerPanel = new Panel();
            titleLabel = new Label();
            settingsPanel = new Panel();
            urlLabel = new Label();
            urlTextBox = new TextBox();
            pathLabel = new Label();
            pathTextBox = new TextBox();
            browseButton = new Button();
            segmentsLabel = new Label();
            segmentsUpDown = new NumericUpDown();
            strategyLabel = new Label();
            strategyCombo = new ComboBox();
            downloadButton = new Button();
            cancelButton = new Button();
            progressPanel = new Panel();
            progressLabel = new Label();
            progressBar = new ProgressBar();
            statusLabel = new Label();
            metricsPanel = new TableLayoutPanel();
            metricThreadsCaption = new Label();
            lblActiveThreads = new Label();
            metricCpuCaption = new Label();
            lblCpuUsage = new Label();
            metricDiskCaption = new Label();
            lblDiskWriteSpeed = new Label();
            logPanel = new Panel();
            logLabel = new Label();
            logRichTextBox = new RichTextBox();
            components = new System.ComponentModel.Container();
            metricsTimer = new System.Windows.Forms.Timer(components);
            headerPanel.SuspendLayout();
            settingsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)segmentsUpDown).BeginInit();
            progressPanel.SuspendLayout();
            metricsPanel.SuspendLayout();
            logPanel.SuspendLayout();
            SuspendLayout();
            //
            // headerPanel
            //
            headerPanel.BackColor = Color.FromArgb(32, 32, 36);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(900, 56);
            headerPanel.TabIndex = 0;
            //
            // titleLabel
            //
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(20, 12);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(292, 30);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "HTTP Segment Downloader";
            //
            // settingsPanel
            //
            settingsPanel.BackColor = Color.FromArgb(245, 246, 248);
            settingsPanel.Controls.Add(urlLabel);
            settingsPanel.Controls.Add(urlTextBox);
            settingsPanel.Controls.Add(pathLabel);
            settingsPanel.Controls.Add(pathTextBox);
            settingsPanel.Controls.Add(browseButton);
            settingsPanel.Controls.Add(segmentsLabel);
            settingsPanel.Controls.Add(segmentsUpDown);
            settingsPanel.Controls.Add(strategyLabel);
            settingsPanel.Controls.Add(strategyCombo);
            settingsPanel.Controls.Add(downloadButton);
            settingsPanel.Controls.Add(cancelButton);
            settingsPanel.Dock = DockStyle.Top;
            settingsPanel.Location = new Point(0, 56);
            settingsPanel.Name = "settingsPanel";
            settingsPanel.Padding = new Padding(16);
            settingsPanel.Size = new Size(900, 188);
            settingsPanel.TabIndex = 1;
            //
            // urlLabel
            //
            urlLabel.AutoSize = true;
            urlLabel.Location = new Point(20, 16);
            urlLabel.Name = "urlLabel";
            urlLabel.Size = new Size(80, 20);
            urlLabel.TabIndex = 0;
            urlLabel.Text = "URL nguồn";
            //
            // urlTextBox
            //
            urlTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            urlTextBox.Location = new Point(20, 40);
            urlTextBox.Name = "urlTextBox";
            urlTextBox.PlaceholderText = "https://example.com/file.zip";
            urlTextBox.Size = new Size(856, 27);
            urlTextBox.TabIndex = 1;
            //
            // pathLabel
            //
            pathLabel.AutoSize = true;
            pathLabel.Location = new Point(20, 76);
            pathLabel.Name = "pathLabel";
            pathLabel.Size = new Size(109, 20);
            pathLabel.TabIndex = 2;
            pathLabel.Text = "Đường dẫn lưu";
            //
            // pathTextBox
            //
            pathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pathTextBox.Location = new Point(20, 100);
            pathTextBox.Name = "pathTextBox";
            pathTextBox.Size = new Size(746, 27);
            pathTextBox.TabIndex = 3;
            //
            // browseButton
            //
            browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browseButton.Location = new Point(778, 98);
            browseButton.Name = "browseButton";
            browseButton.Size = new Size(98, 32);
            browseButton.TabIndex = 4;
            browseButton.Text = "Chọn...";
            browseButton.UseVisualStyleBackColor = true;
            browseButton.Click += browseButton_Click;
            //
            // segmentsLabel
            //
            segmentsLabel.AutoSize = true;
            segmentsLabel.Location = new Point(20, 142);
            segmentsLabel.Name = "segmentsLabel";
            segmentsLabel.Size = new Size(85, 20);
            segmentsLabel.TabIndex = 5;
            segmentsLabel.Text = "Phân đoạn";
            //
            // segmentsUpDown
            //
            segmentsUpDown.Location = new Point(111, 139);
            segmentsUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            segmentsUpDown.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
            segmentsUpDown.Name = "segmentsUpDown";
            segmentsUpDown.Size = new Size(80, 27);
            segmentsUpDown.TabIndex = 6;
            segmentsUpDown.Value = new decimal(new int[] { 4, 0, 0, 0 });
            //
            // strategyLabel
            //
            strategyLabel.AutoSize = true;
            strategyLabel.Location = new Point(210, 142);
            strategyLabel.Name = "strategyLabel";
            strategyLabel.Size = new Size(80, 20);
            strategyLabel.TabIndex = 7;
            strategyLabel.Text = "Chiến lược";
            //
            // strategyCombo
            //
            strategyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            strategyCombo.FormattingEnabled = true;
            strategyCombo.Items.AddRange(new object[]
            {
                "Adaptive (Tự động)",
                "Standard (Chuẩn)",
                "CustomThreadPool",
                "OverlappedIO"
            });
            strategyCombo.Location = new Point(296, 138);
            strategyCombo.Name = "strategyCombo";
            strategyCombo.Size = new Size(220, 28);
            strategyCombo.TabIndex = 8;
            //
            // downloadButton
            //
            downloadButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            downloadButton.BackColor = Color.FromArgb(16, 124, 16);
            downloadButton.FlatStyle = FlatStyle.Flat;
            downloadButton.ForeColor = Color.White;
            downloadButton.Location = new Point(560, 136);
            downloadButton.Name = "downloadButton";
            downloadButton.Size = new Size(150, 36);
            downloadButton.TabIndex = 9;
            downloadButton.Text = "Tải xuống";
            downloadButton.UseVisualStyleBackColor = false;
            downloadButton.Click += downloadButton_Click;
            //
            // cancelButton
            //
            cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cancelButton.Enabled = false;
            cancelButton.Location = new Point(720, 136);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(156, 36);
            cancelButton.TabIndex = 10;
            cancelButton.Text = "Hủy";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;
            //
            // progressPanel
            //
            progressPanel.Controls.Add(progressLabel);
            progressPanel.Controls.Add(progressBar);
            progressPanel.Controls.Add(statusLabel);
            progressPanel.Dock = DockStyle.Top;
            progressPanel.Location = new Point(0, 244);
            progressPanel.Name = "progressPanel";
            progressPanel.Size = new Size(900, 78);
            progressPanel.TabIndex = 2;
            //
            // progressLabel
            //
            progressLabel.AutoSize = true;
            progressLabel.Location = new Point(20, 10);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new Size(78, 20);
            progressLabel.TabIndex = 0;
            progressLabel.Text = "Tiến trình";
            //
            // progressBar
            //
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(20, 34);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(856, 18);
            progressBar.TabIndex = 1;
            //
            // statusLabel
            //
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(20, 56);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(154, 20);
            statusLabel.TabIndex = 2;
            statusLabel.Text = "Sẵn sàng tải xuống";
            //
            // metricsPanel
            //
            metricsPanel.ColumnCount = 3;
            metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            metricsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            metricsPanel.Controls.Add(metricThreadsCaption, 0, 0);
            metricsPanel.Controls.Add(lblActiveThreads, 0, 1);
            metricsPanel.Controls.Add(metricCpuCaption, 1, 0);
            metricsPanel.Controls.Add(lblCpuUsage, 1, 1);
            metricsPanel.Controls.Add(metricDiskCaption, 2, 0);
            metricsPanel.Controls.Add(lblDiskWriteSpeed, 2, 1);
            metricsPanel.Dock = DockStyle.Top;
            metricsPanel.Location = new Point(0, 322);
            metricsPanel.Name = "metricsPanel";
            metricsPanel.Padding = new Padding(16, 8, 16, 8);
            metricsPanel.RowCount = 2;
            metricsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            metricsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            metricsPanel.Size = new Size(900, 88);
            metricsPanel.TabIndex = 3;
            //
            // metricThreadsCaption
            //
            metricThreadsCaption.AutoSize = true;
            metricThreadsCaption.Dock = DockStyle.Fill;
            metricThreadsCaption.ForeColor = Color.DimGray;
            metricThreadsCaption.Name = "metricThreadsCaption";
            metricThreadsCaption.Text = "LUỒNG OS (Threads.Count)";
            metricThreadsCaption.TextAlign = ContentAlignment.BottomLeft;
            //
            // lblActiveThreads
            //
            lblActiveThreads.Dock = DockStyle.Fill;
            lblActiveThreads.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            lblActiveThreads.ForeColor = Color.FromArgb(0, 99, 177);
            lblActiveThreads.Name = "lblActiveThreads";
            lblActiveThreads.Text = "--";
            lblActiveThreads.TextAlign = ContentAlignment.TopLeft;
            //
            // metricCpuCaption
            //
            metricCpuCaption.AutoSize = true;
            metricCpuCaption.Dock = DockStyle.Fill;
            metricCpuCaption.ForeColor = Color.DimGray;
            metricCpuCaption.Name = "metricCpuCaption";
            metricCpuCaption.Text = "CPU PROCESS (%)";
            metricCpuCaption.TextAlign = ContentAlignment.BottomLeft;
            //
            // lblCpuUsage
            //
            lblCpuUsage.Dock = DockStyle.Fill;
            lblCpuUsage.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            lblCpuUsage.ForeColor = Color.FromArgb(192, 64, 0);
            lblCpuUsage.Name = "lblCpuUsage";
            lblCpuUsage.Text = "-- %";
            lblCpuUsage.TextAlign = ContentAlignment.TopLeft;
            //
            // metricDiskCaption
            //
            metricDiskCaption.AutoSize = true;
            metricDiskCaption.Dock = DockStyle.Fill;
            metricDiskCaption.ForeColor = Color.DimGray;
            metricDiskCaption.Name = "metricDiskCaption";
            metricDiskCaption.Text = "GHI ĐĨA (MB/s)";
            metricDiskCaption.TextAlign = ContentAlignment.BottomLeft;
            //
            // lblDiskWriteSpeed
            //
            lblDiskWriteSpeed.Dock = DockStyle.Fill;
            lblDiskWriteSpeed.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            lblDiskWriteSpeed.ForeColor = Color.FromArgb(16, 124, 16);
            lblDiskWriteSpeed.Name = "lblDiskWriteSpeed";
            lblDiskWriteSpeed.Text = "-- MB/s";
            lblDiskWriteSpeed.TextAlign = ContentAlignment.TopLeft;
            //
            // metricsTimer
            //
            metricsTimer.Interval = 500;
            metricsTimer.Tick += metricsTimer_Tick;
            //
            // logPanel
            //
            logPanel.Controls.Add(logRichTextBox);
            logPanel.Controls.Add(logLabel);
            logPanel.Dock = DockStyle.Fill;
            logPanel.Location = new Point(0, 410);
            logPanel.Name = "logPanel";
            logPanel.Padding = new Padding(20, 8, 20, 16);
            logPanel.Size = new Size(900, 378);
            logPanel.TabIndex = 4;
            //
            // logLabel
            //
            logLabel.AutoSize = true;
            logLabel.Dock = DockStyle.Top;
            logLabel.Location = new Point(20, 8);
            logLabel.Name = "logLabel";
            logLabel.Padding = new Padding(0, 0, 0, 6);
            logLabel.Size = new Size(132, 26);
            logLabel.TabIndex = 0;
            logLabel.Text = "Nhật ký tiến trình";
            //
            // logRichTextBox
            //
            logRichTextBox.BackColor = Color.Black;
            logRichTextBox.DetectUrls = false;
            logRichTextBox.Dock = DockStyle.Fill;
            logRichTextBox.Font = new Font("Consolas", 10F);
            logRichTextBox.ForeColor = Color.Gainsboro;
            logRichTextBox.Location = new Point(20, 34);
            logRichTextBox.Name = "logRichTextBox";
            logRichTextBox.ReadOnly = true;
            logRichTextBox.Size = new Size(860, 328);
            logRichTextBox.TabIndex = 1;
            logRichTextBox.Text = "";
            //
            // MainForm
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(900, 760);
            Controls.Add(logPanel);
            Controls.Add(metricsPanel);
            Controls.Add(progressPanel);
            Controls.Add(settingsPanel);
            Controls.Add(headerPanel);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(780, 620);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "HTTP Segment Downloader";
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            settingsPanel.ResumeLayout(false);
            settingsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)segmentsUpDown).EndInit();
            progressPanel.ResumeLayout(false);
            progressPanel.PerformLayout();
            metricsPanel.ResumeLayout(false);
            metricsPanel.PerformLayout();
            logPanel.ResumeLayout(false);
            logPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel headerPanel;
        private Label titleLabel;
        private Panel settingsPanel;
        private Label urlLabel;
        private TextBox urlTextBox;
        private Label pathLabel;
        private TextBox pathTextBox;
        private Button browseButton;
        private Label segmentsLabel;
        private NumericUpDown segmentsUpDown;
        private Label strategyLabel;
        private ComboBox strategyCombo;
        private Button downloadButton;
        private Button cancelButton;
        private Panel progressPanel;
        private Label progressLabel;
        private ProgressBar progressBar;
        private Label statusLabel;
        private TableLayoutPanel metricsPanel;
        private Label metricThreadsCaption;
        private Label lblActiveThreads;
        private Label metricCpuCaption;
        private Label lblCpuUsage;
        private Label metricDiskCaption;
        private Label lblDiskWriteSpeed;
        private System.Windows.Forms.Timer metricsTimer;
        private Panel logPanel;
        private Label logLabel;
        private RichTextBox logRichTextBox;
    }
}
