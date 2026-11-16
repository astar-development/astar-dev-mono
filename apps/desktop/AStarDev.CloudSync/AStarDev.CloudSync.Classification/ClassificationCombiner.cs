using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Merges candidates from several signals into one result.</summary>
public static class ClassificationCombiner
{
    /// <summary>Combines the candidates.</summary>
    /// <param name="path">The classified file path.</param>
    /// <param name="candidates">Candidates from every signal.</param>
    /// <returns>The merged result.</returns>
    public static FileClassificationResult Combine(RelativeSyncPath path, IEnumerable<ClassificationCandidate> candidates) => throw new NotImplementedException($"{path}{candidates}");
}
