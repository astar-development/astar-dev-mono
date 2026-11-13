using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides whether a path is a conflict. Pure: it reads nothing and writes nothing.</summary>
public interface IConflictClassifier
{
    /// <summary>Classifies one path.</summary>
    /// <param name="evidence">The two sides, the baseline and any planner fact.</param>
    /// <returns>The conflict kind, or none when at most one side changed.</returns>
    Option<ConflictKind> Classify(ConflictEvidence evidence);
}
