using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                if (!string.IsNullOrEmpty(hostEntry.HostName))
                {
                    return hostEntry.HostName;
                }
            }
            catch
            {
                //Bỏ qua lỗi và fallback
            }

            // Nếu DNS thất bại, thử bằng NetBIOS (nbtstat)
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nbtstat",
                        Arguments = $"-A {ipAddress}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("<00>") && line.Contains("UNIQUE"))
                    {
                        var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            return parts[0]; 
                        }
                    }
                }
            }
            catch
            {
                // Không tìm thấy qua nbtstat
            }

            return "Không xác định";
        }


        /// <summary>
        /// Quét các cổng phổ biến
        /// </summary>
        private async Task<List<int>> ScanCommonPortsAsync(string ipAddress)
        {
            var openPorts = new List<int>();
            var commonPorts = new[]
            { 21, 22, 23, 25, 53, 80, 110, 139, 143, 443, 445,
          993, 995, 1433, 1521, 3306, 3389, 5432, 5900, 8080, 8443 };

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
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    Blocking = false
                };

                var connectTask = socket.ConnectAsync(IPAddress.Parse(ipAddress), port);
                var timeoutTask = Task.Delay(300); // rút ngắn thời gian quét port 

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && socket.Connected)
                {
                    socket.Dispose();
                    return true;
                }

                socket.Dispose();
            }
            catch (SocketException)
            {
                // Cổng đóng hoặc unreachable
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi quét cổng {port}: {ex.Message}");
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
        public async Task<List<PortScanResult>> ScanPortRangeAsync(string ipAddress, int startPort, int endPort, CancellationToken token)
        {
            if (_isScanning)
            {
                throw new InvalidOperationException("Đang có một quá trình quét đang chạy. Vui lòng dừng trước khi bắt đầu quét mới.");
            }

            StartPortScan();
            var results = new List<PortScanResult>();
            var semaphore = new SemaphoreSlim(50); 
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
        /// Quét một dải cổng UDP cụ thể
        /// </summary>
        public async Task<List<PortScanResult>> ScanUdpPortRangeAsync(string ipAddress, int startPort, int endPort, CancellationToken token)
        {
            var results = new List<PortScanResult>();
            var semaphore = new SemaphoreSlim(50);
            var tasks = new List<Task>();
            for (int port = startPort; port <= endPort; port++)
            {
                if (token.IsCancellationRequested)
                    break;
                int currentPort = port;
                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        if (token.IsCancellationRequested)
                            return;
                        bool isOpen = await IsUdpPortOpenAsync(ipAddress, currentPort);
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
                    catch { }
                    finally
                    {
                        semaphore.Release();
                    }
                }, token);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            return results.OrderBy(r => r.Port).ToList();
        }

        /// <summary>
        /// Kiểm tra cổng UDP có phản hồi không
        /// </summary>
        private async Task<bool> IsUdpPortOpenAsync(string ipAddress, int port, int timeout = 1000)
        {
            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.Client.SendTimeout = timeout;
                    udpClient.Client.ReceiveTimeout = timeout;
                    var remoteEP = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                    byte[] sendBytes = new byte[] { 0x00 };
                    await udpClient.SendAsync(sendBytes, sendBytes.Length, remoteEP);
                    var receiveTask = udpClient.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(timeout)) == receiveTask)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
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

        public async Task ScanSpecificPortAsync(string ipAddress, int port, CancellationToken token)
        {
            await ScanPortRangeAsync(ipAddress, port, port, token);
        }

        internal async Task<IEnumerable<object>> ScanPortRangeAsync(string text, int value1, int value2)
        {
            throw new NotImplementedException();
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
