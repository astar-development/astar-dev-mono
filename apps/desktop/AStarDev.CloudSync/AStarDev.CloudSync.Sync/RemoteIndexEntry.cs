namespace AStarDev.CloudSync.Sync;

/// <summary>A remote index item together with its path relative to the account's sync root (#1400).</summary>
public sealed record RemoteIndexEntry
{
    internal RemoteIndexEntry(RelativeSyncPath path, RemoteItemSnapshot item)
    {
        Path = path;
        Item = item;
    }

    /// <summary>The item's path relative to the sync root.</summary>
    public RelativeSyncPath Path { get; }

    /// <summary>The item as the remote index holds it.</summary>
    public RemoteItemSnapshot Item { get; }
}
