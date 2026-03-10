using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace wmux.Models;

/// <summary>
/// A workspace is a tab in the sidebar. It contains one or more panels
/// arranged in a split layout (like tmux panes).
/// </summary>
public partial class Workspace : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = "Terminal";

    [ObservableProperty]
    private int _colorIndex;

    [ObservableProperty]
    private bool _hasUnreadNotifications;

    [ObservableProperty]
    private bool _isSelected;

    // Root panel — may be split into a tree of panels
    public Panel? RootPanel { get; set; }

    // Currently focused panel in this workspace
    public Panel? FocusedPanel { get; set; }

    // All panels flat list for easy iteration
    public ObservableCollection<Panel> Panels { get; } = [];

    // Sidebar display info — derived from focused terminal panel
    public string? GitBranch => FocusedTerminal?.GitBranch;
    public bool GitDirty => FocusedTerminal?.GitDirty ?? false;
    public string? WorkingDirectory => FocusedTerminal?.WorkingDirectory;
    public List<int> ListeningPorts => FocusedTerminal?.ListeningPorts ?? [];
    public string? LatestNotificationText => FocusedTerminal?.LatestNotificationText;

    private TerminalPanel? FocusedTerminal =>
        FocusedPanel as TerminalPanel ?? Panels.OfType<TerminalPanel>().FirstOrDefault();

    public int TotalUnreadCount => Panels.Sum(p => p.UnreadCount);

    public void AddPanel(Panel panel)
    {
        Panels.Add(panel);
        if (Panels.Count == 1)
        {
            RootPanel = panel;
            FocusedPanel = panel;
        }
        UpdateUnread();
    }

    public void RemovePanel(Panel panel)
    {
        Panels.Remove(panel);
        if (FocusedPanel == panel)
            FocusedPanel = Panels.FirstOrDefault();
        UpdateUnread();
    }

    private void UpdateUnread()
    {
        HasUnreadNotifications = TotalUnreadCount > 0;
    }

    public void NotifyPanelChanged() => UpdateUnread();
}
