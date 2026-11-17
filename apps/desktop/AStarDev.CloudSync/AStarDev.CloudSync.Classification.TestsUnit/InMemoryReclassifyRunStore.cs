using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class InMemoryReclassifyRunStore : IReclassifyRunStore
{
    public Option<ReclassifyRunRecord> Last { get; set; } = Option.None<ReclassifyRunRecord>();

    public int SetCalls { get; private set; }

    public bool FailReads { get; init; }

    public bool FailWrites { get; init; }

    public Task<Result<Option<ReclassifyRunRecord>, ClassificationFailure>> GetLastCompletedAsync(CancellationToken cancellationToken) =>
        Task.FromResult(FailReads
            ? Result.Failure<Option<ReclassifyRunRecord>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable())
            : Result.Success<Option<ReclassifyRunRecord>, ClassificationFailure>(Last));

    public Task<Result<Unit, ClassificationFailure>> SetLastCompletedAsync(ReclassifyRunRecord run, CancellationToken cancellationToken)
    {
        SetCalls++;
        if (FailWrites)
            return Task.FromResult(Result.Failure<Unit, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        Last = Option.Some(run);

        return Task.FromResult(Result.Success<Unit, ClassificationFailure>(Unit.Instance));
    }
}
