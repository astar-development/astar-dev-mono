using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Re-runs the classification rules over every synced file in the database.</summary>
public interface IFileReclassificationService
{
    /// <summary>
    /// Classifies every synced file across all accounts using the current rules and adds any classification the file does not already have. Existing classifications are never removed.
    /// Reports progress after each file. Cancellation surfaces as <see cref="OperationCanceledException"/>.
    /// </summary>
    Task<Result<ReclassificationSummary, string>> ReclassifyAllAsync(IProgress<ReclassificationProgress> progress, CancellationToken cancellationToken = default);
}
