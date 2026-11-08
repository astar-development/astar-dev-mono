namespace AStarDev.CloudSync.Sync;

/// <summary>Holds the sync lock closed: while any drain is held, no new pass can start. Disposing it reopens the lock; disposing it twice has no further effect.</summary>
public sealed class SyncDrain : IDisposable
{
    private readonly Action reopen;
    private int reopened;

    internal SyncDrain(Action reopen) => this.reopen = reopen;

    /// <summary>Reopens the lock.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref reopened, 1) == 0)
            reopen();
    }
}
