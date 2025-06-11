using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        private CancellationTokenSource _portScanCancellationTokenSource;

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

            _portScanCancellationTokenSource = new CancellationTokenSource();

            try
            {
                if (chkCommonPorts.Checked)
                {
                    // Quét cổng phổ biến
                    var commonPorts = new[] {
                        21, 22, 23, 25, 53, 80, 110, 139, 143, 443, 445,
                        993, 995, 1433, 1521, 3306, 3389, 5432, 5900, 8080, 8443
                    };

                    var allResults = new List<PortScanResult>();
                    int scannedPortsCount = 0;

                    foreach (var port in commonPorts)
                    {
                        _portScanCancellationTokenSource.Token.ThrowIfCancellationRequested();
                        try
                        {
                            var results = await _scanner.ScanPortRangeAsync(_targetIP, port, port, _portScanCancellationTokenSource.Token);
                            allResults.AddRange(results);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        finally
                        {
                            scannedPortsCount++;
                            progressBar.Value = (int)((float)scannedPortsCount / commonPorts.Length * 100);
                            lblStatus.Text = $"Đang quét cổng {port} ({scannedPortsCount}/{commonPorts.Length})...";
                        }
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

                    var results = await _scanner.ScanPortRangeAsync(_targetIP, startPort, endPort, _portScanCancellationTokenSource.Token);

                    foreach (var result in results)
                    {
                        AddPortResult(result.Port, result.IsOpen);
                    }

                    progressBar.Value = progressBar.Maximum;
                    lblStatus.Text = $"Hoàn tất. Tìm thấy {results.Count} cổng mở";
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Đã dừng quét.";
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
                _portScanCancellationTokenSource?.Dispose();
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _portScanCancellationTokenSource?.Cancel();
            lblStatus.Text = "Đang dừng quét...";
        }

        private void AddPortResult(int port, bool isOpen)
        {
            var status = isOpen ? "Mở" : "Đóng";
            var serviceName = GetServiceName(port);
            var description = GetServiceDescription(serviceName);

            var rowIndex = dgvResults.Rows.Add(port, status, serviceName, description);
            dgvResults.Rows[rowIndex].DefaultCellStyle.ForeColor = isOpen ? Color.Green : Color.Red;
        }

        private string GetServiceName(int port)
        {
            var services = new Dictionary<int, string>
            {
                { 21, "FTP" }, { 22, "SSH" }, { 23, "Telnet" }, { 25, "SMTP" },
                { 53, "DNS" }, { 80, "HTTP" }, { 110, "POP3" }, { 139, "NetBIOS" },
                { 143, "IMAP" }, { 443, "HTTPS" }, { 445, "SMB" }, { 993, "IMAPS" },
                { 995, "POP3S" }, { 1433, "SQL Server" }, { 1521, "Oracle" }, { 3306, "MySQL" },
                { 3389, "RDP" }, { 5432, "PostgreSQL" }, { 5900, "VNC" }, { 8080, "HTTP Alt" },
                { 8443, "HTTPS Alt" }
            };
            return services.ContainsKey(port) ? services[port] : "Không xác định";
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
                { "IMAP", "Internet Message Access Protocol - Nhận email" },
                { "HTTPS", "HTTP Secure - Web bảo mật" },
                { "SMB", "Server Message Block - Chia sẻ tệp Windows" },
                { "SQL Server", "Microsoft SQL Server Database" },
                { "Oracle", "Oracle Database" },
                { "MySQL", "MySQL Database Server" },
                { "RDP", "Remote Desktop Protocol - Màn hình từ xa" },
                { "PostgreSQL", "PostgreSQL Database" },
                { "VNC", "Virtual Network Computing - Điều khiển máy tính từ xa" },
                { "HTTP Alt", "HTTP Alternate - Web thay thế" },
                { "HTTPS Alt", "HTTPS Alternate - Web bảo mật thay thế" }
            };
            return descriptions.ContainsKey(serviceName) ? descriptions[serviceName] : "Không có mô tả";
        }
    }
}
