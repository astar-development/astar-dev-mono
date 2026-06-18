using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <summary>Mutable state populated by <see cref="IRemoteFolderEnumerator"/> during streaming; safe to read after the stream is exhausted.</summary>
public sealed class RemoteEnumerationContext
{
    /// <summary>True when no sync rules are configured for the account.</summary>
    public bool HadNoRules { get; internal set; }

    /// <summary>All sync rules loaded before streaming begins.</summary>
    public IReadOnlyList<SyncRuleEntity> Rules { get; internal set; } = [];

    /// <summary>Synced items loaded before streaming begins, keyed by remote item ID.</summary>
    public Dictionary<string, SyncedItemEntity> SyncedItems { get; internal set; } = [];

    /// <summary>Remote item IDs seen so far; populated as items are yielded.</summary>
    public HashSet<string> SeenRemoteIds { get; } = new(StringComparer.OrdinalIgnoreCase);
}
