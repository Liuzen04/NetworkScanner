using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkScanner
{
    /// <summary>
    /// Lớp chính để quét và phân tích mạng LAN
    /// </summary>
    public class NetworkScannerCore
    {
        // Import Windows API để lấy MAC Address
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(int destIP, int srcIP, byte[] macAddr, ref int length);

        // Sự kiện thông báo tiến trình
        public event EventHandler<ScanProgressEventArgs> ScanProgressChanged;
        public event EventHandler<DeviceFoundEventArgs> DeviceFound;
        public event EventHandler ScanCompleted;

        private readonly object _lockObject = new object();
        private List<NetworkDevice> _devices = new List<NetworkDevice>();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isScanning = false;

        /// <summary>
        /// Danh sách các thiết bị đã tìm thấy
        /// </summary>
        public List<NetworkDevice> Devices
        {
            get
            {
                lock (_lockObject)
                {
                    return new List<NetworkDevice>(_devices);
                }
            }
        }

        /// <summary>
        /// Trạng thái đang quét
        /// </summary>
        public bool IsScanning => _isScanning;

        /// <summary>
        /// Bắt đầu quét mạng LAN
        /// </summary>
        public async Task StartScanAsync(string baseIP = "192.168.1", int startRange = 1, int endRange = 254)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _devices.Clear();

            var tasks = new List<Task>();
            int totalIPs = endRange - startRange + 1;
            int scannedIPs = 0;

            for (int i = startRange; i <= endRange; i++)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                string ip = $"{baseIP}.{i}";

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var device = await ScanIPAsync(ip);
                        if (device != null)
                        {
                            lock (_lockObject)
                            {
                                _devices.Add(device);
                            }
                            DeviceFound?.Invoke(this, new DeviceFoundEventArgs { Device = device });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nếu cần
                        Console.WriteLine($"Lỗi khi quét {ip}: {ex.Message}");
                    }
                    finally
                    {
                        Interlocked.Increment(ref scannedIPs);
                        int progress = (scannedIPs * 100) / totalIPs;
                        ScanProgressChanged?.Invoke(this, new ScanProgressEventArgs
                        {
                            Progress = progress,
                            CurrentIP = ip,
                            TotalIPs = totalIPs,
                            ScannedIPs = scannedIPs
                        });
                    }
                }, _cancellationTokenSource.Token);

                tasks.Add(task);

                // Giới hạn số lượng task chạy đồng thời
                if (tasks.Count >= 20)
                {
                    await Task.WhenAny(tasks);
                    tasks.RemoveAll(t => t.IsCompleted);
                }
            }

            await Task.WhenAll(tasks);
            ScanCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Dừng quét mạng
        /// </summary>
        public void StopScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Quét một địa chỉ IP cụ thể
        /// </summary>
        private async Task<NetworkDevice> ScanIPAsync(string ipAddress)
        {
            var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 1000);

            if (reply.Status == IPStatus.Success)
            {
                var device = new NetworkDevice
                {
                    IPAddress = ipAddress,
                    IsOnline = true,
                    ResponseTime = reply.RoundtripTime,
                    LastSeen = DateTime.Now
                };

                // Lấy MAC Address
                device.MACAddress = GetMACAddress(ipAddress);

                // Lấy Hostname
                device.Hostname = await GetHostnameAsync(ipAddress);

                // Quét các cổng phổ biến
                device.OpenPorts = await ScanCommonPortsAsync(ipAddress);

                return device;
            }

            return null;
        }

        /// <summary>
        /// Lấy địa chỉ MAC của thiết bị
        /// </summary>
        private string GetMACAddress(string ipAddress)
        {
            try
            {
                IPAddress ip = IPAddress.Parse(ipAddress);
                byte[] macAddr = new byte[6];
                int length = macAddr.Length;

                int dest = BitConverter.ToInt32(ip.GetAddressBytes(), 0);
                if (SendARP(dest, 0, macAddr, ref length) == 0)
                {
                    string[] str = new string[length];
                    for (int i = 0; i < length; i++)
                        str[i] = macAddr[i].ToString("X2");

                    return string.Join(":", str);
                }
            }
            catch { }

            return "Không xác định";
        }

        /// <summary>
        /// Lấy hostname của thiết bị
        /// </summary>
        private async Task<string> GetHostnameAsync(string ipAddress)
        {
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                return hostEntry.HostName;
            }
            catch
            {
                return "Không xác định";
            }
        }

        /// <summary>
        /// Quét các cổng phổ biến
        /// </summary>
        private async Task<List<int>> ScanCommonPortsAsync(string ipAddress)
        {
            var openPorts = new List<int>();
            var commonPorts = new[] { 21, 22, 23, 25, 80, 110, 139, 443, 445, 3389, 8080 };

            var tasks = commonPorts.Select(port => Task.Run(async () =>
            {
                if (await IsPortOpenAsync(ipAddress, port))
                {
                    lock (_lockObject)
                    {
                        openPorts.Add(port);
                    }
                }
            }));

            await Task.WhenAll(tasks);
            return openPorts.OrderBy(p => p).ToList();
        }

        /// <summary>
        /// Kiểm tra xem một cổng có mở không
        /// </summary>
        private async Task<bool> IsPortOpenAsync(string ipAddress, int port)
        {
            try
            {
                using (var tcp = new TcpClient())
                {
                    var connectTask = tcp.ConnectAsync(ipAddress, port);
                    var timeoutTask = Task.Delay(2000);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && tcp.Connected)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                Console.WriteLine($"Lỗi khi kiểm tra cổng {port}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Bắt đầu quét cổng
        /// </summary>
        public void StartPortScan()
        {
            if (!_isScanning)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _isScanning = true;
            }
        }

        /// <summary>
        /// Dừng quét cổng
        /// </summary>
        public void StopPortScan()
        {
            if (_isScanning)
            {
                _cancellationTokenSource?.Cancel();
                _isScanning = false;
            }
        }

        /// <summary>
        /// Quét một dải cổng cụ thể
        /// </summary>
        public async Task<List<PortScanResult>> ScanPortRangeAsync(string ipAddress, int startPort, int endPort)
        {
            if (_isScanning)
            {
                throw new InvalidOperationException("Đang có một quá trình quét đang chạy. Vui lòng dừng trước khi bắt đầu quét mới.");
            }

            StartPortScan();
            var results = new List<PortScanResult>();
            var semaphore = new SemaphoreSlim(10); // Giảm từ 50 xuống 10
            var tasks = new List<Task>();

            try
            {
                for (int port = startPort; port <= endPort; port++)
                {
                    if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                    {
                        break;
                    }

                    int currentPort = port;
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                            {
                                return;
                            }

                            bool isOpen = await IsPortOpenAsync(ipAddress, currentPort);
                            if (isOpen)
                            {
                                var result = new PortScanResult
                                {
                                    Port = currentPort,
                                    IsOpen = true,
                                    ServiceName = GetServiceName(currentPort)
                                };

                                lock (_lockObject)
                                {
                                    results.Add(result);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error silently
                            Console.WriteLine($"Lỗi khi quét cổng {currentPort}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, _cancellationTokenSource.Token);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                StopPortScan();
            }

            return results.OrderBy(r => r.Port).ToList();
        }

        /// <summary>
        /// Lấy tên dịch vụ theo cổng
        /// </summary>
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
                { 5432, "PostgreSQL" },
                { 8080, "HTTP Alternate" }
            };

            return services.ContainsKey(port) ? services[port] : "Không xác định";
        }
    }

    #region Models
    /// <summary>
    /// Thông tin thiết bị mạng
    /// </summary>
    public class NetworkDevice
    {
        public string IPAddress { get; set; }
        public string MACAddress { get; set; }
        public string Hostname { get; set; }
        public bool IsOnline { get; set; }
        public long ResponseTime { get; set; }
        public DateTime LastSeen { get; set; }
        public List<int> OpenPorts { get; set; } = new List<int>();
        public string DeviceType { get; set; } = "Không xác định";
    }

    /// <summary>
    /// Kết quả quét cổng
    /// </summary>
    public class PortScanResult
    {
        public int Port { get; set; }
        public bool IsOpen { get; set; }
        public string ServiceName { get; set; }
    }

    #region Event Arguments
    public class ScanProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }
        public string CurrentIP { get; set; }
        public int TotalIPs { get; set; }
        public int ScannedIPs { get; set; }
    }

    public class DeviceFoundEventArgs : EventArgs
    {
        public NetworkDevice Device { get; set; }
    }
    #endregion
    #endregion
}