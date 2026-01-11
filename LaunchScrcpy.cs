using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace LaunchScrcpy
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // --- Theme Colors ---
    public static class Theme
    {
        public static Color BackDark = Color.FromArgb(32, 32, 32);
        public static Color BackLight = Color.FromArgb(45, 45, 48);
        public static Color Accent = Color.FromArgb(0, 122, 204);
        public static Color TextWhite = Color.FromArgb(241, 241, 241);
        public static Color TextGray = Color.FromArgb(150, 150, 150);
        public static Font FontTitle = new Font("Segoe UI", 12, FontStyle.Bold);
        public static Font FontNormal = new Font("Segoe UI", 10, FontStyle.Regular);
        public static Font FontSmall = new Font("Segoe UI", 8, FontStyle.Regular);
    }

    public class MainForm : Form
    {
        private FlowLayoutPanel flowPanel;
        private ModernButton btnRefresh;
        private ModernButton btnWireless;
        private ModernButton btnLaunch;
        private Label lblStatus;
        private string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private string RepoOwner = "Genymobile";
        private string RepoName = "scrcpy";
        private string ScrcpyPath;

        public MainForm()
        {
            this.Text = "Scrcpy Launcher";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BackDark;
            this.ForeColor = Theme.TextWhite;
            this.Font = Theme.FontNormal;

            InitializeControls();
            ThreadPool.QueueUserWorkItem(new WaitCallback(InitialLoad));
        }

        private void InitializeControls()
        {
            // Top Panel
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 80;
            topPanel.Padding = new Padding(15);
            topPanel.BackColor = Theme.BackLight;

            // Logo & Title Container
            FlowLayoutPanel titlePanel = new FlowLayoutPanel();
            titlePanel.AutoSize = true;
            titlePanel.FlowDirection = FlowDirection.LeftToRight;
            titlePanel.BackColor = Color.Transparent;
            titlePanel.Dock = DockStyle.Left;
            titlePanel.WrapContents = false;

            // Logo
            string logoPath = Path.Combine(BaseDir, "Scrcpy_logo.svg.png");
            if (File.Exists(logoPath))
            {
                try 
                {
                    Bitmap logo = new Bitmap(logoPath);
                    
                    // Set Window Icon
                    IntPtr hicon = logo.GetHicon();
                    this.Icon = Icon.FromHandle(hicon);

                    // UI Logo
                    PictureBox logoBox = new PictureBox();
                    logoBox.Image = logo;
                    logoBox.SizeMode = PictureBoxSizeMode.Zoom;
                    logoBox.Width = 50;
                    logoBox.Height = 50;
                    logoBox.Margin = new Padding(0, 0, 15, 0);
                    titlePanel.Controls.Add(logoBox);
                }
                catch { }
            }

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Scrcpy Launcher";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = Theme.TextWhite;
            lblTitle.AutoSize = true;
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // Center vertically in flow layout
            lblTitle.Margin = new Padding(0, 10, 0, 0); 
            
            titlePanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(titlePanel);

            // Buttons
            btnLaunch = new ModernButton();
            btnLaunch.Text = "LAUNCH SELECTED";
            btnLaunch.Width = 180;
            btnLaunch.Height = 40;
            btnLaunch.Dock = DockStyle.Right;
            btnLaunch.BackColor = Theme.Accent;
            btnLaunch.Click += new EventHandler(BtnLaunch_Click);
            // Vertical center alignment for button
            btnLaunch.Margin = new Padding(0, 5, 0, 0);

            btnRefresh = new ModernButton();
            btnRefresh.Text = "REFRESH";
            btnRefresh.Width = 120;
            btnRefresh.Height = 40;
            btnRefresh.Dock = DockStyle.Right;
            btnRefresh.BackColor = Color.FromArgb(60, 60, 60);
            btnRefresh.Click += new EventHandler(BtnRefresh_Click);
            btnRefresh.Margin = new Padding(0, 5, 0, 0);

            // Spacer
            Panel spacer = new Panel();
            spacer.Width = 10;
            spacer.Dock = DockStyle.Right;
            spacer.BackColor = Color.Transparent;

            // Spacer 2
            Panel spacer2 = new Panel();
            spacer2.Width = 10;
            spacer2.Dock = DockStyle.Right;
            spacer2.BackColor = Color.Transparent;

            btnWireless = new ModernButton();
            btnWireless.Text = "WIRELESS";
            btnWireless.Width = 120;
            btnWireless.Height = 40;
            btnWireless.Dock = DockStyle.Right;
            btnWireless.BackColor = Color.FromArgb(60, 60, 60);
            btnWireless.Click += new EventHandler(BtnWireless_Click);
            btnWireless.Margin = new Padding(0, 5, 0, 0);

            // Container for buttons to align them
            Panel buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Right;
            buttonPanel.Width = 450;
            buttonPanel.Height = 50;
            buttonPanel.BackColor = Color.Transparent;
            buttonPanel.Controls.Add(btnLaunch);
            buttonPanel.Controls.Add(spacer);
            buttonPanel.Controls.Add(btnRefresh);
            buttonPanel.Controls.Add(spacer2);
            buttonPanel.Controls.Add(btnWireless);
            
            topPanel.Controls.Add(buttonPanel);

            // Status Bar
            StatusStrip statusStrip = new StatusStrip();
            statusStrip.BackColor = Theme.BackLight;
            statusStrip.SizingGrip = false;
            
            ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();
            statusLabel.ForeColor = Theme.TextGray;
            statusLabel.Text = "Ready";
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus = new Label(); // Dummy for cross-thread helper
            
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);

            // Main Content
            flowPanel = new FlowLayoutPanel();
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.AutoScroll = true;
            flowPanel.Padding = new Padding(20);
            flowPanel.BackColor = Theme.BackDark;

            this.Controls.Add(flowPanel);
            this.Controls.Add(topPanel);

            // Link status label helper
            lblStatus.Tag = statusLabel;
        }

        private void UpdateStatus(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateStatus), text);
            }
            else
            {
                ((ToolStripStatusLabel)lblStatus.Tag).Text = text;
            }
        }

        private void InitialLoad(object state)
        {
            UpdateStatus("Checking for updates...");
            CheckAndInstallUpdates();
            
            if (string.IsNullOrEmpty(ScrcpyPath))
            {
                UpdateStatus("Scrcpy not found! Please check internet connection.");
                return;
            }

            UpdateStatus("Ready");
            LoadDevices();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadDevices();
        }

        private void BtnWireless_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ScrcpyPath)) 
            {
                MessageBox.Show("Scrcpy/ADB not found yet.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string adbPath = Path.Combine(ScrcpyPath, "adb.exe");
            using (WirelessConnectForm form = new WirelessConnectForm(adbPath))
            {
                form.ShowDialog(this);
            }
            // Auto refresh after closing
            LoadDevices();
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ScrcpyPath)) return;

            int count = 0;
            foreach (Control c in flowPanel.Controls)
            {
                if (c is DeviceCard)
                {
                    DeviceCard card = (DeviceCard)c;
                    if (card.IsSelected)
                    {
                        LaunchScrcpy(card.Serial);
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                MessageBox.Show("Please select at least one device.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadDevices()
        {
            if (string.IsNullOrEmpty(ScrcpyPath)) return;

            UpdateStatus("Scanning for devices...");
            
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(LoadDevices));
                return;
            }

            flowPanel.Controls.Clear();
            ThreadPool.QueueUserWorkItem(new WaitCallback(FetchDevicesAsync));
        }

        private void FetchDevicesAsync(object state)
        {
            string adbPath = Path.Combine(ScrcpyPath, "adb.exe");
            if (!File.Exists(adbPath)) return;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = "devices",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.EndsWith("\tdevice"))
                        {
                            string serial = line.Split('\t')[0];
                            AddDeviceToUI(adbPath, serial);
                        }
                    }
                }
                UpdateStatus("Ready");
            }
            catch (Exception ex)
            {
                UpdateStatus(string.Format("Error: {0}", ex.Message));
            }
        }

        private void AddDeviceToUI(string adbPath, string serial)
        {
            string model = GetProp(adbPath, serial, "ro.product.model");
            string brand = GetProp(adbPath, serial, "ro.product.manufacturer");
            string version = GetProp(adbPath, serial, "ro.build.version.release");
            
            // Capitalize brand
            if (!string.IsNullOrEmpty(brand) && brand.Length > 1)
                brand = char.ToUpper(brand[0]) + brand.Substring(1);

            Image screenshot = GetDeviceScreenshot(adbPath, serial);

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, string, string, string, Image>((s, m, b, v, img) => 
                {
                    DeviceCard card = new DeviceCard(s, m, b, v, img);
                    flowPanel.Controls.Add(card);
                }), serial, model, brand, version, screenshot);
            }
            else
            {
                DeviceCard card = new DeviceCard(serial, model, brand, version, screenshot);
                flowPanel.Controls.Add(card);
            }
        }

        private string GetProp(string adbPath, string serial, string prop)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = string.Format("-s {0} shell getprop {1}", serial, prop),
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit();
                    return output;
                }
            }
            catch { return "Unknown"; }
        }

        private Image GetDeviceScreenshot(string adbPath, string serial)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = string.Format("-s {0} shell screencap -p", serial),
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        p.StandardOutput.BaseStream.CopyTo(ms);
                        p.WaitForExit();
                        try 
                        {
                            ms.Position = 0;
                            return Image.FromStream(ms);
                        }
                        catch
                        {
                            // Fallback for line ending issues
                            return GetDeviceScreenshotFallback(adbPath, serial);
                        }
                    }
                }
            }
            catch { return null; }
        }

        private Image GetDeviceScreenshotFallback(string adbPath, string serial)
        {
            try
            {
                string tempRemote = string.Format("/data/local/tmp/scrcpy_preview_{0}.png", DateTime.Now.Ticks);
                string tempLocal = Path.Combine(Path.GetTempPath(), string.Format("scrcpy_preview_{0}.png", DateTime.Now.Ticks));

                RunAdbCommand(adbPath, string.Format("-s {0} shell screencap -p {1}", serial, tempRemote));
                RunAdbCommand(adbPath, string.Format("-s {0} pull {1} \"{2}\"", serial, tempRemote, tempLocal));
                RunAdbCommand(adbPath, string.Format("-s {0} shell rm {1}", serial, tempRemote));

                if (File.Exists(tempLocal))
                {
                    using (FileStream fs = new FileStream(tempLocal, FileMode.Open, FileAccess.Read))
                    {
                        Image img = Image.FromStream(fs);
                        Image copy = new Bitmap(img);
                        return copy;
                    }
                }
            }
            catch { }
            return null;
        }

        private void RunAdbCommand(string adbPath, string args)
        {
             ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi).WaitForExit();
        }

        private void LaunchScrcpy(string serial)
        {
            string scrcpyExe = Path.Combine(ScrcpyPath, "scrcpy.exe");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = scrcpyExe,
                Arguments = string.Format("-s {0}", serial),
                UseShellExecute = false
            };
            Process.Start(psi);
        }

        private void CheckAndInstallUpdates()
        {
            try
            {
                string downloadUrl;
                string latestVersion = GetLatestVersion(out downloadUrl);

                if (!string.IsNullOrEmpty(latestVersion))
                {
                    string expectedFolderName = string.Format("scrcpy-win64-{0}", latestVersion);
                    string expectedPath = Path.Combine(BaseDir, expectedFolderName);

                    if (!Directory.Exists(expectedPath))
                    {
                        UpdateStatus(string.Format("Downloading update {0}...", latestVersion));
                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            if (InstallScrcpy(latestVersion, downloadUrl))
                            {
                                ScrcpyPath = expectedPath;
                            }
                        }
                    }
                    else
                    {
                        ScrcpyPath = expectedPath;
                    }
                }

                if (string.IsNullOrEmpty(ScrcpyPath))
                {
                    ScrcpyPath = FindExistingScrcpy();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (string.IsNullOrEmpty(ScrcpyPath))
                {
                    ScrcpyPath = FindExistingScrcpy();
                }
            }
        }

        private string GetLatestVersion(out string downloadUrl)
        {
            downloadUrl = null;
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "ScrcpyLauncher");
                    string url = string.Format("https://api.github.com/repos/{0}/{1}/releases/latest", RepoOwner, RepoName);
                    string json = client.DownloadString(url);

                    Match tagMatch = Regex.Match(json, "\"tag_name\":\\s*\"(.*?)\"");
                    string tagName = tagMatch.Success ? tagMatch.Groups[1].Value : null;

                    MatchCollection assets = Regex.Matches(json, "\\{[^\\}]*\"name\":\\s*\"(scrcpy-win64-[^\"]+\\.zip)\"[^\\}]*\"browser_download_url\":\\s*\"([^\"]+)\"[^\\}]*\\}");
                    
                    foreach (Match match in assets)
                    {
                        if (match.Success)
                        {
                            downloadUrl = match.Groups[2].Value;
                            break;
                        }
                    }
                    
                    if (downloadUrl == null)
                    {
                         Match urlMatch = Regex.Match(json, "\"browser_download_url\":\\s*\"([^\"]+scrcpy-win64-[^\"]+\\.zip)\"");
                         if (urlMatch.Success)
                         {
                             downloadUrl = urlMatch.Groups[1].Value;
                         }
                    }

                    return tagName;
                }
            }
            catch { return null; }
        }

        private bool InstallScrcpy(string version, string url)
        {
            string zipPath = Path.Combine(BaseDir, string.Format("scrcpy-win64-{0}.zip", version));
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, zipPath);
                }
                ZipFile.ExtractToDirectory(zipPath, BaseDir);
                File.Delete(zipPath);
                return true;
            }
            catch { return false; }
        }

        private string FindExistingScrcpy()
        {
            string[] dirs = Directory.GetDirectories(BaseDir, "scrcpy-win64-*");
            Array.Sort(dirs);
            Array.Reverse(dirs);
            return dirs.Length > 0 ? dirs[0] : null;
        }
    }

    public class WirelessConnectForm : Form
    {
        private string AdbPath;
        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtPairCode;
        private TextBox txtConnectPort;
        private Label lblStatus;

        public WirelessConnectForm(string adbPath)
        {
            this.AdbPath = adbPath;
            this.Text = "Wireless Debugging";
            this.Size = new Size(400, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BackDark;
            this.ForeColor = Theme.TextWhite;
            this.Font = Theme.FontNormal;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            InitializeControls();
        }

        private void InitializeControls()
        {
            // Main Layout
            FlowLayoutPanel mainPanel = new FlowLayoutPanel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.FlowDirection = FlowDirection.TopDown;
            mainPanel.Padding = new Padding(20);
            mainPanel.WrapContents = false;
            mainPanel.AutoScroll = true;

            // --- PAIRING SECTION ---
            Label lblPairHeader = new Label { Text = "1. Pair Device (Android 11+)", Font = Theme.FontTitle, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            
            Label lblIp = new Label { Text = "IP Address:", AutoSize = true };
            txtIp = new TextBox { Width = 340, BackColor = Theme.BackLight, ForeColor = Theme.TextWhite, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblPort = new Label { Text = "Port:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            txtPort = new TextBox { Width = 340, BackColor = Theme.BackLight, ForeColor = Theme.TextWhite, BorderStyle = BorderStyle.FixedSingle };
            
            Label lblCode = new Label { Text = "Pairing Code:", AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            txtPairCode = new TextBox { Width = 340, BackColor = Theme.BackLight, ForeColor = Theme.TextWhite, BorderStyle = BorderStyle.FixedSingle };

            ModernButton btnPair = new ModernButton { Text = "PAIR DEVICE", Width = 340, Height = 35, BackColor = Theme.Accent, Margin = new Padding(0, 15, 0, 0) };
            btnPair.Click += BtnPair_Click;

            // --- CONNECT SECTION ---
            Label lblConnectHeader = new Label { Text = "2. Connect", Font = Theme.FontTitle, AutoSize = true, Margin = new Padding(0, 25, 0, 10) };
            
            Label lblConnectPort = new Label { Text = "Connect Port (Usually different):", AutoSize = true };
            txtConnectPort = new TextBox { Width = 340, BackColor = Theme.BackLight, ForeColor = Theme.TextWhite, BorderStyle = BorderStyle.FixedSingle };

            ModernButton btnConnect = new ModernButton { Text = "CONNECT", Width = 340, Height = 35, BackColor = Color.FromArgb(40, 167, 69), Margin = new Padding(0, 15, 0, 0) };
            btnConnect.Click += BtnConnect_Click;

            // --- STATUS ---
            lblStatus = new Label { Text = "Ready", AutoSize = true, ForeColor = Theme.TextGray, Margin = new Padding(0, 15, 0, 0), MaximumSize = new Size(340, 0) };

            mainPanel.Controls.AddRange(new Control[] { 
                lblPairHeader, 
                lblIp, txtIp, 
                lblPort, txtPort, 
                lblCode, txtPairCode, 
                btnPair,
                lblConnectHeader,
                lblConnectPort, txtConnectPort,
                btnConnect,
                lblStatus
            });

            this.Controls.Add(mainPanel);
        }

        private void BtnPair_Click(object sender, EventArgs e)
        {
            string ip = txtIp.Text.Trim();
            string port = txtPort.Text.Trim();
            string code = txtPairCode.Text.Trim();

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(code))
            {
                lblStatus.Text = "Please enter IP, Port, and Pairing Code.";
                lblStatus.ForeColor = Color.Red;
                return;
            }

            lblStatus.Text = "Pairing...";
            lblStatus.ForeColor = Theme.TextGray;
            Application.DoEvents();

            ThreadPool.QueueUserWorkItem(state => 
            {
                string result = RunAdbCommand(string.Format("pair {0}:{1} {2}", ip, port, code));
                this.Invoke(new Action(() => 
                {
                    lblStatus.Text = result;
                    lblStatus.ForeColor = result.Contains("Successfully paired") ? Color.LightGreen : Color.Red;
                }));
            });
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            string ip = txtIp.Text.Trim();
            string port = txtConnectPort.Text.Trim();

            if (string.IsNullOrEmpty(port))
            {
                // If user didn't enter connect port, try using the pairing port (unlikely to work for ADB wireless, but fallback) or just IP
                 if (string.IsNullOrEmpty(ip))
                {
                    lblStatus.Text = "Please enter IP.";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }
            }

            string target = ip;
            if (!string.IsNullOrEmpty(port)) target += ":" + port;

            lblStatus.Text = "Connecting...";
            lblStatus.ForeColor = Theme.TextGray;
            Application.DoEvents();

            ThreadPool.QueueUserWorkItem(state => 
            {
                string result = RunAdbCommand(string.Format("connect {0}", target));
                this.Invoke(new Action(() => 
                {
                    lblStatus.Text = result;
                    lblStatus.ForeColor = result.Contains("connected to") ? Color.LightGreen : Color.Red;
                    
                    if (result.Contains("connected to"))
                    {
                        // Optional: Close after delay?
                        // Thread.Sleep(1000);
                        // this.Invoke(new Action(() => this.Close()));
                    }
                }));
            });
        }

        private string RunAdbCommand(string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = AdbPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    if (!string.IsNullOrEmpty(output)) return output.Trim();
                    return error.Trim();
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
    }

    // --- Custom Controls ---

    public class ModernButton : Button
    {
        public ModernButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.ForeColor = Theme.TextWhite;
            this.Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
        }
    }

    public class DeviceCard : Panel
    {
        public string Serial { get; private set; }
        public bool IsSelected { get; private set; }

        private PictureBox picPreview;
        private Label lblModel;
        private Label lblDetails;
        private Panel selectionIndicator;

        public DeviceCard(string serial, string model, string brand, string version, Image preview)
        {
            this.Serial = serial;
            this.Width = 220;
            this.Height = 320;
            this.Margin = new Padding(15);
            this.BackColor = Theme.BackLight;
            this.Cursor = Cursors.Hand;

            // Selection Indicator (Border effect)
            selectionIndicator = new Panel();
            selectionIndicator.Dock = DockStyle.Fill;
            selectionIndicator.BackColor = Color.Transparent;
            selectionIndicator.Padding = new Padding(2); // Border width
            selectionIndicator.Paint += SelectionIndicator_Paint;

            // Content Container
            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Theme.BackLight;

            // Preview Image
            picPreview = new PictureBox();
            picPreview.Dock = DockStyle.Top;
            picPreview.Height = 220;
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.BackColor = Color.Black;
            if (preview != null)
                picPreview.Image = preview;
            
            // Info Panel
            Panel infoPanel = new Panel();
            infoPanel.Dock = DockStyle.Fill;
            infoPanel.Padding = new Padding(10);

            lblModel = new Label();
            lblModel.Text = string.Format("{0} {1}", brand, model);
            lblModel.Font = Theme.FontTitle;
            lblModel.ForeColor = Theme.TextWhite;
            lblModel.Dock = DockStyle.Top;
            lblModel.Height = 25;
            lblModel.AutoEllipsis = true;

            lblDetails = new Label();
            lblDetails.Text = string.Format("Android {0}\nSN: {1}", version, serial);
            lblDetails.Font = Theme.FontSmall;
            lblDetails.ForeColor = Theme.TextGray;
            lblDetails.Dock = DockStyle.Fill;

            infoPanel.Controls.Add(lblDetails);
            infoPanel.Controls.Add(lblModel);

            content.Controls.Add(infoPanel);
            content.Controls.Add(picPreview);
            
            selectionIndicator.Controls.Add(content);
            this.Controls.Add(selectionIndicator);

            // Events
            this.Click += ToggleSelection;
            picPreview.Click += ToggleSelection;
            lblModel.Click += ToggleSelection;
            lblDetails.Click += ToggleSelection;
            infoPanel.Click += ToggleSelection;
        }

        private void ToggleSelection(object sender, EventArgs e)
        {
            IsSelected = !IsSelected;
            selectionIndicator.Invalidate(); // Redraw border
        }

        private void SelectionIndicator_Paint(object sender, PaintEventArgs e)
        {
            if (IsSelected)
            {
                int thickness = 4;
                using (Pen p = new Pen(Theme.Accent, thickness))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, selectionIndicator.Width - 1, selectionIndicator.Height - 1);
                }
            }
        }
    }
}
