using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Signal over the path.</summary>
public sealed class ExtensionSignal : IClassificationSignal
{
    /// <inheritdoc />
    public IReadOnlyList<ClassificationCandidate> Evaluate(RelativeSyncPath path, ClassificationVocabulary vocabulary) => throw new NotImplementedException($"{path}{vocabulary}");
}
