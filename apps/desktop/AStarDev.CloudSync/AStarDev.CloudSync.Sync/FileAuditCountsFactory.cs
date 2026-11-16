namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="FileAuditCounts"/> values.</summary>
public static class FileAuditCountsFactory
{
    /// <summary>Creates the counts; a negative count becomes zero.</summary>
    /// <param name="localFiles">The local file count.</param>
    /// <param name="cloudFiles">The cloud file count.</param>
    public static FileAuditCounts Create(long localFiles, long cloudFiles) => new(Math.Max(localFiles, 0), Math.Max(cloudFiles, 0));
}
