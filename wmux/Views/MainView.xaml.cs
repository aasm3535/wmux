using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using wmux.ViewModels;

namespace wmux.Views;

public sealed partial class MainView : UserControl
{
    public TabManagerViewModel ViewModel { get; } = new();

    private bool _resizing;
    private double _resizeStartX;
    private double _resizeStartWidth;

    public MainView()
    {
        InitializeComponent();
        Loaded += (_, _) => ViewModel.Initialize();
    }

    // ── Sidebar resize ────────────────────────────────────────────────────

    private void OnResizeEnter(object sender, PointerRoutedEventArgs e)
    {
        ResizeHandle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(40, 255, 255, 255));
    }

    private void OnResizeExit(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizing)
            ResizeHandle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    private void OnResizePressed(object sender, PointerRoutedEventArgs e)
    {
        _resizing = true;
        _resizeStartX = e.GetCurrentPoint(RootGrid).Position.X;
        _resizeStartWidth = SidebarColumn.ActualWidth;
        ((UIElement)sender).CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnResizeMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_resizing) return;
        var delta = e.GetCurrentPoint(RootGrid).Position.X - _resizeStartX;
        var newWidth = Math.Clamp(_resizeStartWidth + delta, 160, 360);
        SidebarColumn.Width = new GridLength(newWidth);
        e.Handled = true;
    }

    private void OnResizeReleased(object sender, PointerRoutedEventArgs e)
    {
        _resizing = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        ResizeHandle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Windows.UI.Color.FromArgb(0, 0, 0, 0));
        e.Handled = true;
    }
}
