using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Writes the shipped seed into the stores when its version is new.</summary>
/// <param name="seedSource">Loads the seed.</param>
/// <param name="seedState">Remembers the applied version.</param>
/// <param name="categories">Stores categories.</param>
/// <param name="terms">Stores terms.</param>
/// <param name="logger">Reports the outcome without terms or paths.</param>
public sealed class ClassificationSeeder(IClassificationSeedSource seedSource, ISeedStateStore seedState, ICategoryStore categories, ITermStore terms, ILogger<ClassificationSeeder> logger) : IClassificationSeeder
{
    /// <inheritdoc />
    public Task<ClassificationSeedOutcome> SeedAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{seedSource}{seedState}{categories}{terms}{logger}{cancellationToken}");
}
