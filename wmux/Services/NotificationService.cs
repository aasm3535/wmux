using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using wmux.Models;

namespace wmux.Services;

/// <summary>
/// Handles OS-level toast notifications and in-app notification tracking.
/// Mirrors cmux's TerminalNotificationStore.
/// </summary>
public sealed class NotificationService
{
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        AppNotificationManager.Default.Register();
        _initialized = true;
    }

    public void ShowToast(TerminalNotification notification)
    {
        var builder = new AppNotificationBuilder()
            .AddText(notification.Title)
            .AddText(notification.Body);

        AppNotificationManager.Default.Show(builder.BuildNotification());
    }

    /// <summary>
    /// Send notification only when app is not focused on the relevant pane —
    /// same smart suppression logic as cmux.
    /// </summary>
    public void ShowIfNotFocused(TerminalNotification notification, bool panelIsFocused)
    {
        if (!panelIsFocused)
            ShowToast(notification);
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // Focus the relevant workspace when user clicks the toast
    }

    public void Unregister()
    {
        if (_initialized)
            AppNotificationManager.Default.Unregister();
    }
}
