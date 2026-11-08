using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>One local folder linked to one cloud folder.</summary>
public sealed record SyncPair
{
    internal SyncPair(
        SyncPairId id,
        AccountId accountId,
        string name,
        DirectoryInfo localFolder,
        string remotePath,
        SyncDirection direction,
        SyncStatus status,
        int fileCount,
        Option<DateTimeOffset> lastSync)
    {
        Id = id;
        AccountId = accountId;
        Name = name;
        LocalFolder = localFolder;
        RemotePath = remotePath;
        Direction = direction;
        Status = status;
        FileCount = fileCount;
        LastSync = lastSync;
    }

    /// <summary>The sync pair id.</summary>
    public SyncPairId Id { get; }

    /// <summary>The account holding the cloud folder.</summary>
    public AccountId AccountId { get; }

    /// <summary>The sync pair's display name.</summary>
    public string Name { get; }

    /// <summary>The local folder, under its provider's local root.</summary>
    public DirectoryInfo LocalFolder { get; }

    /// <summary>The cloud folder's path.</summary>
    public string RemotePath { get; }

    /// <summary>Which way changes flow.</summary>
    public SyncDirection Direction { get; }

    /// <summary>The sync pair's sync status.</summary>
    public SyncStatus Status { get; }

    /// <summary>Files in the pair.</summary>
    public int FileCount { get; }

    /// <summary>When the pair last finished a sync, if ever.</summary>
    public Option<DateTimeOffset> LastSync { get; }
}
