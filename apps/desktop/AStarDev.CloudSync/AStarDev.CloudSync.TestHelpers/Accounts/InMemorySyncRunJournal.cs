using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class InMemorySyncRunJournal(TimeProvider? timeProvider = null) : ISyncRunJournal
{
    private readonly Lock gate = new();
    private readonly List<JournaledRun> runs = [];
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public bool Fails { get; set; }

    public Action OnFinish { get; set; } = () => { };

    public bool Killed { get; private set; }

    public void Kill() => Killed = true;

    public void Restart() => Killed = false;

    public IReadOnlyList<JournaledRun> Runs
    {
        get
        {
            lock (gate)
                return [.. runs];
        }
    }

    public Task<Result<SyncRunId, ErrorResponse>> StartAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken)
    {
        if (Fails)
            return Task.FromResult(Result.Failure<SyncRunId, ErrorResponse>(new ErrorResponse("locked")));

        var id = new SyncRunId(Guid.NewGuid());
        lock (gate)
            runs.Add(new JournaledRun(id, accountId, trigger, clock.GetUtcNow(), Option.None<DateTimeOffset>(), Option.None<SyncRunOutcome>()));

        return Task.FromResult(Result.Success<SyncRunId, ErrorResponse>(id));
    }

    public Task<Result<Unit, ErrorResponse>> FinishAsync(SyncRunId runId, SyncRunOutcome outcome, CancellationToken cancellationToken)
    {
        OnFinish();

        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        if (Killed)
            return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));

        lock (gate)
        {
            var index = runs.FindIndex(run => run.Id == runId && run.Outcome is Option<SyncRunOutcome>.None);
            if (index >= 0)
                runs[index] = runs[index] with { Finished = Option.Some(clock.GetUtcNow()), Outcome = Option.Some(outcome) };
        }

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public Task<Result<IReadOnlyList<InterruptedSyncRun>, ErrorResponse>> GetInterruptedAsync(CancellationToken cancellationToken)
    {
        if (Fails)
            return Task.FromResult(Result.Failure<IReadOnlyList<InterruptedSyncRun>, ErrorResponse>(new ErrorResponse("locked")));

        lock (gate)
            return Task.FromResult(Result.Success<IReadOnlyList<InterruptedSyncRun>, ErrorResponse>([.. runs.Where(run => run.Outcome is Option<SyncRunOutcome>.None).Select(run => InterruptedSyncRunFactory.Create(run.Id, run.AccountId))]));
    }

    public Task<Result<Option<DateTimeOffset>, ErrorResponse>> GetLastSyncAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        if (Fails)
            return Task.FromResult(Result.Failure<Option<DateTimeOffset>, ErrorResponse>(new ErrorResponse("locked")));

        lock (gate)
        {
            var last = runs.Where(run => run.AccountId == accountId && run.Outcome is Option<SyncRunOutcome>.Some { Value: SyncRunOutcome.Completed }).Select(run => run.Finished).OfType<Option<DateTimeOffset>.Some>().Select(some => some.Value).DefaultIfEmpty().Max();

            return Task.FromResult(Result.Success<Option<DateTimeOffset>, ErrorResponse>(last == default ? Option.None<DateTimeOffset>() : Option.Some(last)));
        }
    }
}
