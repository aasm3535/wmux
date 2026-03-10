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
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();

        PipeServer.Start();
    }
}
