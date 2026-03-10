using Microsoft.UI.Xaml;
using wmux.Services;

namespace wmux;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    public static PipeServerService PipeServer { get; } = new();
    public static NotificationService Notifications { get; } = new();

    public App()
    {
        InitializeComponent();

        // Catch any unhandled exception and log it before crashing
        UnhandledException += (_, e) =>
        {
            var msg = e.Exception?.ToString() ?? "unknown";
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wmux_crash.txt"),
                msg);
            e.Handled = false;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
            PipeServer.Start();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wmux_launch_crash.txt"),
                ex.ToString());
            throw;
        }
    }
}
