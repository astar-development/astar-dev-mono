using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Scans local sync directories for files that are new or modified relative to the last synced state.
/// Uses <see cref="SyncedItemEntity.RemoteModifiedAt"/> as the baseline for conflict/modification detection.
/// </summary>
public sealed class LocalChangeDetector : ILocalChangeDetector
{
    /// <inheritdoc />
    public List<SyncJob> DetectNewAndModifiedFiles(string accountId, string localBasePath, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> localPathLookup)
    {
        List<SyncJob> jobs = [];

        foreach(var rule in rules.Where(r => r.RuleType == RuleType.Include))
        {
            string localFolderPath = BuildLocalPath(localBasePath, rule.RemotePath);

            if(!Directory.Exists(localFolderPath))
                continue;

            ScanDirectory(accountId, localBasePath, localFolderPath, rules, localPathLookup, jobs);
        }

        Serilog.Log.Information("[LocalChangeDetector] Found {Count} local new/modified files under {Path}", jobs.Count, localBasePath);

        return jobs;
    }

    private static void ScanDirectory(string accountId, string localBasePath, string localDir, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> localPathLookup, List<SyncJob> jobs)
    {
        try
        {
            foreach(string filePath in Directory.EnumerateFiles(localDir))
            {
                var info = new FileInfo(filePath);

                if(IsFileToSkip(info))
                    continue;

                string remotePath = $"/{Path.GetRelativePath(localBasePath, filePath).Replace(Path.DirectorySeparatorChar, '/')}";

                if(!SyncRuleEvaluator.IsIncluded(remotePath, rules))
                    continue;

                var localModified = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);

                if(localPathLookup.TryGetValue(filePath, out var known))
                {
                    if(localModified <= known.RemoteModifiedAt.AddSeconds(5))
                        continue;
                }

                string relativePathForUpload = remotePath.TrimStart('/');

                jobs.Add(new SyncJob
                {
                    AccountId      = accountId,
                    FolderId       = string.Empty,
                    RemoteItemId   = known?.RemoteItemId.Id ?? string.Empty,
                    RelativePath   = relativePathForUpload,
                    LocalPath      = filePath,
                    Direction      = SyncDirection.Upload,
                    FileSize       = info.Length,
                    RemoteModified = localModified,
                    DownloadUrl    = relativePathForUpload
                });
            }

            foreach(string subDir in Directory.EnumerateDirectories(localDir))
            {
                var dirInfo = new DirectoryInfo(subDir);
                if(dirInfo.Attributes.HasFlag(FileAttributes.Hidden) || dirInfo.Name.StartsWith('.'))
                    continue;

                string subRemotePath = $"/{Path.GetRelativePath(localBasePath, subDir).Replace(Path.DirectorySeparatorChar, '/')}";

                if(!SyncRuleEvaluator.IsIncluded(subRemotePath, rules))
                    continue;

                ScanDirectory(accountId, localBasePath, subDir, rules, localPathLookup, jobs);
            }
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

    private static string BuildLocalPath(string localBasePath, string remotePath)
        => Path.Combine(localBasePath, remotePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    private static bool IsFileToSkip(FileInfo info)
        => info.Attributes.HasFlag(FileAttributes.Hidden) || info.Name.StartsWith('.') || IsTemporaryFile(info.Extension);

    private static bool IsTemporaryFile(string extension)
        => extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".temp", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".partial", StringComparison.OrdinalIgnoreCase);
}
