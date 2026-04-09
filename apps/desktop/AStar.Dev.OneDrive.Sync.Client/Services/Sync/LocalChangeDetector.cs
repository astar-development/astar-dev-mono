using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

/// <summary>
/// Scans local sync folders for files that have been created or modified
/// since the last successful sync and returns them as upload jobs.
///
/// Scope: only files inside explicitly included folders.
/// New subfolders are created on OneDrive automatically when their
/// parent upload session is created — no separate folder creation needed.
/// </summary>
public sealed class LocalChangeDetector
{
    /// <summary>
    /// Returns upload jobs for all local files in <paramref name="localFolderPath"/>
    /// that are newer than <paramref name="since"/>.
    /// Pass null for <paramref name="since"/> to queue everything (first upload pass).
    /// </summary>
#pragma warning disable CA1822 // Method is called from another class, not sure why that call is not detected.
    public List<SyncJob> DetectChanges(string accountId, string folderId, string localFolderPath, string remoteFolderPath, DateTimeOffset? since)
#pragma warning restore CA1822
    {
        if(!Directory.Exists(localFolderPath))
        {
            Serilog.Log.Warning("[LocalChangeDetector] Path does not exist: {Path}", localFolderPath);
            return [];
        }

        List<SyncJob> jobs = [];
        var cutoff = since?.UtcDateTime ?? DateTime.MinValue;

        ScanDirectory(
            accountId,
            folderId,
            localFolderPath,
            remoteFolderPath,
            cutoff,
            jobs);

        Serilog.Log.Information("[LocalChangeDetector] Found {Count} changed files in {Path} since {Since}", jobs.Count, localFolderPath, since?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture) ?? "beginning");

        return jobs;
    }

    private static void ScanDirectory(string accountId, string folderId, string localDir, string remoteDir, DateTime cutoff, List<SyncJob> jobs)
    {
        const string UnknownRemoteId = ""; // we don't know the remote ID until the upload completes, so we use an empty string as placeholder
        try
        {
            foreach(string filePath in Directory.EnumerateFiles(localDir, "*", SearchOption.AllDirectories))
            {
                var info = new FileInfo(filePath);

                if(IsFileToSkip(cutoff, info))
                    continue;

                string relativePath = Path.GetRelativePath(localDir, filePath).Replace(Path.DirectorySeparatorChar, '/');

                string remotePath = string.IsNullOrEmpty(remoteDir)
                    ? relativePath
                    : $"{remoteDir}/{relativePath}";

                jobs.Add(new SyncJob
                {
                    AccountId = accountId,
                    FolderId = folderId,
                    RemoteItemId = UnknownRemoteId,
                    RelativePath = relativePath,
                    LocalPath = filePath,
                    Direction = SyncDirection.Upload,
                    FileSize = info.Length,
                    RemoteModified = new DateTimeOffset(
                        info.LastWriteTimeUtc, TimeSpan.Zero),
                    DownloadUrl = remotePath
                });
            }

            ProcessSubDirectories(accountId, folderId, localDir, remoteDir, cutoff, jobs);
        }
        catch(UnauthorizedAccessException ex)
        {
            Serilog.Log.Warning("[LocalChangeDetector] Access denied: {Path} — {Error}", localDir, ex.Message);
        }
        catch(Exception ex)
        {
            Serilog.Log.Error(ex, "[LocalChangeDetector] Error scanning {Path}: {Error}", localDir, ex.Message);
        }
    }

    private static void ProcessSubDirectories(string accountId, string folderId, string localDir, string remoteDir, DateTime cutoff, List<SyncJob> jobs)
    {
        foreach(string subDir in Directory.EnumerateDirectories(localDir))
        {
            var dirInfo = new DirectoryInfo(subDir);
            if(dirInfo.Attributes.HasFlag(FileAttributes.Hidden) || dirInfo.Name.StartsWith('.'))
                continue;

            string subRelative = Path.GetRelativePath(localDir, subDir).Replace(Path.DirectorySeparatorChar, '/');

            string subRemote = string.IsNullOrEmpty(remoteDir)
                    ? subRelative
                    : $"{remoteDir}/{subRelative}";

            ScanDirectory(accountId, folderId, subDir, subRemote, cutoff, jobs);
        }
    }

    private static bool IsFileToSkip(DateTime cutoff, FileInfo info) => info.Attributes.HasFlag(FileAttributes.Hidden) || info.Name.StartsWith('.') || info.Extension is ".tmp" or ".temp" or ".partial" || info.LastWriteTimeUtc <= cutoff || info.CreationTimeUtc <= cutoff;
}
