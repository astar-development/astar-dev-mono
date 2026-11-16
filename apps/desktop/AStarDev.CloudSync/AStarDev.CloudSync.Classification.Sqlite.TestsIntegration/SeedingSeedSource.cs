using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class SeedingSeedSource(ClassificationSeed seed) : IClassificationSeedSource
{
    public Task<Result<ClassificationSeed, ClassificationFailure>> LoadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<ClassificationSeed, ClassificationFailure>(seed));
}
