namespace AStarDev.CloudSync.Sync;

/// <summary>Which uploads keep their session across a restart (#1611).</summary>
public static class UploadSessionLimits
{
    /// <summary>Only files larger than this many bytes (100 MiB) save their upload session; smaller files restart.</summary>
    public const long PersistedAboveBytes = 100L * 1024 * 1024;
}
