using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Writes what a finished sync run did to the activity log (#1564): files downloaded, files uploaded and files deleted on either side. A run that stops early (cancelled or failed) still logs the steps that finished before it stopped (#1578). A run that changed nothing writes nothing. A log that cannot be written is warned about and never fails the run.</summary>
/// <param name="writer">Stores the entries.</param>
/// <param name="timeProvider">Stamps the entries.</param>
/// <param name="logger">Reports a write that failed.</param>
internal sealed class SyncPassActivity(IActivityLogWriter writer, TimeProvider timeProvider, ILogger<SyncPassActivity> logger)
{
    private const string Location = nameof(SyncPassActivity);

    /// <summary>Appends the entries the run earned. The write is not cancellable: the run's work is already done and should be recorded.</summary>
    /// <param name="accountId">The account the run was for.</param>
    /// <param name="outcome">What the run did.</param>
    /// <param name="finishedDownload">The download step's summary if it finished, used when the run did not complete.</param>
    public async Task RecordAsync(AccountId accountId, Result<SyncPassSummary, SyncPassFailure> outcome, Option<DownloadPassSummary> finishedDownload)
    {
        var entries = outcome.Match(summary => EntriesOf(accountId, KindsOf(summary)), _ => EntriesOf(accountId, finishedDownload.Match(DownloadKindsOf, () => [])));
        if (entries.Count == 0)
            return;

        await writer.AppendAsync(entries, CancellationToken.None)
            .MatchAsync(_ => Unit.Instance, _ => Warn())
            .ConfigureAwait(false);
    }

    private static List<ActivityKind> DownloadKindsOf(DownloadPassSummary download) => KindsOf(download, UploadPassSummaryFactory.Create([]));

    private static List<ActivityKind> KindsOf(SyncPassSummary summary) => KindsOf(summary.Download, summary.Upload);

    private static List<ActivityKind> KindsOf(DownloadPassSummary download, UploadPassSummary upload)
    {
        var kinds = new List<ActivityKind>();
        var deleted = download.DeletedLocally + upload.Removed;

        if (download.Downloaded > 0)
            kinds.Add(ActivityKindFactory.CreateDownloaded(download.Downloaded));

        if (upload.Uploaded > 0)
            kinds.Add(ActivityKindFactory.CreateUploaded(upload.Uploaded, upload.BytesUploaded));

        if (deleted > 0)
            kinds.Add(ActivityKindFactory.CreateDeleted(deleted));

        return kinds;
    }

    private IReadOnlyList<ActivityEntry> EntriesOf(AccountId accountId, List<ActivityKind> kinds)
    {
        var occurredAt = timeProvider.GetUtcNow();

        return [.. kinds.Select(kind => ActivityEntryFactory.Create(accountId, Option.None<SyncPairId>(), kind, occurredAt))];
    }

    private Unit Warn()
    {
        LogMessage.Warning(logger, Location, "The sync run could not write to the activity log.");

        return Unit.Instance;
    }
}
