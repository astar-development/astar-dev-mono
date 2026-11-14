using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Writes what a finished sync run did to the activity log (#1564): files downloaded, files uploaded and files deleted on either side. A run that changed nothing, or failed, writes nothing. A log that cannot be written is warned about and never fails the run.</summary>
/// <param name="writer">Stores the entries.</param>
/// <param name="timeProvider">Stamps the entries.</param>
/// <param name="logger">Reports a write that failed.</param>
internal sealed class SyncPassActivity(IActivityLogWriter writer, TimeProvider timeProvider, ILogger<SyncPassActivity> logger)
{
    private const string Location = nameof(SyncPassActivity);

    /// <summary>Appends the entries the run earned. The write is not cancellable: the run's work is already done and should be recorded.</summary>
    /// <param name="accountId">The account the run was for.</param>
    /// <param name="outcome">What the run did.</param>
    public async Task RecordAsync(AccountId accountId, Result<SyncPassSummary, SyncPassFailure> outcome)
    {
        var entries = outcome.Match(summary => EntriesOf(accountId, summary), _ => []);
        if (entries.Count == 0)
            return;

        await writer.AppendAsync(entries, CancellationToken.None)
            .MatchAsync(_ => Unit.Instance, _ => Warn())
            .ConfigureAwait(false);
    }

    private static List<ActivityKind> KindsOf(SyncPassSummary summary)
    {
        var kinds = new List<ActivityKind>();
        var deleted = summary.Download.DeletedLocally + summary.Upload.Removed;

        if (summary.Download.Downloaded > 0)
            kinds.Add(ActivityKindFactory.CreateDownloaded(summary.Download.Downloaded));

        if (summary.Upload.Uploaded > 0)
            kinds.Add(ActivityKindFactory.CreateUploaded(summary.Upload.Uploaded, summary.Upload.BytesUploaded));

        if (deleted > 0)
            kinds.Add(ActivityKindFactory.CreateDeleted(deleted));

        return kinds;
    }

    private IReadOnlyList<ActivityEntry> EntriesOf(AccountId accountId, SyncPassSummary summary)
    {
        var occurredAt = timeProvider.GetUtcNow();

        return [.. KindsOf(summary).Select(kind => ActivityEntryFactory.Create(accountId, Option.None<SyncPairId>(), kind, occurredAt))];
    }

    private Unit Warn()
    {
        LogMessage.Warning(logger, Location, "The sync run could not write to the activity log.");

        return Unit.Instance;
    }
}
