using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <summary>
/// Scans local sync directories for files that are new or modified relative to the last synced state.
/// Uses <see cref="SyncedItemEntity.RemoteModifiedAt"/> as the baseline for conflict/modification detection.
/// </summary>
public sealed class LocalChangeDetector(IFileSystem fileSystem, ILogger<LocalChangeDetector> logger) : ILocalChangeDetector
{
    /// <inheritdoc />
    public IReadOnlyList<SyncJob> DetectNewAndModifiedFiles(string accountId, string localBasePath, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> syncedItemsByLocalPath)
    {
        List<SyncJob> jobs = [];

        foreach (var rule in rules.Where(r => r.RuleType == RuleType.Include))
        {
            string? localFolderPath = ResolveLocalFolderPath(localBasePath, rule.RemotePath);

            if (localFolderPath is null)
                continue;

            ScanDirectory(accountId, localBasePath, localFolderPath, rules, syncedItemsByLocalPath, jobs);
        }

        OneDriveSyncClientMessages.LocalChangeDetectorFound(logger, jobs.Count, localBasePath);

        return jobs;
    }

    private void ScanDirectory(string accountId, string localBasePath, string localDir, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> syncedItemsByLocalPath, List<SyncJob> jobs)
    {
        try
        {
            foreach (string filePath in fileSystem.Directory.EnumerateFiles(localDir))
            {
                var info = fileSystem.FileInfo.New(filePath);

                if (IsFileToSkip(info))
                    continue;

                string remotePath = $"/{fileSystem.Path.GetRelativePath(localBasePath, filePath).Replace(fileSystem.Path.DirectorySeparatorChar, '/')}";

                if (!SyncRuleEvaluator.IsIncluded(remotePath, rules))
                    continue;

                var localModified = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);

                if (syncedItemsByLocalPath.TryGetValue(filePath, out var known))
                {
                    if (localModified <= known.RemoteModifiedAt.AddSeconds(5))
                        continue;
                }

                string relativePathForUpload = remotePath.TrimStart('/');

                var remote = RemoteItemRefFactory.Create(new AccountId(accountId), new OneDriveFolderId("root"), known?.RemoteItemId ?? new OneDriveItemId(string.Empty));
                var target = SyncFileTargetFactory.Create(filePath, relativePathForUpload);
                var metadata = SyncFileMetadataFactory.Create(info.Length, localModified);

                jobs.Add(SyncJobFactory.CreateUpload(remote, target, metadata));
            }

            foreach (string subDir in fileSystem.Directory.EnumerateDirectories(localDir))
            {
                var dirInfo = fileSystem.DirectoryInfo.New(subDir);
                if (dirInfo.Attributes.HasFlag(FileAttributes.Hidden) || dirInfo.Name.StartsWith('.'))
                    continue;

                string subRemotePath = $"/{fileSystem.Path.GetRelativePath(localBasePath, subDir).Replace(fileSystem.Path.DirectorySeparatorChar, '/')}";

                if (!SyncRuleEvaluator.IsIncluded(subRemotePath, rules))
                    continue;

                ScanDirectory(accountId, localBasePath, subDir, rules, syncedItemsByLocalPath, jobs);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            OneDriveSyncClientMessages.LocalChangeDetectorAccessDenied(logger, localDir, ex.Message);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.LocalChangeDetectorError(logger, localDir, ex.Message, ex);
        }
    }

    /// <summary>
    /// Resolves a remote path to its local directory, matching each path segment case-insensitively.
    /// OneDrive remote paths are case-insensitive, but local filesystems (e.g. Linux) may be case-sensitive,
    /// so a literal case-sensitive combine can miss a folder that exists under different casing.
    /// </summary>
    private string? ResolveLocalFolderPath(string localBasePath, string remotePath)
    {
        string currentPath = localBasePath;

        foreach (string segment in remotePath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string exactMatch = currentPath.CombinePath(segment);

            if (fileSystem.Directory.Exists(exactMatch))
            {
                currentPath = exactMatch;

                continue;
            }

            string? caseInsensitiveMatch = fileSystem.Directory.Exists(currentPath)
                ? fileSystem.Directory.EnumerateDirectories(currentPath).FirstOrDefault(dir => string.Equals(fileSystem.Path.GetFileName(dir), segment, StringComparison.OrdinalIgnoreCase))
                : null;

            if (caseInsensitiveMatch is null)
                return null;

            currentPath = caseInsensitiveMatch;
        }

        return currentPath;
    }

    private static bool IsFileToSkip(IFileInfo info)
        => info.Attributes.HasFlag(FileAttributes.Hidden) || info.Name.StartsWith('.') || IsTemporaryFile(info.Extension);

    private static bool IsTemporaryFile(string extension)
        => extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".temp", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".partial", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".download", StringComparison.OrdinalIgnoreCase);
}
