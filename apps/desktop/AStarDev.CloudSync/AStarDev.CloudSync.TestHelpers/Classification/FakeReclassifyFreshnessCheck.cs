using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class FakeReclassifyFreshnessCheck : IReclassifyFreshnessCheck
{
    public Result<ReclassifyFreshness, ClassificationFailure> Outcome { get; set; } = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateUpToDate());

    public int CheckCount { get; private set; }

    public Task<Result<ReclassifyFreshness, ClassificationFailure>> CheckAsync(CancellationToken cancellationToken)
    {
        CheckCount++;

        return Task.FromResult(Outcome);
    }
}
