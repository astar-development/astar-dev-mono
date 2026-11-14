using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Adds up the bytes the files of one phase have moved and reports the total to the feed.</summary>
/// <param name="accountId">The account.</param>
/// <param name="phase">The phase.</param>
/// <param name="total">The bytes the phase has to move.</param>
/// <param name="sink">Takes the reports.</param>
internal sealed class PhaseTally(AccountId accountId, SyncPhase phase, long total, ISyncProgressSink sink)
{
    private long done;

    internal FileProgress ForFile(long size) => new(this, size);

    internal void Add(long bytes)
    {
        if (bytes == 0)
            return;

        sink.Report(accountId, SyncProgressFactory.Create(phase, Interlocked.Add(ref done, bytes), total));
    }
}
