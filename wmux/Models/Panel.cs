using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace wmux.Models;

public enum PanelType { Terminal, Browser }
public enum SplitDirection { Horizontal, Vertical }

public abstract partial class Panel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public abstract PanelType Type { get; }

    [ObservableProperty]
    private bool _isFocused;

    [ObservableProperty]
    private int _unreadCount;

    public ObservableCollection<TerminalNotification> Notifications { get; } = [];

    public void AddNotification(TerminalNotification notification)
    {
        Notifications.Add(notification);
        UnreadCount = Notifications.Count(n => !n.IsRead);
    }

    public void MarkAllRead()
    {
        for (int i = 0; i < Notifications.Count; i++)
            Notifications[i] = Notifications[i].MarkRead();
        UnreadCount = 0;
    }
}
