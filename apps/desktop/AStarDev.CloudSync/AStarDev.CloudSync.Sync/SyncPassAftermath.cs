namespace AStarDev.CloudSync.Sync;

/// <summary>What a sync run does after its four steps, still inside the account's lease.</summary>
/// <param name="quota">Refreshes the account's storage usage.</param>
/// <param name="activity">Writes what the finished run did to the activity log.</param>
/// <param name="journal">Writes the run's start and end to the run journal (#1610).</param>
internal sealed class SyncPassAftermath(IAccountQuotaRefresher quota, SyncPassActivity activity, SyncPassJournal journal)
{
    /// <summary>Refreshes the account's storage usage after the passes.</summary>
    public IAccountQuotaRefresher Quota { get; } = quota;

    /// <summary>The activity log: what a finished run did.</summary>
    public SyncPassActivity Activity { get; } = activity;

    /// <summary>The run journal: when a run started and how it ended.</summary>
    public SyncPassJournal Journal { get; } = journal;
}
