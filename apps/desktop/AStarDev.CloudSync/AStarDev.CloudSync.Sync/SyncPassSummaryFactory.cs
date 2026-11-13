namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPassSummary"/> values.</summary>
public static class SyncPassSummaryFactory
{
    /// <summary>Creates the summary of a finished run.</summary>
    /// <param name="remote">What the remote index update found.</param>
    /// <param name="localChanges">What the local scan found.</param>
    /// <param name="download">What the download pass did.</param>
    /// <param name="upload">What the upload pass did.</param>
    public static SyncPassSummary Create(RemoteUpdateSummary remote, LocalChangeSet localChanges, DownloadPassSummary download, UploadPassSummary upload) => new(remote, localChanges, download, upload);
}
