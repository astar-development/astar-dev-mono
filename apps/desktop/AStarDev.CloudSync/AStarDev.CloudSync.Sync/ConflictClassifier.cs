using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class ConflictClassifier : IConflictClassifier
{
    /// <inheritdoc />
    public Option<ConflictKind> Classify(ConflictEvidence evidence) => throw new NotImplementedException($"{evidence} is not yet classified.");
}
