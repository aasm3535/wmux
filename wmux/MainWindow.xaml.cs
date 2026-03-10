using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace wmux;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetupWindow();
    }

    private void SetupWindow()
    {
        // Mica backdrop (WinUI 3 native material)
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

        // Extend title bar
        ExtendsContentIntoTitleBar = true;

        // Set icon and title
        Title = "wmux";

        // Minimum size
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Closing += (_, args) =>
        {
            MainView.ViewModel.SaveSession();
            App.PipeServer.Dispose();
        };
    }
}
