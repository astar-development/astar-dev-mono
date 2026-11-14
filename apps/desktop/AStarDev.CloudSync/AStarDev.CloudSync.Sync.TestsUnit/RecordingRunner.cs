using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class RecordingRunner : ISyncPassRunner
{
    private readonly Lock gate = new();
    private readonly List<(AccountId AccountId, SyncTrigger Trigger, CancellationToken Token)> runs = [];

    public IReadOnlyList<(AccountId AccountId, SyncTrigger Trigger, CancellationToken Token)> Runs
    {
        get
        {
            lock (gate)
                return [.. runs];
        }
    }

    public Option<WriteGate> Hold { get; set; } = Option.None<WriteGate>();

    public async Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken)
    {
        lock (gate)
            runs.Add((accountId, trigger, cancellationToken));

        var waiting = Hold.Match(held => held.PassAsync().WaitAsync(cancellationToken), () => Task.CompletedTask);
        await waiting.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return cancellationToken.IsCancellationRequested
            ? Result.Failure<SyncPassSummary, SyncPassFailure>(SyncPassFailureFactory.CreateCancelled())
            : Result.Success<SyncPassSummary, SyncPassFailure>(SyncPassSummaryFactory.Create(RemoteUpdateSummaryFactory.Create(RelevantChangeCountsFactory.None, RuleRelocationSummaryFactory.None, Option.None<ResyncKind>(), 1), LocalChangeSetFactory.Create([], 0, 0), DownloadPassSummaryFactory.Create([], []), UploadPassSummaryFactory.Create([])));
    }
}
