using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class SeedingSeedSource(Result<ClassificationSeed, ClassificationFailure> result) : IClassificationSeedSource
{
    public Task<Result<ClassificationSeed, ClassificationFailure>> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(result);
}
