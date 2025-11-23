using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Application_A
{
    public partial class MainWindow : Window
    {
        private readonly HdsClient _client = new HdsClient();
        private const string LogFileLocation = "/var/logs/app/application_a.dlt";

        public MainWindow()
        {
            InitializeComponent();
            AutoRegister();
        }

        // ============================================================
        // LOGGING HELPERS
        // ============================================================
        private void Log(string message, string color = "#444")
        {
            LogList.Items.Add(new LogEntry
            {
                Message = $"{DateTime.Now:HH:mm:ss}  {message}",
                Color = color
            });

            LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
        }

        private void LogJsonResponse(string json, bool ok)
        {
            string pretty = PrettyJson(json);
            string color = ok ? "#0A0" : "#A00";

            Log($"Server Response:\n{pretty}", color);
        }

        public void LogJsonOutgoing(string label, string json)
        {
            Log($"{label}\n{PrettyJson(json)}", "#888");
        }

        private string PrettyJson(string raw)
        {
            try { return JToken.Parse(raw).ToString(Newtonsoft.Json.Formatting.Indented); }
            catch { return raw; }
        }

        // ============================================================
        // AUTO REGISTER ON STARTUP
        // ============================================================
        private async void AutoRegister()
        {
            var (ok, json) = await _client.RegisterAppAsync("Application A", "1.0.0");

            Log(ok ? "Application A registered with HDS." : "FAILED to register with HDS.",
                ok ? "#0A0" : "#A00");

            LogJsonResponse(json, ok);
        }

        // ============================================================
        // SIMULATED FAULTS
        // ============================================================
        private async void SimulateNullPointerFault()
        {
            try { string s = null; _ = s.Length; }
            catch (Exception ex) { Log("Caught exception: " + ex.Message, "#A00"); }
            finally
            {
                var fault = BuildFault_F018();
                var (ok, json) = await _client.ReportFaultAsync(fault);

                Log(ok ? "Sent fault F018F0 to HDS." : "Failed to send fault F018F0.",
                    ok ? "#0A0" : "#A00");

                LogJsonResponse(json, ok);
            }
        }

        private async void SimulateOutOfRangeFault()
        {
            try { int[] arr = new int[3]; _ = arr[10]; }
            catch (Exception ex) { Log("Caught exception: " + ex.Message, "#A00"); }
            finally
            {
                var fault = BuildFault_F021();
                var (ok, json) = await _client.ReportFaultAsync(fault);

                Log(ok ? "Sent fault F021F9 to HDS." : "Failed to send fault F021F9.",
                    ok ? "#0A0" : "#A00");

                LogJsonResponse(json, ok);
            }
        }

        private async void SimulateConfigMismatchFault()
        {
            try { throw new InvalidOperationException("Config missing"); }
            catch (Exception ex) { Log("Caught exception: " + ex.Message, "#A00"); }
            finally
            {
                var fault = BuildFault_F01C();
                var (ok, json) = await _client.ReportFaultAsync(fault);

                Log(ok ? "Sent fault F01CF4 to HDS." : "Failed to send fault F01CF4.",
                    ok ? "#0A0" : "#A00");

                LogJsonResponse(json, ok);
            }
        }

        private async void SimulateWatchdogTimeout()
        {
            try { await Task.Delay(10000); }
            catch (Exception ex) { Log("Caught exception: " + ex.Message, "#A00"); }
            finally
            {
                var fault = BuildFault_F01A();
                var (ok, json) = await _client.ReportFaultAsync(fault);

                Log(ok ? "Sent fault F01AF2 to HDS." : "Failed to send fault F01AF2.",
                    ok ? "#0A0" : "#A00");

                LogJsonResponse(json, ok);
            }
        }

        // ============================================================
        // FAULT BUILDERS (MATCH HDS EXACTLY)
        // ============================================================
        private FaultSignature BuildFault_F018()
        {
            return new FaultSignature
            {
                ApplicationName = "Application A",
                FaultCode = "F018",
                Description = "Null pointer dereference",
                Type = "F0",
                Severity = "Error",
                Timestamp = DateTime.UtcNow,

                CaptureRequest = new CaptureRequest
                {
                    LogFileLocation = LogFileLocation,
                    Capture = new List<string> { "DLTLogs", "MemoryDump" },
                    Environment = new List<string> { "CPU", "RAM", "THREADS" }
                }
            };
        }

        private FaultSignature BuildFault_F021()
        {
            return new FaultSignature
            {
                ApplicationName = "Application A",
                FaultCode = "F021",
                Description = "Out of range array access",
                Type = "F9",
                Severity = "Critical",
                Timestamp = DateTime.UtcNow,

                CaptureRequest = new CaptureRequest
                {
                    LogFileLocation = LogFileLocation,
                    Capture = new List<string> { "DLTLogs", "PCAP" },
                    Environment = new List<string> { "CPU", "RAM" }
                }
            };
        }

        private FaultSignature BuildFault_F01C()
        {
            return new FaultSignature
            {
                ApplicationName = "Application A",
                FaultCode = "F01C",
                Description = "Configuration mismatch detected",
                Type = "F4",
                Severity = "Warning",
                Timestamp = DateTime.UtcNow,

                CaptureRequest = new CaptureRequest
                {
                    LogFileLocation = LogFileLocation,
                    Capture = new List<string> { "DLTLogs" },
                    Environment = new List<string> { "DISK", "RAM" }
                }
            };
        }

        private FaultSignature BuildFault_F01A()
        {
            return new FaultSignature
            {
                ApplicationName = "Application A",
                FaultCode = "F01A",
                Description = "Watchdog timeout",
                Type = "F2",
                Severity = "Error",
                Timestamp = DateTime.UtcNow,

                CaptureRequest = new CaptureRequest
                {
                    LogFileLocation = LogFileLocation,
                    Capture = new List<string> { "DLTLogs", "PCAP", "ThreadDump" },
                    Environment = new List<string> { "CPU", "RAM", "NETWORK", "THREADS" }
                }
            };
        }

        // ============================================================
        // UI BUTTONS
        // ============================================================
        private void Btn_NullPointer(object sender, RoutedEventArgs e) => SimulateNullPointerFault();
        private void Btn_OutOfRange(object sender, RoutedEventArgs e) => SimulateOutOfRangeFault();
        private void Btn_ConfigMismatch(object sender, RoutedEventArgs e) => SimulateConfigMismatchFault();
        private void Btn_Timeout(object sender, RoutedEventArgs e) => SimulateWatchdogTimeout();
    }

    // =================================================================
    // DATA MODELS THAT MUST MATCH HDS EXACTLY
    // =================================================================
    public class LogEntry
    {
        public string Message { get; set; }
        public string Color { get; set; }
    }

    public class CaptureRequest
    {
        public string LogFileLocation { get; set; }
        public List<string> Capture { get; set; }
        public List<string> Environment { get; set; }
    }

    public class FaultSignature
    {
        public string ApplicationName { get; set; }
        public string FaultCode { get; set; }
        public string Type { get; set; }        // F0–FE
        public string Severity { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }

        public CaptureRequest CaptureRequest { get; set; }
    }

    // =================================================================
    // HDS CLIENT
    // =================================================================
    public class HdsClient
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5005")
        };

        public async Task<(bool ok, string json)> RegisterAppAsync(string appName, string version)
        {
            var body = new { application = appName, version = version };

            App.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)App.Current.MainWindow)
                    .LogJsonOutgoing("POST /api/v1/apps/register",
                        JsonSerializer.Serialize(body));
            });

            var result = await _http.PostAsJsonAsync("/api/v1/apps/register", body);
            string json = await result.Content.ReadAsStringAsync();
            return (result.IsSuccessStatusCode, json);
        }

        public async Task<(bool ok, string json)> ReportFaultAsync(FaultSignature fault)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)App.Current.MainWindow)
                    .LogJsonOutgoing("POST /api/v1/faults/report",
                        JsonSerializer.Serialize(fault));
            });

            var result = await _http.PostAsJsonAsync("/api/v1/faults/report", fault);
            string json = await result.Content.ReadAsStringAsync();
            return (result.IsSuccessStatusCode, json);
        }
    }
}
