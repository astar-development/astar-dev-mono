using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Tells whether a reclassify is worth offering.</summary>
public interface IReclassifyFreshnessCheck
{
    /// <summary>Compares the last completed run with the current engine version and the applied seed version. No completed run counts as stale.</summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    Task<Result<ReclassifyFreshness, ClassificationFailure>> CheckAsync(CancellationToken cancellationToken);
}
