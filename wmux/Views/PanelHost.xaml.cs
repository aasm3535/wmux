using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using wmux.Models;

namespace wmux.Views;

/// <summary>
/// Renders one workspace's panels. Supports arbitrary split layouts
/// by building a tree of GridSplitter-based containers.
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

        // Simple split: two panels side by side (vertical split)
        // Full binary tree split to be implemented in SplitController
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) }); // splitter
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = CreatePanelControl(panels[0]);
        Grid.SetColumn(left, 0);

        var splitter = new GridSplitter
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };
        Grid.SetColumn(splitter, 1);

        var right = CreatePanelControl(panels[1]);
        Grid.SetColumn(right, 2);

        RootGrid.Children.Add(left);
        RootGrid.Children.Add(splitter);
        RootGrid.Children.Add(right);
    }

    private static UIElement CreatePanelControl(Panel panel)
    {
        return panel switch
        {
            TerminalPanel tp => new TerminalView { Panel = tp },
            BrowserPanel bp  => new BrowserView  { Panel = bp },
            _ => new TextBlock { Text = "Unknown panel" }
        };
    }
}
