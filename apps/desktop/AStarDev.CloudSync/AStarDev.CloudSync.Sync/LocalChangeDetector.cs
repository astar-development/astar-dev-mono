using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
internal sealed class LocalChangeDetector : ILocalChangeDetector
{
    /// <inheritdoc />
    public Task<Result<LocalChangeSet, LocalScanFailure>> DetectAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{accountId} {cancellationToken} are not yet scanned.");
}
