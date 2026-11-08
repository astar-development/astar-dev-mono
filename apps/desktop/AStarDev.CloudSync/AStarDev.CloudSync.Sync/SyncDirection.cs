namespace AStarDev.CloudSync.Sync;

/// <summary>Which way changes flow for a sync pair.</summary>
public abstract record SyncDirection
{
    private SyncDirection()
    {
    }

    /// <summary>Changes flow both ways.</summary>
    public sealed record TwoWay : SyncDirection
    {
        internal TwoWay()
        {
        }
    }

    /// <summary>Local changes are uploaded; cloud changes are not downloaded.</summary>
    public sealed record UploadOnly : SyncDirection
    {
        internal UploadOnly()
        {
        }
    }

    /// <summary>Cloud changes are downloaded; local changes are not uploaded.</summary>
    public sealed record DownloadOnly : SyncDirection
    {
        internal DownloadOnly()
        {
        }
    }
}
