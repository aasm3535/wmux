using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using wmux.Models;
using wmux.Services;
using wmux.ViewModels;

namespace wmux.Views;

public sealed partial class TerminalView : UserControl
{
    public static readonly DependencyProperty PanelProperty =
        DependencyProperty.Register(nameof(Panel), typeof(TerminalPanel),
            typeof(TerminalView), new PropertyMetadata(null, OnPanelChanged));

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(TabManagerViewModel),
            typeof(TerminalView), new PropertyMetadata(null));

    public TerminalPanel? Panel
    {
        get => (TerminalPanel?)GetValue(PanelProperty);
        set => SetValue(PanelProperty, value);
    }

    public TabManagerViewModel? ViewModel
    {
        get => (TabManagerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private bool _webViewReady;
    private readonly Queue<string> _pendingOutput = new();

    public TerminalView()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        await TerminalWebView.EnsureCoreWebView2Async();
        TerminalWebView.CoreWebView2.Settings.IsScriptEnabled = true;
        TerminalWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        TerminalWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        // Load the xterm.js page
        var htmlPath = Path.Combine(AppContext.BaseDirectory, "Assets", "xterm", "terminal.html");
        TerminalWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
        TerminalWebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
    }

    private void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        _webViewReady = true;
        // Flush any output that arrived before WebView was ready
        while (_pendingOutput.TryDequeue(out var data))
            SendOutputToWebView(data);

        // Wire ConPTY session output to xterm.js
        WireSession();
    }

    private static void OnPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TerminalView tv) tv.WireSession();
    }

    private ConPtySession? _session;

    private void WireSession()
    {
        if (Panel is null || ViewModel is null || !_webViewReady) return;

        _session = ViewModel.GetSession(Panel.Id);
        if (_session is null) return;

        _session.DataReceived += OnTerminalData;

        // Update notification ring
        Panel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TerminalPanel.UnreadCount))
                DispatcherQueue.TryEnqueue(UpdateNotificationRing);
        };
    }

    private void OnTerminalData(string data)
    {
        if (_webViewReady)
            DispatcherQueue.TryEnqueue(() => SendOutputToWebView(data));
        else
            _pendingOutput.Enqueue(data);
    }

    private void SendOutputToWebView(string data)
    {
        var msg = JsonSerializer.Serialize(new { type = "output", data });
        TerminalWebView.CoreWebView2.PostWebMessageAsJson(msg);
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            using var doc = JsonDocument.Parse(args.WebMessageAsJson);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "input":
                    var input = root.GetProperty("data").GetString() ?? "";
                    _session?.Write(input);
                    break;

                case "resize":
                    var cols = root.GetProperty("cols").GetInt32();
                    var rows = root.GetProperty("rows").GetInt32();
                    _session?.Resize(cols, rows);
                    break;
            }
        }
        catch { }
    }

    private void UpdateNotificationRing()
    {
        NotificationRing.Visibility = (Panel?.UnreadCount > 0)
            ? Visibility.Visible : Visibility.Collapsed;
    }
}
