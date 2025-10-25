using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XYZRenderLauncher
{
    public class SplashScreen : Form
    {
        private readonly System.Windows.Forms.Timer fadeTimer = new();
        private readonly System.Windows.Forms.Timer loadingTimer = new();
        private readonly System.Windows.Forms.Timer progressTimer = new();
        private readonly Label lblTitle = new();
        private readonly Label lblInfo = new();
        private readonly Label lblLoading = new();
        private int dotCount = 0;
        private float progressValue = 0;
        private bool isClosing = false;

        public SplashScreen()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(500, 300);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Opacity = 0;

            fadeTimer.Interval = 30;
            loadingTimer.Interval = 200;
            progressTimer.Interval = 30;

            this.Paint += (sender, e) => SplashScreen_Paint(sender, e);
            fadeTimer.Tick += (sender, e) => FadeIn_Tick(sender, e);
            loadingTimer.Tick += (sender, e) => Loading_Tick(sender, e);
            progressTimer.Tick += (sender, e) => Progress_Tick(sender, e);

            this.SetStyle(ControlStyles.DoubleBuffer |
                          ControlStyles.UserPaint |
                          ControlStyles.AllPaintingInWmPaint, true);

            lblTitle = new Label()
            {
                Text = "XYZ Render Launcher",
                ForeColor = Color.FromArgb(0, 122, 204),
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(this.ClientSize.Width, 80),
                Location = new Point(0, 30)
            };

            lblInfo = new Label()
            {
                Text = "Copyright © 2025 XYZ\nVersion 1.0.0",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true
            };
            lblInfo.Location = new Point(this.ClientSize.Width - lblInfo.PreferredWidth - 10,
                                         this.ClientSize.Height - lblInfo.PreferredHeight - 10);
            lblInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            lblLoading = new Label()
            {
                Text = "Initializing",
                ForeColor = Color.LightBlue,
                Font = new Font("Segoe UI", 12, FontStyle.Italic),
                AutoSize = true
            };
            lblLoading.Location = new Point((this.ClientSize.Width - lblLoading.Width) / 2,
                                            this.ClientSize.Height - 80);
            lblLoading.Anchor = AnchorStyles.Bottom;

            this.Controls.AddRange(new Control[] { lblTitle, lblInfo, lblLoading });

            fadeTimer.Start();
            loadingTimer.Start();
            progressTimer.Start();
        }

        private void SplashScreen_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(Color.FromArgb(0, 122, 204), 4f))
            {
                Rectangle rect = new Rectangle(
                    this.ClientSize.Width / 2 - 30,
                    this.ClientSize.Height - 150,
                    60,
                    60);

                e.Graphics.DrawArc(pen, rect, -90, progressValue * 360);
            }
        }

        private void Progress_Tick(object? sender, EventArgs e)
        {
            if (!isClosing)
            {
                progressValue += 0.01f;
                if (progressValue > 1f)
                    progressValue = 1f;

                this.Invalidate();
            }
        }

        private void FadeIn_Tick(object? sender, EventArgs e)
        {
            if (this.Opacity < 1)
                this.Opacity = Math.Min(this.Opacity + 0.05, 1);
            else
                fadeTimer.Stop();
        }

        private void Loading_Tick(object? sender, EventArgs e)
        {
            dotCount = (dotCount + 1) % 4;
            lblLoading.Text = "Initializing" + new string('.', dotCount);
            lblLoading.Left = (this.ClientSize.Width - lblLoading.Width) / 2;
        }

        public void FadeOutAndClose()
        {
            isClosing = true;
            fadeTimer.Stop();
            loadingTimer.Stop();
            progressTimer.Stop();

            System.Windows.Forms.Timer fadeOut = new System.Windows.Forms.Timer { Interval = 30 };
            fadeOut.Tick += (sender, e) =>
            {
                if (this.Opacity > 0)
                    this.Opacity -= 0.05;
                else
                {
                    fadeOut.Stop();
                    this.Close();
                }
            };
            fadeOut.Start();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(30, 30, 30),
                Color.FromArgb(45, 45, 48),
                45f))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }
    }
}
