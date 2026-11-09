namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteChangePage"/> values.</summary>
public static class RemoteChangePageFactory
{
    /// <summary>Creates a page. Missing items become an empty list.</summary>
    public static RemoteChangePage Create(IEnumerable<RemoteItemSnapshot>? items, PageContinuation next) => new([.. items ?? []], next);
}
