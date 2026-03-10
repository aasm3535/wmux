namespace wmux.Models;

public record TerminalNotification(
    Guid Id,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    bool IsRead = false
)
{
    public TerminalNotification MarkRead() => this with { IsRead = true };
}
