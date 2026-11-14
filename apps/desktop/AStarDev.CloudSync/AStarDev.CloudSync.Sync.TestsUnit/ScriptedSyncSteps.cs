using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScriptedSyncSteps : IRemoteIndexUpdater, ILocalChangeDetector, IDownloadPass, IUploadPass
{
    private readonly Lock gate = new();
    private readonly List<string> steps = [];
    private int inDownload;
    private int maxConcurrentDownloads;

    public IReadOnlyList<string> Steps
    {
        get
        {
            lock (gate)
                return [.. steps];
        }
    }

    public int MaxConcurrentDownloads => maxConcurrentDownloads;

    public Result<RemoteUpdateSummary, ProviderError> Index { get; set; } = Result.Success<RemoteUpdateSummary, ProviderError>(RemoteUpdateSummaryFactory.Create(RelevantChangeCountsFactory.None, RuleRelocationSummaryFactory.None, Option.None<ResyncKind>(), 1));

    public Result<LocalChangeSet, LocalScanFailure> Scan { get; set; } = Result.Success<LocalChangeSet, LocalScanFailure>(LocalChangeSetFactory.Create([], 0, 0));

    public Result<DownloadPassSummary, DownloadPassFailure> Downloaded { get; set; } = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create([], []));

    public Result<UploadPassSummary, UploadPassFailure> Uploaded { get; set; } = Result.Success<UploadPassSummary, UploadPassFailure>(UploadPassSummaryFactory.Create([]));

    public Option<WriteGate> DownloadGate { get; set; } = Option.None<WriteGate>();

    public Option<Exception> DownloadThrows { get; set; } = Option.None<Exception>();

    public Option<Exception> UploadThrows { get; set; } = Option.None<Exception>();

    public Action OnDownload { get; set; } = () => { };

    public Task<Result<RemoteUpdateSummary, ProviderError>> UpdateAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Record("index", Index));

    public Task<Result<LocalChangeSet, LocalScanFailure>> DetectAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Record("scan", Scan));

    async Task<Result<DownloadPassSummary, DownloadPassFailure>> IDownloadPass.RunAsync(AccountId accountId, LocalChangeSet localChanges, CancellationToken cancellationToken)
    {
        var outcome = Record("download", Downloaded);
        OnDownload();
        lock (gate)
            maxConcurrentDownloads = Math.Max(maxConcurrentDownloads, ++inDownload);

        var waiting = DownloadGate.Match(held => held.PassAsync().WaitAsync(cancellationToken), () => Task.CompletedTask);
        await waiting.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        lock (gate)
            inDownload--;

        cancellationToken.ThrowIfCancellationRequested();
        DownloadThrows.Match(exception => throw exception, () => Unit.Instance);

        return outcome;
    }

    async Task<Result<UploadPassSummary, UploadPassFailure>> IUploadPass.RunAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken)
    {
        var outcome = Record("upload", Uploaded);
        await Task.Yield();
        UploadThrows.Match(exception => throw exception, () => Unit.Instance);

        return outcome;
    }

    public async Task UntilDownloadsAsync(int count, CancellationToken cancellationToken)
    {
        while (inDownload < count)
            await Task.Delay(5, cancellationToken);
    }

    public Action OnStep { get; set; } = () => { };

    private T Record<T>(string step, T outcome)
    {
        OnStep();
        lock (gate)
            steps.Add(step);

        return outcome;
    }
}
