using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="FileClassificationResult"/> values.</summary>
public static class FileClassificationResultFactory
{
    /// <summary>Creates a result. Candidates are copied and de-duplicated by term; for a repeated term the high confidence candidate wins, otherwise the first.</summary>
    /// <param name="path">The path.</param>
    /// <param name="candidates">The candidates.</param>
    public static FileClassificationResult Create(RelativeSyncPath path, IReadOnlyList<ClassificationCandidate> candidates) => new(path, [.. candidates.GroupBy(candidate => candidate.Term.Id).Select(group => group.OrderBy(candidate => candidate.Confidence is ClassificationConfidence.High ? 0 : 1).First())]);
}
