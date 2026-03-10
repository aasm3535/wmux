using LibGit2Sharp;
using wmux.Models;

namespace wmux.Services;

/// <summary>
/// Reads git info for a given directory. Mirrors cmux's git branch/dirty/PR tracking.
/// </summary>
public static class GitService
{
    public static GitInfo? GetInfo(string directory)
    {
        try
        {
            var repoPath = Repository.Discover(directory);
            if (repoPath is null) return null;

            using var repo = new Repository(repoPath);
            var branch = repo.Head.FriendlyName;
            var isDirty = repo.RetrieveStatus().IsDirty;

            return new GitInfo(branch, isDirty, null);
        }
        catch
        {
            return null;
        }
    }

    public static void UpdatePanel(TerminalPanel panel)
    {
        var info = GetInfo(panel.WorkingDirectory);
        panel.GitBranch = info?.Branch;
        panel.GitDirty = info?.IsDirty ?? false;
    }
}

public record GitInfo(string Branch, bool IsDirty, string? PrStatus);
