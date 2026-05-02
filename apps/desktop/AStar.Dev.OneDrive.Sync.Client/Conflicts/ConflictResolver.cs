using System.Globalization;
using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Conflicts;

public static class ConflictResolver
{
    public static ConflictOutcome Resolve(ConflictPolicy policy, DateTimeOffset localModified, DateTimeOffset remoteModified)
        => policy switch
        {
            ConflictPolicy.Ignore => ConflictOutcome.Skip,
            ConflictPolicy.LocalWins => ConflictOutcome.UseLocal,
            ConflictPolicy.RemoteWins => ConflictOutcome.UseRemote,
            ConflictPolicy.KeepBoth => ConflictOutcome.KeepBoth,
            ConflictPolicy.LastWriteWins => localModified >= remoteModified
                                               ? ConflictOutcome.UseLocal
                                               : ConflictOutcome.UseRemote,
            _ => ConflictOutcome.Skip
        };

    /// <summary>
    /// Generates a "keep both" filename by appending a timestamp suffix.
    /// e.g. report.docx → report (local 2024-01-15 14-32).docx
    /// </summary>
    public static string MakeKeepBothName(string localPath, DateTimeOffset localModified, IFileSystem fileSystem)
    {
        string dir       = fileSystem.Path.GetDirectoryName(localPath) ?? string.Empty;
        string stem      = fileSystem.Path.GetFileNameWithoutExtension(localPath);
        string ext       = fileSystem.Path.GetExtension(localPath);
        string timestamp = localModified.LocalDateTime.ToString("yyyy-MM-dd HH-mm", CultureInfo.CurrentCulture);

        return fileSystem.Path.Combine(dir, $"{stem} (local {timestamp}){ext}");
    }
}
