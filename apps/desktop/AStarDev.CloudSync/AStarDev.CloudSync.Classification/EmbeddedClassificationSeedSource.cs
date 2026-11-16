using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Loads every embedded seed JSON file under a resource prefix and merges them into one seed.</summary>
/// <param name="assembly">The assembly holding the embedded seed files.</param>
/// <param name="resourcePrefix">The manifest resource name prefix that selects the seed files.</param>
public sealed class EmbeddedClassificationSeedSource(System.Reflection.Assembly assembly, string resourcePrefix) : IClassificationSeedSource
{
    /// <inheritdoc />
    public Task<Result<ClassificationSeed, ClassificationFailure>> LoadAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{assembly}{resourcePrefix}{cancellationToken}");
}
