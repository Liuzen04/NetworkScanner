using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace NetworkScanner
{
    public partial class MainForm : Form
    {
        //Biến form chính
        private NetworkScannerCore _scanner;
        private DataGridView dgvDevices;
        private ProgressBar progressBar;
        private Label lblProgress;
        private Button btnScan;
        private Button btnStop;
        private Button btnPortScan;
        private Button btnExport;
        private GroupBox grpScanSettings;
        private TextBox txtBaseIP;
        private NumericUpDown nudStartRange;
        private NumericUpDown nudEndRange;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private TabControl tabControl;
        private TabPage tabDevices;
        private TabPage tabPortScan;
        private TabPage tabNetworkInfo;
        private RichTextBox rtbNetworkInfo;
        private System.Windows.Forms.Timer refreshTimer;

        // Biến cho tab Port Scan
        private TextBox txtTargetIP;
        private DataGridView dgvPorts;
        private NumericUpDown nudStartPort;
        private NumericUpDown nudEndPort;
        private Button btnStartPortScan;
        private Button btnStopPortScan;
        private Label lblPortScanStatus;
        private ProgressBar portScanProgress;

        public MainForm()
        {
            InitializeComponent();
            _scanner = new NetworkScannerCore();
            SetupEventHandlers();
            LoadNetworkInfo();
        }
        private string GetLocalBaseIP()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    var ipProps = ni.GetIPProperties();
                    // Chỉ lấy card có gateway (thường là card đang kết nối mạng)
                    if (ipProps.GatewayAddresses.Any(g => !g.Address.ToString().Equals("0.0.0.0")))
                    {
                        foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                var segments = ip.Address.ToString().Split('.');
                                if (segments.Length == 4)
                                {
                                    return $"{segments[0]}.{segments[1]}.{segments[2]}";
                                }
                            }
                        }
                    }
                }
            }
            return "192.168.1";
        }

        private void InitializeComponent()
        {
            this.Text = "Network Scanner - Ứng dụng quét mạng LAN";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Information;

            // Menu Strip
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Tệp");
            var exportMenuItem = new ToolStripMenuItem("Xuất báo cáo...");
            var exitMenuItem = new ToolStripMenuItem("Thoát");
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { exportMenuItem, new ToolStripSeparator(), exitMenuItem });

            var toolsMenu = new ToolStripMenuItem("Công cụ");
            var encryptMenuItem = new ToolStripMenuItem("Mã hóa dữ liệu");
            var settingsMenuItem = new ToolStripMenuItem("Cài đặt");
            toolsMenu.DropDownItems.AddRange(new ToolStripItem[] { encryptMenuItem, settingsMenuItem });

            var helpMenu = new ToolStripMenuItem("Trợ giúp");
            var aboutMenuItem = new ToolStripMenuItem("Về chương trình");
            helpMenu.DropDownItems.Add(aboutMenuItem);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, toolsMenu, helpMenu });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Tab Control
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            // Tab 1: Quét thiết bị
            tabDevices = new TabPage("Quét thiết bị");
            tabDevices.Padding = new Padding(10);

            // Scan Settings Group
            grpScanSettings = new GroupBox();
            grpScanSettings.Text = "Cài đặt quét";
            grpScanSettings.Location = new Point(10, 10);
            grpScanSettings.Size = new Size(1150, 80);

            var lblBaseIP = new Label();
            lblBaseIP.Text = "Địa chỉ IP cơ sở:";
            lblBaseIP.Location = new Point(20, 30);
            lblBaseIP.Size = new Size(100, 23);

            txtBaseIP = new TextBox();
            txtBaseIP.Text = GetLocalBaseIP();
            txtBaseIP.Location = new Point(125, 27);
            txtBaseIP.Size = new Size(150, 23);

            var lblRange = new Label();
            lblRange.Text = "Phạm vi:";
            lblRange.Location = new Point(300, 30);
            lblRange.Size = new Size(60, 23);

            nudStartRange = new NumericUpDown();
            nudStartRange.Minimum = 1;
            nudStartRange.Maximum = 254;
            nudStartRange.Value = 1;
            nudStartRange.Location = new Point(365, 27);
            nudStartRange.Size = new Size(60, 23);

            var lblTo = new Label();
            lblTo.Text = "đến";
            lblTo.Location = new Point(430, 30);
            lblTo.Size = new Size(30, 23);

            nudEndRange = new NumericUpDown();
            nudEndRange.Minimum = 1;
            nudEndRange.Maximum = 254;
            nudEndRange.Value = 254;
            nudEndRange.Location = new Point(465, 27);
            nudEndRange.Size = new Size(60, 23);

            btnScan = new Button();
            btnScan.Text = "Bắt đầu quét";
            btnScan.Location = new Point(560, 25);
            btnScan.Size = new Size(120, 30);
            btnScan.BackColor = Color.FromArgb(0, 120, 215);
            btnScan.ForeColor = Color.White;
            btnScan.FlatStyle = FlatStyle.Flat;

            btnStop = new Button();
            btnStop.Text = "Dừng quét";
            btnStop.Location = new Point(690, 25);
            btnStop.Size = new Size(120, 30);
            btnStop.BackColor = Color.FromArgb(255, 59, 48);
            btnStop.ForeColor = Color.White;
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.Enabled = false;

            btnPortScan = new Button();
            btnPortScan.Text = "Quét cổng";
            btnPortScan.Location = new Point(820, 25);
            btnPortScan.Size = new Size(120, 30);
            btnPortScan.BackColor = Color.FromArgb(52, 199, 89);
            btnPortScan.ForeColor = Color.White;
            btnPortScan.FlatStyle = FlatStyle.Flat;

            btnExport = new Button();
            btnExport.Text = "Xuất báo cáo";
            btnExport.Location = new Point(950, 25);
            btnExport.Size = new Size(120, 30);
            btnExport.BackColor = Color.FromArgb(255, 149, 0);
            btnExport.ForeColor = Color.White;
            btnExport.FlatStyle = FlatStyle.Flat;

            grpScanSettings.Controls.AddRange(new Control[] {
                lblBaseIP, txtBaseIP, lblRange, nudStartRange, lblTo, nudEndRange,
                btnScan, btnStop, btnPortScan, btnExport
            });

            // Progress Bar
            progressBar = new ProgressBar();
            progressBar.Location = new Point(10, 100);
            progressBar.Size = new Size(1000, 25);
            progressBar.Style = ProgressBarStyle.Continuous;

            lblProgress = new Label();
            lblProgress.Location = new Point(1020, 100);
            lblProgress.Size = new Size(140, 25);
            lblProgress.Text = "Sẵn sàng";
            lblProgress.TextAlign = ContentAlignment.MiddleLeft;

            // DataGridView
            dgvDevices = new DataGridView();
            dgvDevices.Location = new Point(10, 140);
            dgvDevices.Size = new Size(1150, 380);
            dgvDevices.AllowUserToAddRows = false;
            dgvDevices.AllowUserToDeleteRows = false;
            dgvDevices.ReadOnly = true;
            dgvDevices.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDevices.MultiSelect = false;
            dgvDevices.RowHeadersVisible = false;
            dgvDevices.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDevices.BackgroundColor = Color.White;
            dgvDevices.BorderStyle = BorderStyle.Fixed3D;
            dgvDevices.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvDevices.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvDevices.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvDevices.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvDevices.EnableHeadersVisualStyles = false;

            // Thêm các cột
            dgvDevices.Columns.Add("Status", "Trạng thái");
            dgvDevices.Columns.Add("IPAddress", "Địa chỉ IP");      // Chú ý: IPAddress
            dgvDevices.Columns.Add("MACAddress", "Địa chỉ MAC");    // Chú ý: MACAddress
            dgvDevices.Columns.Add("Hostname", "Tên máy");
            dgvDevices.Columns.Add("ResponseTime", "Thời gian phản hồi (ms)");
            dgvDevices.Columns.Add("OpenPorts", "Cổng mở");
            dgvDevices.Columns.Add("LastSeen", "Lần cuối thấy");

            dgvDevices.Columns["Status"].Width = 80;
            dgvDevices.Columns["IPAddress"].Width = 120;
            dgvDevices.Columns["MACAddress"].Width = 150;
            dgvDevices.Columns["Hostname"].Width = 200;
            dgvDevices.Columns["ResponseTime"].Width = 120;
            dgvDevices.Columns["OpenPorts"].Width = 200;
            dgvDevices.Columns["LastSeen"].Width = 150;

            tabDevices.Controls.AddRange(new Control[] {
                grpScanSettings, progressBar, lblProgress, dgvDevices
            });

            // Tab 2: Quét cổng chi tiết
            tabPortScan = new TabPage("Quét cổng chi tiết");
            tabPortScan.Padding = new Padding(10);

            var pnlPortScan = new Panel();
            pnlPortScan.Dock = DockStyle.Fill;

            var lblTargetIP = new Label();
            lblTargetIP.Text = "IP đích:";
            lblTargetIP.Location = new Point(20, 20);
            lblTargetIP.Size = new Size(60, 23);

            txtTargetIP = new TextBox();
            txtTargetIP.Location = new Point(85, 17);
            txtTargetIP.Size = new Size(150, 23);

            var lblPortRange = new Label();
            lblPortRange.Text = "Cổng:";
            lblPortRange.Location = new Point(250, 20);
            lblPortRange.Size = new Size(40, 23);

            nudStartPort = new NumericUpDown();
            nudStartPort.Minimum = 1;
            nudStartPort.Maximum = 65535;
            nudStartPort.Value = 1;
            nudStartPort.Location = new Point(295, 17);
            nudStartPort.Size = new Size(80, 23);

            var lblPortTo = new Label();
            lblPortTo.Text = "đến";
            lblPortTo.Location = new Point(380, 20);
            lblPortTo.Size = new Size(30, 23);

            nudEndPort = new NumericUpDown();
            nudEndPort.Minimum = 1;
            nudEndPort.Maximum = 65535;
            nudEndPort.Value = 1000;
            nudEndPort.Location = new Point(415, 17);
            nudEndPort.Size = new Size(80, 23);

            btnStartPortScan = new Button();
            btnStartPortScan.Text = "Bắt đầu quét";
            btnStartPortScan.Location = new Point(520, 15);
            btnStartPortScan.Size = new Size(120, 30);
            btnStartPortScan.BackColor = Color.FromArgb(52, 199, 89);
            btnStartPortScan.ForeColor = Color.White;
            btnStartPortScan.FlatStyle = FlatStyle.Flat;

            btnStopPortScan = new Button();
            btnStopPortScan.Text = "Dừng quét";
            btnStopPortScan.Location = new Point(650, 15);
            btnStopPortScan.Size = new Size(120, 30);
            btnStopPortScan.BackColor = Color.FromArgb(255, 59, 48);
            btnStopPortScan.ForeColor = Color.White;
            btnStopPortScan.FlatStyle = FlatStyle.Flat;
            btnStopPortScan.Enabled = false;

            lblPortScanStatus = new Label();
            lblPortScanStatus.Text = "Sẵn sàng";
            lblPortScanStatus.Location = new Point(780, 20);
            lblPortScanStatus.Size = new Size(300, 23);

            portScanProgress = new ProgressBar();
            portScanProgress.Location = new Point(20, 50);
            portScanProgress.Size = new Size(1120, 25);
            portScanProgress.Style = ProgressBarStyle.Continuous;

            dgvPorts = new DataGridView();
            dgvPorts.Location = new Point(20, 85);
            dgvPorts.Size = new Size(1120, 425);
            dgvPorts.AllowUserToAddRows = false;
            dgvPorts.ReadOnly = true;
            dgvPorts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPorts.RowHeadersVisible = false;
            dgvPorts.BackgroundColor = Color.White;
            dgvPorts.BorderStyle = BorderStyle.Fixed3D;

            dgvPorts.Columns.Add("Port", "Cổng");
            dgvPorts.Columns.Add("Status", "Trạng thái");
            dgvPorts.Columns.Add("Service", "Dịch vụ");
            dgvPorts.Columns.Add("Description", "Mô tả");

            pnlPortScan.Controls.AddRange(new Control[] {
                lblTargetIP, txtTargetIP, lblPortRange, nudStartPort,
                lblPortTo, nudEndPort, btnStartPortScan, btnStopPortScan,
                lblPortScanStatus, portScanProgress, dgvPorts
            });

            tabPortScan.Controls.Add(pnlPortScan);

            // Tab 3: Thông tin mạng
            tabNetworkInfo = new TabPage("Thông tin mạng");
            tabNetworkInfo.Padding = new Padding(10);

            rtbNetworkInfo = new RichTextBox();
            rtbNetworkInfo.Dock = DockStyle.Fill;
            rtbNetworkInfo.ReadOnly = true;
            rtbNetworkInfo.Font = new Font("Consolas", 10);
            rtbNetworkInfo.BackColor = Color.FromArgb(245, 245, 245);

            var btnRefreshNetInfo = new Button();
            btnRefreshNetInfo.Text = "Làm mới";
            btnRefreshNetInfo.Location = new Point(1040, 10);
            btnRefreshNetInfo.Size = new Size(100, 30);
            btnRefreshNetInfo.BackColor = Color.FromArgb(0, 120, 215);
            btnRefreshNetInfo.ForeColor = Color.White;
            btnRefreshNetInfo.FlatStyle = FlatStyle.Flat;
            btnRefreshNetInfo.Click += (s, e) => LoadNetworkInfo();

            tabNetworkInfo.Controls.AddRange(new Control[] { rtbNetworkInfo, btnRefreshNetInfo });

            tabControl.TabPages.AddRange(new TabPage[] { tabDevices, tabPortScan, tabNetworkInfo });

            // Status Strip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Sẵn sàng");
            statusStrip.Items.Add(statusLabel);

            // Add controls to form
            this.Controls.Add(tabControl);
            this.Controls.Add(statusStrip);

            // Context Menu cho DataGridView
            var contextMenu = new ContextMenuStrip();
            var copyIPMenuItem = new ToolStripMenuItem("Sao chép địa chỉ IP");
            var copyMACMenuItem = new ToolStripMenuItem("Sao chép địa chỉ MAC");
            var scanPortsMenuItem = new ToolStripMenuItem("Quét cổng thiết bị này");
            contextMenu.Items.AddRange(new ToolStripItem[] { copyIPMenuItem, copyMACMenuItem, new ToolStripSeparator(), scanPortsMenuItem });
            dgvDevices.ContextMenuStrip = contextMenu;
            // Event handlers for context menu
            copyIPMenuItem.Click += (s, e) =>
            {
                if (dgvDevices.SelectedRows.Count > 0)
                {
                    var ip = dgvDevices.SelectedRows[0].Cells["IPAddress"].Value?.ToString();
                    if (!string.IsNullOrEmpty(ip))
                        Clipboard.SetText(ip);
                }
            };

            copyMACMenuItem.Click += (s, e) =>
            {
                if (dgvDevices.SelectedRows.Count > 0)
                {
                    var mac = dgvDevices.SelectedRows[0].Cells["MACAddress"].Value?.ToString();
                    if (!string.IsNullOrEmpty(mac))
                        Clipboard.SetText(mac);
                }
            };

            scanPortsMenuItem.Click += (s, e) =>
            {
                if (dgvDevices.SelectedRows.Count > 0)
                {
                    var ip = dgvDevices.SelectedRows[0].Cells["IPAddress"].Value?.ToString();
                    if (!string.IsNullOrEmpty(ip))
                    {
                        txtTargetIP.Text = ip;
                        tabControl.SelectedTab = tabPortScan;
                    }
                }
            };

            // Timer để tự động cập nhật
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 30000; // 30 giây
            refreshTimer.Tick += (s, e) => UpdateDeviceStatus();

            // Event handlers
            btnScan.Click += BtnScan_Click;
            btnStop.Click += BtnStop_Click;
            btnPortScan.Click += BtnPortScan_Click;
            btnExport.Click += BtnExport_Click;
            exitMenuItem.Click += (s, e) => Application.Exit();
            aboutMenuItem.Click += (s, e) => MessageBox.Show(
                "Network Scanner v1.0\nỨng dụng quét mạng LAN\n\nPhát triển bởi: [Long-Hải]\n© 2025",
                "Về chương trình",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            exportMenuItem.Click += BtnExport_Click;
            encryptMenuItem.Click += EncryptMenuItem_Click;

            // Event handlers for port scanning
            btnStartPortScan.Click += BtnStartPortScan_Click;
            btnStopPortScan.Click += BtnStopPortScan_Click;
        }

        private void SetupEventHandlers()
        {
            _scanner.ScanProgressChanged += Scanner_ScanProgressChanged;
            _scanner.DeviceFound += Scanner_DeviceFound;
            _scanner.ScanCompleted += Scanner_ScanCompleted;
        }

        private void Scanner_ScanProgressChanged(object sender, ScanProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Scanner_ScanProgressChanged(sender, e)));
                return;
            }

            progressBar.Value = e.Progress;
            lblProgress.Text = $"{e.ScannedIPs}/{e.TotalIPs} - {e.CurrentIP}";
            statusLabel.Text = $"Đang quét: {e.CurrentIP} ({e.Progress}%)";
        }

        private void Scanner_DeviceFound(object sender, DeviceFoundEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Scanner_DeviceFound(sender, e)));
                return;
            }

            var device = e.Device;
            var portsString = string.Join(", ", device.OpenPorts.Select(p => $"{p}/{GetServiceName(p)}"));

            var rowIndex = dgvDevices.Rows.Add(
                "🟢 Trực tuyến",
                device.IPAddress,
                device.MACAddress,
                device.Hostname,
                device.ResponseTime,
                portsString,
                device.LastSeen.ToString("dd/MM/yyyy HH:mm:ss")
            );

            dgvDevices.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DarkGreen;
        }

        private void Scanner_ScanCompleted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Scanner_ScanCompleted(sender, e)));
                return;
            }

            btnScan.Enabled = true;
            btnStop.Enabled = false;
            progressBar.Value = 100;
            lblProgress.Text = "Hoàn tất";
            statusLabel.Text = $"Quét hoàn tất. Tìm thấy {dgvDevices.Rows.Count} thiết bị.";

            refreshTimer.Start();
        }

        private async void BtnScan_Click(object sender, EventArgs e)
        {
            dgvDevices.Rows.Clear();
            progressBar.Value = 0;
            btnScan.Enabled = false;
            btnStop.Enabled = true;
            statusLabel.Text = "Đang bắt đầu quét...";

            try
            {
                await _scanner.StartScanAsync(
                    txtBaseIP.Text,
                    (int)nudStartRange.Value,
                    (int)nudEndRange.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi quét: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnScan.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _scanner.StopScan();
            btnScan.Enabled = true;
            btnStop.Enabled = false;
            statusLabel.Text = "Đã dừng quét";
            refreshTimer.Stop();
        }

        private void BtnPortScan_Click(object sender, EventArgs e)
        {
            if (dgvDevices.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một thiết bị để quét cổng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ip = dgvDevices.SelectedRows[0].Cells["IPAddress"].Value?.ToString();
            if (!string.IsNullOrEmpty(ip))
            {
                var form = new PortScanForm(ip, _scanner);
                form.ShowDialog();
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveDialog.Title = "Xuất báo cáo quét mạng";
                saveDialog.FileName = $"NetworkScan_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var writer = new System.IO.StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                        {
                            // Tiêu đề báo cáo
                            writer.WriteLine("BÁO CÁO QUÉT MẠNG LAN");
                            writer.WriteLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                            writer.WriteLine($"Phạm vi quét: {txtBaseIP.Text}.{nudStartRange.Value} - {txtBaseIP.Text}.{nudEndRange.Value}");
                            writer.WriteLine($"Tổng số thiết bị tìm thấy: {dgvDevices.Rows.Count}");
                            writer.WriteLine();

                            // Header
                            writer.WriteLine("STT,Địa chỉ IP,Địa chỉ MAC,Tên máy,Thời gian phản hồi (ms),Cổng mở,Lần cuối thấy");

                            // Data
                            for (int i = 0; i < dgvDevices.Rows.Count; i++)
                            {
                                var row = dgvDevices.Rows[i];
                                writer.WriteLine($"{i + 1}," +
                                    $"{row.Cells["IPAddress"].Value}," +
                                    $"{row.Cells["MACAddress"].Value}," +
                                    $"{row.Cells["Hostname"].Value}," +
                                    $"{row.Cells["ResponseTime"].Value}," +
                                    $"\"{row.Cells["OpenPorts"].Value}\"," +
                                    $"{row.Cells["LastSeen"].Value}");
                            }
                        }

                        MessageBox.Show("Xuất báo cáo thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void EncryptMenuItem_Click(object sender, EventArgs e)
        {
            var encryptForm = new EncryptionForm();
            encryptForm.ShowDialog();
        }

        private void LoadNetworkInfo()
        {
            rtbNetworkInfo.Clear();
            rtbNetworkInfo.AppendText("=== THÔNG TIN MẠNG CỤC BỘ ===\n\n");

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    rtbNetworkInfo.SelectionFont = new Font(rtbNetworkInfo.Font, FontStyle.Bold);
                    rtbNetworkInfo.AppendText($"[{ni.Name}] {ni.Description}\n");
                    rtbNetworkInfo.SelectionFont = new Font(rtbNetworkInfo.Font, FontStyle.Regular);

                    rtbNetworkInfo.AppendText($"  Loại: {ni.NetworkInterfaceType}\n");
                    rtbNetworkInfo.AppendText($"  Trạng thái: {ni.OperationalStatus}\n");
                    rtbNetworkInfo.AppendText($"  Tốc độ: {ni.Speed / 1000000} Mbps\n");
                    rtbNetworkInfo.AppendText($"  Địa chỉ MAC: {ni.GetPhysicalAddress()}\n");

                    IPInterfaceProperties ipProps = ni.GetIPProperties();
                    foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            rtbNetworkInfo.AppendText($"  Địa chỉ IPv4: {ip.Address}\n");
                            rtbNetworkInfo.AppendText($"  Subnet Mask: {ip.IPv4Mask}\n");
                        }
                    }

                    foreach (GatewayIPAddressInformation gw in ipProps.GatewayAddresses)
                    {
                        rtbNetworkInfo.AppendText($"  Gateway: {gw.Address}\n");
                    }

                    foreach (IPAddress dns in ipProps.DnsAddresses)
                    {
                        rtbNetworkInfo.AppendText($"  DNS: {dns}\n");
                    }

                    rtbNetworkInfo.AppendText("\n");
                }
            }

            // Thống kê
            rtbNetworkInfo.SelectionFont = new Font(rtbNetworkInfo.Font, FontStyle.Bold);
            rtbNetworkInfo.AppendText("\n=== THỐNG KÊ MẠNG ===\n");
            rtbNetworkInfo.SelectionFont = new Font(rtbNetworkInfo.Font, FontStyle.Regular);

            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            rtbNetworkInfo.AppendText($"Tên máy: {ipGlobalProperties.HostName}\n");
            rtbNetworkInfo.AppendText($"Domain: {ipGlobalProperties.DomainName}\n\n");

            // Kết nối TCP đang hoạt động
            var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();
            rtbNetworkInfo.AppendText($"Số kết nối TCP đang hoạt động: {tcpConnections.Length}\n");

            // Listener TCP
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            rtbNetworkInfo.AppendText($"Số cổng TCP đang lắng nghe: {tcpListeners.Length}\n");

            // UDP listeners
            var udpListeners = ipGlobalProperties.GetActiveUdpListeners();
            rtbNetworkInfo.AppendText($"Số cổng UDP đang lắng nghe: {udpListeners.Length}\n");
        }

        private void UpdateDeviceStatus()
        {
            // Cập nhật trạng thái thiết bị định kỳ
            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                var ip = row.Cells["IPAddress"].Value?.ToString();
                if (!string.IsNullOrEmpty(ip))
                {
                    Task.Run(async () =>
                    {
                        var ping = new Ping();
                        var reply = await ping.SendPingAsync(ip, 1000);

                        Invoke(new Action(() =>
                        {
                            if (reply.Status == IPStatus.Success)
                            {
                                row.Cells["Status"].Value = "🟢 Trực tuyến";
                                row.Cells["ResponseTime"].Value = reply.RoundtripTime;
                                row.Cells["LastSeen"].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                                row.DefaultCellStyle.ForeColor = Color.DarkGreen;
                            }
                            else
                            {
                                row.Cells["Status"].Value = "🔴 Ngoại tuyến";
                                row.DefaultCellStyle.ForeColor = Color.DarkRed;
                            }
                        }));
                    });
                }
            }
        }

        private string GetServiceName(int port)
        {
            var services = new Dictionary<int, string>
            {
                { 21, "FTP" },
                { 22, "SSH" },
                { 23, "Telnet" },
                { 25, "SMTP" },
                { 53, "DNS" },
                { 80, "HTTP" },
                { 110, "POP3" },
                { 139, "NetBIOS" },
                { 443, "HTTPS" },
                { 445, "SMB" },
                { 1433, "SQL Server" },
                { 3306, "MySQL" },
                { 3389, "RDP" },
                { 8080, "HTTP Alt" }
            };

            return services.ContainsKey(port) ? services[port] : "";
        }

        private string GetServiceDescription(string serviceName)
        {
            var descriptions = new Dictionary<string, string>
            {
                { "FTP", "File Transfer Protocol - Truyền tệp" },
                { "SSH", "Secure Shell - Kết nối bảo mật" },
                { "Telnet", "Terminal Network - Kết nối từ xa" },
                { "SMTP", "Simple Mail Transfer Protocol - Gửi email" },
                { "DNS", "Domain Name System - Phân giải tên miền" },
                { "HTTP", "HyperText Transfer Protocol - Web" },
                { "POP3", "Post Office Protocol - Nhận email" },
                { "NetBIOS", "Network Basic Input/Output System" },
                { "HTTPS", "HTTP Secure - Web bảo mật" },
                { "SMB", "Server Message Block - Chia sẻ tệp Windows" },
                { "SQL Server", "Microsoft SQL Server Database" },
                { "MySQL", "MySQL Database Server" },
                { "RDP", "Remote Desktop Protocol - Màn hình từ xa" },
                { "HTTP Alt", "HTTP Alternate - Web thay thế" }
            };

            return descriptions.ContainsKey(serviceName) ? descriptions[serviceName] : "Không có mô tả";
        }

        private async void BtnStartPortScan_Click(object sender, EventArgs e)
        {
            if (_scanner.IsScanning)
            {
                MessageBox.Show("Đang có một quá trình quét đang chạy. Vui lòng dừng trước khi bắt đầu quét mới.", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTargetIP.Text))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ IP đích!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IPAddress.Parse(txtTargetIP.Text);
            }
            catch
            {
                MessageBox.Show("Địa chỉ IP không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dgvPorts.Rows.Clear();
            btnStartPortScan.Enabled = false;
            btnStopPortScan.Enabled = true;
            lblPortScanStatus.Text = $"Đang quét cổng {txtTargetIP.Text}...";
            portScanProgress.Value = 0;
            portScanProgress.Maximum = (int)(nudEndPort.Value - nudStartPort.Value + 1);

            var cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var results = await _scanner.ScanPortRangeAsync(
                    txtTargetIP.Text,
                    (int)nudStartPort.Value,
                    (int)nudEndPort.Value,
                    cancellationTokenSource.Token);

                foreach (var result in results)
                {
                    var row = dgvPorts.Rows.Add(
                        result.Port,
                        "Mở",
                        result.ServiceName,
                        GetServiceDescription(result.ServiceName)
                    );
                    dgvPorts.Rows[row].DefaultCellStyle.ForeColor = Color.Green;
                }

                lblPortScanStatus.Text = $"Hoàn tất. Tìm thấy {results.Count} cổng mở";
                portScanProgress.Value = portScanProgress.Maximum;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi quét cổng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStartPortScan.Enabled = true;
                btnStopPortScan.Enabled = false;
                if (!_scanner.IsScanning)
                {
                    lblPortScanStatus.Text = "Đã dừng quét";
                }
            }
        }

        private void BtnStopPortScan_Click(object sender, EventArgs e)
        {
            _scanner.StopPortScan();
            btnStopPortScan.Enabled = false;
            lblPortScanStatus.Text = "Đang dừng quét...";
        }
    }
}
