namespace AStarDev.CloudSync.Sync;

/// <summary>How much history the run journal keeps (#1610).</summary>
public static class SyncRunJournalLimits
{
    /// <summary>The newest runs kept per account.</summary>
    public const int RunsPerAccount = 100;
}
