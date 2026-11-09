namespace AStarDev.CloudSync.Sync;

/// <summary>One page of the change feed (#1398).</summary>
public sealed record RemoteChangePage
{
    internal RemoteChangePage(IReadOnlyList<RemoteItemSnapshot> items, PageContinuation next)
    {
        Items = items;
        Next = next;
    }

    /// <summary>The items on the page.</summary>
    public IReadOnlyList<RemoteItemSnapshot> Items { get; }

    /// <summary>What follows the page.</summary>
    public PageContinuation Next { get; }
}
