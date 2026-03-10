using CommunityToolkit.Mvvm.ComponentModel;

namespace wmux.Models;

public partial class TerminalPanel : Panel
{
    public override PanelType Type => PanelType.Terminal;

    [ObservableProperty]
    private string _workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    [ObservableProperty]
    private string? _gitBranch;

    [ObservableProperty]
    private bool _gitDirty;

    [ObservableProperty]
    private string? _gitPrStatus;

    [ObservableProperty]
    private List<int> _listeningPorts = [];

    [ObservableProperty]
    private string? _latestNotificationText;

    // Shell process PID managed by ConPtyService
    public int ProcessId { get; set; }
}
