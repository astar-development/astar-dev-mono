using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class SeedingStateStore : ISeedStateStore
{
    public string? Version { get; set; }

    public int SetCalls { get; private set; }

    public bool FailReads { get; init; }

    public bool FailWrites { get; init; }

    public Task<Result<Option<string>, ClassificationFailure>> GetAppliedVersionAsync(CancellationToken cancellationToken) =>
        Task.FromResult(FailReads
            ? Result.Failure<Option<string>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable())
            : Result.Success<Option<string>, ClassificationFailure>(Version is null ? Option.None<string>() : Option.Some(Version)));

    public Task<Result<Unit, ClassificationFailure>> SetAppliedVersionAsync(string version, CancellationToken cancellationToken)
    {
        SetCalls++;
        if (FailWrites)
            return Task.FromResult(Result.Failure<Unit, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        Version = version;

        return Task.FromResult(Result.Success<Unit, ClassificationFailure>(Unit.Instance));
    }
}
