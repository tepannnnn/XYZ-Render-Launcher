using System;
using System.Drawing;
using System.Windows.Forms;

namespace XYZRenderLauncher
{
    partial class XYZ
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblETA = null!;
        private Label lblFrameStats = null!;
        private Label lblAvgFrame = null!;
        private System.Windows.Forms.Timer etaUpdateTimer = null!;
        private Panel statsPanel = null!;
        private TextBox txtBlenderPath = null!;
        private Button btnSelectBlender = null!;
        private TextBox txtBlendFile = null!;
        private Button btnSelectBlend = null!;
        private TextBox txtOutputFolder = null!;
        private Button btnSelectOutput = null!;
        private FlowLayoutPanel settingsPanel = null!;
        private ComboBox cmbResolution = null!;
        private ComboBox cmbFormat = null!;
        private ComboBox cmbDevice = null!;
        private NumericUpDown numSamples = null!;
        private NumericUpDown numFPS = null!;
        private NumericUpDown numStartFrame = null!;
        private NumericUpDown numEndFrame = null!;
        private Button btnCheckFrame = null!;
        private Button btnRender = null!;
        private Button btnStop = null!;
        private FlowLayoutPanel telegramPanel = null!;
        private TextBox txtBotToken = null!;
        private TextBox txtChatId = null!;
        private Button btnNotification = null!;
        private ProgressBar progressBar = null!;
        private TextBox txtLog = null!;
        private CheckBox chkNightMode = null!;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.Text = "XYZ Render Launcher";
            this.ClientSize = new Size(1024, 720);
            this.MinimumSize = new Size(950, 650);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterScreen;

            int margin = 20;
            int top = margin;
            int labelHeight = 18;
            int controlHeight = 24;
            int spacingY = 10;

            Label lblBlenderPath = new Label() { Left = margin, Top = top, Text = "Blender Executable Path:", AutoSize = true };
            txtBlenderPath = new TextBox() { Left = margin, Top = top + labelHeight, Width = this.ClientSize.Width - margin * 3 - 180, Height = controlHeight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            btnSelectBlender = new Button() { Left = txtBlenderPath.Right + 10, Top = txtBlenderPath.Top - 1, Width = 180, Height = controlHeight, Text = "Select Blender", Anchor = AnchorStyles.Top | AnchorStyles.Right };
            this.Controls.AddRange(new Control[] { lblBlenderPath, txtBlenderPath, btnSelectBlender });
            top += labelHeight + controlHeight + spacingY;
            Label lblBlendFile = new Label() { Left = margin, Top = top, Text = "Blend File:", AutoSize = true };
            txtBlendFile = new TextBox() { Left = margin, Top = top + labelHeight, Width = this.ClientSize.Width - margin * 3 - 180, Height = controlHeight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            btnSelectBlend = new Button() { Left = txtBlendFile.Right + 10, Top = txtBlendFile.Top - 1, Width = 180, Height = controlHeight, Text = "Select Blend", Anchor = AnchorStyles.Top | AnchorStyles.Right };
            this.Controls.AddRange(new Control[] { lblBlendFile, txtBlendFile, btnSelectBlend });
            top += labelHeight + controlHeight + spacingY;
            Label lblOutputFolder = new Label() { Left = margin, Top = top, Text = "Output Folder:", AutoSize = true };
            txtOutputFolder = new TextBox() { Left = margin, Top = top + labelHeight, Width = this.ClientSize.Width - margin * 3 - 180, Height = controlHeight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            btnSelectOutput = new Button() { Left = txtOutputFolder.Right + 10, Top = txtOutputFolder.Top - 1, Width = 180, Height = controlHeight, Text = "Select Output Folder", Anchor = AnchorStyles.Top | AnchorStyles.Right };
            this.Controls.AddRange(new Control[] { lblOutputFolder, txtOutputFolder, btnSelectOutput });
            top += labelHeight + controlHeight + spacingY * 2;
            settingsPanel = new FlowLayoutPanel()
            {
                Left = margin,
                Top = top,
                Width = this.ClientSize.Width - 2 * margin,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            void AddLabel(Control parent, string text) => parent.Controls.Add(new Label() { Text = text, AutoSize = true, Margin = new Padding(0, 8, 5, 3) });

            AddLabel(settingsPanel, "Resolution:");
            cmbResolution = new ComboBox() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbResolution.Items.AddRange(new object[] { "1920x1080", "2560x1440", "3840x2160", "7680x4320" });
            cmbResolution.SelectedIndex = 0;
            settingsPanel.Controls.Add(cmbResolution);

            AddLabel(settingsPanel, "Format:");
            cmbFormat = new ComboBox() { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFormat.Items.AddRange(new object[] { "PNG", "JPEG", "OPEN_EXR" });
            cmbFormat.SelectedIndex = 0;
            settingsPanel.Controls.Add(cmbFormat);

            AddLabel(settingsPanel, "Device:");
            cmbDevice = new ComboBox() { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDevice.Items.AddRange(new object[] { "CPU", "CUDA", "OPTIX" });
            cmbDevice.SelectedIndex = 0;
            settingsPanel.Controls.Add(cmbDevice);

            AddLabel(settingsPanel, "Samples:");
            numSamples = new NumericUpDown() { Width = 80, Minimum = 32, Maximum = 2048, Value = 128, Increment = 32 };
            settingsPanel.Controls.Add(numSamples);

            AddLabel(settingsPanel, "FPS:");
            numFPS = new NumericUpDown() { Width = 60, Minimum = 1, Maximum = 240, Value = 24 };
            settingsPanel.Controls.Add(numFPS);

            AddLabel(settingsPanel, "Start Frame:");
            numStartFrame = new NumericUpDown() { Width = 60, Minimum = 1, Maximum = 10000, Value = 1 };
            settingsPanel.Controls.Add(numStartFrame);

            AddLabel(settingsPanel, "End Frame:");
            numEndFrame = new NumericUpDown() { Width = 60, Minimum = 1, Maximum = 10000, Value = 250 };
            settingsPanel.Controls.Add(numEndFrame);
            btnCheckFrame = new Button()
            {
                Text = "Check Frame",
                Width = 100,
                Height = 28,
                Margin = new Padding(10, 0, 0, 0)
            };
            settingsPanel.Controls.Add(btnCheckFrame);

            btnRender = new Button() { Width = 80, Text = "Render" };
            btnStop = new Button() { Width = 80, Text = "Stop" };
            settingsPanel.Controls.AddRange(new Control[] { btnRender, btnStop });

            this.Controls.Add(settingsPanel);
            top += settingsPanel.Height + spacingY * 2;
            telegramPanel = new FlowLayoutPanel()
            {
                Left = margin,
                Top = top,
                Width = this.ClientSize.Width - 2 * margin,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            telegramPanel.Controls.Add(new Label() { Text = "Bot Token:", AutoSize = true, Margin = new Padding(0, 8, 5, 3) });
            txtBotToken = new TextBox() { Width = 300 };
            telegramPanel.Controls.Add(txtBotToken);

            telegramPanel.Controls.Add(new Label() { Text = "Chat ID:", AutoSize = true, Margin = new Padding(10, 8, 5, 3) });
            txtChatId = new TextBox() { Width = 150 };
            telegramPanel.Controls.Add(txtChatId);

            btnNotification = new Button() { Width = 100, Text = "Notify OFF" };
            telegramPanel.Controls.Add(btnNotification);

            this.Controls.Add(telegramPanel);
            top += telegramPanel.Height + spacingY * 2;
            Label lblProgress = new Label() { Left = margin, Top = top, Text = "Progress", AutoSize = true };
            progressBar = new ProgressBar() { Left = margin, Top = top + labelHeight, Width = this.ClientSize.Width - 2 * margin, Height = 25, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            this.Controls.AddRange(new Control[] { lblProgress, progressBar });
            top += labelHeight + 25 + spacingY;
            statsPanel = new Panel()
            {
                Left = margin,
                Top = top,
                Width = this.ClientSize.Width - 2 * margin,
                Height = 60,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblETA = new Label() { Text = "ETA: calculating...", Left = 10, Top = 10, AutoSize = true };
            lblFrameStats = new Label() { Text = "Frame: 0 / 0", Left = 10, Top = 30, AutoSize = true };
            lblAvgFrame = new Label() { Text = "Avg: 0.0s/frame", Left = 150, Top = 30, AutoSize = true };

            statsPanel.Controls.AddRange(new Control[] { lblETA, lblFrameStats, lblAvgFrame });
            this.Controls.Add(statsPanel);
            top += statsPanel.Height + spacingY;
            etaUpdateTimer = new System.Windows.Forms.Timer();
            etaUpdateTimer.Interval = 10000;
            etaUpdateTimer.Tick += (s, e) => UpdateStatsUI();
            etaUpdateTimer.Start();
            Label lblLog = new Label() { Left = margin, Top = top, Text = "Render Log", AutoSize = true };
            txtLog = new TextBox()
            {
                Left = margin,
                Top = top + labelHeight,
                Width = this.ClientSize.Width - 2 * margin,
                Height = this.ClientSize.Height - top - 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.AddRange(new Control[] { lblLog, txtLog });

            chkNightMode = new CheckBox() { Left = margin, Top = this.ClientSize.Height - 40, Width = 120, Text = "Night Mode", Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            this.Controls.Add(chkNightMode);
        }
        private void UpdateStatsUI()
        {
            if (lblETA.InvokeRequired)
            {
                lblETA.Invoke(new Action(UpdateStatsUI));
                return;
            }

            var stats = GetRenderStatistics();
            lblETA.Text = $"ETA: {stats.ETAFormatted}";
            lblFrameStats.Text = $"Frame: {stats.FramesDone} / {stats.TotalFrames}";
            lblAvgFrame.Text = $"Avg: {stats.AvgFrameSeconds:F2}s/frame";
        }
    }
}
