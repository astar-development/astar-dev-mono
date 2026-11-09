namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="PageContinuation"/> cases.</summary>
public static class PageContinuationFactory
{
    /// <summary>Creates the <see cref="PageContinuation.MorePages"/> case.</summary>
    public static PageContinuation CreateMorePages(ChangeCursor next) => new PageContinuation.MorePages(next);

    /// <summary>Creates the <see cref="PageContinuation.CaughtUp"/> case.</summary>
    public static PageContinuation CreateCaughtUp(ChangeCursor delta) => new PageContinuation.CaughtUp(delta);
}
