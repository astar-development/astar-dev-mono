namespace AStarDev.CloudSync.Sync;

/// <summary>Which way changes flow for a sync pair.</summary>
public abstract record SyncDirection
{
    private SyncDirection()
    {
    }

    /// <summary>Changes flow both ways.</summary>
    public sealed record TwoWay : SyncDirection;

    /// <summary>Local changes are uploaded; cloud changes are not downloaded.</summary>
    public sealed record UploadOnly : SyncDirection;

    /// <summary>Cloud changes are downloaded; local changes are not uploaded.</summary>
    public sealed record DownloadOnly : SyncDirection;
}
