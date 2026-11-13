namespace AStarDev.CloudSync.Sync;

/// <summary>What applying a page, or finishing a resync, changed in an account's remote index (#1398).</summary>
public sealed record RemoteIndexDelta
{
    internal RemoteIndexDelta(IReadOnlyList<RemoteChange> changes)
    {
        Changes = changes;
    }

    /// <summary>The changes, in the order they were applied.</summary>
    public IReadOnlyList<RemoteChange> Changes { get; }
}
