using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using wmux.Models;
using wmux.ViewModels;

namespace wmux.Views;

public sealed partial class WorkspaceContentView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(TabManagerViewModel),
            typeof(WorkspaceContentView), new PropertyMetadata(null, OnViewModelChanged));

    public TabManagerViewModel? ViewModel
    {
        get => (TabManagerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public WorkspaceContentView()
    {
        InitializeComponent();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WorkspaceContentView v)
        {
            if (e.OldValue is TabManagerViewModel old)
                old.PropertyChanged -= v.OnVmPropertyChanged;
            if (e.NewValue is TabManagerViewModel vm)
                vm.PropertyChanged += v.OnVmPropertyChanged;
            v.Refresh();
        }
    }

    private void OnVmPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TabManagerViewModel.SelectedWorkspace))
            DispatcherQueue.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        var ws = ViewModel?.SelectedWorkspace;
        EmptyState.Visibility = ws is null ? Visibility.Visible : Visibility.Collapsed;

        if (ws is null)
        {
            WorkspaceHost.Content = null;
            return;
        }

        // Reuse existing PanelHost if it's for the same workspace
        if (WorkspaceHost.Content is PanelHost existing && existing.Workspace == ws)
            return;

        WorkspaceHost.Content = new PanelHost
        {
            Workspace = ws,
            ViewModel = ViewModel   // ← THE FIX: pass ViewModel down
        };
    }
}
