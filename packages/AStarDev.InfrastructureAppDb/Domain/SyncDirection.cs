namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>The direction data flows for a sync job.</summary>
public enum SyncDirection
{
    /// <summary>The remote item is downloaded to local storage.</summary>
    Download,

    /// <summary>The local item is uploaded to remote storage.</summary>
    Upload,

    /// <summary>The item is deleted.</summary>
    Delete
}
