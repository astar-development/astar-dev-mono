using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="FileClassificationResult"/> values.</summary>
public static class FileClassificationResultFactory
{
    /// <summary>Creates a result. Candidates are copied and de-duplicated by term, keeping the first.</summary>
    /// <param name="path">The path.</param>
    /// <param name="candidates">The candidates.</param>
    public static FileClassificationResult Create(RelativeSyncPath path, IReadOnlyList<ClassificationCandidate> candidates) => throw new NotImplementedException($"{path}{candidates}");
}
