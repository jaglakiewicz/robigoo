using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RobigooLauncher
{
    public partial class MainWindow : Window
    {
        private Process? _backend;
        private Process? _frontend;
        private bool _running;
        private bool _backendOk;
        private bool _frontendOk;
        private bool _logsVisible;

        public MainWindow()
        {
            InitializeComponent();
            Log("Robigoo Launcher gotowy.");
        }

        private async void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!_running) await Start();
            else Stop();
        }

        private void BtnBrowser_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("http://localhost:4200") { UseShellExecute = true });
        }

        private void BtnLogs_Click(object sender, RoutedEventArgs e)
        {
            _logsVisible = !_logsVisible;
            LogsPanel.Visibility = _logsVisible ? Visibility.Visible : Visibility.Collapsed;
            
            if (_logsVisible)
            {
                Height = 558;
                MinHeight = 450;
            }
            else
            {
                MinHeight = 300;
                Height = 300;
            }
        }

        private async Task Start()
        {
            var root = FindRoot();
            if (root == null)
            {
                MessageBox.Show("Nie znaleziono Server/Client!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _backendOk = _frontendOk = false;
            BtnBrowser.IsEnabled = false;
            BtnToggle.IsEnabled = false;
            Status("Uruchamianie backendu...", "#F5A623");
            Spinner.Visibility = Visibility.Visible;
            StatusDot.Visibility = Visibility.Collapsed;
            Log($"Root: {root}");

            _backend = Run(Path.Combine(root, "Server"), "dotnet", "run", true);
            await Task.Delay(2000);
            
            Status("Uruchamianie frontendu...", "#F5A623");
            _frontend = Run(Path.Combine(root, "Client"), "cmd.exe", "/c npm start", false);

            _running = true;
            BtnToggleText.Text = "Zatrzymaj serwer";
            BtnToggleIcon.Data = (Geometry)FindResource("StopIcon");
            BtnToggle.Background = new SolidColorBrush(Color.FromRgb(184, 74, 90));
            BtnToggle.IsEnabled = true;
        }

        private void Stop()
        {
            Log("Zatrzymywanie...");
            Kill(_frontend);
            Kill(_backend);
            KillPort(4200);
            KillPort(5235);

            _backend = _frontend = null;
            _running = _backendOk = _frontendOk = false;

            Status("Serwer zatrzymany", "#CCCCCC");
            Spinner.Visibility = Visibility.Collapsed;
            StatusDot.Visibility = Visibility.Visible;
            BtnToggleText.Text = "Uruchom serwer";
            BtnToggleIcon.Data = (Geometry)FindResource("PlayIcon");
            BtnToggle.Background = new SolidColorBrush(Color.FromRgb(61, 133, 119));
            BtnBrowser.IsEnabled = false;
            Log("Zatrzymano.");
        }

        private Process Run(string dir, string exe, string args, bool isBack)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = dir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            p.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    Dispatcher.BeginInvoke(() => { Log(e.Data); CheckReady(e.Data, isBack); });
            };
            p.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    Dispatcher.BeginInvoke(() => Log("[ERR] " + e.Data));
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            return p;
        }

        private void CheckReady(string s, bool isBack)
        {
            if (isBack && !_backendOk && (s.Contains("Now listening") || s.Contains("Application started")))
            {
                _backendOk = true;
                Status("Backend gotowy, oczekiwanie na frontend...", "#F5A623");
                Log("✓ Backend gotowy!");
            }
            if (!isBack && !_frontendOk && (s.Contains("Compiled") || s.Contains("localhost:4200")))
            {
                _frontendOk = true;
                Log("✓ Frontend gotowy!");
            }
            if (_backendOk && _frontendOk)
            {
                Status("Serwer działa", "#3D8577");
                Spinner.Visibility = Visibility.Collapsed;
                StatusDot.Visibility = Visibility.Visible;
                BtnBrowser.IsEnabled = true;
                Log("Aplikacja gotowa!");
            }
        }

        private void Kill(Process? p)
        {
            try { if (p != null && !p.HasExited) p.Kill(true); } catch { }
        }

        private void KillPort(int port)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c for /f \"tokens=5\" %a in ('netstat -aon ^| findstr :{port} ^| findstr LISTENING') do taskkill /F /PID %a",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit(2000);
            }
            catch { }
        }

        private string? FindRoot()
        {
            var exePath = Environment.ProcessPath;
            var exeDir = exePath != null ? Path.GetDirectoryName(exePath) : null;
            var currentDir = Directory.GetCurrentDirectory();
            
            var paths = new List<string>();
            
            // Add exe directory and all parents up to root
            if (!string.IsNullOrEmpty(exeDir))
            {
                var dir = exeDir;
                while (!string.IsNullOrEmpty(dir))
                {
                    paths.Add(dir);
                    var parent = Path.GetDirectoryName(dir);
                    if (parent == dir) break;
                    dir = parent;
                }
            }
            
            // Add current directory and parents
            if (!string.IsNullOrEmpty(currentDir) && currentDir != exeDir)
            {
                var dir = currentDir;
                while (!string.IsNullOrEmpty(dir))
                {
                    if (!paths.Contains(dir)) paths.Add(dir);
                    var parent = Path.GetDirectoryName(dir);
                    if (parent == dir) break;
                    dir = parent;
                }
            }
            
            foreach (var p in paths)
            {
                try
                {
                    var serverPath = Path.Combine(p, "Server");
                    var clientPath = Path.Combine(p, "Client");
                    
                    if (Directory.Exists(serverPath) && Directory.Exists(clientPath))
                    {
                        Log($"Root: {p}");
                        return p;
                    }
                }
                catch { }
            }
            
            Log($"Nie znaleziono Server/Client");
            return null;
        }

        private void Status(string t, string c)
        {
            StatusText.Text = t;
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c));
        }

        private void Log(string m) { LogsBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}\n"); LogsBox.ScrollToEnd(); }

        protected override void OnClosing(CancelEventArgs e) { if (_running) Stop(); base.OnClosing(e); }
    }
}
