namespace AStarDev.CloudSync.Sync;

/// <summary>Why the download pass left a planned item alone without failing (#1400).</summary>
public abstract record DownloadSkipReason
{
    private DownloadSkipReason()
    {
    }

    /// <summary>The remote item was deleted after the plan was made (a 404); the next delta handles it.</summary>
    public sealed record RemoteGone : DownloadSkipReason
    {
        internal RemoteGone()
        {
        }
    }

    /// <summary>The local item was still changing, so it waits for the next pass.</summary>
    public sealed record LocalBusy : DownloadSkipReason
    {
        internal LocalBusy()
        {
        }
    }
}
