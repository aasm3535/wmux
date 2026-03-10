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
    private ConPtySession? _session;

    public TerminalView()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        try
        {
            await TerminalWebView.EnsureCoreWebView2Async();

            var wv = TerminalWebView.CoreWebView2;
            wv.Settings.IsScriptEnabled = true;
            wv.Settings.AreDevToolsEnabled = false;
            wv.Settings.IsWebMessageEnabled = true;
            wv.Settings.IsStatusBarEnabled = false;
            wv.Settings.AreDefaultContextMenusEnabled = false;

            // Virtual host so CDN scripts load correctly from local file
            var baseDir = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(AppContext.BaseDirectory));
            wv.SetVirtualHostNameToFolderMapping(
                "wmux.local", baseDir,
                CoreWebView2HostResourceAccessKind.Allow);

            wv.WebMessageReceived += OnWebMessageReceived;
            wv.NavigationCompleted += OnNavigationCompleted;
            wv.Navigate("https://wmux.local/Assets/xterm/terminal.html");
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wmux_webview.txt"),
                ex.ToString());
        }
    }

    private void OnNavigationCompleted(CoreWebView2 sender,
        CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess) return;
        _webViewReady = true;
        while (_pendingOutput.TryDequeue(out var data))
            SendOutput(data);
        WireSession();
    }

    private static void OnPanelChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is TerminalView tv) tv.WireSession();
    }

    private void WireSession()
    {
        if (Panel is null || ViewModel is null || !_webViewReady) return;

        // Detach old session
        if (_session is not null)
            _session.DataReceived -= OnTerminalData;

        _session = ViewModel.GetSession(Panel.Id);
        if (_session is null) return;

        _session.DataReceived += OnTerminalData;

        Panel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TerminalPanel.UnreadCount))
                DispatcherQueue.TryEnqueue(UpdateNotificationRing);
        };

        // Focus the terminal
        PostMsg("{\"type\":\"focus\"}");
    }

    private void OnTerminalData(string data)
    {
        if (_webViewReady)
            DispatcherQueue.TryEnqueue(() => SendOutput(data));
        else
            _pendingOutput.Enqueue(data);
    }

    private void SendOutput(string data)
    {
        var msg = JsonSerializer.Serialize(new { type = "output", data });
        try { TerminalWebView.CoreWebView2.PostWebMessageAsString(msg); }
        catch { }
    }

    private void PostMsg(string json)
    {
        try { TerminalWebView.CoreWebView2.PostWebMessageAsString(json); }
        catch { }
    }

    private void OnWebMessageReceived(CoreWebView2 sender,
        CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            using var doc = JsonDocument.Parse(args.WebMessageAsJson);
            var root = doc.RootElement;

            // The JS sends a JSON string (PostWebMessageAsString wraps it in quotes)
            // So we may need to parse the inner string
            string json;
            if (root.ValueKind == JsonValueKind.String)
                json = root.GetString()!;
            else
                json = args.WebMessageAsJson;

            using var inner = JsonDocument.Parse(json);
            var r = inner.RootElement;
            var type = r.GetProperty("type").GetString();

            switch (type)
            {
                case "input":
                    _session?.Write(r.GetProperty("data").GetString() ?? "");
                    break;
                case "resize":
                    _session?.Resize(
                        r.GetProperty("cols").GetInt32(),
                        r.GetProperty("rows").GetInt32());
                    break;
                case "ready":
                    // xterm.js is ready — flush any queued output
                    while (_pendingOutput.TryDequeue(out var d))
                        SendOutput(d);
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
