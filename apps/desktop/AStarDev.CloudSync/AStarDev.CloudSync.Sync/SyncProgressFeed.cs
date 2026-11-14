using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc cref="ISyncProgressFeed" />
/// <param name="timeProvider">Times the throttle.</param>
internal sealed class SyncProgressFeed(TimeProvider timeProvider) : ISyncProgressFeed, ISyncProgressSink
{
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromMilliseconds(250);

    private readonly Lock gate = new();
    private readonly Dictionary<AccountId, (SyncProgress Progress, long Stamp)> published = [];

    /// <inheritdoc />
    public event EventHandler<SyncProgressChangedEventArgs>? ProgressChanged;

    /// <inheritdoc />
    public Option<SyncProgress> ProgressOf(AccountId accountId)
    {
        lock (gate)
            return published.TryGetValue(accountId, out var last) ? Option.Some(last.Progress) : Option.None<SyncProgress>();
    }

    /// <inheritdoc />
    public void Report(AccountId accountId, SyncProgress progress)
    {
        if (TryPublish(accountId, progress))
            ProgressChanged?.Invoke(this, new SyncProgressChangedEventArgs(accountId, progress));
    }

    internal void Reset(AccountId accountId)
    {
        lock (gate)
            _ = published.Remove(accountId);
    }

    private static bool IsMilestone(SyncProgress last, SyncProgress next) => last.Phase.GetType() != next.Phase.GetType() || (next.BytesTotal > 0 && next.BytesDone >= next.BytesTotal && last.BytesDone < next.BytesDone);

    private bool TryPublish(AccountId accountId, SyncProgress progress)
    {
        lock (gate)
        {
            var now = timeProvider.GetTimestamp();
            if (published.TryGetValue(accountId, out var last) && !IsMilestone(last.Progress, progress) && timeProvider.GetElapsedTime(last.Stamp, now) < MinimumInterval)
                return false;

            published[accountId] = (progress, now);

            return true;
        }
    }
}
