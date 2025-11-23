using HDS_Demo.Server;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using HDS_Demo.Models;


namespace HDS_Demo
{
    public partial class MainWindow : Window
    {
        private readonly HdsServer _server = new HdsServer();

        public HdsViewModel ViewModel { get; private set; }


        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _server.StartAsync(5005);

            // THIS is the ViewModel instance used by the server
            ViewModel = _server.Services!.GetRequiredService<HdsViewModel>();
            DataContext = ViewModel;

            AppendLog("Application started.");

            // subscribe to events
            var reg = _server.Services.GetService<ApplicationRegistry>();
            if (reg != null)
                reg.ApplicationRegistered += OnApplicationRegistered;

            var faults = _server.Services.GetService<FaultRegistry>();
            if (faults != null)
                faults.FaultReported += OnFaultReported;
        }

        // ============================================================
        // START SERVER
        // ============================================================
        private async void StartServer()
        {
            await _server.StartAsync(5005);

            // Subscribe to registry event
            var registry = _server.Services?.GetService<ApplicationRegistry>();
            if (registry != null)
            {
                registry.ApplicationRegistered += OnApplicationRegistered;
            }

            // Subscribe to fault event
            var faultReg = _server.Services?.GetService<FaultRegistry>();
            if (faultReg != null)
            {
                faultReg.FaultReported += OnFaultReported;
            }

            AppendLog("Server started on http://localhost:5005");
        }

        private void OnApplicationRegistered(RegisteredApplication app)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog(
                    $"App Registered: {app.Application} " +
                    $"v{app.Version} | ID={app.RegistrationId} | Time={app.RegisteredUtc:yyyy-MM-dd HH:mm:ss}"
                );

                // OPTIONAL: Show in a UI list if you later add one
                //ViewModel.RegisteredApps.Add(app);
            });
        }


        private void OnFaultReported(FaultSignature fault)
        {
            Dispatcher.Invoke(() =>
            {
                AppendLog($"FAULT: {fault.ApplicationName} {fault.FaultCode} - {fault.Type} - {fault.Severity} - {fault.Description}");
                ViewModel.UpdateFaultSummary(_server.Services.GetRequiredService<FaultRegistry>());
            });
        }


        // ============================================================
        // SOVD
        // ============================================================

        //private void OnQuerySovdFaults(object sender, RoutedEventArgs e)
        //{
        //    var faults = ViewModel.QuerySovdFaults();

        //    if (faults.Count == 0)
        //        AppendLog("SOVD: No faults stored.");
        //    else
        //        AppendLog("SOVD Faults: " + string.Join(", ", faults));
        //}

        //private void OnRequestSovdPackage(object sender, RoutedEventArgs e)
        //{
        //    var package = ViewModel.RequestSovdPackage();
        //    AppendLog("SOVD Requested Package: " + package);
        //}


        // ============================================================
        // PACKAGE EXPLORER
        // ============================================================

        private void OnPackageDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PackageList.SelectedItem is string path && File.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
                AppendLog("Opened package: " + path);
            }
        }

        private void OnOpenPackageFolder(object sender, RoutedEventArgs e)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiagnosticsPackages");

            if (!Directory.Exists(folder))
            {
                AppendLog("Diagnostics folder does not exist.");
                return;
            }

            System.Diagnostics.Process.Start("explorer.exe", folder);
            AppendLog("Opened folder: " + folder);
        }

        private void OnOpenSelectedPackage(object sender, RoutedEventArgs e)
        {
            if (PackageList.SelectedItem is string path && File.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
                AppendLog("Opened package: " + path);
            }
            else
            {
                AppendLog("No package selected.");
            }
        }


        // ============================================================
        // LOGGING HELPER
        // ============================================================

        private void AppendLog(string message)
        {
            ViewModel.EventLog.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}");
        }
    }
}
