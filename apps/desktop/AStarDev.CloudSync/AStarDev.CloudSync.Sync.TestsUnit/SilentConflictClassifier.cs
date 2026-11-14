using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class SilentConflictClassifier : IConflictClassifier
{
    public Option<ConflictKind> Classify(ConflictEvidence evidence) => Option.None<ConflictKind>();
}
