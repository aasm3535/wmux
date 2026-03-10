using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using wmux.Models;

namespace wmux.Views;

/// <summary>
/// Renders one workspace's panels. Supports split layouts
/// by building Grid-based containers with drag handles.
/// </summary>
public sealed partial class PanelHost : UserControl
{
    public static readonly DependencyProperty WorkspaceProperty =
        DependencyProperty.Register(nameof(Workspace), typeof(Workspace),
            typeof(PanelHost), new PropertyMetadata(null, OnWorkspaceChanged));

    public Workspace? Workspace
    {
        get => (Workspace?)GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    public PanelHost()
    {
        InitializeComponent();
    }

    private static void OnWorkspaceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PanelHost host) host.Rebuild();
    }

    private void Rebuild()
    {
        RootGrid.Children.Clear();
        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (Workspace is null) return;

        var panels = Workspace.Panels.ToList();
        if (panels.Count == 0) return;

        if (panels.Count == 1)
        {
            RootGrid.Children.Add(CreatePanelControl(panels[0]));
            return;
        }

        // Two panels side by side with a simple splitter border
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = CreatePanelControl(panels[0]);
        Grid.SetColumn(left, 0);

        var separator = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 30)),
            Width = 1
        };
        Grid.SetColumn(separator, 1);

        var right = CreatePanelControl(panels[1]);
        Grid.SetColumn(right, 2);

        RootGrid.Children.Add(left);
        RootGrid.Children.Add(separator);
        RootGrid.Children.Add(right);
    }

    private static FrameworkElement CreatePanelControl(Models.Panel panel)
    {
        return panel switch
        {
            TerminalPanel tp => new TerminalView { Panel = tp },
            BrowserPanel bp  => new BrowserView  { Panel = bp },
            _ => new TextBlock { Text = "Unknown panel" }
        };
    }
}
