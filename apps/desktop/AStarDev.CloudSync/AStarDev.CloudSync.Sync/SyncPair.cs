using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>One local folder linked to the cloud folder with the same relative path. A pair is the top-most Include rule of an account (#1397); it stores no absolute path.</summary>
public sealed record SyncPair
{
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Internal constructor called only by SyncPairFactory; records cannot have an internal primary constructor (#1453).")]
    internal SyncPair(
        SyncPairId id,
        AccountId accountId,
        string name,
        LocalRelativePath localFolder,
        Option<RemoteItemId> remoteFolder,
        SyncDirection direction,
        SyncStatus status,
        int fileCount,
        Option<DateTimeOffset> lastSync)
    {
        Id = id;
        AccountId = accountId;
        Name = name;
        LocalFolder = localFolder;
        RemoteFolder = remoteFolder;
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

    /// <summary>The local folder, relative to the account's sync root. The cloud folder mirrors the same relative path.</summary>
    public LocalRelativePath LocalFolder { get; }

    /// <summary>The cloud folder's id, when it is known.</summary>
    public Option<RemoteItemId> RemoteFolder { get; }

    /// <summary>Which way changes flow.</summary>
    public SyncDirection Direction { get; }

    /// <summary>The sync pair's sync status.</summary>
    public SyncStatus Status { get; }

    /// <summary>Files in the pair.</summary>
    public int FileCount { get; }

    /// <summary>When the pair last finished a sync, if ever.</summary>
    public Option<DateTimeOffset> LastSync { get; }
}
