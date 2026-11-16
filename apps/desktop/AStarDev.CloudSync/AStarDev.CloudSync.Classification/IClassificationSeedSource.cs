using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Loads the shipped seed data.</summary>
public interface IClassificationSeedSource
{
    /// <summary>Loads the seed: categories, terms and a version.</summary>
    /// <param name="cancellationToken">Cancels the load.</param>
    Task<Result<ClassificationSeed, ClassificationFailure>> LoadAsync(CancellationToken cancellationToken);
}
