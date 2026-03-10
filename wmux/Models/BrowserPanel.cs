using CommunityToolkit.Mvvm.ComponentModel;

namespace wmux.Models;

public partial class BrowserPanel : Panel
{
    public override PanelType Type => PanelType.Browser;

    [ObservableProperty]
    private string _url = "about:blank";

    [ObservableProperty]
    private string _title = "Browser";

    [ObservableProperty]
    private double _zoomFactor = 1.0;

    public List<string> History { get; } = [];
    public int HistoryIndex { get; set; } = -1;

    public bool CanGoBack => HistoryIndex > 0;
    public bool CanGoForward => HistoryIndex < History.Count - 1;
}
