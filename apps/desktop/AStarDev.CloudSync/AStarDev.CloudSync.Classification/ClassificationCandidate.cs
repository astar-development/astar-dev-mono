namespace AStarDev.CloudSync.Classification;

/// <summary>One term a signal found in a path.</summary>
public sealed record ClassificationCandidate
{
    internal ClassificationCandidate(ClassificationTerm term, ClassificationCategory category, SignalKind signal, ClassificationConfidence confidence)
    {
        Term = term;
        Category = category;
        Signal = signal;
        Confidence = confidence;
    }

    /// <summary>The term.</summary>
    public ClassificationTerm Term { get; }

    /// <summary>The term's category.</summary>
    public ClassificationCategory Category { get; }

    /// <summary>The signal that found it.</summary>
    public SignalKind Signal { get; }

    /// <summary>How sure the signal is.</summary>
    public ClassificationConfidence Confidence { get; }
}
