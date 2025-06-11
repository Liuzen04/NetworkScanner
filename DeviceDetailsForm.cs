using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace NetworkScanner
{
    /// <summary>
    /// Form hiển thị thông tin chi tiết về thiết bị
    /// </summary>
    public class DeviceDetailsForm : Form
    {
        private readonly NetworkDevice _device;
        private RichTextBox rtbDetails;

        public DeviceDetailsForm(NetworkDevice device)
        {
            _device = device;
            InitializeComponent();
            LoadDeviceDetails();
        }

        private void InitializeComponent()
        {
            this.Text = $"Chi tiết thiết bị - {_device.IPAddress}";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            rtbDetails = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };

            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var btnClose = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 30),
                Location = new Point(200, 10),
                DialogResult = DialogResult.OK
            };

            panel.Controls.Add(btnClose);
            this.Controls.AddRange(new Control[] { rtbDetails, panel });
        }

        private void LoadDeviceDetails()
        {
            rtbDetails.Clear();

            AppendTitle("THÔNG TIN THIẾT BỊ");
            AppendLine("");

            AppendLabel("Địa chỉ IP: ");
            AppendValue(_device.IPAddress);
            AppendLine("");

            AppendLabel("Địa chỉ MAC: ");
            AppendValue(_device.MACAddress);
            AppendLine("");

            AppendLabel("Tên máy: ");
            AppendValue(_device.Hostname);
            AppendLine("");

            AppendLabel("Trạng thái: ");
            AppendValue(_device.IsOnline ? "Trực tuyến" : "Ngoại tuyến",
                _device.IsOnline ? Color.Green : Color.Red);
            AppendLine("");

            AppendLabel("Thời gian phản hồi: ");
            AppendValue($"{_device.ResponseTime} ms");
            AppendLine("");

            AppendLabel("Lần cuối thấy: ");
            AppendValue(_device.LastSeen.ToString("dd/MM/yyyy HH:mm:ss"));
            AppendLine("");

            if (_device.OpenPorts.Count > 0)
            {
                AppendLine("");
                AppendTitle("CỔNG MỞ");
                AppendLine("");

                foreach (var port in _device.OpenPorts)
                {
                    AppendValue($"  • Cổng {port}", Color.Blue);
                    var serviceName = GetServiceName(port);
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        AppendValue($" - {serviceName}", Color.DarkGray);
                    }
                    AppendLine("");
                }
            }
        }

        private void AppendTitle(string text)
        {
            rtbDetails.SelectionFont = new Font(rtbDetails.Font.FontFamily, 12, FontStyle.Bold);
            rtbDetails.SelectionColor = Color.FromArgb(0, 120, 215);
            rtbDetails.AppendText(text);
        }

        private void AppendLabel(string text)
        {
            rtbDetails.SelectionFont = new Font(rtbDetails.Font, FontStyle.Bold);
            rtbDetails.SelectionColor = Color.Black;
            rtbDetails.AppendText(text);
        }

        private void AppendValue(string text, Color? color = null)
        {
            rtbDetails.SelectionFont = new Font(rtbDetails.Font, FontStyle.Regular);
            rtbDetails.SelectionColor = color ?? Color.Black;
            rtbDetails.AppendText(text);
        }

        private void AppendLine(string text)
        {
            rtbDetails.AppendText(text + Environment.NewLine);
        }

        private string GetServiceName(int port)
        {
            var services = new Dictionary<int, string>
            {
                { 21, "FTP" }, { 22, "SSH" }, { 23, "Telnet" }, { 25, "SMTP" },
                { 53, "DNS" }, { 80, "HTTP" }, { 110, "POP3" }, { 139, "NetBIOS" },
                { 443, "HTTPS" }, { 445, "SMB" }, { 3389, "RDP" }, { 8080, "HTTP Alt" }
            };

            return services.ContainsKey(port) ? services[port] : "";
        }
    }
}