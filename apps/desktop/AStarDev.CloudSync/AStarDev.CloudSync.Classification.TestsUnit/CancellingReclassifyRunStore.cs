using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class CancellingReclassifyRunStore : IReclassifyRunStore
{
    public Task<Result<Option<ReclassifyRunRecord>, ClassificationFailure>> GetLastCompletedAsync(CancellationToken cancellationToken) => Task.FromCanceled<Result<Option<ReclassifyRunRecord>, ClassificationFailure>>(new CancellationToken(true));

    public Task<Result<Unit, ClassificationFailure>> SetLastCompletedAsync(ReclassifyRunRecord run, CancellationToken cancellationToken) => Task.FromCanceled<Result<Unit, ClassificationFailure>>(new CancellationToken(true));
}
