using HDS_Demo.Models;
using HDS_Demo.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HDS_Demo
{
    public class HdsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        // ============================================================
        // Public UI-bound collections
        // ============================================================

        public ObservableCollection<string> EventLog { get; } = new();
        public ObservableCollection<string> Packages { get; } = new();
        public ObservableCollection<FaultSignature> FaultSummary { get; } = new();
        public ObservableCollection<RegisteredAppView> RegisteredApps { get; } = new();
        public ObservableCollection<FaultView> FaultDescriptions { get; } = new();
        public ObservableCollection<ProcessSnapshot> TopProcesses { get; } = new();

        public int FaultCount => FaultSummary.Count;

        // ============================================================
        // Live metrics
        // ============================================================

        public string CpuUsage { get => _cpu; set { _cpu = value; Notify(nameof(CpuUsage)); } }
        public string RamUsage { get => _ram; set { _ram = value; Notify(nameof(RamUsage)); } }
        public string GpuUsage { get => _gpu; set { _gpu = value; Notify(nameof(GpuUsage)); } }
        public string DiskIo { get => _disk; set { _disk = value; Notify(nameof(DiskIo)); } }
        public string NetUsage { get => _net; set { _net = value; Notify(nameof(NetUsage)); } }
        public string PcapStatus { get => _pcap; set { _pcap = value; Notify(nameof(PcapStatus)); } }

        private string _cpu, _ram, _gpu, _disk, _net, _pcap = "Idle";

        // ============================================================
        // Timers + system counters
        // ============================================================

        private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly PerformanceCounter cpu = new("Processor", "% Processor Time", "_Total");
        private readonly PerformanceCounter availMem = new("Memory", "Available MBytes");
        private readonly PerformanceCounter diskRead = new("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
        private readonly PerformanceCounter diskWrite = new("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

        private long lastNetBytes = 0;

        // ============================================================
        // Constructor
        // ============================================================

        public HdsViewModel()
        {
            cpu.NextValue(); // warmup

            timer.Tick += (_, __) => Tick();
            timer.Start();

            PcapStatus = "Active";
            Log("ViewModel ready.");
        }

        // ============================================================
        // Tick — runs every 1 second
        // ============================================================

        void Tick()
        {
            UpdateMetrics();
            UpdateTopProcesses();
            FetchRegisteredApps();
        }

        // ============================================================
        // THREAD-SAFE UI LOGGING
        // ============================================================

        void Log(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                EventLog.Add($"{timestamp}  {message}");
            });
        }

        // ============================================================
        // Fault trigger — CALLED BY SERVER
        // ============================================================

        public void TriggerFault(FaultSignature fault, FaultRegistry registry)
        {
            // Log capture request (UI thread)
            Log($"Time Series Slice Request:\n{JsonConvert.SerializeObject(fault.CaptureRequest, Formatting.Indented)}");

            // Heavy work runs off-UI-thread
            Task.Run(() =>
            {
                // =======================================================
                // 1. Generate the diagnostic package
                // =======================================================
                string packageFile = GeneratePackage(fault);

                // Attach the package filename to the fault object   ⭐ REQUIRED
                if (packageFile != "ERROR")
                    fault.PackageFile = packageFile;

                // =======================================================
                // 2. Refresh summary list off the UI thread
                // =======================================================
                var summaryList = registry
                    .GetAll()
                    .OrderByDescending(f => f.LastTimestamp)
                    .ToList();

                // =======================================================
                // 3. Apply all UI updates in a single atomic dispatcher call
                // =======================================================
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    // Add package to UI list only if valid
                    if (fault.PackageFile != null)
                        Packages.Add(fault.PackageFile);

                    // Update summary
                    FaultSummary.Clear();
                    foreach (var f in summaryList)
                        FaultSummary.Add(f);

                    // Raise FaultCount changed
                    Notify(nameof(FaultCount));
                });
            });
        }


        // ============================================================
        // Fault Summary refresh (external use also)
        // ============================================================

        public void UpdateFaultSummary(FaultRegistry registry)
        {
            Task.Run(() =>
            {
                var list = registry
                    .GetAll()
                    .OrderByDescending(f => f.LastTimestamp)
                    .ToList();

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    FaultSummary.Clear();
                    foreach (var f in list)
                        FaultSummary.Add(f);

                    Notify(nameof(FaultCount));
                });
            });
        }

        // ============================================================
        // Metrics
        // ============================================================

        void UpdateMetrics()
        {
            try
            {
                CpuUsage = $"{cpu.NextValue():0}%";

                float avail = availMem.NextValue();
                float total = GetTotalRamGb();
                float used = total - (avail / 1024f);
                RamUsage = $"{used:0.0} GB";

                GpuUsage = QueryGpu();
                DiskIo = $"{(diskRead.NextValue() + diskWrite.NextValue()) / 1024 / 1024:0.0} MB/s";

                NetUsage = $"{GetNetworkKb():0.0} KB/s";
            }
            catch (Exception ex)
            {
                Log("UpdateMetrics ERROR: " + ex.Message);
            }
        }

        string QueryGpu()
        {
            try
            {
                using var s = new ManagementObjectSearcher(
                    @"root\CIMV2",
                    "SELECT UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");

                foreach (ManagementObject o in s.Get())
                {
                    if (o["UtilizationPercentage"] is uint u)
                        return $"{u}%";
                }
            }
            catch { }

            return "0%";
        }

        double GetNetworkKb()
        {
            long total = 0;

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var stats = nic.GetIPStatistics();
                total += stats.BytesReceived + stats.BytesSent;
            }

            if (lastNetBytes == 0)
            {
                lastNetBytes = total;
                return 0;
            }

            long delta = total - lastNetBytes;
            lastNetBytes = total;
            return delta / 1024f;
        }

        float GetTotalRamGb()
        {
            MEMORYSTATUSEX m = new();
            if (GlobalMemoryStatusEx(m))
                return m.ullTotalPhys / 1024f / 1024f / 1024f;

            return 8f;
        }

        // ============================================================
        // Top Processes
        // ============================================================

        void UpdateTopProcesses()
        {
            try
            {
                var list = new List<ProcessSnapshot>();

                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        list.Add(new ProcessSnapshot
                        {
                            Pid = p.Id,
                            Name = p.ProcessName,
                            RamMB = p.WorkingSet64 / (1024f * 1024f),
                            Threads = p.Threads.Count
                        });
                    }
                    catch { }
                }

                var top = list
                    .OrderByDescending(x => x.RamMB)
                    .Take(10)
                    .ToList();

                TopProcesses.Clear();
                foreach (var item in top)
                    TopProcesses.Add(item);
            }
            catch (Exception ex)
            {
                Log("UpdateTopProcesses ERROR: " + ex.Message);
            }
        }

        // ============================================================
        // Registered Apps Polling
        // ============================================================

        private async void FetchRegisteredApps()
        {
            try
            {
                using var http = new HttpClient();
                var json = await http.GetStringAsync("http://localhost:5005/api/v1/apps");

                var root = JsonNode.Parse(json);
                var apps = root?["applications"]?.AsArray();
                if (apps == null) return;

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    RegisteredApps.Clear();
                    foreach (var a in apps)
                    {
                        RegisteredApps.Add(new RegisteredAppView
                        {
                            Application = a["application"]?.ToString(),
                            Version = a["version"]?.ToString(),
                            RegistrationId = a["registration_id"]?.ToString(),  
                            Registered = a["registered"]?.ToString(),
                            LastSeen = a["last_seen"]?.ToString(),
                            Online = true
                        });

                    }
                });
            }
            catch { }
        }

        // ============================================================
        // Package Generation (Final Improved Version)
        // ============================================================

        public string GeneratePackage(FaultSignature fault)
        {
            try
            {
                // =======================================================
                // 1. Heavy Work First — Build time-series data
                // =======================================================
                fault.TimeSeries = TimeSeriesBuilder.Build(fault.CaptureRequest);

                // =======================================================
                // 2. Prepare output paths
                // =======================================================
                string baseDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "DiagnosticsPackages");

                Directory.CreateDirectory(baseDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string folderName = $"pkg_{fault.ApplicationName}_{fault.FaultCode}_{timestamp}";
                string workFolder = Path.Combine(baseDir, folderName);
                string zipPath = Path.Combine(baseDir, folderName + ".zip");

                Directory.CreateDirectory(workFolder);

                // =======================================================
                // 3. Create package content into temp working folder
                // =======================================================
                CreateMockFiles(workFolder);
                CreateEventLogSnapshot(workFolder);
                CreateTextReport(workFolder, fault);
                CreateJsonReport(workFolder, fault);

                // =======================================================
                // 4. ZIP the working folder
                // =======================================================
                System.IO.Compression.ZipFile.CreateFromDirectory(workFolder, zipPath);

                // =======================================================
                // 5. Clean up the temporary folder (retry-safe)
                // =======================================================
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Directory.Delete(workFolder, true);
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(75);
                    }
                }

                // =======================================================
                // 6. Store package filename back to fault object  ⭐ IMPORTANT
                // =======================================================
                fault.PackageFile = Path.GetFileName(zipPath);

                // =======================================================
                // 7. Update fault index
                // =======================================================
                UpdateFaultIndex(new
                {
                    id = fault.FaultId,
                    file = fault.PackageFile,
                    fault = fault.FaultCode,
                    timestamp = fault.Timestamp,
                    app = AppInfo.ApplicationName
                });

                // =======================================================
                // 8. UI Log
                // =======================================================
                Application.Current.Dispatcher.BeginInvoke(() =>
                    Log($"Package saved: {fault.PackageFile}"));

                return fault.PackageFile;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                    Log("GeneratePackage ERROR: " + ex.Message));

                return "ERROR";
            }
        }



        // ============================================================
        // Helper: Mock Files
        // ============================================================

        void CreateMockFiles(string folder)
        {
            File.WriteAllText(Path.Combine(folder, "mock_capture.pcap"), "PCAP MOCK DATA");
            File.WriteAllText(Path.Combine(folder, "mock_dlt.log"), "[DLT] Mock diagnostic log");
        }

        // ============================================================
        // Helper: Event Log Snapshot
        // ============================================================

        void CreateEventLogSnapshot(string folder)
        {
            // STEP 1 — Copy EventLog on the UI thread
            List<string> copy = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                copy = EventLog.ToList();
            });

            // SAFETY FALLBACK — if no logs, write something
            if (copy.Count == 0)
            {
                File.WriteAllText(Path.Combine(folder, "event_log.txt"),
                    "[No events available]");
                return;
            }

            // STEP 2 — ALWAYS WRITE THE RAW LOG (unfiltered)
            File.WriteAllLines(Path.Combine(folder, "event_log.txt"), copy);
        }



        // ============================================================
        // Helper: Text Report
        // ============================================================

        void CreateTextReport(string folder, FaultSignature fault)
        {
            using var w = new StreamWriter(Path.Combine(folder, "diagnostic_report.txt"));

            w.WriteLine("=== HDS DIAGNOSTIC REPORT ===");
            w.WriteLine($"Generated: {DateTime.Now:O}");
            w.WriteLine($"App: {AppInfo.ApplicationName}");
            w.WriteLine($"Fault: {fault.FaultCode}");
            w.WriteLine($"Type: {fault.Type}");
            w.WriteLine($"Severity: {fault.Severity}");
            w.WriteLine($"Description: {fault.Description}");
            w.WriteLine($"Timestamp: {fault.Timestamp:O}");
            w.WriteLine();

            w.WriteLine("=== CAPTURE REQUEST ===");
            w.WriteLine($"LogFileLocation: {fault.CaptureRequest.LogFileLocation}");
            w.WriteLine("Capture:");
            foreach (var c in fault.CaptureRequest.Capture)
                w.WriteLine($" - {c}");
            w.WriteLine("Environment:");
            foreach (var e in fault.CaptureRequest.Environment)
                w.WriteLine($" - {e}");
            w.WriteLine();

            w.WriteLine("=== SNAPSHOT ===");
            w.WriteLine($"CPU: {CpuUsage}");
            w.WriteLine($"RAM: {RamUsage}");
            w.WriteLine($"GPU: {GpuUsage}");
            w.WriteLine($"Disk: {DiskIo}");
            w.WriteLine($"Net: {NetUsage}");
        }

        // ============================================================
        // Helper: JSON report
        // ============================================================

        void CreateJsonReport(string folder, FaultSignature fault)
        {
            var obj = new
            {
                fault = new
                {
                    id = fault.FaultId,
                    code = fault.FaultCode,
                    type = fault.Type,
                    severity = fault.Severity,
                    description = fault.Description,
                    timestamp = fault.Timestamp.ToString("O"),
                    app = AppInfo.ApplicationName
                },

                capture_request = fault.CaptureRequest,
                timeseries = fault.TimeSeries,

                snapshot = new
                {
                    cpu = CpuUsage,
                    ram = RamUsage,
                    gpu = GpuUsage,
                    disk = DiskIo,
                    network = NetUsage
                },

                event_log = EventLog.ToList()
            };

            string json = System.Text.Json.JsonSerializer.Serialize(obj,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(Path.Combine(folder, "fault.json"), json);
        }

        // ============================================================
        // Helper: Fault Index
        // ============================================================

        void UpdateFaultIndex(object entry)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiagnosticsPackages");
            string indexFile = Path.Combine(baseDir, "fault_index.json");

            List<object> list;

            if (File.Exists(indexFile))
            {
                var json = File.ReadAllText(indexFile);
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<object>>>(json);
                list = dict["packages"];
            }
            else list = new List<object>();

            list.Add(entry);

            var updated = new Dictionary<string, List<object>> { ["packages"] = list };

            File.WriteAllText(
                indexFile,
                System.Text.Json.JsonSerializer.Serialize(updated,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
            );
        }

        // ============================================================
        // Native memory struct
        // ============================================================

        [StructLayout(LayoutKind.Sequential)]
        class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX m);
    }

    // ============================================================
    // Models (unchanged)
    // ============================================================

    public class ProcessSnapshot
    {
        public int Pid { get; set; }
        public string Name { get; set; }
        public float RamMB { get; set; }
        public int Threads { get; set; }
    }

    public class RegisteredAppView
    {
        public string Application { get; set; }
        public string Version { get; set; }
        public string Registered { get; set; }
        public string LastSeen { get; set; }

        public string RegistrationId { get; set; }
        public bool Online { get; set; }
    }

    public class FaultView
    {
        public string FaultCode { get; set; }
        public string Severity { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Timestamp { get; set; }
        public string App { get; set; }
        public string FaultId { get; set; }
    }
}
