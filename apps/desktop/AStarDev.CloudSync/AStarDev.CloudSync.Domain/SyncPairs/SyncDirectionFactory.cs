using AStarDev.CloudSync.Core.SyncPairs;

namespace AStarDev.CloudSync.Domain.SyncPairs;

/// <summary>Creates <see cref="SyncDirection"/> cases.</summary>
public static class SyncDirectionFactory
{
    /// <summary>Creates a two-way direction.</summary>
    public static SyncDirection CreateTwoWay() => new SyncDirection.TwoWay();

    /// <summary>Creates an upload-only direction.</summary>
    public static SyncDirection CreateUploadOnly() => new SyncDirection.UploadOnly();

    /// <summary>Creates a download-only direction.</summary>
    public static SyncDirection CreateDownloadOnly() => new SyncDirection.DownloadOnly();
}
