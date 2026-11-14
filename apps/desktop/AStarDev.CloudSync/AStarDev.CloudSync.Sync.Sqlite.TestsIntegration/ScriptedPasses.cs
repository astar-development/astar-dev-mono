using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class ScriptedPasses : IRemoteIndexUpdater, ILocalChangeDetector, IDownloadPass, IUploadPass
{
    public LocalChangeSet Scan { get; set; } = LocalChangeSetFactory.Create([], 0, 0);

    public IReadOnlyList<DownloadItemOutcome> Downloaded { get; set; } = [];

    public IReadOnlyList<UploadItemOutcome> Uploaded { get; set; } = [];

    public Task<Result<RemoteUpdateSummary, ProviderError>> UpdateAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<RemoteUpdateSummary, ProviderError>(RemoteUpdateSummaryFactory.Create(RelevantChangeCountsFactory.None, RuleRelocationSummaryFactory.None, Option.None<ResyncKind>(), 1)));

    public Task<Result<LocalChangeSet, LocalScanFailure>> DetectAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Result.Success<LocalChangeSet, LocalScanFailure>(Scan));

    Task<Result<DownloadPassSummary, DownloadPassFailure>> IDownloadPass.RunAsync(AccountId accountId, LocalChangeSet localChanges, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create(Downloaded, [])));

    Task<Result<UploadPassSummary, UploadPassFailure>> IUploadPass.RunAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<UploadPassSummary, UploadPassFailure>(UploadPassSummaryFactory.Create(Uploaded)));
}
