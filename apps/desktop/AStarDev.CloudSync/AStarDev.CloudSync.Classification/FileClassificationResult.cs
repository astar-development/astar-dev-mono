using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>The candidates found for one file. No candidates means the file is unclassified.</summary>
public sealed record FileClassificationResult
{
    internal FileClassificationResult(RelativeSyncPath path, IReadOnlyList<ClassificationCandidate> candidates)
    {
        Path = path;
        Candidates = candidates;
    }

    /// <summary>The classified path.</summary>
    public RelativeSyncPath Path { get; }

    /// <summary>The candidates, one per term.</summary>
    public IReadOnlyList<ClassificationCandidate> Candidates { get; }
}
