using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class TokenHonouringReclassifyRunStore : IReclassifyRunStore
{
    private readonly InMemoryReclassifyRunStore stored = new();

    public Option<ReclassifyRunRecord> Last => stored.Last;

    public Task<Result<Option<ReclassifyRunRecord>, ClassificationFailure>> GetLastCompletedAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled<Result<Option<ReclassifyRunRecord>, ClassificationFailure>>(cancellationToken) : stored.GetLastCompletedAsync(cancellationToken);

    public Task<Result<Unit, ClassificationFailure>> SetLastCompletedAsync(ReclassifyRunRecord run, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled<Result<Unit, ClassificationFailure>>(cancellationToken) : stored.SetLastCompletedAsync(run, cancellationToken);
}
