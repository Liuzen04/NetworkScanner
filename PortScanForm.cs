using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace NetworkScanner
{
    /// <summary>
    /// Form quét cổng chi tiết cho một thiết bị
    /// </summary>
    public class PortScanForm : Form
    {
        private readonly string _targetIP;
        private readonly NetworkScannerCore _scanner;
        private DataGridView dgvResults;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Button btnScan;
        private Button btnStop;
        private NumericUpDown nudStartPort;
        private NumericUpDown nudEndPort;
        private CheckBox chkCommonPorts;

        public PortScanForm(string targetIP, NetworkScannerCore scanner)
        {
            _targetIP = targetIP;
            _scanner = scanner;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Quét cổng - {_targetIP}";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Icon = SystemIcons.Information;

            var lblTitle = new Label
            {
                Text = $"Quét cổng cho thiết bị: {_targetIP}",
                Location = new Point(20, 20),
                Size = new Size(760, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            var lblStartPort = new Label
            {
                Text = "Cổng bắt đầu:",
                Location = new Point(20, 70),
                Size = new Size(100, 23)
            };

            nudStartPort = new NumericUpDown
            {
                Location = new Point(125, 68),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 65535,
                Value = 1
            };

            var lblEndPort = new Label
            {
                Text = "Cổng kết thúc:",
                Location = new Point(220, 70),
                Size = new Size(100, 23)
            };

            nudEndPort = new NumericUpDown
            {
                Location = new Point(325, 68),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 65535,
                Value = 1000
            };

            chkCommonPorts = new CheckBox
            {
                Text = "Chỉ quét cổng phổ biến",
                Location = new Point(430, 70),
                Size = new Size(200, 23),
                Checked = false
            };

            btnScan = new Button
            {
                Text = "Bắt đầu quét",
                Location = new Point(20, 110),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(52, 199, 89),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnStop = new Button
            {
                Text = "Dừng quét",
                Location = new Point(150, 110),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(255, 59, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            progressBar = new ProgressBar
            {
                Location = new Point(20, 160),
                Size = new Size(600, 25),
                Style = ProgressBarStyle.Continuous
            };

            lblStatus = new Label
            {
                Location = new Point(630, 160),
                Size = new Size(130, 25),
                Text = "Sẵn sàng",
                TextAlign = ContentAlignment.MiddleLeft
            };

            dgvResults = new DataGridView
            {
                Location = new Point(20, 200),
                Size = new Size(740, 340),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D
            };

            dgvResults.Columns.Add("Port", "Cổng");
            dgvResults.Columns.Add("Status", "Trạng thái");
            dgvResults.Columns.Add("Service", "Dịch vụ");
            dgvResults.Columns.Add("Description", "Mô tả");

            dgvResults.Columns[0].Width = 80;
            dgvResults.Columns[1].Width = 100;
            dgvResults.Columns[2].Width = 150;
            dgvResults.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            btnScan.Click += BtnScan_Click;
            btnStop.Click += BtnStop_Click;
            chkCommonPorts.CheckedChanged += (s, e) =>
            {
                nudStartPort.Enabled = !chkCommonPorts.Checked;
                nudEndPort.Enabled = !chkCommonPorts.Checked;
            };

            this.Controls.AddRange(new Control[] {
                lblTitle, lblStartPort, nudStartPort, lblEndPort, nudEndPort,
                chkCommonPorts, btnScan, btnStop, progressBar, lblStatus, dgvResults
            });
        }

        private async void BtnScan_Click(object sender, EventArgs e)
        {
            if (_scanner.IsScanning)
            {
                MessageBox.Show("Đang có một quá trình quét đang chạy. Vui lòng dừng trước khi bắt đầu quét mới.", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dgvResults.Rows.Clear();
            progressBar.Value = 0;
            btnScan.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = "Đang quét...";

            try
            {
                if (chkCommonPorts.Checked)
                {
                    // Quét cổng phổ biến
                    var commonPorts = new[] {
                        21, 22, 23, 25, 53, 80, 110, 139, 143, 443, 445,
                        993, 995, 1433, 1521, 3306, 3389, 5432, 5900, 8080, 8443
                    };

                    // Thay vì quét từng cổng, gọi ScanPortRangeAsync cho từng cổng
                    var allResults = new List<PortScanResult>();

                    foreach (var port in commonPorts)
                    {
                        if (!_scanner.IsScanning) break; // Kiểm tra nếu đã dừng

                        var results = await _scanner.ScanPortRangeAsync(_targetIP, port, port);
                        allResults.AddRange(results);
                        
                        // Cập nhật tiến trình
                        progressBar.Value = (int)((float)allResults.Count / commonPorts.Length * 100);
                        lblStatus.Text = $"Đang quét cổng {port}...";
                    }

                    foreach (var result in allResults)
                    {
                        AddPortResult(result.Port, result.IsOpen);
                    }

                    lblStatus.Text = $"Hoàn tất. Tìm thấy {allResults.Count} cổng mở";
                }
                else
                {
                    // Quét dải cổng
                    int startPort = (int)nudStartPort.Value;
                    int endPort = (int)nudEndPort.Value;

                    lblStatus.Text = $"Đang quét cổng {startPort} - {endPort}...";
                    progressBar.Maximum = endPort - startPort + 1;

                    var results = await _scanner.ScanPortRangeAsync(_targetIP, startPort, endPort);

                    foreach (var result in results)
                    {
                        AddPortResult(result.Port, result.IsOpen);
                    }

                    progressBar.Value = progressBar.Maximum;
                    lblStatus.Text = $"Hoàn tất. Tìm thấy {results.Count} cổng mở";
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi quét: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnScan.Enabled = true;
                btnStop.Enabled = false;
                if (!_scanner.IsScanning)
                {
                    lblStatus.Text = "Đã dừng quét";
                }
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _scanner.StopPortScan();
            btnStop.Enabled = false;
            lblStatus.Text = "Đang dừng quét...";
        }

        private async Task<bool> IsPortOpenAsync(string ip, int port)
        {
            try
            {
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    var connectTask = tcp.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(1000);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    return completedTask == connectTask && tcp.Connected;
                }
            }
            catch
            {
                return false;
            }

        }

        private void AddPortResult(int port, bool isOpen)
        {
            if (!isOpen) return;

            var serviceName = GetServiceName(port);
            var description = GetServiceDescription(serviceName);

            var row = dgvResults.Rows.Add(port, "Mở", serviceName, description);
            dgvResults.Rows[row].DefaultCellStyle.ForeColor = Color.Green;
        }

        private string GetServiceName(int port)
        {
            var services = new Dictionary<int, string>
            {
                { 21, "FTP" }, { 22, "SSH" }, { 23, "Telnet" }, { 25, "SMTP" },
                { 53, "DNS" }, { 80, "HTTP" }, { 110, "POP3" }, { 139, "NetBIOS" },
                { 143, "IMAP" }, { 443, "HTTPS" }, { 445, "SMB" }, { 993, "IMAPS" },
                { 995, "POP3S" }, { 1433, "SQL Server" }, { 1521, "Oracle" },
                { 3306, "MySQL" }, { 3389, "RDP" }, { 5432, "PostgreSQL" },
                { 5900, "VNC" }, { 8080, "HTTP Alt" }, { 8443, "HTTPS Alt" }
            };

            return services.ContainsKey(port) ? services[port] : "Không xác định";
        }

        private string GetServiceDescription(string serviceName)
        {
            var descriptions = new Dictionary<string, string>
            {
                { "FTP", "File Transfer Protocol" },
                { "SSH", "Secure Shell - Kết nối bảo mật" },
                { "Telnet", "Kết nối từ xa không bảo mật" },
                { "SMTP", "Gửi thư điện tử" },
                { "DNS", "Phân giải tên miền" },
                { "HTTP", "Giao thức Web" },
                { "HTTPS", "Giao thức Web bảo mật" },
                { "RDP", "Remote Desktop - Màn hình từ xa Windows" },
                { "MySQL", "Cơ sở dữ liệu MySQL" },
                { "SMB", "Chia sẻ tệp Windows" }
            };

            return descriptions.ContainsKey(serviceName) ? descriptions[serviceName] : "";
        }
    }
}