using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using wmux.Models;

namespace wmux.Views;

public sealed partial class BrowserView : UserControl
{
    public static readonly DependencyProperty PanelProperty =
        DependencyProperty.Register(nameof(Panel), typeof(BrowserPanel),
            typeof(BrowserView), new PropertyMetadata(null, OnPanelChanged));

    public BrowserPanel? Panel
    {
        get => (BrowserPanel?)GetValue(PanelProperty);
        set => SetValue(PanelProperty, value);
    }

    public BrowserView()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        await BrowserWebView.EnsureCoreWebView2Async();
    }

    private static void OnPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BrowserView bv && e.NewValue is BrowserPanel panel)
        {
            bv.AddressBar.Text = panel.Url;
            bv.BrowserWebView.Source = new Uri(panel.Url);
        }
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (BrowserWebView.CanGoBack) BrowserWebView.GoBack();
    }

    private void OnRefresh(object sender, RoutedEventArgs e) =>
        BrowserWebView.Reload();

    private void OnOpenInBrowser(object sender, RoutedEventArgs e)
    {
        var url = BrowserWebView.Source?.ToString() ?? "";
        if (!string.IsNullOrEmpty(url))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void OnAddressKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
            Navigate(AddressBar.Text);
    }

    private void Navigate(string input)
    {
        var url = input.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? input
            : $"https://{input}";
        BrowserWebView.Source = new Uri(url);
        if (Panel is not null) Panel.Url = url;
    }

    private void OnNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        AddressBar.Text = args.Uri;
    }

    private void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (Panel is not null) Panel.Title = BrowserWebView.CoreWebView2.DocumentTitle;
    }
}
