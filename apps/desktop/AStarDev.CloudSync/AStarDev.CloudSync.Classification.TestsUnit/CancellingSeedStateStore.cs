using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class CancellingSeedStateStore : ISeedStateStore
{
    public Task<Result<Option<string>, ClassificationFailure>> GetAppliedVersionAsync(CancellationToken cancellationToken) => Task.FromCanceled<Result<Option<string>, ClassificationFailure>>(new CancellationToken(true));

    public Task<Result<Unit, ClassificationFailure>> SetAppliedVersionAsync(string version, CancellationToken cancellationToken) => Task.FromCanceled<Result<Unit, ClassificationFailure>>(new CancellationToken(true));
}
