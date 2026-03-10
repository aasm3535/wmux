namespace wmux.Services;

/// <summary>
/// Parses OSC (Operating System Command) escape sequences from terminal output.
/// cmux uses OSC 9, 99, and 777 for notifications.
///
/// Format: ESC ] <code> ; <text> BEL
///         \x1b]9;Title\x07
/// </summary>
public static class OscParser
{
    private const char ESC = '\x1b';
    private const char BEL = '\x07';
    private const string ST = "\x1b\\"; // String Terminator alternative

    public static IEnumerable<OscSequence> Parse(string data)
    {
        int i = 0;
        while (i < data.Length)
        {
            int escPos = data.IndexOf(ESC, i);
            if (escPos == -1) break;

            if (escPos + 1 < data.Length && data[escPos + 1] == ']')
            {
                int start = escPos + 2;
                int end = data.IndexOf(BEL, start);
                int stEnd = data.IndexOf(ST, start, StringComparison.Ordinal);

                if (end == -1 && stEnd == -1) { i = escPos + 1; continue; }

                int termPos = (end == -1) ? stEnd : (stEnd == -1) ? end : Math.Min(end, stEnd);
                bool usedSt = termPos == stEnd;

                string payload = data[start..termPos];
                int semicolon = payload.IndexOf(';');
                if (semicolon > 0 && int.TryParse(payload[..semicolon], out int code))
                {
                    yield return new OscSequence(code, payload[(semicolon + 1)..]);
                }

                i = termPos + (usedSt ? ST.Length : 1);
            }
            else
            {
                i = escPos + 1;
            }
        }
    }
}

public record OscSequence(int Code, string Text)
{
    /// <summary>OSC 9 — ConEmu/cmux notification: body only</summary>
    public bool IsNotification => Code == 9 || Code == 99 || Code == 777;

    public (string Title, string Body) ParseNotification()
    {
        // OSC 777 format: "notify;Title;Body"
        if (Code == 777)
        {
            var parts = Text.Split(';', 3);
            if (parts.Length >= 3) return (parts[1], parts[2]);
            if (parts.Length == 2) return (parts[1], "");
        }
        // OSC 9 / 99: just the body
        return ("wmux", Text);
    }
}
