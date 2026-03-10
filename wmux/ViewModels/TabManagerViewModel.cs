using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using wmux.Models;
using wmux.Services;

namespace wmux.ViewModels;

/// <summary>
/// Central orchestrator — equivalent of cmux's TabManager.
/// Manages workspaces (tabs), panels, splits, and notifications.
/// </summary>
public partial class TabManagerViewModel : ObservableObject
{
    public ObservableCollection<Workspace> Workspaces { get; } = [];

    [ObservableProperty]
    private Workspace? _selectedWorkspace;

    [ObservableProperty]
    private bool _isNotificationPanelOpen;

    // Active ConPTY sessions keyed by panel ID
    private readonly Dictionary<Guid, ConPtySession> _sessions = [];

    // History for back/forward navigation (up to 50 entries)
    private readonly List<Guid> _workspaceHistory = [];
    private int _historyIndex = -1;

    public static readonly string[] TabColors =
    [
        "#6366F1", "#8B5CF6", "#EC4899", "#EF4444", "#F97316",
        "#EAB308", "#22C55E", "#14B8A6", "#06B6D4", "#3B82F6",
        "#A855F7", "#F43F5E", "#10B981", "#0EA5E9", "#84CC16",
        "#F59E0B"
    ];

    public TabManagerViewModel()
    {
        App.PipeServer.CommandReceived += OnPipeCommand;
        // Sessions are started lazily via Initialize() to avoid crashing during XAML construction
    }

    /// <summary>Call after the window is loaded to start terminal sessions.</summary>
    public void Initialize()
    {
        try { RestoreSession(); }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wmux_vm_crash.txt"),
                ex.ToString());
            // Fallback: at least create one empty workspace without a session
            var ws = new Workspace { Name = "Terminal 1" };
            Workspaces.Add(ws);
            SelectedWorkspace = ws;
        }
    }

    // ── Workspace management ──────────────────────────────────────────────────

    [RelayCommand]
    public void NewWorkspace(string? workingDirectory = null)
    {
        var ws = new Workspace
        {
            Name = $"Terminal {Workspaces.Count + 1}",
            ColorIndex = Workspaces.Count % TabColors.Length
        };

        var panel = new TerminalPanel
        {
            WorkingDirectory = workingDirectory
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        ws.AddPanel(panel);
        Workspaces.Add(ws);
        SelectWorkspace(ws);
        StartSession(panel);
        UpdateGitInfoAsync(panel);
    }

    [RelayCommand]
    public void CloseWorkspace(Workspace ws)
    {
        foreach (var panel in ws.Panels.ToList())
            StopSession(panel.Id);

        Workspaces.Remove(ws);

        if (SelectedWorkspace == ws)
            SelectedWorkspace = Workspaces.LastOrDefault();
    }

    public void SelectWorkspace(Workspace ws)
    {
        if (SelectedWorkspace is not null)
            SelectedWorkspace.IsSelected = false;

        SelectedWorkspace = ws;
        ws.IsSelected = true;
        ws.FocusedPanel?.MarkAllRead();
        ws.NotifyPanelChanged();

        // History
        if (_historyIndex < _workspaceHistory.Count - 1)
            _workspaceHistory.RemoveRange(_historyIndex + 1,
                _workspaceHistory.Count - _historyIndex - 1);
        _workspaceHistory.Add(ws.Id);
        if (_workspaceHistory.Count > 50) _workspaceHistory.RemoveAt(0);
        _historyIndex = _workspaceHistory.Count - 1;
    }

    // ── Split panes ───────────────────────────────────────────────────────────

    [RelayCommand]
    public void SplitHorizontal() => SplitCurrentWorkspace(SplitDirection.Horizontal);

    [RelayCommand]
    public void SplitVertical() => SplitCurrentWorkspace(SplitDirection.Vertical);

    private void SplitCurrentWorkspace(SplitDirection direction)
    {
        if (SelectedWorkspace is null) return;
        var ws = SelectedWorkspace;
        var sourcePanel = ws.FocusedPanel as TerminalPanel;

        var newPanel = new TerminalPanel
        {
            WorkingDirectory = sourcePanel?.WorkingDirectory
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        ws.AddPanel(newPanel);
        StartSession(newPanel);
        UpdateGitInfoAsync(newPanel);
        // Split layout is managed by the View (SplitView / custom container)
    }

    // ── Browser panel ─────────────────────────────────────────────────────────

    [RelayCommand]
    public void OpenBrowser(string url = "https://www.google.com")
    {
        if (SelectedWorkspace is null) return;
        var panel = new BrowserPanel { Url = url };
        SelectedWorkspace.AddPanel(panel);
    }

    // ── Sessions ──────────────────────────────────────────────────────────────

    private void StartSession(TerminalPanel panel)
    {
        var session = new ConPtySession();
        _sessions[panel.Id] = session;

        session.DataReceived += data => OnTerminalData(panel, data);
        session.ProcessExited += () => OnProcessExited(panel);

        var shell = GetDefaultShell();
        session.Start(shell, panel.WorkingDirectory);
        panel.ProcessId = session.ProcessId;
    }

    private void StopSession(Guid panelId)
    {
        if (_sessions.TryGetValue(panelId, out var session))
        {
            session.Dispose();
            _sessions.Remove(panelId);
        }
    }

    public ConPtySession? GetSession(Guid panelId) =>
        _sessions.TryGetValue(panelId, out var s) ? s : null;

    // ── Terminal data processing ──────────────────────────────────────────────

    private void OnTerminalData(TerminalPanel panel, string data)
    {
        // Parse OSC notification sequences
        foreach (var osc in OscParser.Parse(data))
        {
            if (!osc.IsNotification) continue;

            var (title, body) = osc.ParseNotification();
            var notification = new TerminalNotification(Guid.NewGuid(), title, body, DateTimeOffset.Now);

            panel.LatestNotificationText = body;
            panel.AddNotification(notification);

            var isFocused = SelectedWorkspace?.FocusedPanel == panel;
            App.Notifications.ShowIfNotFocused(notification, isFocused);

            // Update sidebar badge on UI thread
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
            {
                SelectedWorkspace?.NotifyPanelChanged();
            });
        }
    }

    private void OnProcessExited(TerminalPanel panel)
    {
        _sessions.Remove(panel.Id);
        var ws = Workspaces.FirstOrDefault(w => w.Panels.Contains(panel));
        ws?.RemovePanel(panel);
        if (ws?.Panels.Count == 0)
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(
                () => CloseWorkspace(ws));
    }

    // ── Git ───────────────────────────────────────────────────────────────────

    private static async void UpdateGitInfoAsync(TerminalPanel panel)
    {
        await Task.Run(() => GitService.UpdatePanel(panel));
    }

    // ── Pipe commands (CLI integration) ───────────────────────────────────────

    private void OnPipeCommand(PipeCommand cmd)
    {
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
        {
            switch (cmd.Action)
            {
                case "new-workspace":
                    NewWorkspace(cmd.Args?.GetValueOrDefault("cwd"));
                    break;
                case "close-workspace":
                    if (SelectedWorkspace is not null) CloseWorkspace(SelectedWorkspace);
                    break;
                case "split-horizontal":
                    SplitHorizontal();
                    break;
                case "split-vertical":
                    SplitVertical();
                    break;
                case "open-browser":
                    OpenBrowser(cmd.Args?.GetValueOrDefault("url") ?? "https://www.google.com");
                    break;
                case "notify":
                    var n = new TerminalNotification(Guid.NewGuid(),
                        cmd.Args?.GetValueOrDefault("title") ?? "wmux",
                        cmd.Args?.GetValueOrDefault("body") ?? "",
                        DateTimeOffset.Now);
                    App.Notifications.ShowToast(n);
                    break;
            }
        });
    }

    // ── Session persistence ───────────────────────────────────────────────────

    private void RestoreSession()
    {
        var snapshots = SessionPersistenceService.Load();
        foreach (var snap in snapshots)
            NewWorkspace(snap.Panels.FirstOrDefault()?.WorkingDirectory);

        if (Workspaces.Count == 0)
            NewWorkspace();
    }

    public void SaveSession() =>
        SessionPersistenceService.Save(Workspaces);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetDefaultShell()
    {
        var pwsh = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PowerShell", "7", "pwsh.exe");
        if (File.Exists(pwsh)) return pwsh;

        return "cmd.exe";
    }
}
