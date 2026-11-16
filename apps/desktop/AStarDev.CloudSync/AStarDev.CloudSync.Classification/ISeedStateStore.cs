using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Remembers which seed version was applied, so a changed seed is applied again.</summary>
public interface ISeedStateStore
{
    /// <summary>Reads the applied seed version, or none when no seed was applied yet.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<Option<string>, ClassificationFailure>> GetAppliedVersionAsync(CancellationToken cancellationToken);

    /// <summary>Records the seed version that was applied.</summary>
    /// <param name="version">The version.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ClassificationFailure>> SetAppliedVersionAsync(string version, CancellationToken cancellationToken);
}
