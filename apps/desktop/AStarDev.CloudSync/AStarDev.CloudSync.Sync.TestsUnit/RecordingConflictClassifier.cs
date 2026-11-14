using System.Collections.Concurrent;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class RecordingConflictClassifier(ConflictKind answer) : IConflictClassifier
{
    private readonly ConcurrentQueue<ConflictSignal> signals = new();

    public IReadOnlyList<ConflictSignal> Signals => [.. signals];

    public Option<ConflictKind> Classify(ConflictEvidence evidence)
    {
        signals.Enqueue(evidence.Signal);

        return Option.Some(answer);
    }
}
