using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc cref="ISyncProgressFeed" />
/// <param name="timeProvider">Times the throttle.</param>
internal sealed class SyncProgressFeed(TimeProvider timeProvider) : ISyncProgressFeed, ISyncProgressSink
{
    /// <inheritdoc />
    public event EventHandler<SyncProgressChangedEventArgs>? ProgressChanged;

    /// <inheritdoc />
    public Option<SyncProgress> ProgressOf(AccountId accountId) => throw new NotImplementedException($"{accountId} {timeProvider} {ProgressChanged}");

    /// <inheritdoc />
    public void Report(AccountId accountId, SyncProgress progress) => throw new NotImplementedException($"{accountId} {progress}");

    internal void Reset(AccountId accountId) => throw new NotImplementedException($"{accountId}");
}
