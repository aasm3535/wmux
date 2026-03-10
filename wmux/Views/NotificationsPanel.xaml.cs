using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using wmux.Models;
using wmux.ViewModels;

namespace wmux.Views;

public sealed partial class NotificationsPanel : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(TabManagerViewModel),
            typeof(NotificationsPanel), new PropertyMetadata(null));

    public TabManagerViewModel? ViewModel
    {
        get => (TabManagerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public NotificationsPanel()
    {
        InitializeComponent();
    }

    public IEnumerable<TerminalNotification> GetAllNotifications()
    {
        if (ViewModel is null) return Enumerable.Empty<TerminalNotification>();
        return ViewModel.Workspaces
            .SelectMany(w => w.Panels)
            .SelectMany(p => p.Notifications)
            .OrderByDescending(n => n.CreatedAt);
    }

    private void OnOverlayClick(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (ViewModel is not null) ViewModel.IsNotificationPanelOpen = false;
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null) ViewModel.IsNotificationPanelOpen = false;
    }

    private void OnClearAll(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        foreach (var panel in ViewModel.Workspaces.SelectMany(w => w.Panels))
            panel.MarkAllRead();
    }
}
