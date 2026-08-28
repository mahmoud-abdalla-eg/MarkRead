using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using MarkRead.Services;

namespace MarkRead;

public partial class App : Application
{
    private const string PipeName = "MarkRead_IPC_Pipe_98741";

    private CancellationTokenSource? _pipeCts;
    private MainWindow? _mainWindow;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        if (e.Args.Contains("--register", StringComparer.OrdinalIgnoreCase))
        {
            ShellIntegration.RegisterFileAssociations();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--unregister", StringComparer.OrdinalIgnoreCase))
        {
            ShellIntegration.UnregisterFileAssociations();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--default", StringComparer.OrdinalIgnoreCase))
        {
            ShellIntegration.PromptSetAsDefault();
            Environment.Exit(0);
            return;
        }

        var currentProc = Process.GetCurrentProcess();
        var otherInstances = Process.GetProcessesByName(currentProc.ProcessName)
                                    .Where(p => p.Id != currentProc.Id)
                                    .ToArray();

        if (otherInstances.Length > 0)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(3000);

                using var writer = new StreamWriter(client, Encoding.UTF8);
                writer.WriteLine(string.Join("|", e.Args));
                writer.Flush();
            }
            catch
            {
                // Fallback exit if pipe connection failed
            }

            Environment.Exit(0);
            return;
        }

        StartNamedPipeServer();

        _mainWindow = new MainWindow();
        _mainWindow.Show();

        // Initial file argument handling
        if (e.Args.Length > 0)
        {
            foreach (var arg in e.Args)
            {
                if (File.Exists(arg))
                {
                    _mainWindow.OpenFile(arg);
                }
            }
        }
    }

    private void StartNamedPipeServer()
    {
        _pipeCts = new CancellationTokenSource();
        var token = _pipeCts.Token;

        Task.Run(async () =>
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await server.WaitForConnectionAsync(token);

                        using (var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true))
                        {
                            string? payload = await reader.ReadLineAsync(token);

                            if (!string.IsNullOrWhiteSpace(payload))
                            {
                                string[] files = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);
                                Dispatcher.Invoke(() =>
                                {
                                    BringWindowToFront();
                                    foreach (var file in files)
                                    {
                                        if (File.Exists(file))
                                        {
                                            _mainWindow?.OpenFile(file);
                                        }
                                    }
                                });
                            }
                        }

                        server.Disconnect();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        try { server.Disconnect(); } catch { }
                    }
                }
            }
            catch
            {
                // Ignore server init errors
            }
        }, token);
    }

    private void BringWindowToFront()
    {
        if (_mainWindow == null) return;

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        var handle = new WindowInteropHelper(_mainWindow).Handle;
        ShowWindowAsync(handle, SW_RESTORE);
        SetForegroundWindow(handle);
        _mainWindow.Activate();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _pipeCts?.Cancel();
    }
}
