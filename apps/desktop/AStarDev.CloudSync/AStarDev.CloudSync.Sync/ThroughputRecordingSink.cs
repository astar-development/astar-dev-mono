using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Offers every report to the feed, which throttles it for the UI, and to the throughput recorder, which needs all of them.</summary>
/// <param name="feed">Publishes the progress.</param>
/// <param name="recorder">Samples the bytes moved.</param>
internal sealed class ThroughputRecordingSink(ISyncProgressSink feed, ThroughputRecorder recorder) : ISyncProgressSink
{
    /// <inheritdoc />
    public void Report(AccountId accountId, SyncProgress progress)
    {
        feed.Report(accountId, progress);
        recorder.Record(accountId, progress);
    }
}
