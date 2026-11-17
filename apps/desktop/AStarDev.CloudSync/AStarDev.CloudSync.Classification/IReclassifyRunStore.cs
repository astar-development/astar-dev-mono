using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Keeps the record of the last reclassify run that completed.</summary>
public interface IReclassifyRunStore
{
    /// <summary>Reads the last completed run, or none when no run has completed yet.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<Option<ReclassifyRunRecord>, ClassificationFailure>> GetLastCompletedAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the record of the last completed run.</summary>
    /// <param name="run">The run that completed.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ClassificationFailure>> SetLastCompletedAsync(ReclassifyRunRecord run, CancellationToken cancellationToken);
}
