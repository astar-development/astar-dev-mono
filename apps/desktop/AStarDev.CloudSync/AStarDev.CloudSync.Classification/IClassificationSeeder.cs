namespace AStarDev.CloudSync.Classification;

/// <summary>Writes the shipped seed into the classification stores. Call it at startup.</summary>
public interface IClassificationSeeder
{
    /// <summary>Applies the seed when its version differs from the recorded one. Never throws; a failure or a cancel is an outcome and leaves the version unrecorded.</summary>
    /// <param name="cancellationToken">Cancels the run.</param>
    Task<ClassificationSeedOutcome> SeedAsync(CancellationToken cancellationToken);
}
