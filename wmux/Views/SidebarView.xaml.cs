using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using wmux.Models;
using wmux.ViewModels;

namespace wmux.Views;

public sealed partial class SidebarView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(TabManagerViewModel),
            typeof(SidebarView), new PropertyMetadata(null));

    public TabManagerViewModel ViewModel
    {
        get => (TabManagerViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public SidebarView()
    {
        InitializeComponent();
    }

    private void OnWorkspaceClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Workspace ws)
            ViewModel?.SelectWorkspace(ws);
    }

    private void OnNotificationsBellClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.IsNotificationPanelOpen = !ViewModel.IsNotificationPanelOpen;
    }
}
