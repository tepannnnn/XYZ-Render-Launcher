using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace XYZRenderLauncher
{
    public partial class XYZ : Form
    {
        private Process? blenderProcess;
        private readonly System.Diagnostics.Stopwatch frameStopwatch = new System.Diagnostics.Stopwatch();
        private int lastReportedDone = 0;
        private double lastFrameSeconds = 0.0;
        private double avgFrameSeconds = 0.0;
        private const double EMA_ALPHA = 0.2;

        public class RenderStats
        {
            public int FramesDone { get; set; }
            public int TotalFrames { get; set; }
            public double LastFrameSeconds { get; set; }
            public double AvgFrameSeconds { get; set; }
            public double ETASeconds { get; set; }
            public string ETAFormatted { get; set; } = "";
        }

        public event EventHandler<RenderStats>? RenderStatsUpdated;

        private readonly string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private readonly Config config = new Config();

        private string telegramBotToken = "";
        private string telegramChatId = "";
        private bool notifyOnFinish = false;
        private DateTime? renderStartTime = null;
        private double averageFrameTime = 0;
        private int framesRendered = 0;

        private class Config
        {
            public string BlenderPath { get; set; } = "";
            public string TelegramBotToken { get; set; } = "";
            public string TelegramChatId { get; set; } = "";
        }

        public XYZ()
        {
            InitializeComponent();

            try
            {
                try
                {
                    var asm = System.Reflection.Assembly.GetExecutingAssembly();
                    string resourceName = "XYZRenderLauncher.Resources.logo.ico";
                    using var stream = asm.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        this.Icon = new Icon(stream);
                    }
                    else
                    {
                        string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.ico");
                        if (File.Exists(p)) this.Icon = new Icon(p);
                    }
                }
                catch
                {
                    string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    string iconPath = Path.Combine(exeDir, @"..\..\logo\logo.ico");
                    iconPath = Path.GetFullPath(iconPath);
                    if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            try
            {
                LoadConfig();

                if (cmbFormat.Items.Count > 0) cmbFormat.SelectedIndex = 0;
                if (cmbResolution.Items.Count > 0) cmbResolution.SelectedIndex = 0;
                if (cmbDevice.Items.Count > 0) cmbDevice.SelectedIndex = 0;

                numSamples.Value = 128;
                numFPS.Value = 24;
                numStartFrame.Value = 1;
                numEndFrame.Value = 250;

                btnRender.Enabled = true;
                btnStop.Enabled = false;

                btnSelectBlender.Click += BtnSelectBlender_Click;
                btnSelectBlend.Click += BtnSelectBlend_Click;
                btnSelectOutput.Click += BtnSelectOutput_Click;
                btnRender.Click += BtnRender_Click;
                btnStop.Click += BtnStop_Click;
                btnNotification.Click += BtnNotification_Click;
                chkNightMode.CheckedChanged += ChkNightMode_CheckedChanged;
                btnCheckFrame.Click += BtnCheckFrame_Click;

                txtBotToken.Text = config.TelegramBotToken;
                txtChatId.Text = config.TelegramChatId;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saat InitializeComponent : " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configFile))
                {
                    string json = File.ReadAllText(configFile);
                    var loaded = JsonSerializer.Deserialize<Config>(json);
                    if (loaded != null)
                    {
                        config.BlenderPath = loaded.BlenderPath;
                        config.TelegramBotToken = loaded.TelegramBotToken;
                        config.TelegramChatId = loaded.TelegramChatId;

                        txtBlenderPath.Text = config.BlenderPath;
                        txtBotToken.Text = config.TelegramBotToken;
                        txtChatId.Text = config.TelegramChatId;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading config: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                config.BlenderPath = txtBlenderPath.Text;
                config.TelegramBotToken = txtBotToken.Text;
                config.TelegramChatId = txtChatId.Text;
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFile, json);
            }
            catch (Exception ex)
            {
                Log($"Error saving config: {ex.Message}");
            }
        }

        private void BtnSelectBlender_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog { Filter = "Blender Executable|blender.exe" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtBlenderPath.Text = dlg.FileName;
                SaveConfig();
            }
        }

        private void BtnSelectBlend_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog { Filter = "Blend Files|*.blend" };
            if (dlg.ShowDialog() == DialogResult.OK) txtBlendFile.Text = dlg.FileName;
        }

        private void BtnSelectOutput_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) txtOutputFolder.Text = dlg.SelectedPath;
        }

        private void BtnNotification_Click(object? sender, EventArgs e)
        {
            notifyOnFinish = !notifyOnFinish;
            btnNotification.Text = notifyOnFinish ? "Notification: ON" : "Notification: OFF";
        }

        private async Task SendTelegramMessageAsync(string message)
        {
            telegramBotToken = txtBotToken.Text;
            telegramChatId = txtChatId.Text;
            SaveConfig();

            if (string.IsNullOrWhiteSpace(telegramBotToken) || string.IsNullOrWhiteSpace(telegramChatId))
                return;

            try
            {
                string url = $"https://api.telegram.org/bot{telegramBotToken}/sendMessage?chat_id={telegramChatId}&text={Uri.EscapeDataString(message)}";
                using var client = new System.Net.Http.HttpClient();
                await client.GetAsync(url);
            }
            catch (Exception ex)
            {
                Log("Failed to send Telegram message: " + ex.Message);
            }
        }

        private void BtnCheckFrame_Click(object? sender, EventArgs e)
        {
            string blenderExe = txtBlenderPath.Text;
            string blendFile = txtBlendFile.Text;

            if (!File.Exists(blenderExe) || !File.Exists(blendFile))
            {
                MessageBox.Show("Check path Blender and file .blend!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnRender.Enabled = false;
            btnStop.Enabled = true;
            progressBar.Value = 0;
            Log("Checking frames...");

            Task.Run(() =>
            {
                try
                {
                    string pythonExpr = "import bpy; print(bpy.context.scene.frame_start, bpy.context.scene.frame_end)";

                    Process proc = new Process();
                    proc.StartInfo.FileName = blenderExe;
                    proc.StartInfo.Arguments = $"--background \"{blendFile}\" --python-expr \"{pythonExpr}\"";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.CreateNoWindow = true;

                    proc.Start();

                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();

                    proc.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        SafeInvoke(() => MessageBox.Show("Error: " + error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
                        Log("Error checking frames: " + error);
                    }
                    else
                    {
                        bool found = false;
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2
                                && int.TryParse(parts[0], out int startFrame)
                                && int.TryParse(parts[1], out int endFrame))
                            {
                                SafeInvoke(() =>
                                {
                                    numStartFrame.Value = startFrame;
                                    numEndFrame.Value = endFrame;
                                    Log($"Frames detected: Start={startFrame}, End={endFrame}");
                                    MessageBox.Show($"Frames detected:\nStart Frame: {startFrame}\nEnd Frame: {endFrame}", "Frame Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                });
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            Log("Failed to parse frame info from Blender output: " + output);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Error checking frames: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    Log("Exception: " + ex.Message);
                }
                finally
                {
                    SafeInvoke(() =>
                    {
                        btnRender.Enabled = true;
                        btnStop.Enabled = false;
                    });
                }
            });
        }

        private void BtnRender_Click(object? sender, EventArgs e)
{
    frameStopwatch.Reset();
    lastReportedDone = 0;
    lastFrameSeconds = 0;
    avgFrameSeconds = 0;

    string blenderExe = txtBlenderPath.Text;
    string blendFile = txtBlendFile.Text;
    string outputFolder = txtOutputFolder.Text;

    if (!File.Exists(blenderExe) || !File.Exists(blendFile) || !Directory.Exists(outputFolder))
    {
        MessageBox.Show("Check path Blender, file .blend, and folder output!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    btnRender.Enabled = false;
    btnStop.Enabled = true;
    progressBar.Value = 0;
    txtLog.Clear();

    if (lblETA != null) lblETA.Text = "ETA calculating...";

    int startFrame = (int)numStartFrame.Value;
    int endFrame = (int)numEndFrame.Value;
    int totalFrames = Math.Max(1, endFrame - startFrame + 1);
    int samples = (int)numSamples.Value;
    int fps = (int)numFPS.Value;

    string resolution = cmbResolution.SelectedItem?.ToString() ?? "1920x1080";
    string[] resParts = resolution.Split('x');
    int resX = int.Parse(resParts[0]);
    int resY = int.Parse(resParts[1]);

    string format = cmbFormat.SelectedItem?.ToString() ?? "PNG";

    renderStartTime = DateTime.Now;
    averageFrameTime = 0;
    framesRendered = 0;

    Task.Run(() =>
    {
        string tempScript = Path.Combine(Path.GetTempPath(), $"blender_render_{Guid.NewGuid()}.py");

        string pythonScript = $@"
import bpy, os

# Get selected device from UI
device_type = '{cmbDevice.SelectedItem?.ToString() ?? "CPU"}'
print(f'Setting up render device: {{device_type}}')

# GPU device setup with error handling
try:
    cycles_prefs = bpy.context.preferences.addons['cycles'].preferences
    
    # Configure compute device type based on UI selection
    if device_type == 'OPTIX':
        print('Configuring OptiX device...')
        cycles_prefs.compute_device_type = 'OPTIX'
        bpy.context.scene.cycles.device = 'GPU'
        
        # Force GPU compute
        bpy.context.scene.cycles.use_cpu = False
        
        # Additional OptiX optimizations
        if hasattr(bpy.context.scene.cycles, 'use_denoising'):
            bpy.context.scene.cycles.use_denoising = True
            bpy.context.scene.cycles.denoiser = 'OPTIX'
        
        # Ensure compositing uses GPU
        if bpy.context.scene.use_nodes:
            bpy.context.scene.render.use_compositing = True
            bpy.context.scene.render.use_sequencer = True
            for node in bpy.context.scene.node_tree.nodes:
                if hasattr(node, 'use_gpu'):
                    node.use_gpu = True
                if hasattr(node, 'gpu_type'):
                    node.gpu_type = 'OPTIX'
                    
    elif device_type == 'CUDA':
        print('Configuring CUDA device...')
        cycles_prefs.compute_device_type = 'CUDA'
        bpy.context.scene.cycles.device = 'GPU'
        bpy.context.scene.cycles.use_cpu = False
        
    else:
        print('Using CPU device')
        bpy.context.scene.cycles.device = 'CPU'
    
    # Enable devices if using GPU
    if device_type != 'CPU':
        cycles_prefs.refresh_devices()
        cycles_prefs.get_devices()
        for device in cycles_prefs.devices:
            if device.type == device_type:
                device.use = True
                print(f'Enabled device: {{device.name}}')

except Exception as e:
    print(f'Warning: Could not setup {{device_type}} rendering: {{e}}')
    print('Falling back to CPU rendering')
    bpy.context.scene.cycles.device = 'CPU'

scene = bpy.context.scene

# Configure render settings
scene.render.engine = 'CYCLES'
scene.cycles.samples = {samples}
scene.render.fps = {fps}
scene.frame_start = {startFrame}
scene.frame_end = {endFrame}

scene.render.resolution_x = {resX}
scene.render.resolution_y = {resY}
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = '{format}'

# Additional GPU optimizations
if device_type != 'CPU':
    scene.render.use_persistent_data = True
    if hasattr(scene.cycles, 'tile_size'):
        scene.cycles.tile_size = 256
    if hasattr(scene.render, 'use_simplify'):
        scene.render.use_simplify = True
    if hasattr(scene.render, 'simplify_gpu_subdivision'):
        scene.render.simplify_gpu_subdivision = True

outPath = r'{outputFolder}'
os.makedirs(outPath, exist_ok=True)

print('Starting render...', flush=True)
totalFrames = scene.frame_end - scene.frame_start + 1

for idx, frame in enumerate(range(scene.frame_start, scene.frame_end + 1)):
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(outPath, f'render_{{frame:04d}}.{format.ToLower()}')
    bpy.ops.render.render(write_still=True)
    print(f'RENDER_PROGRESS:{{idx+1}}/{{totalFrames}}', flush=True)

print('Rendering completed.', flush=True)
";
        File.WriteAllText(tempScript, pythonScript);

        try
        {
            blenderProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = blenderExe,
                    Arguments = $"--background \"{blendFile}\" --python \"{tempScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            blenderProcess.OutputDataReceived += (s, e) => HandleProcessOutput(e.Data, totalFrames);
            blenderProcess.ErrorDataReceived += (s, e) => HandleProcessOutput(e.Data, totalFrames);

            blenderProcess.Exited += async (s, e) =>
            {
                UpdateStats(totalFrames, totalFrames);
                RenderStatsUpdated?.Invoke(this, GetRenderStatistics());

                SafeInvoke(() =>
                {
                    btnRender.Enabled = true;
                    btnStop.Enabled = false;
                    progressBar.Value = 100;
                    Log("Rendering completed.");
                });

                if (notifyOnFinish)
                {
                    string message = $"✅ Blender Render DONE!\nFolder: {txtOutputFolder.Text}\nFrames: {numStartFrame.Value}-{numEndFrame.Value}";
                    await SendTelegramMessageAsync(message);
                }

                TryDelete(tempScript);
            };

            blenderProcess.Start();
            blenderProcess.BeginOutputReadLine();
            blenderProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
            TryDelete(tempScript);
            SafeInvoke(() =>
            {
                btnRender.Enabled = true;
                btnStop.Enabled = false;
            });
        }
    });
}

        private void HandleProcessOutput(string? data, int totalFrames)
        {
            if (string.IsNullOrEmpty(data)) return;
            Log(data);

            if (data.StartsWith("RENDER_PROGRESS:"))
            {
                string[] parts = data.Replace("RENDER_PROGRESS:", "").Split('/');
                if (parts.Length == 2
                    && int.TryParse(parts[0], out int done)
                    && int.TryParse(parts[1], out int tot))
                {
                    UpdateStats(done, totalFrames);

                    framesRendered = done;
                    averageFrameTime = avgFrameSeconds;

                    SafeInvoke(() =>
                    {
                        progressBar.Value = Math.Min(100, (int)((done / (double)totalFrames) * 100));
                        lblFrameStats.Text = $"Frame: {done} / {totalFrames}";
                        lblAvgFrame.Text = $"Avg: {averageFrameTime:F2}s/frame";
                        lblETA.Text = $"ETA: {GetRenderStatistics().ETAFormatted}";
                    });

                    RenderStatsUpdated?.Invoke(this, GetRenderStatistics());
                }
            }
        }

        private void UpdateStats(int done, int totalFrames)
        {
            try
            {
                if (!frameStopwatch.IsRunning)
                {
                    frameStopwatch.Start();
                    lastReportedDone = done;
                    return;
                }

                int diff = done - lastReportedDone;
                if (diff <= 0)
                {
                    return;
                }

                double elapsed = frameStopwatch.Elapsed.TotalSeconds;
                double perFrame = elapsed / Math.Max(1, diff);

                lastFrameSeconds = perFrame;
                if (avgFrameSeconds <= 0) avgFrameSeconds = perFrame;
                else avgFrameSeconds = avgFrameSeconds * (1 - EMA_ALPHA) + perFrame * EMA_ALPHA;

                lastReportedDone = done;
                frameStopwatch.Restart();
            }
            catch { }
        }

        public RenderStats GetRenderStatistics()
        {
            int uiTotal = (int)(numEndFrame.Value - numStartFrame.Value + 1);
            int remainingFrames = Math.Max(0, uiTotal - lastReportedDone);
            double etaSec = Math.Max(0.0, remainingFrames * avgFrameSeconds);
            TimeSpan ts = TimeSpan.FromSeconds(etaSec);

            return new RenderStats
            {
                FramesDone = lastReportedDone,
                TotalFrames = uiTotal,
                LastFrameSeconds = lastFrameSeconds,
                AvgFrameSeconds = avgFrameSeconds,
                ETASeconds = etaSec,
                ETAFormatted = $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s"
            };
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            try
            {
                if (blenderProcess != null && !blenderProcess.HasExited)
                {
                    blenderProcess.Kill(true);
                    Log("Render stopped by user.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error stopping process: {ex.Message}");
            }
            finally
            {
                SafeInvoke(() =>
                {
                    btnRender.Enabled = true;
                    btnStop.Enabled = false;
                });
                frameStopwatch.Reset();
                lastReportedDone = 0;
                lastFrameSeconds = 0;
                avgFrameSeconds = 0;
                RenderStatsUpdated?.Invoke(this, GetRenderStatistics());
            }
        }

        private void Log(string message) => SafeInvoke(() => txtLog.AppendText(message + Environment.NewLine));
        private void SafeInvoke(Action action) { if (InvokeRequired) Invoke(action); else action(); }
        private void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

        private void ApplyTheme(bool nightMode)
        {
            this.BackColor = nightMode ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is TextBox || ctrl is ComboBox || ctrl is NumericUpDown)
                {
                    ctrl.BackColor = nightMode ? Color.FromArgb(50, 50, 50) : SystemColors.Window;
                    ctrl.ForeColor = nightMode ? Color.White : SystemColors.ControlText;
                }
                else
                {
                    ctrl.ForeColor = nightMode ? Color.White : SystemColors.ControlText;
                }

                if (ctrl.HasChildren) ApplyThemeRecursive(ctrl, nightMode);
            }
        }

        private void ApplyThemeRecursive(Control parent, bool nightMode)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is TextBox || ctrl is ComboBox || ctrl is NumericUpDown)
                {
                    ctrl.BackColor = nightMode ? Color.FromArgb(50, 50, 50) : SystemColors.Window;
                    ctrl.ForeColor = nightMode ? Color.White : SystemColors.ControlText;
                }
                else
                {
                    ctrl.ForeColor = nightMode ? Color.White : SystemColors.ControlText;
                }
                if (ctrl.HasChildren) ApplyThemeRecursive(ctrl, nightMode);
            }
        }

        private void ChkNightMode_CheckedChanged(object? sender, EventArgs e)
        {
            ApplyTheme(chkNightMode.Checked);
        }
    }
}
