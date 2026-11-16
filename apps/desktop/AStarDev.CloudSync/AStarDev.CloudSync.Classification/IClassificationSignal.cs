using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>One way of finding candidates in a path. A pure function over an in-memory vocabulary.</summary>
public interface IClassificationSignal
{
    /// <summary>Finds the candidates this signal sees in <paramref name="path"/>. The vocabulary holds no ignored terms.</summary>
    /// <param name="path">The path below the sync root.</param>
    /// <param name="vocabulary">The categories and terms to match against.</param>
    IReadOnlyList<ClassificationCandidate> Evaluate(RelativeSyncPath path, ClassificationVocabulary vocabulary);
}
