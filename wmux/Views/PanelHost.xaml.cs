using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using wmux.Models;
using wmux.ViewModels;

namespace wmux.Views;

public sealed partial class PanelHost : UserControl
{
    public static readonly DependencyProperty WorkspaceProperty =
        DependencyProperty.Register(nameof(Workspace), typeof(Workspace),
            typeof(PanelHost), new PropertyMetadata(null, OnChanged));

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(TabManagerViewModel),
            typeof(PanelHost), new PropertyMetadata(null, OnChanged));

    public Workspace? Workspace
    {
        get => (Workspace?)GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    public TabManagerViewModel? ViewModel
    {
        get => (TabManagerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public PanelHost()
    {
        InitializeComponent();
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PanelHost host) host.Rebuild();
    }

    private void Rebuild()
    {
        RootGrid.Children.Clear();
        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (Workspace is null || ViewModel is null) return;

        var panels = Workspace.Panels.ToList();
        if (panels.Count == 0) return;

        if (panels.Count == 1)
        {
            RootGrid.Children.Add(MakeControl(panels[0]));
            return;
        }

        // Two panels side by side
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = MakeControl(panels[0]);
        Grid.SetColumn(left, 0);

        var sep = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 40, 48))
        };
        Grid.SetColumn(sep, 1);

        var right = MakeControl(panels[1]);
        Grid.SetColumn(right, 2);

        RootGrid.Children.Add(left);
        RootGrid.Children.Add(sep);
        RootGrid.Children.Add(right);
    }

    private FrameworkElement MakeControl(Models.Panel panel) => panel switch
    {
        TerminalPanel tp => new TerminalView { Panel = tp, ViewModel = ViewModel },
        BrowserPanel  bp => new BrowserView  { Panel = bp },
        _                => new TextBlock { Text = "unknown" }
    };
}
