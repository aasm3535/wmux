using System.Text.Json;
using wmux.Models;

namespace wmux.Services;

/// <summary>
/// Saves/restores workspace layout to %APPDATA%\wmux\session.json.
/// Mirrors cmux's SessionPersistence.
/// </summary>
public static class SessionPersistenceService
{
    private static readonly string SessionFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "wmux", "session.json");

    public static void Save(IEnumerable<Workspace> workspaces)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SessionFile)!);
            var data = workspaces.Select(w => new WorkspaceSnapshot(
                w.Id, w.Name, w.ColorIndex,
                w.Panels.OfType<TerminalPanel>().Select(p => new PanelSnapshot(
                    p.Id, PanelType.Terminal, p.WorkingDirectory)).ToList()
            )).ToList();

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SessionFile, json);
        }
        catch { }
    }

    public static List<WorkspaceSnapshot> Load()
    {
        try
        {
            if (!File.Exists(SessionFile)) return [];
            var json = File.ReadAllText(SessionFile);
            return JsonSerializer.Deserialize<List<WorkspaceSnapshot>>(json) ?? [];
        }
        catch { return []; }
    }
}

public record WorkspaceSnapshot(Guid Id, string Name, int ColorIndex, List<PanelSnapshot> Panels);
public record PanelSnapshot(Guid Id, PanelType Type, string WorkingDirectory);
